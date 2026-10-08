using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace Quartermaster;

[HarmonyPatch]
internal static class CraftingPatches
{
    [ThreadStatic] internal static bool LocalCountsOnly;
    private static bool StorageEnabled => Plugin.Enabled.Value && Plugin.CraftFromContainers.Value;

    // Warehouse counts are advisory for the UI and fetch planning only. Crafting
    // and building first fetch materials, then vanilla alone pays from the player.
    // Never create output and attempt to cover an unpaid shortfall afterwards.

    [HarmonyPatch(typeof(Player), "HaveRequirementItems"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RecipeCounts(IEnumerable<CodeInstruction> instructions) => RouteCounts(instructions);

    [HarmonyPatch(typeof(Player), "HaveRequirements", new[] { typeof(Piece), typeof(Player.RequirementMode) }), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> BuildingCounts(IEnumerable<CodeInstruction> instructions) => RouteCounts(instructions);

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DisplayCounts(IEnumerable<CodeInstruction> instructions) => RouteCounts(instructions);

    [HarmonyPatch(typeof(Player), "GetFirstRequiredItem"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> IngredientSelection(IEnumerable<CodeInstruction> instructions)
    {
        var native=AccessTools.Method(typeof(Inventory),"GetItem",new[]{typeof(string),typeof(int),typeof(bool)});
        var combined=AccessTools.Method(typeof(CraftingPatches),nameof(FindIngredientIncludingWarehouse));
        foreach(var instruction in RouteCounts(instructions))
        {
            if(instruction.Calls(native)){instruction.opcode=OpCodes.Call;instruction.operand=combined;}
            yield return instruction;
        }
    }
    internal static ItemDrop.ItemData FindIngredientIncludingWarehouse(Inventory inventory,string name,int quality,bool equipped)
    {
        var carried=inventory.GetItem(name,quality,equipped);
        var player=Player.m_localPlayer;
        if(carried!=null||LocalCountsOnly||!StorageEnabled||!player||!ReferenceEquals(inventory,player.GetInventory()))return carried;
        foreach(var chest in WarehouseService.CraftStores(player,true))
        {
            if(InventoryTransfers.AvailableInContainer(chest,name,quality,true)<1)continue;
            var item=chest.GetInventory().GetAllItems().FirstOrDefault(i=>i.m_shared.m_name==name&&i.m_quality==quality&&i.m_worldLevel>=Game.m_worldLevel&&i.m_stack>0);
            if(item!=null)return item;
        }
        return null;
    }

    private static IEnumerable<CodeInstruction> RouteCounts(IEnumerable<CodeInstruction> instructions)
    {
        var native = AccessTools.Method(typeof(Inventory), "CountItems", new[] { typeof(string), typeof(int), typeof(bool) });
        var combined = AccessTools.Method(typeof(CraftingPatches), nameof(CountItemsIncludingWarehouse));
        // Work on copies, retaining both branch targets and exception boundaries.
        return instructions.Select(original =>
        {
            var copy = new CodeInstruction(original);
            if (copy.Calls(native)) { copy.opcode = OpCodes.Call; copy.operand = combined; }
            return copy;
        });
    }

    internal static int CountItemsIncludingWarehouse(Inventory inventory, string name, int quality, bool matchWorldLevel)
    {
        int carried = inventory.CountItems(name, quality, matchWorldLevel);
        var player = Player.m_localPlayer;
        if (LocalCountsOnly || !StorageEnabled || !player || !ReferenceEquals(inventory, player.GetInventory())) return carried;
        long combined = (long)carried + WarehouseService.CountAvailableNearPlayer(name, quality, matchWorldLevel);
        return (int)Math.Min(int.MaxValue, combined);
    }
}
