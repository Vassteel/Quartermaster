using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

internal sealed class BaseNetwork
{
    internal string Group;
    internal List<Container> Hubs, Chests;
    internal List<Component> Machines;
    internal bool Controls => Hubs.Count > 0 && ContainerRegistry.GetView(Hubs[0]).IsOwner();
    internal bool Contains(Vector3 point) => Hubs.Any(h => h && (h.transform.position - point).sqrMagnitude <= Plugin.Range.Value * Plugin.Range.Value);
}

internal static class Automation
{
    internal static readonly List<Component> Devices = new List<Component>();
    internal static readonly List<MonsterAI> Animals = new List<MonsterAI>();
    internal static readonly List<ItemDrop> Outputs = new List<ItemDrop>();
    private static readonly Dictionary<Component, string> Statuses = new Dictionary<Component, string>();
    private static readonly Dictionary<MonsterAI, ItemDrop> Feeding = new Dictionary<MonsterAI, ItemDrop>();
    private static readonly Dictionary<Component, (string json, MachineSettings settings)> SettingsCache = new Dictionary<Component, (string, MachineSettings)>();
    private static List<BaseNetwork> networks = new List<BaseNetwork>();
    private static int deviceCursor, depositCursor, animalCursor, outputCursor;
    private const string MachineKey = "Hearthward.machine.v1";
    internal static void Clear()
    {
        Devices.Clear(); Animals.Clear(); Outputs.Clear(); Statuses.Clear(); Feeding.Clear(); SettingsCache.Clear(); networks.Clear();
        deviceCursor = depositCursor = animalCursor = outputCursor = 0;
    }
    internal static void Register(Component c)
    {
        if (c is CookingStation rack && !rack.m_requireFire) return;
        if (c && c.GetComponent<Piece>() && !Devices.Contains(c)) Devices.Add(c);
    }
    internal static ZNetView View(Component c) => c ? c.GetComponent<ZNetView>() : null;
    internal static bool Owned(Component c) => c && View(c) && View(c).IsValid() && View(c).IsOwner();
    internal static MachineSettings Settings(Component c)
    {
        var v = View(c); string json = v && v.IsValid() ? v.GetZDO().GetString(MachineKey, "") : "";
        if (SettingsCache.TryGetValue(c, out var cached) && cached.json == json) return cached.settings;
        MachineSettings s;
        try { s = string.IsNullOrEmpty(json) ? new MachineSettings() : JsonUtility.FromJson<MachineSettings>(json); }
        catch { s = new MachineSettings { Paused = true }; }
        s.Caps = s.Caps ?? new List<ProductCap>();
        bool migrated = false;
        // Early builds used the recipe's display-name token where its nonserialized
        // ItemData prefab reference was unset. Keep those values under canonical IDs.
        foreach (var group in Products(c).GroupBy(p => p.m_itemData.m_shared.m_name))
        {
            var ids = group.Select(InventoryTransfers.PrefabId).Distinct().ToList();
            if (ids.Count == 1) migrated |= s.MigrateCapId(group.Key, ids[0]);
        }
        if (migrated && Owned(c)) { json = JsonUtility.ToJson(s); v.GetZDO().Set(MachineKey, json); }
        SettingsCache[c] = (json, s); return s;
    }
    internal static bool Save(Component c, MachineSettings s)
    {
        if (!Owned(c) || !PrivateArea.CheckAccess(c.transform.position, 0f, false, true)) return false;
        string json = JsonUtility.ToJson(s); View(c).GetZDO().Set(MachineKey, json); SettingsCache[c] = (json, s); return true;
    }
    internal static string Status(Component c) => c && Statuses.TryGetValue(c, out var s) ? s : "Waiting for a Deposit Chest in range";
    internal static void SetStatus(Component c, string s) { if (c) Statuses[c] = s; }
    internal static IEnumerable<ItemDrop> Products(Component c)
    {
        if (c is Smelter s) return s.m_conversion.Where(x => x.m_to).Select(x => x.m_to).Distinct();
        if (c is Fermenter f) return f.m_conversion.Where(x => x.m_to).Select(x => x.m_to).Distinct();
        if (c is CookingStation rack) return rack.m_conversion.Where(x => x.m_to).Select(x => x.m_to).Distinct();
        return Enumerable.Empty<ItemDrop>();
    }
    internal static BaseNetwork Network(Component c, string group = null)
    {
        group = group ?? (c is Container chest ? ContainerRegistry.GetSettings(chest).Group : Settings(c).Group);
        return networks.FirstOrDefault(n => Policy.SameGroup(n.Group, group) && n.Contains(c.transform.position));
    }
    private static void RebuildNetworks()
    {
        var chests = ContainerRegistry.All.Where(ContainerRegistry.Accessible).ToList();
        networks = chests.Where(c => ContainerRegistry.GetSettings(c).Deposit)
            .GroupBy(c => Policy.Group(ContainerRegistry.GetSettings(c).Group))
            .Select(g => new BaseNetwork { Group = g.Key, Hubs = g.OrderBy(c => ContainerRegistry.GetView(c).GetZDO().m_uid.ToString(), StringComparer.Ordinal).ToList() }).ToList();
        foreach (var n in networks)
        {
            n.Chests = chests.Where(c => Policy.SameGroup(ContainerRegistry.GetSettings(c).Group, n.Group) && n.Contains(c.transform.position)).ToList();
            n.Machines = Devices.Where(c => c && n.Contains(c.transform.position) && Policy.SameGroup(Settings(c).Group, n.Group)).ToList();
        }
    }
    internal static void Tick()
    {
        foreach (var c in ContainerRegistry.All.Where(c => !c).ToArray()) ContainerRegistry.Unregister(c);
        foreach (var c in Devices.Where(c => !c).ToArray()) { Devices.Remove(c); Statuses.Remove(c); SettingsCache.Remove(c); }
        Animals.RemoveAll(c => !c); Outputs.RemoveAll(c => !c);
        foreach (var a in Feeding.Keys.Where(a => !a || !Feeding[a]).ToArray()) Feeding.Remove(a);
        foreach (var c in ContainerRegistry.All) ContainerRegistry.Learn(c);
        RebuildNetworks();
        Rotate(ContainerRegistry.All.Where(c => c && (ContainerRegistry.GetSettings(c).Deposit || ContainerRegistry.GetSettings(c).AutoSort)).ToList(), ref depositCursor, Plugin.Budget.Value, ProcessChest);
        Rotate(Devices, ref deviceCursor, Plugin.Budget.Value, ProcessMachine);
        Rotate(Animals, ref animalCursor, Plugin.Budget.Value, Feed);
        Rotate(Outputs, ref outputCursor, Plugin.Budget.Value, CollectOutput);
    }
    private static void Rotate<T>(List<T> list, ref int cursor, int budget, Action<T> action)
    {
        if (list.Count == 0) { cursor = 0; return; }
        int count = Math.Min(budget, list.Count); cursor %= list.Count;
        for (int i = 0; i < count; i++) action(list[(cursor + i) % list.Count]);
        cursor = (cursor + count) % list.Count;
    }
    private static List<Container> Destinations(BaseNetwork n, ItemDrop.ItemData item, Vector3 point, Container exclude = null, bool fallbackToDeposit = false)
    {
        if (n == null) return new List<Container>();
        string id = InventoryTransfers.ItemId(item);
        var list = n.Chests.Where(c => c != exclude && ContainerRegistry.IsUsable(c, false) && ContainerRegistry.GetSettings(c).Accepts(id))
            .OrderBy(c => ContainerRegistry.GetSettings(c).Overflow ? 2 : ContainerRegistry.GetSettings(c).Preferred ? 0 : 1)
            .ThenBy(c => (c.transform.position - point).sqrMagnitude)
            .ThenBy(c => ContainerRegistry.GetView(c).GetZDO().m_uid.ToString(), StringComparer.Ordinal).ToList();
        if (fallbackToDeposit) list.AddRange(n.Hubs.Where(c => c != exclude && ContainerRegistry.IsUsable(c, false)));
        return list;
    }
    internal static int Route(BaseNetwork n, ItemDrop.ItemData item, int count, Vector3 point, Inventory source = null, Container exclude = null, bool fallbackToDeposit = false)
    {
        int remaining = count;
        foreach (var c in Destinations(n, item, point, exclude, fallbackToDeposit))
        {
            int moved = source == null ? InventoryTransfers.AddCopy(c.GetInventory(), item, remaining, false)
                : InventoryTransfers.Move(source, c.GetInventory(), item, remaining, false);
            remaining -= moved;
            if (moved > 0) { ChestVisual.Pulse(c); ContainerRegistry.Learn(c); }
            if (remaining <= 0) break;
        }
        return count - remaining;
    }
    private static void ProcessChest(Container c)
    {
        if (!ContainerRegistry.IsUsable(c, false)) return;
        var s = ContainerRegistry.GetSettings(c); var n = Network(c);
        if (s.Deposit)
        {
            if (n == null || !n.Controls) { SetStatus(c, "Waiting for base network owner"); return; }
            if (!DepositGull.ReadyForNextSlot(c)) return;
            var sorted = DepositSorting.OneSlot(c.GetInventory().GetAllItems(),
                (item, count) => Route(n, item, count, c.transform.position, c.GetInventory(), c));
            string blocked = sorted.BlockedItem == null ? "" :
                (Destinations(n, sorted.BlockedItem, c.transform.position, c).Count == 0 ? "No destination: " : "Storage full or busy: ") + Label(sorted.BlockedItem);
            SetStatus(c, sorted.Moved > 0 ? "Sorted one slot · " + sorted.Moved + " items" : blocked.Length > 0 ? blocked : "Ready for deposits");
            DepositGull.Report(c, sorted.Moved, blocked.Length > 0, sorted.Item);
        }
        if (s.AutoSort)
        {
            InventoryTransfers.StackWithin(c.GetInventory());
            WarehouseService.SortInventory(c.GetInventory(), false);
        }
    }
    private static bool CanRun(Component c, out BaseNetwork n)
    {
        n = Network(c);
        if (Settings(c).Paused) { SetStatus(c, "Automation paused"); return false; }
        if (n == null) { SetStatus(c, "No Deposit Chest in this base group and range"); return false; }
        if (!n.Controls || !Owned(c)) { SetStatus(c, "Waiting for base network ownership"); return false; }
        if (!PrivateArea.CheckAccess(c.transform.position, 0f, false, true)) { SetStatus(c, "Ward access blocked"); return false; }
        return true;
    }
    private static void ProcessMachine(Component c)
    {
        if (!CanRun(c, out var n)) return;
        if (c is Smelter s) ProcessSmelter(n, s);
        else if (c is Fermenter f) ProcessFermenter(n, f);
        else if (c is Fireplace fire) ProcessFire(n, fire);
        else if (c is CookingStation rack) ProcessCooking(n, rack);
    }
    internal static long Stock(BaseNetwork n, string id)
    {
        long count = 0;
        // Include inaccessible-to-transfer/in-use/foreign-owned stock within this accessible network.
        foreach (var c in n.Chests) foreach (var item in c.GetInventory().GetAllItems())
            if (InventoryTransfers.ItemId(item) == id) count += item.m_stack;
        foreach (var d in Outputs) if (d && n.Contains(d.transform.position) && InventoryTransfers.ItemId(d.m_itemData) == id) count += d.m_itemData.m_stack;
        return count;
    }
    internal static long Queued(BaseNetwork n, string id)
    {
        long total = 0;
        foreach (var c in n.Machines)
        {
            var v = View(c); if (!v || !v.IsValid()) continue;
            var z = v.GetZDO();
            if (c is Smelter s)
            {
                for (int i = 0; i < z.GetInt(ZDOVars.s_queued); i++)
                {
                    string input = z.GetString("item" + i);
                    var conv = s.m_conversion.FirstOrDefault(x => x.m_from && x.m_from.name == input);
                    if (conv?.m_to && InventoryTransfers.PrefabId(conv.m_to) == id) total++;
                }
                string processed = z.GetString(ZDOVars.s_spawnOre);
                var ready = s.m_conversion.FirstOrDefault(x => x.m_from && x.m_from.name == processed);
                if (ready?.m_to && InventoryTransfers.PrefabId(ready.m_to) == id) total += z.GetInt(ZDOVars.s_spawnAmount);
            }
            else if (c is Fermenter f)
            {
                int content = z.GetInt(ZDOVars.s_content);
                var conv = f.m_conversion.FirstOrDefault(x => x.m_from && x.m_from.name.GetStableHashCode() == content);
                if (conv?.m_to && InventoryTransfers.PrefabId(conv.m_to) == id) total += conv.m_producedItems;
                if (f.IsInvoking("DelayedTap"))
                {
                    int tapped = Traverse.Create(f).Field<int>("m_delayedTapItem").Value;
                    var pending = f.m_conversion.FirstOrDefault(x => x.m_from && x.m_from.name.GetStableHashCode() == tapped);
                    if (pending?.m_to && InventoryTransfers.PrefabId(pending.m_to) == id) total += pending.m_producedItems;
                }
            }
            else if (c is CookingStation rack)
            {
                for (int i = 0; i < rack.m_slots.Length; i++)
                {
                    string slot = z.GetString("slot" + i);
                    if (slot.Length == 0) continue;
                    if (z.GetInt("slotstatus" + i) == 2) { if (slot == id) total++; continue; }
                    if (rack.m_conversion.Any(recipe => recipe.m_from && recipe.m_to
                        && Policy.CookingSlotProduces(slot, InventoryTransfers.PrefabId(recipe.m_from), InventoryTransfers.PrefabId(recipe.m_to), id))) total++;
                }
            }
        }
        return total;
    }
    private static int EffectiveCap(BaseNetwork n, string id)
    {
        var caps = n.Machines.Where(c => Products(c).Any(p => InventoryTransfers.PrefabId(p) == id)).Select(c => Settings(c).Cap(id));
        return caps.Any() ? caps.Min() : 200;
    }
    private static bool ProductionAllowed(BaseNetwork n, Component c, ItemDrop output, int batch)
    {
        string id = InventoryTransfers.PrefabId(output);
        long queued = Queued(n, id);
        int cap = EffectiveCap(n, id);
        if (Policy.BatchesAllowed(cap, Stock(n, id), queued, batch, 1) == 0)
        { SetStatus(c, Label(output.m_itemData) + " cap reached (" + cap + ", including queued output)"); return false; }
        var sample = NewOutput(output, false);
        long room = Destinations(n, sample, c.transform.position, null, true).Sum(ch => (long)InventoryTransfers.CapacityFor(ch.GetInventory(), sample, false));
        if (room < queued + batch) { SetStatus(c, "Output storage full or busy"); return false; }
        return true;
    }
    private static bool Take(BaseNetwork n, ItemDrop input, Func<ChestSettings, bool> permission, out ItemDrop.ItemData taken, out Container source)
    {
        taken = null; source = null;
        string id = InventoryTransfers.PrefabId(input);
        foreach (var c in n.Chests)
        {
            var settings = ContainerRegistry.GetSettings(c);
            if (settings.Deposit || !permission(settings) || !ContainerRegistry.IsUsable(c, false)) continue;
            var item = c.GetInventory().GetAllItems().FirstOrDefault(i => InventoryTransfers.ItemId(i) == id && i.m_worldLevel >= Game.m_worldLevel);
            if (item == null) continue;
            var copy = item.Clone(); copy.m_stack = 1;
            if (!c.GetInventory().RemoveItem(item, 1)) continue;
            taken = copy; source = c; return true;
        }
        return false;
    }
    private static void Restore(Container source, ItemDrop.ItemData item)
    {
        if (source && InventoryTransfers.AddCopy(source.GetInventory(), item, 1, false) == 1) return;
        // If an overlapping mod changed the source during its callback, return the item physically.
        ItemDrop.DropItem(item, 1, source ? source.transform.position + Vector3.up : Player.m_localPlayer.transform.position, Quaternion.identity);
    }
    private static bool Supply(BaseNetwork n, Component device, ItemDrop input, Func<ChestSettings, bool> permission,
        string method, Func<ItemDrop.ItemData, object[]> arguments, Func<bool> accepted)
    {
        if (!Take(n, input, permission, out var item, out var source)) return false;
        try { AccessTools.Method(device.GetType(), method).Invoke(device, arguments(item)); }
        catch (Exception e) { Plugin.Log.LogWarning("Machine supply handler: " + e.GetBaseException().Message); }
        if (accepted()) return true;
        Restore(source, item); SetStatus(device, "Input not accepted; returned to storage"); return false;
    }
    private static void ProcessSmelter(BaseNetwork n, Smelter s)
    {
        var z = View(s).GetZDO();
        int initialQueue = z.GetInt(ZDOVars.s_queued);
        SetStatus(s, initialQueue > 0 ? ProcessingStatus(s) : "No eligible production ingredients");
        int count = 0;
        for (int attempt = 0; attempt < 8 && z.GetInt(ZDOVars.s_queued) < s.m_maxOre; attempt++)
        {
            bool added = false;
            foreach (var conv in s.m_conversion)
            {
                if (!conv.m_from || !conv.m_to) continue;
                if (!Settings(s).AllowValuableWood && Policy.ProtectedWood(conv.m_from.name)) continue;
                if (!ProductionAllowed(n, s, conv.m_to, 1)) continue;
                int before = z.GetInt(ZDOVars.s_queued);
                // Local-owner handler is synchronous; the next device sees the updated queue immediately.
                if (!Supply(n, s, conv.m_from, x => x.ProcessingSupply, "RPC_AddOre", item => new object[] { 0L, conv.m_from.name, item.m_cheated }, () => z.GetInt(ZDOVars.s_queued) > before)) continue;
                added = true; count++; break;
            }
            if (!added) break;
        }
        if (z.GetInt(ZDOVars.s_queued) > 0 && s.m_fuelItem && s.m_maxFuel > 0)
        {
            for (int i = 0; i < 8 && z.GetFloat(ZDOVars.s_fuel) < s.m_maxFuel - 1; i++)
            {
                float before = z.GetFloat(ZDOVars.s_fuel);
                if (!Supply(n, s, s.m_fuelItem, x => x.FuelSupply, "RPC_AddFuel", item => new object[] { 0L }, () => z.GetFloat(ZDOVars.s_fuel) > before))
                { SetStatus(s, "Waiting for " + Label(s.m_fuelItem.m_itemData)); break; }
            }
        }
        if (count > 0) SetStatus(s, ProcessingStatus(s));
    }
    internal static void RecoverProcessorClock(Smelter s)
    {
        if (!Plugin.Enabled.Value || !Owned(s) || !s.GetComponent<Piece>() || !ZNet.instance) return;
        var z = View(s).GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        long last = z.GetLong(ZDOVars.s_startTime, now);
        float accumulated = z.GetFloat(ZDOVars.s_accTime);
        float corrected = Policy.ValidProcessorAccumulator(accumulated);
        bool invalidClock = last < 0 || last > now;
        if (!invalidClock && accumulated == corrected) return;
        if (invalidClock) z.Set(ZDOVars.s_startTime, now);
        if (accumulated != corrected) z.Set(ZDOVars.s_accTime, corrected);
        Plugin.Log.LogWarning("Recovered processor clock for " + s.name + ": accumulated=" + accumulated
            + "s, future/invalid timestamp=" + invalidClock + ". Queued input and bake progress preserved.");
    }
    private static string ProcessingStatus(Smelter s)
    {
        var z = View(s).GetZDO();
        int queued = z.GetInt(ZDOVars.s_queued);
        // Read vanilla state rather than implying that visible flames mean progress.
        if (Traverse.Create(s).Field<bool>("m_blockedSmoke").Value) return "Processing blocked: smoke cannot escape";
        if (s.m_requiresRoof && !Traverse.Create(s).Field<bool>("m_haveRoof").Value) return "Processing blocked: needs a roof";
        if (s.m_maxFuel > 0 && z.GetFloat(ZDOVars.s_fuel) <= 0) return "Processing blocked: waiting for fuel";
        if (s.m_windmill && s.m_windmill.GetPowerOutput() <= 0) return "Processing blocked: waiting for wind";
        if (s.m_secPerProduct <= 0) return "Processing blocked: invalid production time";
        return "Processing " + z.GetFloat(ZDOVars.s_bakeTimer).ToString("0") + "/" + s.m_secPerProduct.ToString("0")
            + "s · " + queued + " queued" + (queued >= s.m_maxOre ? " (full)" : "");
    }
    private static int FreeCookingSlot(CookingStation rack)
    {
        var z = View(rack).GetZDO();
        for (int i = 0; i < rack.m_slots.Length; i++) if (z.GetString("slot" + i).Length == 0) return i;
        return -1;
    }
    internal static void HarvestCooking(CookingStation rack)
    {
        if (!Plugin.Enabled.Value || !rack.m_requireFire || !rack.GetComponent<Piece>() || !Owned(rack)) return;
        var n = Network(rack);
        if (n == null || !n.Controls || Settings(rack).Paused || !PrivateArea.CheckAccess(rack.transform.position, 0f, false, true)) return;
        var z = View(rack).GetZDO();
        // This runs on native cooking updates, independently of the base work budget.
        // Removing a completed item uses the game's ejection handler, with one output
        // per input. Manual collection retains the game's own skill/bonus handling.
        for (int i = 0; i < rack.m_slots.Length; i++)
        {
            if (!Policy.CookingSlotReady(z.GetString("slot" + i), z.GetInt("slotstatus" + i))) continue;
            try
            {
                AccessTools.Method(typeof(CookingStation), "RPC_RemoveDoneItem").Invoke(rack,
                    new object[] { 0L, rack.transform.position + rack.transform.forward * 2f, 1 });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Cooking rack removal failed: " + e.GetBaseException().Message); break; }
        }
    }
    private static void ProcessCooking(BaseNetwork n, CookingStation rack)
    {
        HarvestCooking(rack);
        if (!(bool)AccessTools.Method(typeof(CookingStation), "IsFireLit").Invoke(rack, null))
        { SetStatus(rack, "Needs a lit fire beneath the cooking station"); return; }
        int loaded = 0;
        SetStatus(rack, FreeCookingSlot(rack) < 0 ? "Cooking · all slots occupied" : "Waiting for raw food in eligible base storage");
        for (int attempt = 0; attempt < rack.m_slots.Length; attempt++)
        {
            int slot = FreeCookingSlot(rack);
            if (slot < 0) break;
            bool added = false;
            foreach (var recipe in rack.m_conversion)
            {
                if (!recipe.m_from || !recipe.m_to || !ProductionAllowed(n, rack, recipe.m_to, 1)) continue;
                string input = InventoryTransfers.PrefabId(recipe.m_from);
                if (!Supply(n, rack, recipe.m_from, settings => settings.ProcessingSupply, "RPC_AddItem",
                    item => new object[] { 0L, input, item.m_cheated }, () => View(rack).GetZDO().GetString("slot" + slot) == input)) continue;
                added = true; loaded++; break;
            }
            if (!added) break;
        }
        if (loaded > 0) SetStatus(rack, "Cooking · finished food ejects before collection");
    }
    private static void ProcessFire(BaseNetwork n, Fireplace f)
    {
        var z = View(f).GetZDO();
        if (f.m_infiniteFuel || !f.m_fuelItem) { SetStatus(f, "No fuel required"); return; }
        if (z.GetInt(ZDOVars.s_state, 1) == 0) { SetStatus(f, "Switched off"); return; }
        float fuel = z.GetFloat(ZDOVars.s_fuel);
        if (fuel > f.m_maxFuel * .25f) { SetStatus(f, "Fueled"); return; }
        for (int i = 0; i < 8 && Mathf.CeilToInt(z.GetFloat(ZDOVars.s_fuel)) < f.m_maxFuel; i++)
        {
            float before = z.GetFloat(ZDOVars.s_fuel);
            if (!Supply(n, f, f.m_fuelItem, x => x.FuelSupply, "RPC_AddFuel", item => new object[] { 0L }, () => z.GetFloat(ZDOVars.s_fuel) > before))
            { SetStatus(f, "Waiting for " + Label(f.m_fuelItem.m_itemData)); return; }
        }
        SetStatus(f, "Fueled");
    }
    private static void ProcessFermenter(BaseNetwork n, Fermenter f)
    {
        var z = View(f).GetZDO();
        AccessTools.Method(typeof(Fermenter), "UpdateCover").Invoke(f, new object[] { 0f, true });
        if (f.IsInvoking("DelayedTap")) { SetStatus(f, "Tapping"); return; }
        int content = z.GetInt(ZDOVars.s_content);
        var conv = f.m_conversion.FirstOrDefault(x => x.m_from && x.m_from.name.GetStableHashCode() == content);
        if (content != 0 && conv != null)
        {
            double elapsed = (double)AccessTools.Method(typeof(Fermenter), "GetFermentationTime").Invoke(f, null);
            if (elapsed <= f.m_fermentationDuration)
            {
                bool roof = Traverse.Create(f).Field<bool>("m_hasRoof").Value;
                bool exposed = Traverse.Create(f).Field<bool>("m_exposed").Value;
                SetStatus(f, !roof ? "Needs a roof" : exposed ? "Needs shelter" : "Fermenting · " + Mathf.Clamp((float)(elapsed / f.m_fermentationDuration * 100), 0, 100).ToString("0") + "%"); return;
            }
            // Tap through the native handler so the game emits the actual batch and effects.
            AccessTools.Method(typeof(Fermenter), "RPC_Tap").Invoke(f, new object[] { 0L });
            SetStatus(f, f.IsInvoking("DelayedTap") ? "Tapping · output collected after 60 seconds" : "Ready to tap");
            return;
        }
        if (!Traverse.Create(f).Field<bool>("m_hasRoof").Value || Traverse.Create(f).Field<bool>("m_exposed").Value)
        { SetStatus(f, "Needs roof and shelter"); return; }
        SetStatus(f, "Waiting for a mead base");
        foreach (var recipe in f.m_conversion)
        {
            if (!recipe.m_from || !recipe.m_to || !ProductionAllowed(n, f, recipe.m_to, recipe.m_producedItems)) continue;
            if (!Supply(n, f, recipe.m_from, x => x.ProcessingSupply, "RPC_AddItem", item => new object[] { 0L, recipe.m_from.name.GetStableHashCode(), item.m_cheated }, () => z.GetInt(ZDOVars.s_content) != 0)) continue;
            SetStatus(f, "Fermenting · 0%"); return;
        }
    }
    internal static ItemDrop.ItemData NewOutput(ItemDrop prefab, bool cheated)
    {
        var item = prefab.m_itemData.Clone(); item.m_dropPrefab = prefab.gameObject; item.m_worldLevel = (byte)Game.m_worldLevel;
        item.m_cheated = cheated && !PlayerProfile.s_bypassCheatChecks; return item;
    }
    private static void CollectOutput(ItemDrop d)
    {
        if (!Owned(d) || d.m_itemData == null) return;
        if (!ProductionOutput.Ready(d)) return;
        string group = View(d).GetZDO().GetString(ProductionOutput.GroupKey, "");
        var n = networks.FirstOrDefault(x => Policy.SameGroup(group, x.Group) && x.Contains(d.transform.position));
        if (n == null || !n.Controls) return;
        int moved = Route(n, d.m_itemData, d.m_itemData.m_stack, d.transform.position, null, null, true);
        if (moved <= 0) return;
        if (moved == d.m_itemData.m_stack) View(d).Destroy(); else d.SetStack(d.m_itemData.m_stack - moved);
    }
    private static void Feed(MonsterAI ai)
    {
        if (!ai || !Owned(ai) || ai.IsAlerted() || !PrivateArea.CheckAccess(ai.transform.position, 0f, false, true)) return;
        var tame = ai.GetComponent<Tameable>(); var character = ai.GetComponent<Character>();
        if (!tame || !character || character.IsDead() || !tame.IsHungry() || Feeding.ContainsKey(ai)) return;
        var n = networks.FirstOrDefault(x => x.Controls && x.Contains(ai.transform.position)); if (n == null) return;
        foreach (var food in ai.m_consumeItems)
        {
            if (!food || !Take(n, food, s => s.LivestockFeed && (tame.IsTamed() || s.FeedUntamed), out var item, out var source)) continue;
            var drop = ItemDrop.DropItem(item, 1, ai.transform.position + ai.transform.forward * .7f + Vector3.up * .2f, Quaternion.identity);
            if (drop) Feeding[ai] = drop;
            else Restore(source, item);
            break;
        }
    }
    internal static string Label(ItemDrop.ItemData item) => Localization.instance != null ? Localization.instance.Localize(item?.m_shared?.m_name ?? "Unknown item") : item?.m_shared?.m_name ?? "Unknown item";
}
