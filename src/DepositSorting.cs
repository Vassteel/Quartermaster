using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

internal sealed class SlotSortResult
{
    internal ItemDrop.ItemData Item, BlockedItem;
    internal int Moved;
}

internal static class DepositSorting
{
    internal static SlotSortResult OneSlot(IEnumerable<ItemDrop.ItemData> items, Func<ItemDrop.ItemData,int,int> route)
    {
        var result=new SlotSortResult();
        // Snapshot in visible slot order: routing mutates the inventory. Skip blocked slots
        // so one unmatched item cannot prevent later slots from finding their storage.
        foreach(var item in items.OrderBy(i=>i.m_gridPos.y).ThenBy(i=>i.m_gridPos.x).ToArray())
        {
            int before=item.m_stack;
            if(before<=0) continue;
            int moved=route(item,before);
            if(moved<before) result.BlockedItem=item;
            if(moved<=0) continue;
            result.Item=item; result.Moved=moved;
            break; // At most one occupied slot transfers any items in this cycle.
        }
        return result;
    }
}
