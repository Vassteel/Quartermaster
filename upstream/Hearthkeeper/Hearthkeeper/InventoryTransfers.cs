using System;
using System.Collections.Generic;

namespace Hearthkeeper;

internal static class InventoryTransfers
{
	internal static int CountType(Inventory inventory, string sharedName, int quality = -1, bool matchWorldLevel = false)
	{
		if (inventory == null || string.IsNullOrEmpty(sharedName))
		{
			return 0;
		}
		int num = 0;
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		for (int i = 0; i < allItems.Count; i++)
		{
			ItemDrop.ItemData itemData = allItems[i];
			if (itemData != null && itemData.m_shared != null && !(itemData.m_shared.m_name != sharedName) && (quality < 0 || itemData.m_quality == quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				num += itemData.m_stack;
			}
		}
		return num;
	}

	internal static int AvailableInContainer(Container container, string sharedName, int quality = -1, bool matchWorldLevel = false)
	{
		Inventory inventory = ContainerRegistry.SafeInventory(container);
		int num = CountType(inventory, sharedName, quality, matchWorldLevel);
		if (num <= 0)
		{
			return 0;
		}
		int num2 = CountType(inventory, sharedName);
		int num3 = ContainerRegistry.ReserveFor(container, sharedName);
		return Math.Max(0, Math.Min(num, num2 - num3));
	}

	internal static int CapacityFor(Inventory destination, ItemDrop.ItemData source, bool matchingOnly)
	{
		if (destination == null || source == null || source.m_shared == null)
		{
			return 0;
		}
		int num = 0;
		bool flag = false;
		List<ItemDrop.ItemData> allItems = destination.GetAllItems();
		for (int i = 0; i < allItems.Count; i++)
		{
			ItemDrop.ItemData itemData = allItems[i];
			if (itemData != null && itemData.IsSameType(source))
			{
				flag = true;
				num += Math.Max(0, itemData.m_shared.m_maxStackSize - itemData.m_stack);
			}
		}
		if (matchingOnly && !flag)
		{
			return 0;
		}
		int num2 = Math.Max(0, destination.GetWidth() * destination.GetHeight() - destination.NrOfItems());
		return num + num2 * Math.Max(1, source.m_shared.m_maxStackSize);
	}

	internal static int Move(Inventory source, Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
	{
		if (source == null || destination == null || item == null || !source.ContainsItem(item))
		{
			return 0;
		}
		int val = Math.Min(item.m_stack, Math.Max(0, requested));
		val = Math.Min(val, CapacityFor(destination, item, matchingOnly));
		if (val <= 0)
		{
			return 0;
		}
		ItemDrop.ItemData itemData = item.Clone();
		itemData.m_stack = val;
		if (!destination.AddItem(itemData))
		{
			return 0;
		}
		if (!source.RemoveItem(item, val))
		{
			HearthkeeperPlugin.Log.LogError("A guarded inventory move could not remove its source stack after adding the destination stack.");
			return 0;
		}
		return val;
	}

	internal static int AddCopy(Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
	{
		int num = Math.Min(Math.Max(0, requested), CapacityFor(destination, item, matchingOnly));
		if (num <= 0)
		{
			return 0;
		}
		ItemDrop.ItemData itemData = item.Clone();
		itemData.m_stack = num;
		if (!destination.AddItem(itemData))
		{
			return 0;
		}
		return num;
	}

	internal static int Remove(Inventory inventory, string sharedName, int amount, int quality, bool matchWorldLevel)
	{
		if (inventory == null || amount <= 0)
		{
			return 0;
		}
		int num = amount;
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(inventory.GetAllItems());
		list.Sort((ItemDrop.ItemData a, ItemDrop.ItemData b) => a.m_stack.CompareTo(b.m_stack));
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			if (num <= 0)
			{
				break;
			}
			ItemDrop.ItemData itemData = list[num2];
			if (!(itemData.m_shared.m_name != sharedName) && (quality < 0 || itemData.m_quality == quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				int num3 = Math.Min(num, itemData.m_stack);
				if (inventory.RemoveItem(itemData, num3))
				{
					num -= num3;
				}
			}
		}
		return amount - num;
	}

	internal static bool HasType(Inventory inventory, ItemDrop.ItemData item)
	{
		if (inventory == null || item == null)
		{
			return false;
		}
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		for (int i = 0; i < allItems.Count; i++)
		{
			if (allItems[i] != null && allItems[i].IsSameType(item))
			{
				return true;
			}
		}
		return false;
	}
}
