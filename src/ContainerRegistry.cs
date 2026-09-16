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
        if (c.GetComponentInParent<Ship>() || !c.GetComponent<Piece>() || !PrefabName(c).Contains("chest")) return;
        if (!All.Contains(c)) All.Add(c);
        Owners[c.GetInventory()] = c;
        if (!c.GetComponent<ChestVisual>()) c.gameObject.AddComponent<ChestVisual>();
        Learn(c);
    }
    internal static void Unregister(Container c)
    {
        All.Remove(c); Cache.Remove(c);
        Automation.ForgetStatus(c);
        foreach (var k in Owners.Where(p => p.Value == c).Select(p => p.Key).ToArray()) Owners.Remove(k);
    }
    internal static void Clear() { All.Clear(); Owners.Clear(); Cache.Clear(); }
    internal static Inventory SafeInventory(Container c) => c ? c.GetInventory() : null;
    internal static Container OwnerOf(Inventory i) => i != null && Owners.TryGetValue(i, out var c) && c ? c : null;
    internal static ZNetView GetView(Container c) => c ? (c.m_rootObjectOverride ? c.m_rootObjectOverride : c.GetComponent<ZNetView>()) : null;
    internal static string PrefabName(Component c) => c ? c.gameObject.name.Replace("(Clone)", "") : "";
    internal static bool Accessible(Container c)
    {
        if (!c || SafeInventory(c) == null || !Owners.ContainsKey(c.GetInventory()) || !Player.m_localPlayer) return false;
        var v = GetView(c);
        if (!v || !v.IsValid()) return false;
        if (!(bool)CheckAccess.Invoke(c, new object[] { Player.m_localPlayer.GetPlayerID() })) return false;
        return !c.m_checkGuardStone || PrivateArea.CheckAccess(c.transform.position, 0f, false, true);
    }
    internal static bool IsUsable(Container c, bool allowInUse)
    {
        if (!Plugin.Enabled.Value || !Accessible(c) || !GetView(c).IsOwner()) return false;
        if (allowInUse) return !c.IsInUse() || Plugin.OpenContainer == c;
        return !c.IsInUse() && GetView(c).GetZDO().GetInt(ZDOVars.s_inUse) == 0;
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
        try { s = string.IsNullOrEmpty(json) ? new ChestSettings() : JsonUtility.FromJson<ChestSettings>(json); }
        catch { s = new ChestSettings { AcceptStorage = false, CraftingSupply = false, FuelSupply = false, ProcessingSupply = false }; }
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
    internal static void Learn(Container c)
    {
        if (!c || !GetView(c) || !GetView(c).IsValid() || !GetView(c).IsOwner()) return;
        var s = GetSettings(c);
        if (s.Observe(c.GetInventory().GetAllItems().Select(InventoryTransfers.ItemId)))
        {
            string json = JsonUtility.ToJson(s);
            GetView(c).GetZDO().Set(SettingsKey, json); Cache[c] = (json, s);
        }
    }
    // Learned item types replace the old need to reserve a physical sample.
    internal static int ReserveFor(Container c, string name) => 0;
}
