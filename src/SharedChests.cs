using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Multi-user chests (game side of SharedChestCore). Several players can have the same
// chest open. The first opener owns it as in vanilla; later openers are granted a view
// without an ownership change, and every change they make is sent to the owner as an
// exact slot transaction (see SharedChestCore for the rules that keep item totals).
// Applies only to Quartermaster-registered storage (chests, drawers, Apothecary furniture)
// and only between clients that announced this protocol; anyone else gets vanilla behavior.
internal static class SharedChests
{
    internal const int Protocol = 1;
    internal const string ReceiptsKey = "Quartermaster.sharedReceipts", ViewedKey = "Quartermaster.sharedViewedUntil";
    private const string HelloRpc = "QM_SharedHello_v1", TxRpc = "QM_SharedTx_v1", ResultRpc = "QM_SharedTxResult_v1",
        ViewRpc = "QM_SharedView_v1", SettingsRpc = "QM_SharedSettings_v1", SettingsResultRpc = "QM_SharedSettingsResult_v1";
    private static readonly long DeadlineTicks = TimeSpan.FromSeconds(6).Ticks, GraceTicks = TimeSpan.FromSeconds(8).Ticks,
        ViewedTicks = TimeSpan.FromSeconds(30).Ticks, ViewedRefreshTicks = TimeSpan.FromSeconds(15).Ticks;
    private const float ViewerExpiry = 4f, HeartbeatEvery = 1f, MaxSenderDistance = 12f;

    internal static bool Available { get; private set; }
    internal static bool Active => Available && Plugin.Enabled.Value && Plugin.SharedChests != null && Plugin.SharedChests.Value;

    private static readonly FieldInfo InUseField = AccessTools.Field(typeof(Container), "m_inUse");
    private static readonly FieldInfo LastRevisionField = AccessTools.Field(typeof(Container), "m_lastRevision");
    private static readonly MethodInfo LoadMethod = AccessTools.Method(typeof(Container), "Load");
    private static readonly MethodInfo CheckForChangesMethod = AccessTools.Method(typeof(Container), "CheckForChanges");
    private static readonly MethodInfo CheckAccessMethod = AccessTools.Method(typeof(Container), "CheckAccess");
    private static readonly MethodInfo ChangedMethod = AccessTools.Method(typeof(Inventory), "Changed");
    private static readonly FieldInfo CurrentField = AccessTools.Field(typeof(InventoryGui), "m_currentContainer");
    private static readonly FieldInfo DragItemField = AccessTools.Field(typeof(InventoryGui), "m_dragItem");
    private static readonly FieldInfo DragInventoryField = AccessTools.Field(typeof(InventoryGui), "m_dragInventory");
    private static readonly FieldInfo DragAmountField = AccessTools.Field(typeof(InventoryGui), "m_dragAmount");
    private static readonly FieldInfo DragGoField = AccessTools.Field(typeof(InventoryGui), "m_dragGo");
    private static readonly MethodInfo SetupDragMethod = AccessTools.Method(typeof(InventoryGui), "SetupDragItem");

    // ---------- state ----------
    private static readonly Dictionary<long, int> Peers = new Dictionary<long, int>();
    private static SharedLedger ledger;
    private static readonly SharedRefusals Refused = new SharedRefusals();
    private static readonly Dictionary<string, ZDOID> ChestIds = new Dictionary<string, ZDOID>();
    private static readonly Dictionary<ZDOID, Dictionary<long, float>> Viewers = new Dictionary<ZDOID, Dictionary<long, float>>();
    private static readonly List<EscrowLine> Undelivered = new List<EscrowLine>();
    private static readonly Dictionary<long, (Action<string> Done, float Until)> SettingsWaiting = new Dictionary<long, (Action<string>, float)>();
    private static readonly HashSet<Container> Unconfirmed = new HashSet<Container>();   // local copies showing changes the owner has not confirmed
    private static Vector3 lastPlayerPosition;
    private static long settingsNext;
    private static Container viewing;         // chest this client currently has open without owning it
    private static float viewingSince, nextHeartbeat, nextNotice;
    private static Container session;         // chest open in the inventory screen (owned or viewed)
    private static bool sessionViewed;
    private static ZRoutedRpc registered;
    private static bool greeted;

    private static long Self => ZDOMan.GetSessionID();
    private static long Now => ZNet.instance ? ZNet.instance.GetTime().Ticks : DateTime.UtcNow.Ticks;
    private static SharedLedger Ledger => ledger ?? (ledger = new SharedLedger(DateTime.UtcNow.Ticks & 0x7fffffffffL));

