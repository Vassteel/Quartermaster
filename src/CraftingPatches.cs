using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Quartermaster;

[HarmonyPatch]
internal static class CraftingPatches
{
	private struct NamedRemovalState
	{
		internal int Requested;

		internal int Before;
	}

	internal static bool CraftingRemoval;

	[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
	[HarmonyPrefix]
	private static void CraftingPrefix()
	{
		CraftingRemoval = Plugin.Enabled.Value && Plugin.CraftFromContainers.Value;
	}

	[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
	[HarmonyFinalizer]
	private static Exception CraftingFinalizer(Exception __exception)
	{
		CraftingRemoval = false;
		return __exception;
	}

	[HarmonyPatch(typeof(Player), "ConsumeResources")]
	[HarmonyPrefix]
	private static bool ConsumeResourcesPrefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
	{
		if (!Plugin.Enabled.Value || !Plugin.CraftFromContainers.Value || __instance != Player.m_localPlayer)
		{
			return true;
		}
		CraftingStation currentCraftingStation = __instance.GetCurrentCraftingStation();
		foreach (Piece.Requirement requirement in requirements)
		{
			if ((!(currentCraftingStation != null) || currentCraftingStation.m_upgrader == requirement.m_upgraderResource) && (!(currentCraftingStation == null) || !requirement.m_upgraderResource) && !(requirement.m_resItem == null))
			{
				int num = requirement.GetAmount(qualityLevel) * multiplier;
				if (num > 0)
				{
					string name = requirement.m_resItem.m_itemData.m_shared.m_name;
					WarehouseService.RemovePlayerThenNearby(__instance, name, num, itemQuality, matchWorldLevel: true);
				}
			}
		}
		return false;
	}

	[HarmonyPatch(typeof(Inventory), "RemoveItem", new Type[]
	{
		typeof(string),
		typeof(int),
		typeof(int),
		typeof(bool)
	})]
	[HarmonyPrefix]
	private static void NamedRemovePrefix(Inventory __instance, string name, ref int amount, int itemQuality, bool worldLevelBased, out NamedRemovalState __state)
	{
		__state = new NamedRemovalState
		{
			Requested = amount,
			Before = 0
		};
		if (amount > 0)
		{
			if (CraftingRemoval && Player.m_localPlayer != null && __instance == Player.m_localPlayer.GetInventory())
			{
				__state.Before = InventoryTransfers.CountType(__instance, name, itemQuality, worldLevelBased);
			}

		}
	}

	[HarmonyPatch(typeof(Inventory), "RemoveItem", new Type[]
	{
		typeof(string),
		typeof(int),
		typeof(int),
		typeof(bool)
	})]
	[HarmonyPostfix]
	private static void NamedRemovePostfix(Inventory __instance, string name, int itemQuality, bool worldLevelBased, NamedRemovalState __state)
	{
		if (__state.Requested > 0 && CraftingRemoval && !(Player.m_localPlayer == null) && __instance == Player.m_localPlayer.GetInventory())
		{
			int num = InventoryTransfers.CountType(__instance, name, itemQuality, worldLevelBased);
			int num2 = Math.Max(0, __state.Before - num);
			int num3 = Math.Max(0, __state.Requested - num2);
			if (num3 > 0)
			{
				WarehouseService.RemoveNearby(Player.m_localPlayer, name, num3, itemQuality, worldLevelBased);
			}
		}
	}

	[HarmonyPatch(typeof(Player), "HaveRequirementItems")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> RecipeCountsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return ReplaceCountCalls(instructions);
	}

	[HarmonyPatch(typeof(Player), "HaveRequirements", new Type[]
	{
		typeof(Piece),
		typeof(Player.RequirementMode)
	})]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> BuildCountsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return ReplaceCountCalls(instructions);
	}

	[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> RequirementUiTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return ReplaceCountCalls(instructions);
	}

	private static IEnumerable<CodeInstruction> ReplaceCountCalls(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo original = AccessTools.Method(typeof(Inventory), "CountItems", new Type[3]
		{
			typeof(string),
			typeof(int),
			typeof(bool)
		});
		MethodInfo replacement = AccessTools.Method(typeof(CraftingPatches), "CountItemsIncludingWarehouse");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(original))
			{
				yield return new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(instruction);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	internal static int CountItemsIncludingWarehouse(Inventory inventory, string name, int quality, bool matchWorldLevel)
	{
		int num = inventory.CountItems(name, quality, matchWorldLevel);
		if (!Plugin.Enabled.Value || !Plugin.CraftFromContainers.Value || Player.m_localPlayer == null)
		{
			return num;
		}
		if (inventory != Player.m_localPlayer.GetInventory())
		{
			return num;
		}
		return num + WarehouseService.CountAvailableNearPlayer(name, quality, matchWorldLevel);
	}

}
