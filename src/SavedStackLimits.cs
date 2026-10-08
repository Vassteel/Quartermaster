using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[HarmonyPatch]
internal static class SavedStackLimits
{
    // Both current and legacy inventory saves pass through this reconstruction
    // overload. Keep their quantities even when the newly configured cap is lower.
    // ChestStackOverflow can then split/eject them under normal ownership checks.
    internal static int SavedQuantity(int saved, int maximum) => saved;

    [HarmonyTranspiler, HarmonyPatch(typeof(Inventory), "AddItem", new[] {
        typeof(int), typeof(int), typeof(float), typeof(Vector2i), typeof(bool),
        typeof(int), typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>),
        typeof(int), typeof(bool), typeof(bool), typeof(bool) })]
    private static IEnumerable<CodeInstruction> PreserveSavedStack(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var min = AccessTools.Method(typeof(Mathf), nameof(Mathf.Min), new[] { typeof(int), typeof(int) });
        var maxStack = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_maxStackSize));
        int matches = 0;
        for (int i = 1; i < code.Count; i++)
        {
            if (!code[i].Calls(min) || !code[i - 1].LoadsField(maxStack)) continue;
            code[i].operand = AccessTools.Method(typeof(SavedStackLimits), nameof(SavedQuantity));
            matches++;
        }
        if (matches != 1) throw new InvalidOperationException("Saved-stack hook did not match the quantity clamp exactly once.");
        return code;
    }
}