    // ---------- install ----------
    internal static void Install(Harmony harmony)
    {
        var applied = new List<(MethodBase Original, MethodInfo Patch)>();
        void Patch(MethodBase original, string prefix = null, string postfix = null, string transpiler = null, string finalizer = null)
        {
            if (original == null) throw new MissingMethodException("Shared chests: game method not found");
            HarmonyMethod Hm(string n) => n == null ? null : new HarmonyMethod(AccessTools.Method(typeof(SharedChests), n));
            foreach (var n in new[] { prefix, postfix, transpiler, finalizer }) if (n != null) applied.Add((original, AccessTools.Method(typeof(SharedChests), n)));
            try { harmony.Patch(original, prefix: Hm(prefix), postfix: Hm(postfix), transpiler: Hm(transpiler), finalizer: Hm(finalizer), ilmanipulator: null); }
            catch (Exception e) { throw new InvalidOperationException(original.DeclaringType?.Name + "." + original.Name + ": " + e.GetBaseException().Message, e); }
        }
        try
        {
            if (new object[] { InUseField, LastRevisionField, LoadMethod, CheckForChangesMethod, CheckAccessMethod, ChangedMethod, CurrentField,
                DragItemField, DragInventoryField, DragAmountField, DragGoField, SetupDragMethod }.Any(m => m == null))
                throw new MissingMemberException("Shared chests: a game member was not found");
            Patch(AccessTools.Method(typeof(Container), "RPC_RequestOpen"), prefix: nameof(OpenRequested));
            Patch(AccessTools.Method(typeof(InventoryGui), "UpdateContainer"), prefix: nameof(BeforeUpdateContainer), transpiler: nameof(ShowViewedContainer));
            Patch(LoadMethod, postfix: nameof(AfterLoad));
            Patch(AccessTools.Method(typeof(Container), "StackAll"), prefix: nameof(StackAllRequested));
            Patch(AccessTools.Method(typeof(InventoryGui), "OnSelectedItem"), prefix: nameof(BeforeSelect), finalizer: nameof(AfterAction));
            Patch(AccessTools.Method(typeof(InventoryGui), "OnTakeAll"), prefix: nameof(BeforeAction), finalizer: nameof(AfterAction));
            Patch(AccessTools.Method(typeof(InventoryGui), "OnStackAll"), prefix: nameof(BeforeAction), finalizer: nameof(AfterAction));
            Patch(AccessTools.Method(typeof(InventoryGui), "OnRightClickItem"), prefix: nameof(RightClick));
            Patch(AccessTools.Method(typeof(InventoryGui), "OnDropOutside"), prefix: nameof(DropOutside));
            Available = true;
        }
        catch (Exception e)
        {
            Available = false;
            foreach (var (original, patch) in applied) { try { harmony.Unpatch(original, patch); } catch { } }
            Plugin.Log.LogWarning("Shared chests unavailable; chests keep the game's one-player-at-a-time behavior: " + e.GetBaseException().Message);
        }
    }

    internal static void Clear()
    {
        if (ledger != null)
            foreach (var tx in ledger.Abandon())
                Plugin.Log.LogWarning("[shared chest] left the world with an unsettled transaction " + tx.Id + " on chest " + tx.Chest
                    + "; withheld: " + Describe(tx.TakeIn) + "; given: " + Describe(tx.GiveOut) + ". If items are missing, this is what to restore.");
        ledger = null; Peers.Clear(); Refused.Clear(); ChestIds.Clear(); Viewers.Clear(); SettingsWaiting.Clear();
        if (Undelivered.Count > 0) Plugin.Log.LogWarning("[shared chest] undelivered on leaving: " + Describe(Undelivered));
        // ZRoutedRpc keeps its handlers until a new instance replaces it; registering again would fail.
        Undelivered.Clear(); Unconfirmed.Clear(); viewing = null; session = null; sessionViewed = false; greeted = false;
    }
    private static string Describe(IEnumerable<EscrowLine> lines) =>
        string.Join(", ", lines.Select(l => l.Count + "x " + (Materialize(l.Key, 1, default)?.m_shared?.m_name ?? "unknown item")));

    // ---------- item keys ----------
    private static int itemVersion = -1;
    private static int ItemVersion
    {
        get
        {
            if (itemVersion < 0) { var p = new ZPackage(); new Inventory("qm-shared", null, 1, 1).Save(p); p.SetPos(0); itemVersion = p.ReadInt(); }
            return itemVersion;
        }
    }
    internal static string Key(ItemDrop.ItemData item)
    {
        var c = item.Clone();
        c.m_stack = 1; c.m_gridPos = new Vector2i(0, 0); c.m_equipped = false;
        // Durability is saved in hundredths; canonicalize so the owner's live value and a
        // viewer's reloaded value always produce the same key.
        double x = (double)item.m_durability * 100.0;
        int hundredths = (int)Math.Floor(x + 1e-4 + Math.Abs(x) * 2e-7);
        c.m_durability = (hundredths + 0.5f) / 100f;
        var p = new ZPackage(); c.Save(p);
        return Convert.ToBase64String(p.GetArray());
    }
    internal static SlotValue Value(ItemDrop.ItemData item) => item == null || item.m_stack <= 0 ? null : new SlotValue(Key(item), item.m_stack);
    internal static ItemDrop.ItemData Materialize(string key, int stack, Vector2i pos)
    {
        try
        {
            var bytes = Convert.FromBase64String(key);
            var ms = new MemoryStream(); var w = new BinaryWriter(ms);
            w.Write(ItemVersion); w.Write((ushort)1); w.Write(bytes); w.Flush();
            var tmp = new Inventory("qm-shared", null, 1, 1);
            tmp.Load(new ZPackage(ms.ToArray()));
            var item = tmp.GetAllItems().FirstOrDefault();
            if (item?.m_shared == null) return null;
            item.m_stack = stack; item.m_gridPos = pos; item.m_equipped = false;
            return item;
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] unreadable item record: " + e.GetBaseException().Message); return null; }
    }
    private static Dictionary<SlotPos, SlotValue> Slots(Inventory inv)
    {
        var d = new Dictionary<SlotPos, SlotValue>();
        foreach (var item in inv.GetAllItems()) { var v = Value(item); if (v != null) d[new SlotPos(item.m_gridPos.x, item.m_gridPos.y)] = v; }
        return d;
    }

