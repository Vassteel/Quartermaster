using Quartermaster;
using Slots = InventorySlots.InventorySlotsPlugin;

static class Program
{
    static int checks;
    static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    static ItemDrop.ItemData Put(Inventory inv, string name, int x, int y)
    {
        var item = new ItemDrop.ItemData { m_shared = new() { m_name = name }, m_dropPrefab = new() { name = name }, m_stack = 1, m_gridPos = new(x, y) };
        inv.GetAllItems().Add(item);
        return item;
    }
    static void Main()
    {
        var player = Player.m_localPlayer = new Player { Inventory = new Inventory(4, 7) };
        var inv = player.Inventory;
        Check(ExternalSlotCompatibility.HasInventorySlots, "Detect exact installed assembly and type");
#if BROKEN_SLOT_API
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 1), "Missing policy contract protects ordinary cells");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 5), "Missing policy contract protects equipment cells");
        Put(inv, "Z", 1, 1); Put(inv, "A", 2, 1);
        Check(!WarehouseService.SortInventory(inv, true, true), "No sorting with unknown reservations");
        Check(WarehouseService.StoreAllInOpenChest(player, new Container { InUse = true }) == 0, "No depositing with unknown reservations");
        Check(Plugin.Log.Warnings == 1, "Changed contract warns once");
#else
        Slots.UsableRows = 3;
        Slots.Favorites.Add((1, 1)); // Occupied favorite.
        Slots.Favorites.Add((2, 1)); // Empty favorite must not be used as a sort destination.
        Slots.ExternalReserved.Add((3, 2));
        Check(!ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 2), "Unlocked collapsed rows remain usable");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 3), "Locked regular row protected");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 5), "Equipment tail protected");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 6), "Quick-slot tail protected");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(3, 2), "External reservation protected");
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(2, 1), "Empty favorite protected");
        var hotbar = Put(inv, "Hotbar", 0, 0);
        var equipped = Put(inv, "Equipped", 0, 1); equipped.m_equipped = true;
        var favorite = Put(inv, "Favorite", 1, 1);
        var z = Put(inv, "Z", 3, 1);
        var a = Put(inv, "A", 0, 2);
        var locked = Put(inv, "Locked", 0, 3);
        var gear = Put(inv, "Gear", 0, 5);
        var quick = Put(inv, "Quick", 0, 6);
        var external = Put(inv, "External", 3, 2);
        Check(WarehouseService.SortInventory(inv, true, true), "Sort ordinary unlocked cells");
        Check(a.m_gridPos.x == 3 && a.m_gridPos.y == 1 && z.m_gridPos.x == 0 && z.m_gridPos.y == 2, "Sort skips occupied and empty protected cells");
        Check(favorite.m_gridPos.x == 1 && favorite.m_gridPos.y == 1 && gear.m_gridPos.y == 5 && quick.m_gridPos.y == 6 && locked.m_gridPos.y == 3, "Reserved items stay in place");
        Check(hotbar.m_gridPos.y == 0 && equipped.m_gridPos.x == 0 && external.m_gridPos.x == 3, "Hotbar, equipped item and external cell unchanged");
        Check(!WarehouseService.SortInventory(inv, true, true), "Repeated sort stable");
        var chest = new Container { InUse = true };
        Check(WarehouseService.StoreAllInOpenChest(player, chest) == 2, "Deposit only two ordinary items");
        Check(inv.GetAllItems().Count == 7 && inv.ContainsItem(favorite) && inv.ContainsItem(gear) && inv.ContainsItem(quick), "Deposit keeps equipment, quick slots and favorites");
        Slots.UsableRows = 4;
        Check(!ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 3), "Newly unlocked rows become usable without restarting");
        Slots.Throw = true;
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0, 2), "Policy exception protects inventory");
        int calls = Slots.Calls;
        Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(3, 1) && Slots.Calls == calls, "Failed policy remains protected without repeated exceptions");
        Check(Plugin.Log.Warnings == 1, "Policy failure warns once");
#endif
        Console.WriteLine($"PASS: {checks} InventorySlots compatibility checks.");
    }
}

namespace InventorySlots
{
    // Match the nonpublic signatures verified against the installed 1.5.11 DLL.
    public static class InventorySlotsPlugin
    {
        internal static int UsableRows = 3, Calls;
        internal static bool Throw;
        internal static readonly HashSet<(int, int)> Favorites = new(), ExternalReserved = new();
#if !BROKEN_SLOT_API
        private static bool IsUsableRegularCell(Inventory inventory, Player player, Vector2i pos)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("Simulated policy failure");
            return pos.y >= 0 && pos.y < UsableRows && pos.x >= 0 && pos.x < inventory.GetWidth() && !ExternalReserved.Contains((pos.x, pos.y));
        }
        private static bool IsFavoriteSlot(Player player, Vector2i pos) => Favorites.Contains((pos.x, pos.y));
#endif
    }
}
