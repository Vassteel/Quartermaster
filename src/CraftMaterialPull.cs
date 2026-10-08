using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

internal static class CraftMaterialPull
{
    internal sealed class Need
    {
        internal string Name;
        internal int Count, Quality;
    }
    internal static bool TryPull(Inventory carried, Need[] needs, Container[] sources, Action<string> message)
    {
        // Simulate the entire fetch before touching a real inventory. This catches
        // several ingredients competing for the same last empty player slot.
        var preview = new Inventory("Quartermaster material capacity",null,carried.GetWidth(),carried.GetHeight());
        preview.GetAllItems().AddRange(carried.GetAllItems().Select(i => i.Clone()));
        var moves = new List<(Container Chest, ItemDrop.ItemData Item, int Count)>();
        var allocated = new Dictionary<ItemDrop.ItemData,int>();
        foreach (var n in needs)
        {
            int left = Math.Max(0,n.Count-InventoryTransfers.CountType(carried,n.Name,n.Quality,true));
            foreach (var c in sources)
            foreach (var item in c.GetInventory().GetAllItems())
            {
                if (left == 0) break;
                if (item.m_shared.m_name != n.Name || (n.Quality >= 0 && item.m_quality != n.Quality) || item.m_worldLevel < Game.m_worldLevel) continue;
                allocated.TryGetValue(item,out int used);
                int take = Math.Min(left,item.m_stack-used);
                if (take <= 0) continue;
                if (InventoryTransfers.AddCopy(preview,item,take,false) != take)
                { message("Make room in your inventory for the required materials"); return false; }
                moves.Add((c,item,take)); allocated[item]=used+take; left-=take;
            }
            if (left > 0) { message("Materials changed; try again"); return false; }
        }
        foreach (var move in moves)
        {
            int moved = InventoryTransfers.Move(move.Chest.GetInventory(),carried,move.Item,move.Count,false);
            // Inventory.Changed only auto-saves for the local owner. Explicitly
            // persist withdrawals from foreign-owned chests as well.
            if (moved > 0) CraftStorageAccess.Save(move.Chest);
            if (moved != move.Count)
            { message("Material transfer interrupted; collected materials remain in your inventory"); return false; }
        }
        return true;
    }
}
