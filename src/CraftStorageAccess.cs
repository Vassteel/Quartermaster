using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Craft-from-container access is independent of automation's owner-only writes.
// Uses native Load/Save without taking ownership or reserving/opening a chest.
internal static class CraftStorageAccess
{
    private static readonly MethodInfo SaveInventory = AccessTools.Method(typeof(Container), "Save");

    internal static bool CanSupply(Container chest)
    {
        if (!Plugin.Enabled.Value || !ContainerRegistry.CanObserve(chest)) return false;
        var view = ContainerRegistry.GetView(chest);
        if (chest.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) != 0 || SharedChests.Viewed(chest)) return false;
        ContainerRegistry.Refresh(chest);
        return true;
    }

    internal static IEnumerable<Container> Nearby(Player player)
    {
        if (!player) return Enumerable.Empty<Container>();
        float range = Plugin.CraftRange.Value;
        return ContainerRegistry.All.Where(c => c &&
                (c.transform.position-player.transform.position).sqrMagnitude <= range*range &&
                ContainerRegistry.GetSettings(c).CraftingSupply && CanSupply(c))
            .OrderBy(c => (c.transform.position-player.transform.position).sqrMagnitude)
            .ThenBy(c => ContainerRegistry.GetView(c).GetZDO().m_uid.ToString(), StringComparer.Ordinal).ToArray();
    }

    internal static void Save(Container chest) => SaveInventory.Invoke(chest, null);

    internal static bool Run(string name, Container[] sources, Func<bool> valid, Action work,
        Action<string> result, Action afterPull = null)
    {
        if (!Plugin.Enabled.Value || !Player.m_localPlayer || Player.m_localPlayer.IsDead() ||
            Player.m_localPlayer.IsTeleporting()) return false;
        try
        {
            if (sources.Length == 0 || !sources.All(CanSupply) || !valid())
            { result?.Invoke("Materials changed or a chest is open; try again"); return false; }
            // Owners of foreign sources yield automation briefly and refuse handoffs while this
            // withdrawal is saved, shrinking the window for a conflicting same-revision write.
            ChestOwnership.Touch(sources);
            work();
            afterPull?.Invoke();
            return true;
        }
        catch (Exception error)
        {
            Plugin.Log.LogError(name+" failed: "+error);
            result?.Invoke("Material transfer failed; check the log");
            return false;
        }
    }
}
