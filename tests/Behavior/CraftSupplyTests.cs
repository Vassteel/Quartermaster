using Quartermaster;
using Need=Quartermaster.CraftMaterialPull.Need;
static class CraftSupplyTests
{
    internal static void Run(Action<bool,string> check)
    {
        check(CraftSupplyPlan.Select(new[]{0},new[]{new[]{90}}).Length==0,"carried materials require no leases");
        check(CraftSupplyPlan.Select(new[]{5},new[]{new[]{8},new[]{9},new[]{10}}).SequenceEqual(new[]{0}),"nearest sufficient chest only");
        check(CraftSupplyPlan.Select(new[]{5,5},new[]{new[]{5,0},new[]{0,5},new[]{5,5}}).SequenceEqual(new[]{2}),"one covering chest beats two partial matches");
        check(CraftSupplyPlan.Select(new[]{5,5},new[]{new[]{3,0},new[]{2,5},new[]{0,0}}).SequenceEqual(new[]{0,1}),"complementary pair covers requirements");
        check(CraftSupplyPlan.Select(new[]{5},Enumerable.Range(0,5).Select(_=>new[]{1}).ToArray())==null,"five-chest fetch refused");
        check(CraftSupplyPlan.Select(new[]{4},Enumerable.Range(0,9).Select(_=>new[]{1}).ToArray()).SequenceEqual(new[]{0,1,2,3}),"four-chest cap and nearest tie break");
        // Compare cardinality with exhaustive subset coverage over small warehouses.
        var random=new Random(55);
        for(int trial=0;trial<500;trial++)
        {
            var need=Enumerable.Range(0,3).Select(_=>random.Next(1,12)).ToArray();
            var stock=Enumerable.Range(0,8).Select(_=>Enumerable.Range(0,3).Select(_=>random.Next(0,5)).ToArray()).ToArray();
            int best=9;
            for(int mask=1;mask<256;mask++)
            {
                var ids=Enumerable.Range(0,8).Where(i=>(mask&(1<<i))!=0).ToArray();
                if(ids.Length<=4&&Enumerable.Range(0,3).All(j=>ids.Sum(i=>stock[i][j])>=need[j]))best=Math.Min(best,ids.Length);
            }
            var selected=CraftSupplyPlan.Select(need,stock);
            check(best==9?selected==null:selected!=null&&selected.Length==best,"minimum covering cardinality agrees with exhaustive oracle");
        }
        ItemDrop.ItemData Item(string name,int count,int quality=1)=>new(){m_shared=new(){m_name=name,m_maxStackSize=50},m_dropPrefab=new(){name=name},m_stack=count,m_quality=quality};
        Container Chest(params ItemDrop.ItemData[] items){var c=new Container{Inventory=new Inventory(8,4)};c.Inventory.GetAllItems().AddRange(items);return c;}
        var wood=Item("Wood",20);wood.m_customData["origin"]="test";
        var stone=Item("Stone",30);var source=Chest(wood,stone);
        var player=new Inventory(4,2);player.GetAllItems().Add(Item("Wood",3));
        var needs=new[]{new Need{Name="Wood",Count=5,Quality=1},new Need{Name="Stone",Count=4,Quality=1}};
        check(CraftMaterialPull.TryPull(player,needs,new[]{source},_=>{}),"fetch succeeds");
        check(wood.m_stack==18&&stone.m_stack==26,"only exact deficits removed");
        check(InventoryTransfers.CountType(player,"Wood")==5&&InventoryTransfers.CountType(player,"Stone")==4,"player funded exactly");
        check(player.GetAllItems().Any(i=>i.m_customData.ContainsKey("origin")),"fetched metadata preserved");
        check(CraftMaterialPull.TryPull(player,needs,new[]{source},_=>{})&&wood.m_stack==18,"rechecking carried counts prevents double fetch");
        var full=new Inventory(1,1);full.GetAllItems().Add(Item("Other",1));
        check(!CraftMaterialPull.TryPull(full,needs,new[]{source},_=>{})&&wood.m_stack==18&&stone.m_stack==26,"full inventory changes no sources");
        var oneSlot=new Inventory(1,1);
        check(!CraftMaterialPull.TryPull(oneSlot,needs,new[]{source},_=>{})&&oneSlot.NrOfItems()==0&&wood.m_stack==18,"joint capacity check prevents partial fetch");
        var missing=new[]{new Need{Name="Wood",Count=3,Quality=1},new Need{Name="Missing",Count=1,Quality=1}};
        check(!CraftMaterialPull.TryPull(new Inventory(4,2),missing,new[]{source},_=>{})&&wood.m_stack==18,"changed stock aborts before first removal");
        check(!CraftMaterialPull.TryPull(new Inventory(4,2),new[]{new Need{Name="Wood",Count=1,Quality=2}},new[]{source},_=>{}),"quality cannot be substituted");
        Game.m_worldLevel=1;
        check(!CraftMaterialPull.TryPull(new Inventory(4,2),needs,new[]{source},_=>{}),"world-level requirement preserved");
        Game.m_worldLevel=0;
    }
}
