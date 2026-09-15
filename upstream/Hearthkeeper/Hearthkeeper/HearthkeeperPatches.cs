using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Hearthkeeper;

[HarmonyPatch]
internal static class HearthkeeperPatches
{
	private struct NamedRemovalState
	{
		internal int Requested;

		internal int Before;
	}

	internal static bool CraftingRemoval;

	[HarmonyPatch(typeof(Container), "Awake")]
	[HarmonyPrefix]
	private static void ContainerAwakePrefix(Container __instance)
	{
		ContainerSizeService.ApplyBeforeAwake(__instance);
	}

	[HarmonyPatch(typeof(Container), "Awake")]
	[HarmonyPostfix]
	private static void ContainerAwakePostfix(Container __instance)
	{
		ContainerRegistry.Register(__instance);
		ContainerSizeService.RememberAppliedSize(__instance);
	}

	[HarmonyPatch(typeof(ItemDrop), "Awake")]
	[HarmonyPostfix]
	private static void ItemAwakePostfix(ItemDrop __instance)
	{
		WarehouseService.RegisterGroundItem(__instance);
	}

	[HarmonyPatch(typeof(Fireplace), "Awake")]
	[HarmonyPostfix]
	private static void FireplaceAwakePostfix(Fireplace __instance)
	{
		AutoRefuelService.Register(__instance);
	}

	[HarmonyPatch(typeof(Smelter), "Awake")]
	[HarmonyPostfix]
	private static void SmelterAwakePostfix(Smelter __instance)
	{
		AutoRefuelService.Register(__instance);
	}

	[HarmonyPatch(typeof(ItemDrop), "OnDestroy")]
	[HarmonyPostfix]
	private static void ItemDestroyPostfix(ItemDrop __instance)
	{
		WarehouseService.UnregisterGroundItem(__instance);
	}

	[HarmonyPatch(typeof(MonsterAI), "Awake")]
	[HarmonyPostfix]
	private static void MonsterAwakePostfix(MonsterAI __instance)
	{
		WarehouseService.RegisterCreature(__instance);
	}

	[HarmonyPatch(typeof(ObjectDB), "Awake")]
	[HarmonyPostfix]
	private static void ObjectDbAwakePostfix()
	{
		StackSizeService.Apply();
	}

	[HarmonyPatch(typeof(InventoryGui), "Show")]
	[HarmonyPostfix]
	private static void InventoryShowPostfix(Container container)
	{
		HearthkeeperPlugin.OpenContainer = container;
	}

	[HarmonyPatch(typeof(InventoryGui), "Awake")]
	[HarmonyPostfix]
	private static void InventoryAwakePostfix(InventoryGui __instance)
	{
		HearthkeeperPlugin.AttachInterface(__instance);
	}

	[HarmonyPatch(typeof(InventoryGui), "Hide")]
	[HarmonyPostfix]
	private static void InventoryHidePostfix()
	{
		HearthkeeperPlugin.OpenContainer = null;
	}

