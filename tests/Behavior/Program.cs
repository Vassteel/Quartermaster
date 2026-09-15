using Quartermaster;
using System.Text.Json;
int checks = 0;
void Assert(bool condition, string name) { checks++; if (!condition) throw new Exception("FAIL: " + name); }
ItemDrop.ItemData Item(string name, int count, int max=50, int quality=1) => new() { m_dropPrefab=new Prefab{name=name},m_shared=new ItemDrop.SharedData{m_name=name,m_maxStackSize=max},m_stack=count,m_quality=quality };
void Put(Inventory inventory, ItemDrop.ItemData item, int slot=0) { item.m_gridPos=new Vector2i(slot%inventory.GetWidth(),slot/inventory.GetWidth());inventory.GetAllItems().Add(item); }
long Total(params Inventory[] inventories) => inventories.Sum(i=>i.GetAllItems().Sum(s=>(long)s.m_stack));
string Fingerprint(ItemDrop.ItemData i)=>$"{i.m_dropPrefab.name}:{i.m_quality}:{i.m_variant}:{i.m_worldLevel}:{i.m_crafterID}:{i.m_durability}:{i.m_cheated}:"+string.Join(";",i.m_customData.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value));
Dictionary<string,long> Payload(params Inventory[] inventories)=>inventories.SelectMany(i=>i.GetAllItems()).GroupBy(Fingerprint).ToDictionary(g=>g.Key,g=>g.Sum(i=>(long)i.m_stack));
void Conserved(Dictionary<string,long> before, params Inventory[] inventories) { var after=Payload(inventories);Assert(before.Count==after.Count&&before.All(p=>after.TryGetValue(p.Key,out var n)&&n==p.Value),"quantity and metadata conserved"); }
void Valid(Inventory inventory) { var items=inventory.GetAllItems();Assert(items.All(i=>i.m_stack>0&&i.m_stack<=i.m_shared.m_maxStackSize),"native stack limits");Assert(items.All(i=>i.m_gridPos.x>=0&&i.m_gridPos.x<inventory.GetWidth()&&i.m_gridPos.y>=0&&i.m_gridPos.y<inventory.GetHeight()),"valid slots");Assert(items.Select(i=>(i.m_gridPos.x,i.m_gridPos.y)).Distinct().Count()==items.Count,"unique slots"); }
var chest = new ChestSettings();
Assert(chest.Observe(new[]{"Wood","Wood","Coal"}),"learn manually placed types");
Assert(chest.Remembered.Count==2,"no duplicate memory");
chest.Observe(Array.Empty<string>());Assert(chest.Accepts("Wood"),"empty chest remembers wood");
chest.Forget("Wood");chest.Observe(new[]{"Wood"});Assert(!chest.Accepts("Wood"),"forgotten item does not relearn from old contents");
chest.Restore("Wood");Assert(chest.Accepts("Wood"),"explicit restore works");
chest.Deposit=true;Assert(!chest.Accepts("Wood")&&!chest.Observe(new[]{"Stone"}),"deposit cannot become routing destination or learn inbound loot");
chest.Deposit=false;chest.Overflow=true;Assert(chest.Accepts("Unknown")&&!chest.Observe(new[]{"Unknown"}),"overflow accepts without contaminating item memory");
chest.AcceptStorage=false;Assert(!chest.Accepts("Wood"),"receiving switch wins over assignment");
var json=JsonSerializer.Serialize(chest,new JsonSerializerOptions{IncludeFields=true});
var reloaded=JsonSerializer.Deserialize<ChestSettings>(json,new JsonSerializerOptions{IncludeFields=true});
Assert(reloaded.Remembered.SequenceEqual(chest.Remembered)&&reloaded.Forgotten.SequenceEqual(chest.Forgotten),"settings model roundtrip retains memory");
Assert(Policy.SameGroup(" Home ","home")&&!Policy.SameGroup("Home","Outpost"),"base groups normalize");
Assert(Policy.ProtectedWood("FineWood")&&Policy.ProtectedWood("RoundLog")&&!Policy.ProtectedWood("Wood"),"valuable wood protection");
// Cap edits belong to output IDs, even when visible rows/pages change order between saves.
var machineCaps = new MachineSettings();
var expectedCaps = new Dictionary<string,int> { ["Copper"]=200, ["Tin"]=200, ["Iron"]=200 };
foreach(var edit in new[]{("Copper",250),("Copper",800),("Tin",350),("Iron",900),("Copper",125),("Tin",0),("Iron",100000),("Copper",700)})
{
    machineCaps.SetCap(edit.Item1,edit.Item2); expectedCaps[edit.Item1]=edit.Item2;
    var savedCaps=JsonSerializer.Serialize(machineCaps,new JsonSerializerOptions{IncludeFields=true});
    machineCaps=JsonSerializer.Deserialize<MachineSettings>(savedCaps,new JsonSerializerOptions{IncludeFields=true});
    machineCaps.Caps.Reverse();
    foreach(var pair in expectedCaps) Assert(machineCaps.Cap(pair.Key)==pair.Value,"repeated save stays with output item, not row index");
    Assert(machineCaps.Caps.Select(c=>c.Item).Distinct().Count()==machineCaps.Caps.Count,"repeated edits do not duplicate product caps");
}
var kilnCaps=new MachineSettings();
foreach(int amount in new[]{250,300,800,125,0,500}){kilnCaps.SetCap("Coal",amount);Assert(kilnCaps.Cap("Coal")==amount,"kiln cap can be saved repeatedly");}
Assert(kilnCaps.Caps.Count==1,"repeated kiln edits retain one coal entry");
foreach(var name in new[]{"Wood","Coal","CopperOre","Copper","MeadBaseHealthMinor"})
{
    var stored=Item(name,50); stored.m_shared.m_name="$item_"+name.ToLowerInvariant();
    var recipeData=stored.Clone(); recipeData.m_dropPrefab=null;
    var recipe=new ItemDrop{gameObject=new Prefab{name=name},m_itemData=recipeData};
    Assert(InventoryTransfers.ItemId(recipeData)!=InventoryTransfers.ItemId(stored),"reproduce uninitialized recipe metadata mismatch");
    Assert(InventoryTransfers.PrefabId(recipe)==InventoryTransfers.ItemId(stored),"recipe component ID matches inventory item before prefab Awake");
}
var legacyCaps=new MachineSettings();legacyCaps.SetCap("$item_coal",750);legacyCaps.SetCap("$item_copper",125);
Assert(legacyCaps.MigrateCapId("$item_coal","Coal"),"migrate legacy recipe display-name cap");
Assert(legacyCaps.Cap("Coal")==750&&legacyCaps.Cap("$item_copper")==125,"migration preserves target value and other products");
Assert(!legacyCaps.MigrateCapId("$item_coal","Coal"),"cap migration is idempotent");
legacyCaps.SetCap("Copper",900);legacyCaps.MigrateCapId("$item_copper","Copper");
Assert(legacyCaps.Cap("Copper")==900&&!legacyCaps.Caps.Any(c=>c.Item=="$item_copper"),"existing canonical cap wins over stale alias");
long producedAt=DateTime.UtcNow.Ticks, collectAt=producedAt+Policy.OutputDelayTicks;
Assert(!Policy.OutputReady(producedAt,collectAt),"new output remains available on the ground");
Assert(!Policy.OutputReady(collectAt-1,collectAt),"no collection before the full grace interval");
Assert(Policy.OutputReady(collectAt,collectAt)&&Policy.OutputReady(collectAt+TimeSpan.TicksPerSecond,collectAt),"output becomes eligible at 60 seconds");
Assert(!Policy.OutputReady(producedAt,0),"legacy output requires an initialized deadline");
Assert(!Policy.OutputReady(producedAt-TimeSpan.TicksPerMinute,collectAt),"backwards clock cannot collect output early");
Assert(Policy.BatchesAllowed(200,199,1,1,25)==0,"uncollected ground stock still counts toward the production cap");
Assert(Policy.ValidProcessorAccumulator(-900)==0,"negative processing backlog cannot stall a manually loaded kiln");
Assert(Policy.ValidProcessorAccumulator(.75f)==.75f&&Policy.ValidProcessorAccumulator(120)==120,"valid fractional and offline processing time is preserved");
Assert(Policy.ValidProcessorAccumulator(float.NaN)==0&&Policy.ValidProcessorAccumulator(float.PositiveInfinity)==0,"invalid processing accumulators recover safely");
Assert(!Policy.CookingSlotReady("RawMeat",0)&&!Policy.CookingSlotReady("",1),"raw and empty cooking slots are never harvested");
Assert(Policy.CookingSlotReady("CookedMeat",1)&&Policy.CookingSlotReady("Coal",2),"finished and already burnt slots are cleared for pickup");
Assert(Policy.CookingSlotProduces("RawMeat","RawMeat","CookedMeat","CookedMeat"),"raw meat in a cooking slot reserves finished-food allowance");
Assert(Policy.CookingSlotProduces("CookedMeat","RawMeat","CookedMeat","CookedMeat"),"cooked food on the rack still counts toward its cap");
Assert(!Policy.CookingSlotProduces("","RawMeat","CookedMeat","CookedMeat")&&!Policy.CookingSlotProduces("Coal","RawMeat","CookedMeat","CookedMeat"),"empty and burnt slots do not reserve cooked-meat allowance");
Assert(!Policy.CookingSlotProduces("RawMeat","RawMeat","CookedMeat","CookedLoxMeat"),"cooking caps remain separate for different foods");
Assert(Policy.BatchesAllowed(200,198,2,1,5)==0,"two loaded rack slots and pending ground food prevent extra raw food consumption");
Assert(Policy.BatchesAllowed(200,199,1,1,25)==0,"kilns count pending output");
Assert(Policy.BatchesAllowed(200,197,0,6,1)==0,"whole mead batch cannot overshoot");
Assert(Policy.BatchesAllowed(200,180,12,6,1)==1,"mead batch fits remaining allowance");
Assert(Policy.BatchesAllowed(0,0,0,1,20)==0,"zero cap stops input");
Assert(Policy.BatchesAllowed(200,long.MaxValue,0,1,20)==0,"large stocks cannot overflow cap arithmetic");
Assert(Policy.BatchesAllowed(200,long.MaxValue,long.MaxValue,1,20)==0,"extreme stored and queued values cannot wrap");
// Many machines compete against the same stock + in-flight ledger.
long queued=0;int stock=17;for(int machine=0;machine<60;machine++){int add=Policy.BatchesAllowed(200,stock,queued,6,1);queued+=add*6;Assert(stock+queued<=200,"many fermenters share cap");}
Assert(Policy.BatchesAllowed(200,stock+queued-12,0,6,1)==1,"production resumes after stock consumption");
// Destination completely full, partial capacity, metadata, native maximum size.
var source=new Inventory(4,2);var destination=new Inventory(1,1);Put(source,Item("Wood",50));Put(destination,Item("Wood",47));
var payload=Payload(source,destination);Assert(InventoryTransfers.Move(source,destination,source.GetAllItems()[0],50,false)==3,"partial transfer moves only actual space");Conserved(payload,source,destination);
Assert(InventoryTransfers.Move(source,destination,source.GetAllItems()[0],50,false)==0,"full chest changes nothing");Conserved(payload,source,destination);
var output=new Inventory(2,1);Assert(InventoryTransfers.AddCopy(output,Item("Coal",1),200,false)==100,"multi-stack production split at game maximum");Valid(output);
var customSource=new Inventory(2,1);var customDest=new Inventory(1,1);var a=Item("Arrow",30);a.m_customData["owner"]="A";var b=Item("Arrow",10);b.m_customData["owner"]="B";Put(customSource,a);Put(customDest,b);
Assert(InventoryTransfers.Move(customSource,customDest,a,30,false)==0,"different custom data cannot merge");
// Change callbacks must observe BOTH inventories after a transfer, never an intermediate duplication.
var cs=new Inventory(1,1);var cd=new Inventory(1,1);Put(cs,Item("Stone",40));cs.OnChanged=()=>Assert(Total(cs,cd)==40,"source callback observes conserved state");cd.OnChanged=()=>Assert(Total(cs,cd)==40,"destination callback observes conserved state");InventoryTransfers.Move(cs,cd,cs.GetAllItems()[0],30,false);
// Randomized stress: quantities and every payload remain exact under repeated partial moves and sorting.
var random=new Random(9414);
for(int trial=0;trial<1000;trial++)
{
    var src=new Inventory(4,3);var dst=new Inventory(3,3);
    int count=random.Next(1,12);
    for(int i=0;i<count;i++){var item=Item("Item"+random.Next(3),random.Next(1,51),50,random.Next(1,3));item.m_variant=random.Next(2);item.m_worldLevel=random.Next(2);item.m_cheated=random.Next(2)==1;item.m_customData["tag"]="v"+random.Next(2);Put(src,item,i);}
    var before=Payload(src,dst);
    for(int pass=0;pass<4;pass++)
    {
        foreach(var item in src.GetAllItems().ToArray())InventoryTransfers.Move(src,dst,item,random.Next(0,80),false);
        InventoryTransfers.StackWithin(src);InventoryTransfers.StackWithin(dst);
        Conserved(before,src,dst);Valid(src);Valid(dst);
    }
}
StationTests.Run(Assert);
ItemTagTests.Run(Assert);
GullTests.Run(Assert);
SlotSortingTests.Run(Assert);
Console.WriteLine($"PASS: {checks} assertions; station coverage, learned storage, forgotten-item persistence, routing exclusions, production cap scenarios, metadata preservation, callbacks and 1000 randomized inventory trials.");
