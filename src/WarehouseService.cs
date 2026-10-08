using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

internal static class WarehouseService
{
    internal static IEnumerable<Container> CraftStores(Player player,bool observe=false)
    {
        if (!player || !Plugin.CraftFromContainers.Value) yield break;
        foreach (var chest in CraftStorageAccess.Nearby(player))
            if (ContainerRegistry.GetSettings(chest).CraftingSupply) yield return chest;
    }

    internal static int CountAvailableNearPlayer(string sharedName, int quality, bool matchWorldLevel)
    {
        long available = 0;
        foreach (var chest in CraftStores(Player.m_localPlayer,true))
            available += InventoryTransfers.AvailableInContainer(chest, sharedName, quality, matchWorldLevel);
        return (int)Math.Min(available, int.MaxValue);
    }

    internal static int StoreAllInOpenChest(Player player, Container container)
    {
        // A shared chest this player only views is changed through its owner (ChestUi wraps this call).
        if (!player || !container || !(ContainerRegistry.IsUsable(container, allowInUse: true) || SharedChests.Viewing(container))) return 0;
        var carried = player.GetInventory();
        var storage = ContainerRegistry.SafeInventory(container);
        if (carried == null || storage == null || ReferenceEquals(carried, storage)) return 0;

        var eligible = carried.GetAllItems().Where(item => item?.m_shared != null
            && item.m_gridPos.y > 0 && !item.m_shared.m_questItem
            && !player.IsItemEquiped(item) && !ExternalSlotCompatibility.IsProtectedPlayerSlot(item)).ToArray();
        int deposited = 0;
        foreach (var item in eligible)
            deposited += InventoryTransfers.Move(carried, storage, item, item.m_stack, matchingOnly: false);
        if (deposited > 0) ChestVisual.Pulse(container);
        return deposited;
    }

    internal static bool SortInventory(Inventory inventory, bool preserveFirstRow, bool preserveEquipped = false)
    {
        if (inventory == null) return false;
        int columns = inventory.GetWidth(), rows = inventory.GetHeight();
        if (columns < 1 || rows < 1) return false;
        var owner = Player.m_localPlayer;
        bool personal = preserveEquipped && owner && ReferenceEquals(owner.GetInventory(), inventory);
        var fixedCells = new bool[columns, rows];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                fixedCells[x, y] = (preserveFirstRow && y == 0)
                    || (personal && ExternalSlotCompatibility.IsProtectedPlayerSlot(x, y));

        var movable = new List<ItemDrop.ItemData>();
        var occupied = new bool[columns, rows];
        foreach (var item in inventory.GetAllItems())
        {
            if (item?.m_shared == null) return false;
            int x = item.m_gridPos.x, y = item.m_gridPos.y;
            // Leave malformed storage untouched rather than overwriting a saved position.
            if (x < 0 || x >= columns || y < 0 || y >= rows || occupied[x, y]) return false;
            occupied[x, y] = true;
            if (personal && owner.IsItemEquiped(item)) fixedCells[x, y] = true;
            if (!fixedCells[x, y]) movable.Add(item);
        }
        var positions = new List<Vector2i>();
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                if (!fixedCells[x, y]) positions.Add(new Vector2i(x, y));
        if (positions.Count < movable.Count) return false;

        var ordered = movable.OrderBy(item => item.m_shared.m_itemType)
            .ThenBy(item => item.m_shared.m_name, StringComparer.Ordinal)
            .ThenByDescending(item => item.m_quality).ThenByDescending(item => item.m_stack)
            .ThenBy(item => item.m_gridPos.y).ThenBy(item => item.m_gridPos.x).ToArray();
        bool changed = false;
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i].m_gridPos.x == positions[i].x && ordered[i].m_gridPos.y == positions[i].y) continue;
            ordered[i].m_gridPos = positions[i];
            changed = true;
        }
        if (changed) InventoryTransfers.Notify(inventory);
        return changed;
    }
}
