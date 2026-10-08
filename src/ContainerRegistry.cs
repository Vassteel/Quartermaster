using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

internal static class ContainerRegistry
{
    internal static readonly List<Container> All = new List<Container>();
    private static readonly Dictionary<Inventory, Container> Owners = new Dictionary<Inventory, Container>();
    private static readonly Dictionary<Container, (string json, ChestSettings settings)> Cache = new Dictionary<Container, (string, ChestSettings)>();
    // Stable world-data key retained from the initial development build.
    internal const string SettingsKey = "Hearthward.chest.v1";
    private static readonly System.Reflection.MethodInfo CheckAccess = AccessTools.Method(typeof(Container), "CheckAccess");
    internal static void Register(Container c)
    {
        if (!c || SafeInventory(c) == null || !GetView(c) || !GetView(c).IsValid()) return;
        // Do not automate graves, ships, carts, loot containers or dungeon chests.
        if (c.GetComponentInParent<Ship>() || !c.GetComponent<Piece>() || (!PrefabName(c).Contains("chest") && Apothecary.Definition(PrefabName(c)) == null && !ExternalStorageCompatibility.Recognizes(c))) return;
        if (!All.Contains(c)) All.Add(c);
        Owners[c.GetInventory()] = c;
        if (!c.GetComponent<ChestVisual>()) c.gameObject.AddComponent<ChestVisual>();
        Learn(c);
    }
    internal static void Unregister(Container c)
    {
        All.Remove(c); Cache.Remove(c); StorageObservation.Forget(c); PlayerUse.Forget(c);
        Automation.ForgetStatus(c);
        foreach (var k in Owners.Where(p => p.Value == c).Select(p => p.Key).ToArray()) Owners.Remove(k);
    }
    internal static void Clear() { All.Clear(); Owners.Clear(); Cache.Clear(); StorageObservation.Clear(); PlayerUse.Clear(); }
    internal static Inventory SafeInventory(Container c) => c ? c.GetInventory() : null;
    internal static Container OwnerOf(Inventory i) => i != null && Owners.TryGetValue(i, out var c) && c ? c : null;
    internal static ZNetView GetView(Container c) => c ? (c.m_rootObjectOverride ? c.m_rootObjectOverride : c.GetComponent<ZNetView>()) : null;
    internal static string PrefabName(Component c) => c ? c.gameObject.name.Replace("(Clone)", "") : "";
    internal static bool Accessible(Container c)
    {
        if (!c || SafeInventory(c) == null || !Player.m_localPlayer) return false;
        // Awake may precede valid network state; a character change can also clear
        // the registry while the scene's existing chests survive. Recover on access.
        if (!Owners.ContainsKey(c.GetInventory())) Register(c);
        if (!Owners.ContainsKey(c.GetInventory())) return false;
        var v = GetView(c);
        if (!v || !v.IsValid()) return false;
        if (!(bool)CheckAccess.Invoke(c, new object[] { Player.m_localPlayer.GetPlayerID() })) return false;
        return !c.m_checkGuardStone || PrivateArea.CheckAccess(c.transform.position, 0f, false, true);
    }
    internal static bool IsUsable(Container c, bool allowInUse)
    {
        if (!Plugin.Enabled.Value || !Accessible(c) || !NativeStorageAccess.CanWrite(GetView(c))) return false;
        bool busy = c.IsInUse() || GetView(c).GetZDO().GetInt(ZDOVars.s_inUse) != 0 || SharedChests.Viewed(c);
        if (busy && (!allowInUse || Plugin.OpenContainer != c)) return false;
        Refresh(c);
        return true;
    }
    // Only automation observes this grace period. It never denies a player Open or craft.
    private static readonly AutomationCourtesy<Container> PlayerUse = new AutomationCourtesy<Container>();
    internal static bool CanAutomate(Container c)
    {
        if (!c || !GetView(c) || !GetView(c).IsValid()) return false;
        bool busy = c.IsInUse() || GetView(c).GetZDO().GetInt(ZDOVars.s_inUse) != 0 || SharedChests.Viewed(c);
        // A chest claimed unilaterally waits one cycle so a newer server snapshot cannot be overwritten.
        return !PlayerUse.Yield(c, busy, Time.unscaledTime) && IsUsable(c, false) && ChestOwnership.Settled(GetView(c));
    }
    // Another client announced a craft withdrawal from this chest: yield and do not hand it over meanwhile.
    internal static void Yield(Container c, float seconds) { if (c) PlayerUse.Hold(c, Time.unscaledTime + seconds); }
    internal static bool Yielding(Container c) => c && PlayerUse.Holding(c, Time.unscaledTime);
    private static readonly System.Reflection.MethodInfo LoadInventory=AccessTools.Method(typeof(Container),"Load");
    internal static void Refresh(Container c)
    {if(c&&GetView(c)&&GetView(c).IsValid())LoadInventory.Invoke(c,null);}
    internal static bool CanObserve(Container c)
    {
        if(PostalParcel.IsParcel(c)||!Plugin.Enabled.Value||!Accessible(c))return false;
        return true;
    }
    internal static List<Container> VisibleStores(Vector3 point,float range)
    {
        float sq=range*range;
        return All.Where(c=>c&&(c.transform.position-point).sqrMagnitude<=sq&&CanObserve(c))
            .OrderBy(c=>(c.transform.position-point).sqrMagnitude).ToList();
    }
    internal static List<Container> Nearby(Vector3 point, float range, bool requireAccept, bool allowInUse, string group = null)
    {
        float sq = range * range;
        return All.Where(c => c && (c.transform.position - point).sqrMagnitude <= sq && IsUsable(c, allowInUse)
            && (!requireAccept || GetSettings(c).AcceptStorage) && (group == null || Policy.SameGroup(GetSettings(c).Group, group)))
            .OrderBy(c => (c.transform.position - point).sqrMagnitude).ThenBy(c => GetView(c).GetZDO().m_uid.ToString(), StringComparer.Ordinal).ToList();
    }
    internal static ChestSettings GetSettings(Container c)
    {
        var v = GetView(c);
        string json = v && v.IsValid() ? v.GetZDO().GetString(SettingsKey, "") : "";
        if (c && Cache.TryGetValue(c, out var entry) && entry.json == json) return entry.settings;
        ChestSettings s;
        try { s = string.IsNullOrEmpty(json) ? ChestDefaults.Create(PrefabName(c)) : JsonUtility.FromJson<ChestSettings>(json); }
        catch { s = new ChestSettings { AcceptStorage = false, CraftingSupply = false, FuelSupply = false, ProcessingSupply = false }; }
        if(PostalMailbox.IsMailbox(c)||PostalParcel.IsParcel(c))
        {
            s.Deposit=s.AcceptStorage=s.CraftingSupply=s.FuelSupply=s.ProcessingSupply=s.LivestockFeed=s.FeedUntamed=s.AutoSort=s.Learn=s.AutoAssign=s.Overflow=false;
        }
        s.Remembered = s.Remembered ?? new List<string>(); s.Forgotten = s.Forgotten ?? new List<string>();
        if (c) Cache[c] = (json, s);
        return s;
    }
    internal static bool SaveSettings(Container c, ChestSettings s)
    {
        if (!IsUsable(c, true)) return false;
        string json = JsonUtility.ToJson(s);
        GetView(c).GetZDO().Set(SettingsKey, json); Cache[c] = (json, s);
        return true;
    }
    // Settings sent by another player viewing this chest; the sender's access was checked by the caller.
    internal static bool SaveSettingsAsOwner(Container c, ChestSettings s)
    {
        if (!Plugin.Enabled.Value || !c || !NativeStorageAccess.CanWrite(GetView(c))) return false;
        if (PostalMailbox.IsMailbox(c) || PostalParcel.IsParcel(c)) return false;
        string json = JsonUtility.ToJson(s);
        GetView(c).GetZDO().Set(SettingsKey, json); Cache[c] = (json, s);
        return true;
    }
    internal static void Learn(Container c)
    {
        if (!c || !GetView(c) || !GetView(c).IsValid() || !NativeStorageAccess.CanWrite(GetView(c))) return;
        var s = GetSettings(c);
        if (s.Observe(c.GetInventory().GetAllItems().Select(InventoryTransfers.ItemId)))
        {
            string json = JsonUtility.ToJson(s);
            GetView(c).GetZDO().Set(SettingsKey, json); Cache[c] = (json, s);
        }
    }
}
