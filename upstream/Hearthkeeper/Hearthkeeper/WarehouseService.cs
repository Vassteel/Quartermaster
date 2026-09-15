using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthkeeper;

internal static class WarehouseService
{
	private static readonly List<ItemDrop> GroundItems = new List<ItemDrop>();

	private static readonly List<MonsterAI> Creatures = new List<MonsterAI>();

	private static readonly Dictionary<MonsterAI, ItemDrop> PendingFeed = new Dictionary<MonsterAI, ItemDrop>();

	internal static void RegisterGroundItem(ItemDrop item)
	{
		if (item != null && !GroundItems.Contains(item))
		{
			GroundItems.Add(item);
		}
	}

	internal static void UnregisterGroundItem(ItemDrop item)
	{
		GroundItems.Remove(item);
	}

	internal static void RegisterCreature(MonsterAI creature)
	{
		if (creature != null && !Creatures.Contains(creature))
		{
			Creatures.Add(creature);
		}
	}

	internal static void UnregisterCreature(MonsterAI creature)
	{
		Creatures.Remove(creature);
		PendingFeed.Remove(creature);
	}

	internal static int StoreMatchingFromPlayer(Player player)
	{
		if (player == null)
		{
			return 0;
		}
		Inventory inventory = player.GetInventory();
		List<Container> list = ContainerRegistry.Nearby(player.transform.position, HearthkeeperPlugin.InventoryStoreRange.Value, requireAccept: true, allowInUse: false);
		Container openMatchingDestination = GetOpenMatchingDestination(player);
		Inventory inventory2 = ContainerRegistry.SafeInventory(openMatchingDestination);
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>(inventory.GetAllItems());
		int num = 0;
		for (int i = 0; i < list2.Count; i++)
		{
			ItemDrop.ItemData itemData = list2[i];
			if (itemData == null || player.IsItemEquiped(itemData) || ExternalSlotCompatibility.IsProtectedPlayerSlot(itemData) || (HearthkeeperPlugin.ProtectHotbar.Value && itemData.m_gridPos.y == 0))
			{
				continue;
			}
			int num2 = itemData.m_stack;
			if (inventory2 != null && InventoryTransfers.HasType(inventory2, itemData))
			{
				int num3 = InventoryTransfers.Move(inventory, inventory2, itemData, num2, matchingOnly: true);
				num += num3;
				num2 -= num3;
				if (num3 > 0)
				{
					ChestGlowService.Pulse(openMatchingDestination);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (num2 <= 0)
				{
					break;
				}
				if (list[j] == openMatchingDestination)
				{
					continue;
				}
				Inventory inventory3 = ContainerRegistry.SafeInventory(list[j]);
				if (InventoryTransfers.HasType(inventory3, itemData))
				{
					int num4 = InventoryTransfers.Move(inventory, inventory3, itemData, num2, matchingOnly: true);
					num += num4;
					num2 -= num4;
					if (num4 > 0)
					{
						ChestGlowService.Pulse(list[j]);
					}
				}
			}
		}
		return num;
	}

	private static Container GetOpenMatchingDestination(Player player)
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (player == null || openContainer == null)
		{
			return null;
		}
		if (!ContainerRegistry.IsUsable(openContainer, allowInUse: true))
		{
			return null;
		}
		if (!ContainerRegistry.GetSettings(openContainer).AcceptStorage)
		{
			return null;
		}
		float value = HearthkeeperPlugin.InventoryStoreRange.Value;
		if (!((openContainer.transform.position - player.transform.position).sqrMagnitude <= value * value))
		{
			return null;
		}
		return openContainer;
	}

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
			if (itemData != null && !player.IsItemEquiped(itemData) && itemData.m_gridPos.y != 0 && !ExternalSlotCompatibility.IsProtectedPlayerSlot(itemData))
			{
				num += InventoryTransfers.Move(inventory, inventory2, itemData, itemData.m_stack, matchingOnly: false);
			}
		}
		if (num > 0)
		{
			ChestGlowService.Pulse(container);
		}
		return num;
	}

	internal static int CountAvailableNearPlayer(string sharedName, int quality, bool matchWorldLevel)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || !HearthkeeperPlugin.CraftFromContainers.Value)
		{
			return 0;
		}
		List<Container> list = ContainerRegistry.Nearby(localPlayer.transform.position, HearthkeeperPlugin.CraftRange.Value, requireAccept: false, allowInUse: false);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
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
		if (num2 <= 0 || !HearthkeeperPlugin.CraftFromContainers.Value)
		{
			return num;
		}
		return num + RemoveNearby(player, sharedName, num2, quality, matchWorldLevel);
	}

	internal static int RemoveNearby(Player player, string sharedName, int amount, int quality, bool matchWorldLevel)
	{
		if (player == null || amount <= 0 || !HearthkeeperPlugin.CraftFromContainers.Value)
		{
			return 0;
		}
		int num = 0;
		int num2 = amount;
		List<Container> list = ContainerRegistry.Nearby(player.transform.position, HearthkeeperPlugin.CraftRange.Value, requireAccept: false, allowInUse: false);
		for (int i = 0; i < list.Count; i++)
		{
			if (num2 <= 0)
			{
				break;
			}
			Container container = list[i];
			int val = InventoryTransfers.AvailableInContainer(container, sharedName, quality, matchWorldLevel);
			int num3 = InventoryTransfers.Remove(amount: Math.Min(num2, val), inventory: ContainerRegistry.SafeInventory(container), sharedName: sharedName, quality: quality, matchWorldLevel: matchWorldLevel);
			num += num3;
			num2 -= num3;
		}
		return num;
	}

	internal static int SortWarehouse(Player player)
	{
		if (player == null)
		{
			return 0;
		}
		List<Container> list = ContainerRegistry.Nearby(player.transform.position, HearthkeeperPlugin.WarehouseSortRange.Value, requireAccept: false, allowInUse: false);
		int num = 0;
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			Container container = list[num2];
			Inventory inventory = ContainerRegistry.SafeInventory(container);
			List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>(inventory.GetAllItems());
			for (int i = 0; i < list2.Count; i++)
			{
				ItemDrop.ItemData itemData = list2[i];
				int val = InventoryTransfers.AvailableInContainer(container, itemData.m_shared.m_name, itemData.m_quality);
				int num3 = Math.Min(itemData.m_stack, val);
				for (int j = 0; j < num2; j++)
				{
					if (num3 <= 0)
					{
						break;
					}
					Container container2 = list[j];
					if (!ContainerRegistry.GetSettings(container2).AcceptStorage)
					{
						continue;
					}
					Inventory inventory2 = ContainerRegistry.SafeInventory(container2);
					if (InventoryTransfers.HasType(inventory2, itemData))
					{
						int num4 = InventoryTransfers.Move(inventory, inventory2, itemData, num3, matchingOnly: true);
						num += num4;
						num3 -= num4;
						if (num4 > 0)
						{
							ChestGlowService.Pulse(container2);
						}
					}
				}
			}
		}
		return num;
	}

	internal static int AutomaticSortWarehouse(Player player, out int arrangedChests)
	{
		arrangedChests = 0;
		if (player == null)
		{
			return 0;
		}
		int result = SortWarehouse(player);
		List<Container> list = ContainerRegistry.Nearby(player.transform.position, HearthkeeperPlugin.WarehouseSortRange.Value, requireAccept: false, allowInUse: false);
		for (int i = 0; i < list.Count; i++)
		{
			if (SortInventory(ContainerRegistry.SafeInventory(list[i]), preserveFirstRow: false))
			{
				arrangedChests++;
			}
		}
		return result;
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

	internal static void ProcessGroundItems()
	{
		if (!HearthkeeperPlugin.GroundStorage.Value || Player.m_localPlayer == null)
		{
			return;
		}
		CleanupGround();
		int num = HearthkeeperPlugin.GroundItemsPerCycle.Value;
		int num2 = GroundItems.Count - 1;
		while (num2 >= 0 && num > 0)
		{
			ItemDrop itemDrop = GroundItems[num2];
			if (ValidGroundItem(itemDrop))
			{
				num--;
				List<Container> list = ContainerRegistry.Nearby(itemDrop.transform.position, HearthkeeperPlugin.GroundPickupRange.Value, requireAccept: true, allowInUse: false);
				for (int i = 0; i < list.Count; i++)
				{
					if (!(itemDrop != null))
					{
						break;
					}
					if (itemDrop.m_itemData.m_stack <= 0)
					{
						break;
					}
					Inventory inventory = ContainerRegistry.SafeInventory(list[i]);
					if (!InventoryTransfers.HasType(inventory, itemDrop.m_itemData))
					{
						continue;
					}
					ZNetView component = itemDrop.GetComponent<ZNetView>();
					if (component == null || !component.IsValid())
					{
						break;
					}
					if (!component.IsOwner())
					{
						itemDrop.RequestOwn();
						break;
					}
					int num3 = InventoryTransfers.AddCopy(inventory, itemDrop.m_itemData, itemDrop.m_itemData.m_stack, matchingOnly: true);
					if (num3 > 0)
					{
						ChestGlowService.Pulse(list[i]);
						int num4 = itemDrop.m_itemData.m_stack - num3;
						if (num4 <= 0)
						{
							GroundItems.RemoveAt(num2);
							component.Destroy();
							break;
						}
						itemDrop.SetStack(num4);
					}
				}
			}
			num2--;
		}
	}

	internal static void ProcessFeeding()
	{
		if (!HearthkeeperPlugin.LivestockFeeding.Value)
		{
			return;
		}
		CleanupCreatures();
		for (int i = 0; i < Creatures.Count; i++)
		{
			MonsterAI monsterAI = Creatures[i];
			if (!CanFeed(monsterAI) || (PendingFeed.TryGetValue(monsterAI, out var value) && value != null))
			{
				continue;
			}
			PendingFeed.Remove(monsterAI);
			List<Container> list = ContainerRegistry.Nearby(monsterAI.transform.position, HearthkeeperPlugin.FeedRange.Value, requireAccept: false, allowInUse: false);
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (flag)
				{
					break;
				}
				Container container = list[j];
				if (!ContainerRegistry.GetSettings(container).LivestockFeed)
				{
					continue;
				}
				Inventory inventory = ContainerRegistry.SafeInventory(container);
				for (int k = 0; k < monsterAI.m_consumeItems.Count; k++)
				{
					ItemDrop itemDrop = monsterAI.m_consumeItems[k];
					if (itemDrop == null || itemDrop.m_itemData == null)
					{
						continue;
					}
					string name = itemDrop.m_itemData.m_shared.m_name;
					if (InventoryTransfers.AvailableInContainer(container, name) < 1)
					{
						continue;
					}
					ItemDrop.ItemData itemData = FindItem(inventory, name);
					if (itemData == null)
					{
						continue;
					}
					ItemDrop.ItemData itemData2 = itemData.Clone();
					itemData2.m_stack = 1;
					Vector3 position = monsterAI.transform.position + monsterAI.transform.forward * 0.7f + Vector3.up * 0.2f;
					ItemDrop itemDrop2 = ItemDrop.DropItem(itemData2, 1, position, Quaternion.identity);
					if (!(itemDrop2 == null))
					{
						if (InventoryTransfers.Remove(inventory, name, 1, itemData.m_quality, matchWorldLevel: false) == 1)
						{
							PendingFeed[monsterAI] = itemDrop2;
							flag = true;
							break;
						}
						ZNetView component = itemDrop2.GetComponent<ZNetView>();
						if (component != null && component.IsValid() && component.IsOwner())
						{
							component.Destroy();
						}
					}
				}
			}
		}
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

	private static bool ValidGroundItem(ItemDrop drop)
	{
		if (drop == null || drop.m_itemData == null || drop.m_itemData.m_stack <= 0 || drop.IsPiece())
		{
			return false;
		}
		ZNetView component = drop.GetComponent<ZNetView>();
		if (component != null && component.IsValid())
		{
			return drop.CanPickup();
		}
		return false;
	}

	private static bool CanFeed(MonsterAI ai)
	{
		if (ai == null || ai.m_consumeItems == null || ai.m_consumeItems.Count == 0)
		{
			return false;
		}
		Tameable component = ai.GetComponent<Tameable>();
		if (component == null || !component.IsHungry())
		{
			return false;
		}
		Character component2 = ai.GetComponent<Character>();
		if (component2 == null || component2.IsDead())
		{
			return false;
		}
		ZNetView component3 = ai.GetComponent<ZNetView>();
		if (component3 != null && component3.IsValid())
		{
			return component3.IsOwner();
		}
		return false;
	}

	private static ItemDrop.ItemData FindItem(Inventory inventory, string sharedName)
	{
		if (inventory == null)
		{
			return null;
		}
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		for (int i = 0; i < allItems.Count; i++)
		{
			if (allItems[i].m_shared.m_name == sharedName)
			{
				return allItems[i];
			}
		}
		return null;
	}

	private static void CleanupGround()
	{
		for (int num = GroundItems.Count - 1; num >= 0; num--)
		{
			if (GroundItems[num] == null)
			{
				GroundItems.RemoveAt(num);
			}
		}
	}

	private static void CleanupCreatures()
	{
		for (int num = Creatures.Count - 1; num >= 0; num--)
		{
			if (Creatures[num] == null)
			{
				Creatures.RemoveAt(num);
			}
		}
		List<MonsterAI> list = new List<MonsterAI>();
		foreach (KeyValuePair<MonsterAI, ItemDrop> item in PendingFeed)
		{
			if (item.Key == null || item.Value == null)
			{
				list.Add(item.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			PendingFeed.Remove(list[i]);
		}
	}
}
