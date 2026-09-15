using System;
using System.Collections.Generic;

namespace Quartermaster;

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

    internal static string ItemId(ItemDrop.ItemData item) => item?.m_dropPrefab ? item.m_dropPrefab.name : item?.m_shared?.m_name ?? "";
    // Recipe/fuel references are prefab components: their ItemData.m_dropPrefab is
    // nonserialized and is initialized only when an ItemDrop instance runs Awake.
    internal static string PrefabId(ItemDrop prefab) => prefab ? prefab.gameObject.name : "";
    internal static bool Stackable(ItemDrop.ItemData a, ItemDrop.ItemData b)
    {
        if (a == null || b == null || ItemId(a) != ItemId(b) || !a.IsSameType(b)
            || a.m_quality != b.m_quality || a.m_variant != b.m_variant || a.m_worldLevel != b.m_worldLevel
            || a.m_crafterID != b.m_crafterID || a.m_crafterName != b.m_crafterName
            || a.m_durability != b.m_durability || a.m_cheated != b.m_cheated) return false;
        var x = a.m_customData; var y = b.m_customData;
        if ((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
        if (x != null) foreach (var pair in x) if (y == null || !y.TryGetValue(pair.Key, out var v) || v != pair.Value) return false;
        return true;
    }
    internal static int CapacityFor(Inventory destination, ItemDrop.ItemData source, bool matchingOnly)
    {
        if (destination == null || source?.m_shared == null) return 0;
        bool match = false; long capacity = 0;
        foreach (var item in destination.GetAllItems())
        {
            match |= ItemId(item) == ItemId(source);
            if (Stackable(item, source)) capacity += Math.Max(0, item.m_shared.m_maxStackSize - item.m_stack);
        }
        if (matchingOnly && !match) return 0;
        capacity += Math.Max(0, destination.GetWidth() * destination.GetHeight() - destination.NrOfItems()) * (long)Math.Max(1, source.m_shared.m_maxStackSize);
        return (int)Math.Min(int.MaxValue, capacity);
    }
    // Mutate both inventories before notifying listeners. No AddItem partial-failure duplication,
    // and no mixing of equipment/custom metadata. Each new stack stays at the game's limit.
    internal static int Move(Inventory source, Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
    {
        if (source == null || source == destination || item == null || !source.ContainsItem(item)) return 0;
        int count = Math.Min(item.m_stack, Math.Min(Math.Max(0, requested), CapacityFor(destination, item, matchingOnly)));
        if (count <= 0) return 0;
        var copy = item.Clone();
        int moved = AddRaw(destination, copy, count);
        item.m_stack -= moved;
        if (item.m_stack == 0) source.GetAllItems().Remove(item);
        Notify(source); Notify(destination);
        return moved;
    }
    internal static int AddCopy(Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
    {
        int count = Math.Min(Math.Max(0, requested), CapacityFor(destination, item, matchingOnly));
        if (count <= 0) return 0;
        int moved = AddRaw(destination, item, count); Notify(destination); return moved;
    }
    private static int AddRaw(Inventory inventory, ItemDrop.ItemData item, int count)
    {
        int left = count;
        var items = inventory.GetAllItems();
        foreach (var stack in items)
        {
            if (!Stackable(stack, item)) continue;
            int n = Math.Min(left, Math.Max(0, stack.m_shared.m_maxStackSize - stack.m_stack));
            stack.m_stack += n; left -= n;
            if (left == 0) return count;
        }
        var occupied = new HashSet<int>(); int width = inventory.GetWidth();
        foreach (var stack in items) occupied.Add(stack.m_gridPos.y * width + stack.m_gridPos.x);
        for (int slot = 0; slot < width * inventory.GetHeight() && left > 0; slot++)
        {
            if (occupied.Contains(slot)) continue;
            var stack = item.Clone(); stack.m_stack = Math.Min(left, Math.Max(1, item.m_shared.m_maxStackSize));
            stack.m_gridPos = new Vector2i(slot % width, slot / width); stack.m_equipped = false;
            items.Add(stack); left -= stack.m_stack;
        }
        return count - left;
    }
    internal static void Notify(Inventory inventory)
    {
        // Recalculate weight and invoke game's persistence callbacks.
        try { HarmonyLib.AccessTools.Method(typeof(Inventory), "Changed").Invoke(inventory, new object[] { false, false }); }
        catch (Exception e) { Plugin.Log?.LogError("Inventory changed callback failed: " + e); }
    }
    internal static void StackWithin(Inventory inventory)
    {
        var items = inventory.GetAllItems(); bool changed = false;
        for (int i = 0; i < items.Count; i++)
            for (int j = items.Count - 1; j > i; j--)
            {
                if (!Stackable(items[i], items[j])) continue;
                int n = Math.Min(items[j].m_stack, Math.Max(0, items[i].m_shared.m_maxStackSize - items[i].m_stack));
                if (n <= 0) continue;
                items[i].m_stack += n; items[j].m_stack -= n; changed = true;
                if (items[j].m_stack == 0) items.RemoveAt(j);
            }
        if (changed) Notify(inventory);
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