    private sealed class InventorySlots : ISharedChestSlots
    {
        private readonly Inventory inv;
        internal InventorySlots(Inventory inventory) { inv = inventory; }
        public int Width => inv.GetWidth();
        public int Height => inv.GetHeight();
        public SlotValue Get(int x, int y) => Value(inv.GetItemAt(x, y));
        public bool Accepts(SlotValue v)
        {
            var item = Materialize(v.Key, v.Stack, new Vector2i(0, 0));
            return item != null && (InventoryTransfers.Admission?.Invoke(inv, item) ?? true);
        }
        public void Apply(IReadOnlyList<SlotChange> changes)
        {
            // Build every new item first: a record that cannot be read aborts with nothing changed.
            var created = new List<ItemDrop.ItemData>();
            foreach (var c in changes)
                if (c.After != null) created.Add(Materialize(c.After.Key, c.After.Stack, new Vector2i(c.X, c.Y)) ?? throw new InvalidOperationException("unreadable item record"));
            var list = inv.GetAllItems();
            foreach (var c in changes) list.RemoveAll(i => i.m_gridPos.x == c.X && i.m_gridPos.y == c.Y);
            list.AddRange(created);
            // The change has happened; a failing change listener must not turn it into a refusal.
            try { ChangedMethod.Invoke(inv, new object[] { false, false }); }
            catch (Exception e) { Plugin.Log.LogWarning("[shared chest] change listener failed after applying: " + e.GetBaseException().Message); }
        }
    }

    // ---------- which chests ----------
    internal static bool Supported(Container c)
    {
        if (!c || !ContainerRegistry.All.Contains(c) || c.m_wagon) return false;
        if (PostalParcel.IsParcel(c) || PostalMailbox.IsMailbox(c) || ExternalStorageCompatibility.Recognizes(c)) return false;
        var v = ContainerRegistry.GetView(c);
        return v && v.IsValid();
    }
    internal static bool Compatible(long session) => session != 0 && Peers.TryGetValue(session, out int p) && p == Protocol;
    private static Container Current => InventoryGui.instance ? CurrentField.GetValue(InventoryGui.instance) as Container : null;

    // This client has the chest open without owning it and can work on it through its owner.
    internal static bool Viewing(Container c)
    {
        if (!Active || !Supported(c) || c.IsOwner()) return false;
        long owner = ContainerRegistry.GetView(c).GetZDO().GetOwner();
        return owner != 0 && owner != Self && Compatible(owner) && c == Current;
    }

    // Someone is working in this chest: automation, ownership handoffs and crafting leave it alone.
    internal static bool Viewed(Container c)
    {
        if (!Active || !c) return false;
        var v = ContainerRegistry.GetView(c);
        if (!v || !v.IsValid()) return false;
        if (v.IsOwner() && HasViewers(v.GetZDO().m_uid)) return true;
        return v.GetZDO().GetLong(ViewedKey, 0L) > Now;
    }
    private static bool HasViewers(ZDOID id)
    {
        if (!Viewers.TryGetValue(id, out var map)) return false;
        float t = Time.unscaledTime;
        foreach (var k in map.Where(e => e.Value < t).Select(e => e.Key).ToList()) map.Remove(k);
        return map.Count > 0;
    }

    // ---------- open handshake (owner side) ----------
    // Returning false answers the request here (other prefixes, e.g. the parcel guard, may already deny it).
    private static bool OpenRequested(Container __instance, long uid, long playerID)
    {
        try
        {
            if (!Active || !Supported(__instance) || uid == Self || !Compatible(uid)) return true;
            var view = ContainerRegistry.GetView(__instance);
            if (!view.IsOwner()) return true;
            // Nobody else is in it: the game hands it over to the opener as usual.
            if (!__instance.IsInUse() && !HasViewers(view.GetZDO().m_uid)) return true;
            if (!(bool)CheckAccessMethod.Invoke(__instance, new object[] { playerID })) { view.InvokeRPC(uid, "RPC_OpenResponse", false); return false; }
            ZDOMan.instance.ForceSendZDO(uid, view.GetZDO().m_uid);
            AddViewer(view, uid);
            view.InvokeRPC(uid, "RPC_OpenResponse", true);
            Plugin.Log.LogInfo("[shared chest] " + __instance.m_name + " opened for peer " + uid + " alongside its owner");
            return false;
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] open handling failed; using the game's behavior: " + e.GetBaseException().Message); return true; }
    }
    private static void AddViewer(ZNetView view, long uid)
    {
        var id = view.GetZDO().m_uid;
        if (!Viewers.TryGetValue(id, out var map)) Viewers[id] = map = new Dictionary<long, float>();
        map[uid] = Time.unscaledTime + ViewerExpiry;
        var z = view.GetZDO();
        if (z.GetLong(ViewedKey, 0L) - Now < ViewedRefreshTicks) z.Set(ViewedKey, Now + ViewedTicks);
    }

    // ---------- inventory screen ----------
    // Transpiler: keep the container panel visible for a chest this client views without owning.
    private static IEnumerable<CodeInstruction> ShowViewedContainer(IEnumerable<CodeInstruction> instructions)
    {
        var isOwner = AccessTools.Method(typeof(Container), nameof(Container.IsOwner));
        var shows = AccessTools.Method(typeof(SharedChests), nameof(ShowsContainer));
        bool done = false;
        foreach (var ins in instructions)
        {
            if (!done && ins.Calls(isOwner)) { done = true; yield return new CodeInstruction(OpCodes.Call, shows).WithLabels(ins.labels).WithBlocks(ins.blocks); continue; }
            yield return ins;
        }
        if (!done) throw new InvalidOperationException("Shared chests: container panel check not found");
    }
    internal static bool ShowsContainer(Container c) => c && (c.IsOwner() || Viewing(c));

