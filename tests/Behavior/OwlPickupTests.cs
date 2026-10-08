using System;
using System.Linq;
using Quartermaster;

internal static class OwlPickupTests
{
    internal static void Run(Action<bool,string> check)
    {
        var source=new ItemDrop.ItemData{m_dropPrefab=new Prefab{name="Wood"},m_shared=new ItemDrop.SharedData{m_name="Wood",m_maxStackSize=50},
            m_stack=1000,m_quality=2,m_crafterID=123,m_crafterName="Viking",m_cheated=true};
        source.m_customData["custom"]="kept";
        var inventory=new Inventory(1,1);int taken=0;
        inventory.OnChanged=()=>check(source.m_stack+inventory.GetAllItems().Sum(i=>i.m_stack)==1000,"pickup listeners observe conserved quantities");
        for(int i=0;i<3;i++)check(InventoryTransfers.ReceiveOne(inventory,source,()=>{source.m_stack--;taken++;return true;}),"a small cleanup trip receives individual items");
        check(taken==3&&source.m_stack==997&&inventory.GetAllItems().Single().m_stack==3,"three-item trip leaves the rest of an oversized pile intact");
        var stored=inventory.GetAllItems().Single();
        check(stored.m_quality==2&&stored.m_crafterID==123&&stored.m_cheated&&stored.m_customData["custom"]=="kept","ground collection preserves item metadata");
        check(!InventoryTransfers.ReceiveOne(inventory,source,()=>false)&&stored.m_stack==3,"ownership failure consumes and stores nothing");
        stored.m_stack=50;bool called=false;
        check(!InventoryTransfers.ReceiveOne(inventory,source,()=>{called=true;return true;})&&!called,"a full chest never consumes a world drop");
        var other=new Inventory(1,1);
        check(!InventoryTransfers.ReceiveOne(other,source,()=>false)&&other.NrOfItems()==0,"failed source removal never creates a destination stack");
    }
}
