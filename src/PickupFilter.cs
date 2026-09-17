using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Character data follows the character across worlds and normal saves.
[HarmonyPatch]
internal static class PickupFilter
{
    internal const string SaveKey = "Quartermaster.IgnoredPickupTypes.v1";
    private static Player owner;
    private static string saved;
    private static readonly HashSet<string> ignored = new HashSet<string>(StringComparer.Ordinal);
    [ThreadStatic] private static Player automaticPlayer;

    internal static string ItemId(ItemDrop.ItemData item) => item?.m_dropPrefab ? item.m_dropPrefab.name : null;
    internal static int Count(Player player) { Load(player); return ignored.Count; }
    internal static string[] Types(Player player) { Load(player); return ignored.OrderBy(x => x, StringComparer.Ordinal).ToArray(); }
    internal static bool IsIgnored(Player player, string id) { Load(player); return id != null && ignored.Contains(id); }
    private static void Load(Player player)
    {
        string value = null;
        if (player) player.m_customData.TryGetValue(SaveKey, out value);
        if (ReferenceEquals(owner, player) && saved == value) return;
        owner = player; saved = value; ignored.Clear();
        if (value != null) foreach (string id in value.Split('\n')) if (!string.IsNullOrWhiteSpace(id)) ignored.Add(id);
    }
    internal static bool SetIgnored(Player player, string id, bool value)
    {
        if (!player || string.IsNullOrWhiteSpace(id) || id.Contains("\n")) return false;
        Load(player);
        bool changed = value ? ignored.Add(id) : ignored.Remove(id);
        if (!changed) return false;
        saved = string.Join("\n", ignored.OrderBy(x => x, StringComparer.Ordinal));
        if (ignored.Count == 0) { player.m_customData.Remove(SaveKey); saved = null; }
        else player.m_customData[SaveKey] = saved;
        return true;
    }
    internal static void Clear() { owner = null; saved = null; ignored.Clear(); automaticPlayer = null; }

    // This replaces only the vanilla eligibility read, before ownership requests or attraction.
    // Never write ItemDrop.m_autoPickup: it is shared with other players and mods.
    internal static bool AllowAutomatic(ItemDrop item, Player player) => item.m_autoPickup &&
        (!Plugin.Enabled.Value || player != Player.m_localPlayer || !IsIgnored(player, ItemId(item.m_itemData)));

    [HarmonyPatch(typeof(Player), "AutoPickup"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> FilterAutomatic(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var field = AccessTools.Field(typeof(ItemDrop), "m_autoPickup");
        int matches = code.Count(i => i.LoadsField(field));
        if (matches != 1) throw new InvalidOperationException("Quartermaster pickup filter expected one auto-pickup eligibility check, found " + matches);
        foreach (var instruction in code)
        {
            if (!instruction.LoadsField(field)) { yield return instruction; continue; }
            // Retain branch labels and exception metadata on the first replacement instruction.
            instruction.opcode = OpCodes.Ldarg_0; instruction.operand = null;
            yield return instruction;
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PickupFilter), nameof(AllowAutomatic)));
        }
    }
    [HarmonyPatch(typeof(Player), "AutoPickup"), HarmonyPrefix]
    private static void BeginAutomatic(Player __instance, out Player __state) { __state = automaticPlayer; automaticPlayer = __instance; }
    [HarmonyPatch(typeof(Player), "AutoPickup"), HarmonyFinalizer]
    private static void EndAutomatic(Player __state) { automaticPlayer = __state; }

    internal sealed class PickupAttempt
    {
        internal string Id;
        internal ItemDrop.ItemData Item;
        internal int Stack;
    }
    [HarmonyPatch(typeof(Humanoid), "Pickup"), HarmonyPrefix]
    private static void BeforePickup(Humanoid __instance, GameObject go, out PickupAttempt __state)
    {
        // Capture before vanilla destroys a collected drop. Failed attempts keep the filter.
        __state = null;
        if (!Plugin.Enabled.Value || __instance != Player.m_localPlayer || automaticPlayer == Player.m_localPlayer || !go) return;
        var item = go.GetComponent<ItemDrop>()?.m_itemData;
        string id = ItemId(item);
        if (id != null && IsIgnored(Player.m_localPlayer, id)) __state = new PickupAttempt { Id = id, Item = item, Stack = item.m_stack };
    }
    [HarmonyPatch(typeof(Humanoid), "Pickup"), HarmonyPostfix]
    private static void AfterPickup(Humanoid __instance, bool __result, PickupAttempt __state)
    {
        // Vanilla can move part of a stack and return false when the remaining slots are full.
        if (__state != null && (__result || __state.Item.m_stack < __state.Stack) && SetIgnored(__instance as Player, __state.Id, false))
            __instance.Message(MessageHud.MessageType.TopLeft, "Auto pickup enabled: " + Label(__state.Id));
    }
    internal static string Label(string id)
    {
        var prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(id) : null;
        var item = prefab ? prefab.GetComponent<ItemDrop>() : null;
        return item ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : id;
    }
}