    private static void BeforeUpdateContainer()
    {
        try
        {
            var c = Current;
            if (!c || !Active) { session = c; sessionViewed = false; return; }
            if (c.IsOwner())
            {
                // Became the owner while viewing: the local copy may show changes the old
                // owner never applied. Reload real contents before the game marks it in use.
                if (Unconfirmed.Contains(c) || (session == c && sessionViewed)) ReloadView(c, force: true);
                session = c; sessionViewed = false;
                return;
            }
            if (!Viewing(c)) { session = c; sessionViewed = false; return; }
            if ((bool)InUseField.GetValue(c)) InUseField.SetValue(c, false);   // lost ownership while open
            CheckForChangesMethod.Invoke(c, null);                              // live refresh, not once per second
            session = c; sessionViewed = true;
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] view update failed: " + e.GetBaseException().Message); }
    }
    // Replace the local copy with the chest's saved contents (also for an owner who has it open).
    private static void ReloadView(Container c, bool force)
    {
        if (!c) return;
        var view = ContainerRegistry.GetView(c);
        if (!view || !view.IsValid()) return;
        bool inUse = (bool)InUseField.GetValue(c);
        InUseField.SetValue(c, false);
        LastRevisionField.SetValue(c, uint.MaxValue);
        if (force)
        {
            if (view.GetZDO().GetByteArray(ZDOVars.s_items) == null)
            {
                // Never saved: Load would keep whatever the local copy shows.
                c.GetInventory().GetAllItems().Clear();
                try { ChangedMethod.Invoke(c.GetInventory(), new object[] { false, false }); } catch { }
            }
            else LoadMethod.Invoke(c, null);
            FixDrag(c.GetInventory());
        }
        if (inUse && view.IsOwner()) InUseField.SetValue(c, true);
        Unconfirmed.Remove(c);
    }
    private static bool HasPending(Container c)
    {
        if (ledger == null || !c) return false;
        var id = ContainerRegistry.GetView(c)?.GetZDO()?.m_uid ?? ZDOID.None;
        return id != ZDOID.None && ledger.PendingFor(id.ToString()).Any();
    }

    // After a viewer's copy reloads, re-apply still-pending changes and keep the dragged item valid.
    private static void AfterLoad(Container __instance, bool __result)
    {
        if (!__result || !Active || !__instance || __instance.IsOwner() || ledger == null) return;
        try
        {
            var id = ContainerRegistry.GetView(__instance)?.GetZDO()?.m_uid ?? ZDOID.None;
            if (id == ZDOID.None) return;
            string key = id.ToString();
            var slots = new InventorySlots(__instance.GetInventory());
            foreach (var tx in ledger.PendingFor(key)) if (SharedChestOwner.Overlay(slots, tx.Changes)) Unconfirmed.Add(__instance);
            FixDrag(__instance.GetInventory());
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] view refresh failed: " + e.GetBaseException().Message); }
    }
    private static void FixDrag(Inventory inv)
    {
        var gui = InventoryGui.instance;
        if (!gui || DragInventoryField.GetValue(gui) != inv) return;
        var drag = DragItemField.GetValue(gui) as ItemDrop.ItemData;
        if (drag == null || inv.ContainsItem(drag)) return;
        var same = inv.GetItemAt(drag.m_gridPos.x, drag.m_gridPos.y);
        if (same != null && Key(same) == Key(drag))
        {
            DragItemField.SetValue(gui, same);
            DragAmountField.SetValue(gui, Math.Min((int)DragAmountField.GetValue(gui), same.m_stack));
        }
        else SetupDragMethod.Invoke(gui, new object[] { null, null, 1 });
    }

    // ---------- viewer actions ----------
    internal sealed class Scope
    {
        internal Container Chest; internal Inventory ChestInventory, PlayerInventory;
        internal Dictionary<SlotPos, SlotValue> ChestBefore, PlayerBefore;
    }
    private static Scope active;

    internal static Scope Begin(Container c)
    {
        if (active != null || !Viewing(c) || !Player.m_localPlayer) return null;
        var player = Player.m_localPlayer.GetInventory();
        return active = new Scope { Chest = c, ChestInventory = c.GetInventory(), PlayerInventory = player,
            ChestBefore = Slots(c.GetInventory()), PlayerBefore = Slots(player) };
    }

    internal static void End(Scope s)
    {
        if (s == null || active != s) return;
        active = null;
        bool sent = false;
        try
        {
            var plan = SharedPlan.Build(s.ChestBefore, Slots(s.ChestInventory), s.PlayerBefore, Slots(s.PlayerInventory));
            if (!plan.ChestChanged) return;
            bool reachable = OwnerReachable(s.Chest);
            if (!plan.Conserved || !reachable)
            {
                Restore(s.ChestInventory, s.ChestBefore, false);
                Restore(s.PlayerInventory, s.PlayerBefore, true);
                if (!reachable) { Notice("The chest's owner is not reachable right now; try again"); return; }
                Plugin.Log.LogWarning("[shared chest] an action that did not keep item totals was undone (chest " + s.Chest.m_name + ")");
                Notice("That action is not available in a shared chest");
                return;
            }
            foreach (var line in plan.TakeIn)
                if (!Withhold(s.PlayerInventory, line))
                {
                    // Should not happen: the gain is where the diff found it. Undo everything locally.
                    Restore(s.ChestInventory, s.ChestBefore, false);
                    Restore(s.PlayerInventory, s.PlayerBefore, true);
                    Plugin.Log.LogWarning("[shared chest] could not hold back received items; action undone");
                    return;
                }
            Send(s.Chest, plan.Changes, plan.TakeIn, plan.GiveOut);
            sent = true;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError("[shared chest] action failed: " + e);
            if (!sent)
            {
                try { Restore(s.ChestInventory, s.ChestBefore, false); } catch { }
                try { Restore(s.PlayerInventory, s.PlayerBefore, true); } catch { }
            }
        }
    }

