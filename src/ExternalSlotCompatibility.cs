using System;
using System.Reflection;

namespace Quartermaster;

internal static class ExternalSlotCompatibility
{
    private delegate bool CellQuery(int column, int row, out string slotName);
    private sealed class SlotApi
    {
        internal CellQuery Contains;
        internal Func<int, int, bool> Protected;
        internal Func<int> VisibleRows;
        internal bool Failed;
    }
    private static readonly Lazy<SlotApi> Api = new Lazy<SlotApi>(Connect);
    private static readonly Lazy<Type> InventorySlotsType = new Lazy<Type>(() =>
        Type.GetType("InventorySlots.InventorySlotsPlugin, InventorySlots", throwOnError: false));

    internal static bool HasInventorySlots => InventorySlotsType.Value != null;

    internal static bool IsProtectedPlayerSlot(ItemDrop.ItemData item) =>
        item != null && IsProtectedPlayerSlot(item.m_gridPos.x, item.m_gridPos.y);

    internal static bool IsProtectedPlayerSlot(int x, int y)
    {
        var api = Api.Value;
        if (api == null) return false;
        // A detected but broken equipment API must never expose its slots to Deposit All.
        if (api.Failed) return true;
        try
        {
            if (api.Protected != null) return api.Protected(x, y);
            if (api.VisibleRows != null && y >= api.VisibleRows()) return true;
            return api.Contains(x, y, out _);
        }
        catch (Exception error)
        {
            api.Failed = true;
            Plugin.Log?.LogWarning("Equipment slot protection paused inventory actions: " + error.GetBaseException().Message);
            return true;
        }
    }

    private static SlotApi Connect()
    {
        var api = new SlotApi();
        try
        {
            if (HasInventorySlots)
            {
                // InventorySlots 1.5.11 has no public slot API. Bind its own cell
                // policy, including locked rows and favorites, rather than guess
                // which tail rows hold equipment. A changed contract fails closed.
                const BindingFlags policyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                var slotsType = InventorySlotsType.Value;
                var regular = slotsType.GetMethod("IsUsableRegularCell", policyFlags, null,
                    new[] { typeof(Inventory), typeof(Player), typeof(Vector2i) }, null);
                var favorite = slotsType.GetMethod("IsFavoriteSlot", policyFlags, null,
                    new[] { typeof(Player), typeof(Vector2i) }, null);
                if (regular == null || favorite == null) throw new MissingMethodException(slotsType.FullName, "Inventory slot policy");
                var isRegular = (Func<Inventory, Player, Vector2i, bool>)Delegate.CreateDelegate(typeof(Func<Inventory, Player, Vector2i, bool>), regular);
                var isFavorite = (Func<Player, Vector2i, bool>)Delegate.CreateDelegate(typeof(Func<Player, Vector2i, bool>), favorite);
                api.Protected = (x, y) =>
                {
                    var player = Player.m_localPlayer;
                    var pos = new Vector2i(x, y);
                    return !player || !isRegular(player.GetInventory(), player, pos) || isFavorite(player, pos);
                };
                Plugin.Log?.LogInfo("Inventory actions respect InventorySlots equipment, quick slots, locked rows and favorites.");
                return api;
            }
            // Plus retains the API namespace but changes the assembly name.
            var type = Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlotsPlus", throwOnError: false)
                ?? Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlots", throwOnError: false);
            if (type == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            var cell = type.GetMethod("IsSlotCell", flags, null,
                new[] { typeof(int), typeof(int), typeof(string).MakeByRefType() }, null);
            if (cell == null) throw new MissingMethodException(type.FullName, "IsSlotCell");
            api.Contains = (CellQuery)Delegate.CreateDelegate(typeof(CellQuery), cell);
            var rows = type.GetMethod("GetVisibleRows", flags, null, Type.EmptyTypes, null);
            if (rows != null) api.VisibleRows = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), rows);
            Plugin.Log?.LogInfo("Inventory actions respect equipment and quick-slot reservations.");
        }
        catch (Exception error)
        {
            api.Failed = true;
            Plugin.Log?.LogWarning("Equipment slot API could not be connected; inventory actions are protected: " + error.GetBaseException().Message);
        }
        return api;
    }
}
