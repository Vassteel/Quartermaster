using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[HarmonyPatch]
internal static class MachineShortcut
{
    // Vanilla KeyHints.Update also uses F9, to cycle and SAVE the controller
    // layout. Reserve that key while it is the configured Quartermaster shortcut.
    // Filter its result, leaving the rest of the native hint update untouched.
    internal static bool AllowLayoutCycle(bool pressed) => pressed &&
        !(Plugin.Enabled.Value && Plugin.MachineKey.Value.MainKey == KeyCode.F9 && Plugin.MachineKey.Value.IsDown());

    [HarmonyTranspiler, HarmonyPatch(typeof(KeyHints), "Update")]
    private static IEnumerable<CodeInstruction> KeepControllerLayout(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var keyDown = AccessTools.Method(typeof(ZInput), nameof(ZInput.GetKeyDown), new[] { typeof(KeyCode), typeof(bool) });
        int matches = 0;
        for (int i = 2; i < code.Count; i++)
        {
            if (!code[i].Calls(keyDown) || !code[i - 1].LoadsConstant(1) || !code[i - 2].LoadsConstant((int)KeyCode.F9)) continue;
            code.Insert(++i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MachineShortcut), nameof(AllowLayoutCycle))));
            matches++;
        }
        if (matches != 1) throw new InvalidOperationException("Controller-layout shortcut hook did not match F9 exactly once.");
        return code;
    }
}
