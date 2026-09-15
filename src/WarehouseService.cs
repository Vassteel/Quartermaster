using System;
using System.Collections.Generic;
using UnityEngine;
namespace Quartermaster;
internal static class WarehouseService
{
	internal static int StoreAllInOpenChest(Player player, Container container)
	{
		if (player == null || container == null || !ContainerRegistry.IsUsable(container, allowInUse: true))
		{
			return 0;
		}
		Inventory inventory = player.GetInventory();
		Inventory inventory2 = ContainerRegistry.SafeInventory(container);
		if (inventory == null || inventory2 == null || inventory == inventory2)
		{
			return 0;
		}
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(inventory.GetAllItems());
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			ItemDrop.ItemData itemData = list[i];
			if (itemData != null && !itemData.m_shared.m_questItem && !player.IsItemEquiped(itemData) && itemData.m_gridPos.y != 0 && !ExternalSlotCompatibility.IsProtectedPlayerSlot(itemData))
			{
				num += InventoryTransfers.Move(inventory, inventory2, itemData, itemData.m_stack, matchingOnly: false);
			}
		}
		if (num > 0)
		{
			ChestVisual.Pulse(container);
		}
		return num;
	}
	internal static int CountAvailableNearPlayer(string sharedName, int quality, bool matchWorldLevel)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || !Plugin.CraftFromContainers.Value)
		{
			return 0;
		}
		List<Container> list = ContainerRegistry.Nearby(localPlayer.transform.position, Plugin.CraftRange.Value, requireAccept: false, allowInUse: false);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (!ContainerRegistry.GetSettings(list[i]).CraftingSupply) continue;
			num += InventoryTransfers.AvailableInContainer(list[i], sharedName, quality, matchWorldLevel);
		}
		return num;
	}
	internal static int RemovePlayerThenNearby(Player player, string sharedName, int amount, int quality, bool matchWorldLevel)
	{
		if (player == null || amount <= 0)
		{
			return 0;
		}
		int num = InventoryTransfers.Remove(player.GetInventory(), sharedName, amount, quality, matchWorldLevel);
		int num2 = amount - num;
		if (num2 <= 0 || !Plugin.CraftFromContainers.Value)
		{
			return num;
		}
		return num + RemoveNearby(player, sharedName, num2, quality, matchWorldLevel);
	}
	internal static int RemoveNearby(Player player, string sharedName, int amount, int quality, bool matchWorldLevel)
	{
		if (player == null || amount <= 0 || !Plugin.CraftFromContainers.Value)
		{
			return 0;
		}
		int num = 0;
		int num2 = amount;
		List<Container> list = ContainerRegistry.Nearby(player.transform.position, Plugin.CraftRange.Value, requireAccept: false, allowInUse: false);
		for (int i = 0; i < list.Count; i++)
		{
			if (num2 <= 0)
			{
				break;
			}
			Container container = list[i];
			if (!ContainerRegistry.GetSettings(container).CraftingSupply) continue;
			int val = InventoryTransfers.AvailableInContainer(container, sharedName, quality, matchWorldLevel);
			int num3 = InventoryTransfers.Remove(amount: Math.Min(num2, val), inventory: ContainerRegistry.SafeInventory(container), sharedName: sharedName, quality: quality, matchWorldLevel: matchWorldLevel);
			num += num3;
			num2 -= num3;
		}
		return num;
	}
	internal static bool SortInventory(Inventory inventory, bool preserveFirstRow, bool preserveEquipped = false)
	{
		if (inventory == null)
		{
			return false;
		}
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		HashSet<int> hashSet = new HashSet<int>();
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		int width = inventory.GetWidth();
		int height = inventory.GetHeight();
		Player player = (preserveEquipped ? Player.m_localPlayer : null);
		bool flag = player != null && inventory == player.GetInventory();
		if (flag)
		{
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					if (ExternalSlotCompatibility.IsProtectedPlayerSlot(j, i))
					{
						hashSet.Add(i * width + j);
					}
				}
			}
		}
		for (int k = 0; k < allItems.Count; k++)
		{
			ItemDrop.ItemData itemData = allItems[k];
			if ((!preserveFirstRow || itemData.m_gridPos.y != 0) && (!flag || (!player.IsItemEquiped(itemData) && !ExternalSlotCompatibility.IsProtectedPlayerSlot(itemData))))
			{
				list.Add(itemData);
				continue;
			}
			int num = itemData.m_gridPos.y * width + itemData.m_gridPos.x;
			if (num >= 0 && num < width * height)
			{
				hashSet.Add(num);
			}
		}
		list.Sort(CompareItems);
		int l = (preserveFirstRow ? width : 0);
		bool flag2 = false;
		for (int m = 0; m < list.Count; m++)
		{
			for (; l < width * height && hashSet.Contains(l); l++)
			{
			}
			if (l >= width * height)
			{
				break;
			}
			Vector2i vector2i = new Vector2i(l % width, l / width);
			if (list[m].m_gridPos != vector2i)
			{
				list[m].m_gridPos = vector2i;
				flag2 = true;
			}
			l++;
		}
		if (flag2 && inventory.m_onChanged != null)
		{
			inventory.m_onChanged();
		}
		return flag2;
	}
	private static int CompareItems(ItemDrop.ItemData a, ItemDrop.ItemData b)
	{
		int num = a.m_shared.m_itemType.CompareTo(b.m_shared.m_itemType);
		if (num != 0)
		{
			return num;
		}
		int num2 = string.Compare(a.m_shared.m_name, b.m_shared.m_name, StringComparison.Ordinal);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = b.m_quality.CompareTo(a.m_quality);
		if (num3 != 0)
		{
			return num3;
		}
		int num4 = b.m_stack.CompareTo(a.m_stack);
		if (num4 != 0)
		{
			return num4;
		}
		int num5 = a.m_gridPos.y.CompareTo(b.m_gridPos.y);
		if (num5 == 0)
		{
			return a.m_gridPos.x.CompareTo(b.m_gridPos.x);
		}
		return num5;
	}
}