	[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
	[HarmonyPrefix]
	private static void CraftingPrefix()
	{
		CraftingRemoval = true;
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
		if (!HearthkeeperPlugin.Enabled.Value || !HearthkeeperPlugin.CraftFromContainers.Value || __instance != Player.m_localPlayer)
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
			ClampProtectedRemoval(__instance, name, itemQuality, worldLevelBased, ref amount);
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

	[HarmonyPatch(typeof(Inventory), "RemoveItem", new Type[]
	{
		typeof(ItemDrop.ItemData),
		typeof(int)
	})]
	[HarmonyPrefix]
	private static bool StackRemovePrefix(Inventory __instance, ItemDrop.ItemData item, ref int amount, ref bool __result)
	{
		Container container = ContainerRegistry.OwnerOf(__instance);
		if (container == null || !ContainerRegistry.GetSettings(container).ManualLock || item == null)
		{
			return true;
		}
		int val = InventoryTransfers.AvailableInContainer(container, item.m_shared.m_name, item.m_quality);
		amount = Math.Min(amount, val);
		if (amount > 0)
		{
			return true;
		}
		__result = false;
		return false;
	}

	[HarmonyPatch(typeof(Inventory), "RemoveOneItem")]
	[HarmonyPrefix]
	private static bool RemoveOnePrefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
	{
		Container container = ContainerRegistry.OwnerOf(__instance);
		if (container == null || !ContainerRegistry.GetSettings(container).ManualLock || item == null)
		{
			return true;
		}
		if (InventoryTransfers.AvailableInContainer(container, item.m_shared.m_name, item.m_quality) > 0)
		{
			return true;
		}
		__result = false;
		return false;
	}

	[HarmonyPatch(typeof(Inventory), "RemoveItem", new Type[] { typeof(int) })]
	[HarmonyPrefix]
	private static bool IndexRemovePrefix(Inventory __instance, int index, ref bool __result)
	{
		Container container = ContainerRegistry.OwnerOf(__instance);
		if (container == null || !ContainerRegistry.GetSettings(container).ManualLock)
		{
			return true;
		}
		ItemDrop.ItemData item = __instance.GetItem(index);
		if (item != null && InventoryTransfers.AvailableInContainer(container, item.m_shared.m_name, item.m_quality) >= item.m_stack)
		{
			return true;
		}
		__result = false;
		return false;
	}

	[HarmonyPatch(typeof(Inventory), "MoveItemToThis", new Type[]
	{
		typeof(Inventory),
		typeof(ItemDrop.ItemData)
	})]
	[HarmonyPrefix]
	private static bool MoveWholePrefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
	{
		Container container = ContainerRegistry.OwnerOf(fromInventory);
		if (container == null || !ContainerRegistry.GetSettings(container).ManualLock || item == null)
		{
			return true;
		}
		int val = InventoryTransfers.AvailableInContainer(container, item.m_shared.m_name, item.m_quality);
		InventoryTransfers.Move(fromInventory, __instance, item, Math.Min(item.m_stack, val), matchingOnly: false);
		return false;
	}

	[HarmonyPatch(typeof(Inventory), "MoveItemToThis", new Type[]
	{
		typeof(Inventory),
		typeof(ItemDrop.ItemData),
		typeof(int),
		typeof(int),
		typeof(int)
	})]
	[HarmonyPrefix]
	private static void MoveAmountPrefix(Inventory fromInventory, ItemDrop.ItemData item, ref int amount)
	{
		Container container = ContainerRegistry.OwnerOf(fromInventory);
		if (!(container == null) && ContainerRegistry.GetSettings(container).ManualLock && item != null)
		{
			amount = Math.Min(amount, InventoryTransfers.AvailableInContainer(container, item.m_shared.m_name, item.m_quality));
		}
	}

	[HarmonyPatch(typeof(Inventory), "MoveAll")]
	[HarmonyPrefix]
	private static bool MoveAllPrefix(Inventory __instance, Inventory fromInventory)
	{
		Container container = ContainerRegistry.OwnerOf(fromInventory);
		if (container == null || !ContainerRegistry.GetSettings(container).ManualLock)
		{
			return true;
		}
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		for (int i = 0; i < list.Count; i++)
		{
			ItemDrop.ItemData itemData = list[i];
			int val = InventoryTransfers.AvailableInContainer(container, itemData.m_shared.m_name, itemData.m_quality);
			InventoryTransfers.Move(fromInventory, __instance, itemData, Math.Min(itemData.m_stack, val), matchingOnly: false);
		}
		return false;
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
		MethodInfo replacement = AccessTools.Method(typeof(HearthkeeperPatches), "CountItemsIncludingWarehouse");
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
		if (!HearthkeeperPlugin.Enabled.Value || !HearthkeeperPlugin.CraftFromContainers.Value || Player.m_localPlayer == null)
		{
			return num;
		}
		if (inventory != Player.m_localPlayer.GetInventory())
		{
			return num;
		}
		return num + WarehouseService.CountAvailableNearPlayer(name, quality, matchWorldLevel);
	}

	private static void ClampProtectedRemoval(Inventory inventory, string name, int quality, bool matchWorldLevel, ref int amount)
	{
		Container container = ContainerRegistry.OwnerOf(inventory);
		if (!(container == null) && ContainerRegistry.GetSettings(container).ManualLock)
		{
			amount = Math.Min(amount, InventoryTransfers.AvailableInContainer(container, name, quality, matchWorldLevel));
		}
	}
}
