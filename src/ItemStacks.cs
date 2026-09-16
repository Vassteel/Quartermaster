using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Quartermaster;

// Set a limit, never multiply it or rewrite saved inventory quantities.
[HarmonyPatch]
internal static class ItemStacks
{
    private static readonly Dictionary<ItemDrop.ItemData.SharedData, int> originals = new();
    private static bool enabled;
    private static int limit;

    internal static void Configure(bool active, int maximum)
    {
        enabled = active;
        limit = Math.Max(2, Math.Min(100000, maximum));
    }

    internal static void Apply(ItemDrop.ItemData.SharedData data)
    {
        if (!enabled || data == null || data.m_maxStackSize <= 1) return;
        if (!originals.ContainsKey(data)) originals.Add(data, data.m_maxStackSize);
        data.m_maxStackSize = limit;
    }

    internal static void Restore()
    {
        foreach (var entry in originals)
            if (entry.Key.m_maxStackSize == limit) entry.Key.m_maxStackSize = entry.Value;
        originals.Clear();
        enabled = false;
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake"), HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyAfter("toxo.stackincrease")]
    private static void AfterAwake(ObjectDB __instance) => ApplyDatabase(__instance);

    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB"), HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyAfter("toxo.stackincrease")]
    private static void AfterCopy(ObjectDB __instance) => ApplyDatabase(__instance);

    [HarmonyPatch(typeof(ObjectDB), "UpdateRegisters"), HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyAfter("toxo.stackincrease")]
    private static void AfterRegistration(ObjectDB __instance) => ApplyDatabase(__instance);

    internal static void ApplyDatabase(ObjectDB database)
    {
        if (!enabled || database.m_items == null) return;
        foreach (var prefab in database.m_items)
            if (prefab) Apply(prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared);
    }
}
