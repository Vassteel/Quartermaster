using System;
using System.Collections.Generic;
using System.Linq;
using Quartermaster;

internal static class OverflowTests
{
    internal static void Run(Action<bool,string> check)
    {
        // Lowering the cap first fills matching partial stacks and free chest
        // slots; only stock that physically cannot fit should reach the ground.
        var splitting = new ItemDrop.ItemData { m_dropPrefab = new Prefab { name = "Stone" }, m_shared = new ItemDrop.SharedData { m_name = "Stone", m_maxStackSize = 50 }, m_stack = 235 };
        splitting.m_customData["note"] = "keep";
        var roomy = new Inventory(3, 1); roomy.GetAllItems().Add(splitting);
        var partial = splitting.Clone(); partial.m_stack = 15; partial.m_gridPos = new Vector2i(1, 0); roomy.GetAllItems().Add(partial);
        var spilled = new List<ItemDrop.ItemData>();
        roomy.OnChanged = () => check(roomy.GetAllItems().Sum(x => x.m_stack) + spilled.Sum(x => x.m_stack) == 250, "split and spill callbacks preserve total stock");
        check(InventoryTransfers.SplitExcess(roomy) == 85, "fill partial stack and one free slot before spilling");
        check(splitting.m_stack == 150 && partial.m_stack == 50 && roomy.NrOfItems() == 3, "fill every available chest slot");
        check(roomy.GetAllItems().All(x => x.m_customData["note"] == "keep"), "split stacks preserve metadata");
        check(InventoryTransfers.EjectExcess(roomy, 4, copy => { spilled.Add(copy); return true; }) == 2, "only two excess stacks fall from full chest");
        check(roomy.GetAllItems().Sum(x => x.m_stack) == 150 && spilled.Sum(x => x.m_stack) == 100, "full chest plus dropped stock equals original 250");
        var enough = new Inventory(5, 1); var small = splitting.Clone(); small.m_stack = 175; enough.GetAllItems().Add(small);
        check(InventoryTransfers.SplitExcess(enough) == 125 && enough.GetAllItems().Sum(x => x.m_stack) == 175, "ample chest space retains all stock");
        check(InventoryTransfers.EjectExcess(enough, 4, _ => throw new Exception("Unnecessary world drop")) == 0, "no spill when all normal stacks fit");
        var full = new Inventory(1, 1); var held = splitting.Clone(); held.m_stack = 1000; full.GetAllItems().Add(held);
        check(InventoryTransfers.SplitExcess(full) == 0 && held.m_stack == 1000, "full chest retains excess until drop succeeds");
        check(InventoryTransfers.EjectExcess(full, 4, _ => false) == 0 && held.m_stack == 1000, "blocked/failed spill cannot lose stock");
        var item=new ItemDrop.ItemData{m_dropPrefab=new Prefab{name="Wood"},m_shared=new ItemDrop.SharedData{m_name="Wood",m_maxStackSize=50},m_stack=1000,m_quality=2,m_crafterID=42,m_crafterName="Crafter",m_cheated=true};
        item.m_customData["enchanted"]="yes";
        var chest=new Inventory(2,1);chest.GetAllItems().Add(item);
        var drops=new List<ItemDrop.ItemData>();
        chest.OnChanged=()=>check(item.m_stack+drops.Sum(d=>d.m_stack)==1000,"overflow listeners see conserved quantities");
        check(InventoryTransfers.EjectExcess(chest,4,copy=>{drops.Add(copy);return true;})==4,"overflow obeys per-cycle world-drop budget");
        check(item.m_stack==800&&drops.All(d=>d.m_stack==50),"first cycle emits four legal stacks");
        int calls=0;check(InventoryTransfers.EjectExcess(chest,4,copy=>{calls++;return false;})==0&&item.m_stack==800&&calls==1,"failed spawn leaves source untouched and ends this chest's cycle");
        for(int i=0;i<10;i++)InventoryTransfers.EjectExcess(chest,4,copy=>{drops.Add(copy);return true;});
        check(item.m_stack==50&&drops.Count==19&&drops.Sum(d=>d.m_stack)==950,"lowering 1000 to 50 keeps 50 and ejects exactly 950");
        check(drops.All(d=>d.m_quality==2&&d.m_crafterID==42&&d.m_crafterName=="Crafter"&&d.m_cheated&&d.m_customData["enchanted"]=="yes"),"overflow preserves metadata on every real drop");
        check(InventoryTransfers.EjectExcess(chest,4,copy=>{throw new Exception("unexpected spawn");})==0,"legal chest contents never spill again");
        chest.OnChanged=null;
        int world=drops.Sum(d=>d.m_stack);
        for(int i=0;i<60;i++)InventoryTransfers.ReceiveOne(chest,drops[0],()=>{world--;return true;});
        check(chest.GetAllItems().Sum(d=>d.m_stack)==100&&world==900,"owl fills only legal available capacity after ejection");
        check(InventoryTransfers.EjectExcess(chest,4,copy=>true)==0,"pickup cannot cause an overflow loop");
        var rng=new Random(743);
        for(int t=0;t<200;t++)
        {
            int limit=rng.Next(2,500),total=rng.Next(1,2500);item.m_shared.m_maxStackSize=limit;item.m_stack=total;
            chest.GetAllItems().Clear();chest.GetAllItems().Add(item);int emitted=0,largest=0;
            while(InventoryTransfers.EjectExcess(chest,4,copy=>{emitted+=copy.m_stack;largest=Math.Max(largest,copy.m_stack);return true;})>0){}
            check(item.m_stack==Math.Min(total,limit)&&item.m_stack+emitted==total&&largest<=limit,"randomized overflow conserves items and caps every spawned stack");
        }
    }
}
