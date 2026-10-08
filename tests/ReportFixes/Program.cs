using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Quartermaster;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool yes, string label) { checks++; if (!yes) throw new Exception(label); }
    static List<CodeInstruction> Patch(Type type, string name, List<CodeInstruction> code) =>
        ((IEnumerable<CodeInstruction>)AccessTools.Method(type, name).Invoke(null, new object[] { code })).ToList();
    static void Main()
    {
        foreach (int layout in new[] { 0, 1, 2 })
        {
            Plugin.Enabled.Value = true; Plugin.MachineKey.Value.MainKey = KeyCode.F9; Plugin.MachineKey.Value.Down = true;
            int current = layout;
            if (MachineShortcut.AllowLayoutCycle(true)) current = (current + 1) % 3;
            Check(current == layout, "Machine F9 preserves every controller layout");
        }
        Check(!MachineShortcut.AllowLayoutCycle(false), "No synthetic press");
        Plugin.Enabled.Value = false; Check(MachineShortcut.AllowLayoutCycle(true), "Disabled mod leaves vanilla shortcut available");
        Plugin.Enabled.Value = true; Plugin.MachineKey.Value.MainKey = KeyCode.F8;
        Check(MachineShortcut.AllowLayoutCycle(true), "Rebound machine key releases F9");
        Plugin.MachineKey.Value.MainKey = KeyCode.F9; Plugin.MachineKey.Value.Down = false;
        Check(MachineShortcut.AllowLayoutCycle(true), "Unmatched keyboard modifiers do not reserve key");
        var keyCode = new List<CodeInstruction> { new(OpCodes.Ldc_I4, (int)KeyCode.F9), new(OpCodes.Ldc_I4_1), new(OpCodes.Call, AccessTools.Method(typeof(ZInput), nameof(ZInput.GetKeyDown))), new(OpCodes.Ret) };
        var keys = Patch(typeof(MachineShortcut), "KeepControllerLayout", keyCode);
        Check(keys.Count == 5 && keys[3].Calls(AccessTools.Method(typeof(MachineShortcut), "AllowLayoutCycle")), "Filter follows native F9 read without skipping hint updates");
        var field = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_maxStackSize");
        var savedCode = new List<CodeInstruction> { new(OpCodes.Ldarg_0), new(OpCodes.Ldarg_1), new(OpCodes.Ldfld, field), new(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Min")), new(OpCodes.Ret) };
        var patched = Patch(typeof(SavedStackLimits), "PreserveSavedStack", savedCode);
        var method = new DynamicMethod("RestoredQuantity", typeof(int), new[] { typeof(int), typeof(ItemDrop.ItemData.SharedData) }, typeof(Program).Module, true);
        var il = method.GetILGenerator();
        foreach (var instruction in patched)
            if (instruction.operand is MethodInfo call) il.Emit(instruction.opcode, call);
            else if (instruction.operand is FieldInfo f) il.Emit(instruction.opcode, f);
            else il.Emit(instruction.opcode);
        var restore = (Func<int, ItemDrop.ItemData.SharedData, int>)method.CreateDelegate(typeof(Func<int, ItemDrop.ItemData.SharedData, int>));
        foreach (int maximum in new[] { 1, 20, 50, 100, 1000 })
        foreach (int saved in new[] { 1, 19, 50, 1000, 100000 })
        {
            var data = new ItemDrop.ItemData.SharedData { m_maxStackSize = maximum };
            Check(restore(saved, data) == saved && data.m_maxStackSize == maximum, "Reload retains quantity without increasing item limit");
        }
        foreach (var target in new[] { (typeof(SavedStackLimits), "PreserveSavedStack"), (typeof(MachineShortcut), "KeepControllerLayout") })
        {
            bool rejected = false;
            try { Patch(target.Item1, target.Item2, new() { new(OpCodes.Ret) }); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
            Check(rejected, "Changed game contract fails explicitly");
        }
        Console.WriteLine($"PASS: {checks} saved quantity and controller-layout regression checks.");
    }
}
public class Inventory { }
public class KeyHints { }
public class ItemDrop { public class ItemData { public class SharedData { public int m_maxStackSize; } } }
public static class ZInput { public static bool GetKeyDown(KeyCode code, bool logWarning = true) => true; }
namespace UnityEngine
{
    public enum KeyCode { F8 = 289, F9 = 290 }
    public struct Vector2i { }
    public static class Mathf { public static int Min(int x, int y) => Math.Min(x, y); }
}
namespace Quartermaster
{
    static class Plugin { internal static Setting<bool> Enabled = new() { Value = true }; internal static Setting<Shortcut> MachineKey = new() { Value = new() }; }
    class Setting<T> { internal T Value; }
    class Shortcut { internal KeyCode MainKey = KeyCode.F9; internal bool Down = true; internal bool IsDown() => Down; }
}