    // The chest still has an owner running this protocol to send the change to.
    private static bool OwnerReachable(Container c)
    {
        var v = ContainerRegistry.GetView(c);
        if (!v || !v.IsValid() || v.IsOwner()) return false;
        long owner = v.GetZDO().GetOwner();
        return owner != 0 && owner != Self && Compatible(owner);
    }

    // Run QM's own buttons (sort, deposit all) on a viewed chest.
    internal static bool Run(Container c, Action action)
    {
        var s = Begin(c);
        if (s == null) return false;
        try { action(); }
        finally { End(s); }
        return true;
    }

    private static bool Withhold(Inventory inv, EscrowLine line)
    {
        var item = inv.GetItemAt(line.X, line.Y);
        if (item == null || item.m_stack < line.Count || Key(item) != line.Key) return false;
        var p = Player.m_localPlayer;
        if (p && p.IsItemEquiped(item)) p.UnequipItem(item, false);
        return inv.RemoveItem(item, line.Count);
    }

    // Undo a local-only change: put the listed slots back exactly as they were.
    private static void Restore(Inventory inv, Dictionary<SlotPos, SlotValue> before, bool player)
    {
        var now = Slots(inv);
        var changes = new List<SlotChange>();
        foreach (var pos in before.Keys.Union(now.Keys))
        {
            before.TryGetValue(pos, out var b); now.TryGetValue(pos, out var a);
            if (!SlotValue.Same(a, b)) changes.Add(new SlotChange { X = pos.X, Y = pos.Y, Before = a, After = b });
        }
        if (changes.Count == 0) return;
        if (player && Player.m_localPlayer)
            foreach (var c in changes) { var item = inv.GetItemAt(c.X, c.Y); if (item != null && Player.m_localPlayer.IsItemEquiped(item)) Player.m_localPlayer.UnequipItem(item, false); }
        new InventorySlots(inv).Apply(changes);
    }

    private static void Send(Container c, List<SlotChange> changes, IEnumerable<EscrowLine> takeIn, IEnumerable<EscrowLine> giveOut)
    {
        var view = ContainerRegistry.GetView(c);
        var id = view.GetZDO().m_uid;
        long owner = view.GetZDO().GetOwner();
        string key = id.ToString(); ChestIds[key] = id;
        var tx = Ledger.Open(key, owner, Now + DeadlineTicks, changes, takeIn, giveOut);
        var pkg = new ZPackage();
        pkg.Write(tx.Id); pkg.Write(tx.Deadline); pkg.Write(changes.Count);
        foreach (var ch in changes)
        {
            pkg.Write(ch.X); pkg.Write(ch.Y);
            WriteValue(pkg, ch.Before); WriteValue(pkg, ch.After);
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, TxRpc, id, pkg);
        Unconfirmed.Add(c);
        Trace("sent " + tx.Id + " to " + owner + " (" + changes.Count + " slots)", c);
    }
    private static void WriteValue(ZPackage p, SlotValue v) { p.Write(v != null); if (v != null) { p.Write(v.Key); p.Write(v.Stack); } }
    private static SlotValue ReadValue(ZPackage p) => p.ReadBool() ? new SlotValue(p.ReadString(), p.ReadInt()) : null;

