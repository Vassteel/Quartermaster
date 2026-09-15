using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[HarmonyPatch]
internal static class StationCoverage
{
    [HarmonyPatch(typeof(CraftingStation), "HaveBuildStationInRange"), HarmonyPrefix]
    internal static void CaptureBuildPoint(Vector3 point, out Vector3 __state)
    {
        // Vanilla flattens its point argument to each station's height. Preserve
        // the original position for the storage area's three-dimensional radius.
        __state = point;
    }

    [HarmonyPatch(typeof(CraftingStation), "HaveBuildStationInRange"), HarmonyPostfix]
    internal static void ExtendBuildStation(string name, Vector3 __state,
        List<CraftingStation> ___m_allStations, ref CraftingStation __result)
    {
        if (__result) return;
        try { __result = Find(name, __state, ___m_allStations); }
        catch (Exception e) { Plugin.Log.LogWarning("Station coverage check failed: " + e.GetBaseException().Message); }
    }

    internal static CraftingStation Find(string name, Vector3 point, IEnumerable<CraftingStation> stations)
    {
        if (!Plugin.Enabled.Value || !Plugin.ExtendStationCoverage.Value || !Player.m_localPlayer) return null;
        float radiusSquared = Plugin.Range.Value * Plugin.Range.Value;
        foreach (var hub in ContainerRegistry.All)
        {
            if (!hub || (hub.transform.position - point).sqrMagnitude > radiusSquared
                || !ContainerRegistry.GetSettings(hub).Deposit || !ContainerRegistry.Accessible(hub)) continue;
            foreach (var station in stations)
            {
                if (!station || !station.isActiveAndEnabled || station.m_name != name
                    || (station.transform.position - hub.transform.position).sqrMagnitude > radiusSquared
                    || !station.GetComponent<Piece>()) continue;
                var view = station.GetComponent<ZNetView>();
                if (view && view.IsValid() && PrivateArea.CheckAccess(station.transform.position, 0f, false, true))
                    return station;
            }
        }
        return null;
    }
}
