using Quartermaster;

internal static class SlotSortingTests
{
    internal static void Run(Action<bool,string> check)
    {
        ItemDrop.ItemData Item(string name,int count,int x)=>new() {
            m_dropPrefab=new Prefab { name=name }, m_stack=count,m_gridPos=new Vector2i(x,0),
            m_shared=new ItemDrop.SharedData { m_name=name,m_maxStackSize=50 }
        };
        var source=new Inventory(4,1); var dest=new Inventory(4,1);
        var first=Item("Wood",20,0); var second=Item("Stone",30,1);
        source.GetAllItems().Add(second); source.GetAllItems().Add(first);
        int calls=0;
        var result=DepositSorting.OneSlot(source.GetAllItems(),(item,count)=> { calls++; return InventoryTransfers.Move(source,dest,item,count,false); });
        check(result.Item==first && result.Moved==20 && calls==1,"one occupied slot moves in visible order, regardless of list insertion order");
        check(source.GetAllItems().Count==1 && source.GetAllItems()[0]==second && second.m_stack==30,"later slot remains untouched until next cycle");
        check(dest.GetAllItems().Sum(i=>i.m_stack)==20,"one slot transfers its whole stack");
        result=DepositSorting.OneSlot(source.GetAllItems(),(item,count)=>InventoryTransfers.Move(source,dest,item,count,false));
        check(result.Item==second && result.Moved==30 && source.GetAllItems().Count==0,"next cycle processes next slot");
        check(dest.GetAllItems().Sum(i=>i.m_stack)==50,"sequential sorting conserves all items");

        var blocked=Item("NoStorage",1,0); var later=Item("Wood",2,1);
        source.GetAllItems().Add(blocked);source.GetAllItems().Add(later);calls=0;
        result=DepositSorting.OneSlot(source.GetAllItems(),(item,count)=> { calls++;return item==blocked ? 0 : InventoryTransfers.Move(source,dest,item,count,false); });
        check(result.Item==later && result.BlockedItem==blocked && calls==2,"unmatched first slot does not starve a routable later slot");
        check(blocked.m_stack==1 && source.GetAllItems().Count==1,"blocked payload stays in the chest");
        result=DepositSorting.OneSlot(source.GetAllItems(),(item,count)=>0);
        check(result.Moved==0 && result.Item==null && result.BlockedItem==blocked,"fully blocked pass produces no sorted-slot event");

        var partialSource=new Inventory(2,1);var fullDest=new Inventory(1,1);
        var partial=Item("Coal",20,0);var untouched=Item("Wood",1,1);
        fullDest.GetAllItems().Add(Item("Coal",45,0));partialSource.GetAllItems().Add(partial);partialSource.GetAllItems().Add(untouched);
        calls=0;
        result=DepositSorting.OneSlot(partialSource.GetAllItems(),(item,count)=> { calls++;return InventoryTransfers.Move(partialSource,fullDest,item,count,false); });
        check(result.Moved==5 && result.Item==partial && result.BlockedItem==partial && calls==1,"partial slot transfer still stops this cycle");
        check(partial.m_stack==15 && untouched.m_stack==1,"partial remainder and later slot are preserved");
        check(DepositSorting.OneSlot(Array.Empty<ItemDrop.ItemData>(),(item,count)=>throw new Exception()).Moved==0,"empty chest performs no routing");

        var throws=new SlotThrows(); throws.Begin(10);
        check(throws.Remaining==3 && !throws.Take(10.3f),"every processed slot queues three throws after the scoop");
        check(throws.Take(10.34f) && !throws.Take(10.34f),"first throw cannot repeat in one frame");
        check(throws.Take(11) && throws.Take(11.7f),"second and third throws are spaced apart");
        check(throws.Remaining==0 && !throws.Take(15),"exactly three throws, with no fourth");
        throws.Begin(20);
        check(throws.Take(24) && throws.Take(25) && throws.Take(26) && throws.Remaining==0,"slow frames do not expire or lose throws");
        throws.Begin(30);throws.Clear();check(!throws.Take(35) && throws.Remaining==0,"hiding or unloading clears pending work");
    }
}