    // Prefix + finalizer pairs around the game's inventory screen actions.
    private static bool BeforeSelect(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, InventoryGrid.Modifier mod, out Scope __state)
    {
        __state = null;
        var c = Current;
        if (!Viewing(c)) return true;
        // Dropping a chest item straight onto the ground: the owner removes it, then it drops here.
        if (DragGoField.GetValue(__instance) == null && item != null && mod == InventoryGrid.Modifier.Drop && grid.GetInventory() == c.GetInventory())
        {
            TakeFromChest(c, item, item.m_stack, EscrowDestination.Drop);
            return false;
        }
        __state = Begin(c);
        return true;
    }
    private static bool BeforeAction(out Scope __state)
    {
        __state = Begin(Current);
        return true;
    }
    private static Exception AfterAction(Exception __exception, Scope __state)
    {
        End(__state);
        return __exception;
    }
    private static bool StackAllRequested(Container __instance)
    {
        if (!Viewing(__instance) || !Player.m_localPlayer) return true;
        Run(__instance, () => __instance.GetInventory().StackAll(Player.m_localPlayer.GetInventory(), true));
        return false;
    }
    private static bool RightClick(InventoryGrid grid, ItemDrop.ItemData item)
    {
        var c = Current; var p = Player.m_localPlayer;
        if (item == null || !p || !Viewing(c) || grid.GetInventory() != c.GetInventory()) return true;
        if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable || !p.CanConsumeItem(item, true)) return true;
        TakeFromChest(c, item, 1, EscrowDestination.Consume);
        return false;
    }
    private static bool DropOutside(InventoryGui __instance)
    {
        var c = Current;
        if (!Viewing(c) || DragGoField.GetValue(__instance) == null) return true;
        if (DragInventoryField.GetValue(__instance) != c.GetInventory()) return true;
        var item = DragItemField.GetValue(__instance) as ItemDrop.ItemData;
        int amount = (int)DragAmountField.GetValue(__instance);
        SetupDragMethod.Invoke(__instance, new object[] { null, null, 1 });
        if (item != null && c.GetInventory().ContainsItem(item)) TakeFromChest(c, item, Math.Min(amount, item.m_stack), EscrowDestination.Drop);
        return false;
    }
    // A chest item leaves for somewhere other than the inventory (eaten or dropped): it only
    // reaches its destination once the owner has removed it from the chest.
    private static void TakeFromChest(Container c, ItemDrop.ItemData item, int amount, EscrowDestination destination)
    {
        if (amount <= 0 || !OwnerReachable(c)) return;
        var before = Value(item);
        var after = item.m_stack - amount > 0 ? new SlotValue(before.Key, item.m_stack - amount) : null;
        var changes = new List<SlotChange> { new SlotChange { X = item.m_gridPos.x, Y = item.m_gridPos.y, Before = before, After = after } };
        new InventorySlots(c.GetInventory()).Apply(changes);
        Send(c, changes, new[] { new EscrowLine { Key = before.Key, Count = amount, Destination = destination } }, null);
    }

    // ---------- owner side: transactions ----------
    private static Container FindChest(ZDOID id)
    {
        var go = ZNetScene.instance ? ZNetScene.instance.FindInstance(id) : null;
        if (!go) return null;
        return go.GetComponentsInChildren<Container>(true).FirstOrDefault(c => ContainerRegistry.GetView(c)?.GetZDO()?.m_uid == id);
    }
    private static bool SenderAllowed(Container c, long sender)
    {
        var view = ContainerRegistry.GetView(c);
        foreach (var p in Player.GetAllPlayers())
        {
            if (!p || p.GetZDOID().UserID != sender) continue;
            if ((p.transform.position - c.transform.position).sqrMagnitude > MaxSenderDistance * MaxSenderDistance) return false;
            return NativeStorageAccess.AllowedForPlayer(view, p.GetPlayerID());
        }
        return false;
    }

    private static void OnTx(long sender, ZDOID id, ZPackage pkg)
    {
        long txId = 0; var result = SharedTxResult.Invalid;
        try
        {
            txId = pkg.ReadLong(); long deadline = pkg.ReadLong(); int count = pkg.ReadInt();
            if (count < 0 || count > SharedChestOwner.MaxChanges) throw new InvalidDataException("bad change count");
            var changes = new List<SlotChange>(count);
            for (int i = 0; i < count; i++) changes.Add(new SlotChange { X = pkg.ReadInt(), Y = pkg.ReadInt(), Before = ReadValue(pkg), After = ReadValue(pkg) });
            var zdo = ZDOMan.instance.GetZDO(id);
            var chest = FindChest(id);
            var view = chest ? ContainerRegistry.GetView(chest) : null;
            if (!Active || zdo == null || !view || !view.IsValid() || !view.IsOwner())
                result = SharedChestOwner.NotOwner(zdo?.GetString(ReceiptsKey, ""), Refused, sender, txId);
            else
            {
                string receipts = zdo.GetString(ReceiptsKey, "");
                if (SharedReceipts.TryFind(receipts, sender, txId, out var earlier)) result = earlier;
                else
                {
                    if (!Supported(chest) || !SenderAllowed(chest, sender)) { result = SharedTxResult.Denied; receipts = SharedReceipts.Add(receipts, sender, txId, result); }
                    else if (!ChestOwnership.Settled(view)) { result = SharedTxResult.Conflict; receipts = SharedReceipts.Add(receipts, sender, txId, result); }
                    else
                    {
                        // Work on the saved contents, not a copy that may predate them (just gained ownership,
                        // or a copy last loaded up to a second ago). An owner with the chest open is current.
                        if (!chest.IsInUse()) { LastRevisionField.SetValue(chest, uint.MaxValue); LoadMethod.Invoke(chest, null); }
                        try { result = SharedChestOwner.Handle(new InventorySlots(chest.GetInventory()), ref receipts, sender, txId, changes, Now, deadline, Refused); }
                        catch (Exception e)
                        {
                            Plugin.Log.LogWarning("[shared chest] could not apply a change from peer " + sender + ": " + e.GetBaseException().Message);
                            result = SharedTxResult.Invalid; receipts = SharedReceipts.Add(receipts, sender, txId, result);
                        }
                    }
                    if (result == SharedTxResult.Applied) CraftStorageAccess.Save(chest);   // contents saved even if a change listener failed
                    zdo.Set(ReceiptsKey, receipts);   // same update as the contents just saved
                    if (result == SharedTxResult.Applied) { AddViewer(view, sender); FixDrag(chest.GetInventory()); ChestVisual.Pulse(chest); }
                }
            }
            Trace("tx " + txId + " from " + sender + ": " + result, chest);
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] bad request from peer " + sender + ": " + e.GetBaseException().Message); }
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ResultRpc, id, txId, (int)result);
    }

    // ---------- viewer side: results ----------
    private sealed class Sink : ISharedPlayerSink
    {
        public void Give(EscrowLine line)
        {
            if (Player.m_localPlayer && !Player.m_localPlayer.IsDead()) { Deliver(line); return; }
            // No living player to receive them: put them on the ground where the player was.
            var item = Materialize(line.Key, line.Count, new Vector2i(0, 0));
            if (item != null && ZNetScene.instance) { ItemDrop.DropItem(item, line.Count, lastPlayerPosition + Vector3.up, Quaternion.identity); Plugin.Log.LogInfo("[shared chest] " + line.Count + "x " + item.m_shared.m_name + " dropped where the player was (no living player to receive them)"); }
            else Undelivered.Add(line);
        }
    }
    private static readonly Sink sink = new Sink();

    private static void Deliver(EscrowLine line)
    {
        var p = Player.m_localPlayer; var inv = p.GetInventory();
        var item = Materialize(line.Key, line.Count, new Vector2i(0, 0));
        if (item == null) { Plugin.Log.LogError("[shared chest] could not recreate " + line.Count + " item(s) to return; record " + line.Key); return; }
        int left = line.Count;
        if (line.Destination == EscrowDestination.Drop) { DropAtFeet(p, item, left); return; }
        int max = Math.Max(1, item.m_shared.m_maxStackSize);
        if (line.X >= 0 && line.Y >= 0 && line.X < inv.GetWidth() && line.Y < inv.GetHeight())
        {
            var at = inv.GetItemAt(line.X, line.Y);
            if (at == null)
            {
                int n = Math.Min(left, max);
                var placed = item.Clone(); placed.m_stack = n; placed.m_gridPos = new Vector2i(line.X, line.Y);
                inv.GetAllItems().Add(placed); left -= n;
                ChangedMethod.Invoke(inv, new object[] { false, false });
            }
            else if (Key(at) == line.Key && at.m_stack < max)
            {
                int n = Math.Min(left, max - at.m_stack); at.m_stack += n; left -= n;
                ChangedMethod.Invoke(inv, new object[] { false, false });
            }
        }
        if (left > 0) left -= InventoryTransfers.AddCopy(inv, item, left, false);
        if (left > 0) DropAtFeet(p, item, left);
        if (line.Destination == EscrowDestination.Consume)
        {
            var eat = inv.GetAllItems().FirstOrDefault(i => Key(i) == line.Key);
            if (eat != null) p.UseItem(inv, eat, true);
        }
    }
    private static void DropAtFeet(Player p, ItemDrop.ItemData item, int count)
    {
        var drop = item.Clone(); drop.m_stack = count;
        ItemDrop.DropItem(drop, count, p.transform.position + p.transform.forward * .7f + Vector3.up, p.transform.rotation);
    }

    private static void OnResult(long sender, ZDOID id, long txId, int result)
    {
        if (ledger == null) return;
        var tx = ledger.Pending.FirstOrDefault(t => t.Id == txId);
        if (tx == null || tx.Owner != sender) return;   // only the peer it was sent to may answer
        // "Applied" is only settled once the receipt arrives in this client's copy of the chest data,
        // which came through the server: if the owner crashed before sending its update, the change
        // never reached the world and must not be paid out. Refusals never changed the chest.
        if ((SharedTxResult)result == SharedTxResult.Applied) return;
        Settled(ledger.Resolve(txId, false, sink), (SharedTxResult)result);
    }
    private static void Settled(SharedPendingTx tx, SharedTxResult result)
    {
        if (tx == null) return;
        Trace("tx " + tx.Id + " settled: " + (tx.Applied ? "applied" : result.ToString()), null);
        // Show the chest's real contents once nothing else is in flight for it.
        if (ChestIds.TryGetValue(tx.Chest, out var id))
        {
            var chest = FindChest(id);
            if (chest && !HasPending(chest) && (!tx.Applied || Unconfirmed.Contains(chest))) ReloadView(chest, force: true);
        }
        if (tx.Applied) return;
        Notice(result == SharedTxResult.Denied ? "The chest's owner could not accept that (ward or storage rules)" : "The chest changed; try again");
    }
    private static void Notice(string text)
    {
        if (Time.unscaledTime < nextNotice || !Player.m_localPlayer) return;
        nextNotice = Time.unscaledTime + 1.5f;
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text);
    }

    // ---------- presence, heartbeats, polling ----------
    private static void OnHello(long sender, int protocol)
    {
        bool known = Peers.TryGetValue(sender, out int was) && was == protocol;
        Peers[sender] = protocol;
        if (!known && Active && sender != Self) ZRoutedRpc.instance.InvokeRoutedRPC(sender, HelloRpc, Protocol);
    }
    private static void OnView(long sender, ZDOID id, bool open)
    {
        var chest = FindChest(id);
        var view = chest ? ContainerRegistry.GetView(chest) : null;
        if (!Active || !view || !view.IsValid() || !view.IsOwner() || !Compatible(sender)) return;
        if (open) AddViewer(view, sender);
        else if (Viewers.TryGetValue(id, out var map)) map.Remove(sender);
    }

    internal static void Tick()
    {
        if (ZRoutedRpc.instance != null && registered != ZRoutedRpc.instance)
        {
            registered = ZRoutedRpc.instance; greeted = false;
            registered.Register<int>(HelloRpc, OnHello);
            registered.Register<ZDOID, ZPackage>(TxRpc, OnTx);
            registered.Register<ZDOID, long, int>(ResultRpc, OnResult);
            registered.Register<ZDOID, bool>(ViewRpc, OnView);
            registered.Register<ZDOID, long, string>(SettingsRpc, OnSettings);
            registered.Register<ZDOID, long, int>(SettingsResultRpc, OnSettingsResult);
        }
        if (!Available || ZRoutedRpc.instance == null || !ZNet.instance) return;
        if (Player.m_localPlayer && !Player.m_localPlayer.IsDead()) lastPlayerPosition = Player.m_localPlayer.transform.position;
        // A copy that shows unconfirmed changes must never be saved: if this client now owns it, reload first.
        foreach (var chest in Unconfirmed.ToList())
        {
            if (!chest) { Unconfirmed.Remove(chest); continue; }
            if (chest.IsOwner()) { Plugin.Log.LogInfo("[shared chest] became owner of " + chest.m_name + " with unconfirmed changes shown; reloading saved contents"); ReloadView(chest, force: true); }
        }
        foreach (var expired in SettingsWaiting.Where(w => w.Value.Until < Time.unscaledTime).Select(w => w.Key).ToList())
        { var done = SettingsWaiting[expired].Done; SettingsWaiting.Remove(expired); done?.Invoke("No answer from the chest's owner; try again"); }
        if (!greeted && Active && Player.m_localPlayer)
        {
            greeted = true;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, HelloRpc, Protocol);
        }
        // Heartbeat to the owner while viewing; tell it when done.
        var c = Current;
        if (viewing && (viewing != c || !Viewing(viewing)))
        {
            var v = ContainerRegistry.GetView(viewing);
            if (v && v.IsValid() && !v.IsOwner()) ZRoutedRpc.instance.InvokeRoutedRPC(v.GetZDO().GetOwner(), ViewRpc, v.GetZDO().m_uid, false);
            viewing = null;
        }
        if (Viewing(c))
        {
            if (viewing != c) { viewing = c; viewingSince = Time.unscaledTime; nextHeartbeat = 0; }
            if (Time.unscaledTime - viewingSince > .5f && Time.unscaledTime >= nextHeartbeat)
            {
                nextHeartbeat = Time.unscaledTime + HeartbeatEvery;
                var v = ContainerRegistry.GetView(c);
                ZRoutedRpc.instance.InvokeRoutedRPC(v.GetZDO().GetOwner(), ViewRpc, v.GetZDO().m_uid, true);
            }
        }
        // Owner: forget silent viewers and clear the shared marker once nobody is left.
        foreach (var id in Viewers.Keys.ToList())
        {
            if (HasViewers(id)) continue;
            Viewers.Remove(id);
            var z = ZDOMan.instance.GetZDO(id);
            if (z != null && z.GetOwner() == Self && z.GetLong(ViewedKey, 0L) > Now) z.Set(ViewedKey, 0L);
        }
        // Settle transactions whose reply was lost, from receipts in the chest data.
        if (ledger != null && ledger.Count > 0)
            foreach (var tx in ledger.Poll(Self, k => ChestIds.TryGetValue(k, out var zid) ? ZDOMan.instance.GetZDO(zid)?.GetString(ReceiptsKey, "") : null, Now, GraceTicks, sink))
                Settled(tx, tx.Applied ? SharedTxResult.Applied : SharedTxResult.Conflict);
        if (Undelivered.Count > 0 && Player.m_localPlayer && !Player.m_localPlayer.IsDead())
        {
            var lines = Undelivered.ToList(); Undelivered.Clear();
            foreach (var line in lines) Deliver(line);
        }
    }

    // ---------- chest settings from a viewer ----------
    internal static bool RequestSettings(Container c, ChestSettings settings, Action<string> done)
    {
        if (!Viewing(c)) return false;
        var v = ContainerRegistry.GetView(c);
        long id = ++settingsNext;
        SettingsWaiting[id] = (done, Time.unscaledTime + 10f);
        ZRoutedRpc.instance.InvokeRoutedRPC(v.GetZDO().GetOwner(), SettingsRpc, v.GetZDO().m_uid, id, JsonUtility.ToJson(settings));
        return true;
    }
    private static void OnSettings(long sender, ZDOID id, long requestId, string json)
    {
        int ok = 0;
        try
        {
            var chest = FindChest(id);
            var view = chest ? ContainerRegistry.GetView(chest) : null;
            if (Active && view && view.IsValid() && view.IsOwner() && Supported(chest) && SenderAllowed(chest, sender) && json != null && json.Length < 65536)
            {
                var s = JsonUtility.FromJson<ChestSettings>(json);
                if (s != null) ok = ContainerRegistry.SaveSettingsAsOwner(chest, s) ? 1 : 0;
            }
        }
        catch (Exception e) { Plugin.Log.LogWarning("[shared chest] settings from peer " + sender + " not saved: " + e.GetBaseException().Message); }
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SettingsResultRpc, id, requestId, ok);
    }
    private static void OnSettingsResult(long sender, ZDOID id, long requestId, int ok)
    {
        if (!SettingsWaiting.TryGetValue(requestId, out var waiting)) return;
        SettingsWaiting.Remove(requestId);
        waiting.Done?.Invoke(ok == 1 ? "Saved by the chest's owner" : "The chest's owner could not save that");
    }

    private static void Trace(string text, Container c)
    {
        if (Plugin.OwnershipTrace == null || !Plugin.OwnershipTrace.Value) return;
        Plugin.Log.LogInfo("[shared chest] " + text + (c ? " · " + c.m_name : ""));
    }
}
