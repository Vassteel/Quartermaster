using Quartermaster;
internal static class StackCompatibilityTests
{
    internal static void Run(Action<bool,string> check)
    {
        ItemDrop.ItemData Item(string id,int count,float durability)=>new(){m_dropPrefab=new Prefab{name=id},m_shared=new ItemDrop.SharedData{m_name=id,m_maxStackSize=500},m_stack=count,m_durability=durability};
        var resin=new Inventory(2,1);resin.GetAllItems().Add(Item("Resin",6,100));var second=Item("Resin",23,0);second.m_gridPos=new Vector2i(1,0);resin.GetAllItems().Add(second);
        InventoryTransfers.StackWithin(resin);
        check(resin.NrOfItems()==1&&resin.GetAllItems()[0].m_stack==29,"ordinary resin merges despite unused durability differences");
        var meat=new Inventory(3,1);for(int i=0;i<3;i++){var item=Item("RawMeat",new[]{24,5,6}[i],new[]{100,0,50}[i]);item.m_gridPos=new Vector2i(i,0);meat.GetAllItems().Add(item);}
        InventoryTransfers.StackWithin(meat);check(meat.NrOfItems()==1&&meat.GetAllItems()[0].m_stack==35,"three ordinary raw meat stacks consolidate without loss");
        var a=Item("Wood",490,100);var b=Item("Wood",23,0);var target=new Inventory(1,1);target.GetAllItems().Add(a);var source=new Inventory(1,1);source.GetAllItems().Add(b);
        check(InventoryTransfers.CapacityFor(target,b,false)==10,"capacity and transfer agree for unused durability");
        check(InventoryTransfers.Move(source,target,b,23,false)==10&&a.m_stack==500&&b.m_stack==13,"partial transfer preserves limit and exact remainder");
        a=Item("Arrow",1,100);b=Item("Arrow",1,0);a.m_shared.m_useDurability=true;
        check(!InventoryTransfers.Stackable(a,b)&&!InventoryTransfers.Stackable(b,a),"durability remains significant if either item uses it");
        a=Item("Wood",1,100);b=Item("Wood",1,0);b.m_crafterName=null;
        check(InventoryTransfers.Stackable(a,b),"empty and absent crafter names are equivalent");
        b.m_crafterName="Viking";check(!InventoryTransfers.Stackable(a,b),"named crafter metadata is retained");b.m_crafterName="";
        b.m_cheated=true;check(!InventoryTransfers.Stackable(a,b),"different cheat flags remain separate");b.m_cheated=false;
        b.m_worldLevel=1;check(!InventoryTransfers.Stackable(a,b),"world-level differences are preserved");b.m_worldLevel=0;
        b.m_quality=2;check(!InventoryTransfers.Stackable(a,b),"quality differences are preserved");b.m_quality=1;
        b.m_variant=1;check(!InventoryTransfers.Stackable(a,b),"variant differences are preserved");b.m_variant=0;
        b.m_customData["enchantment"]="special";check(!InventoryTransfers.Stackable(a,b),"custom item data remains separate");
    }
}
