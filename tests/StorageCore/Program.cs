using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Quartermaster;
using SlotApi=EquipmentAndQuickSlots.API;

static class Program {
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static object Call(string method,params object[] args)=>typeof(CraftingPatches).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
 static ItemDrop.ItemData Put(Inventory inv,string name,int quantity,int x=0,int y=0,int quality=1,int world=0){var i=new ItemDrop.ItemData{m_shared=new(){m_name=name},m_dropPrefab=new(){name=name},m_stack=quantity,m_gridPos=new(x,y),m_quality=quality,m_worldLevel=world};inv.GetAllItems().Add(i);return i;}
 static Player Reset(){var p=Player.m_localPlayer=new Player();ContainerRegistry.Stores.Clear();Plugin.Enabled.Value=Plugin.CraftFromContainers.Value=true;SlotApi.Protected.Clear();Game.m_worldLevel=0;SharedOperations.Networked=true;SharedOperations.Executing=false;CraftingPatches.LocalCountsOnly=false;return p;}
 static Container Store(string name,int count){var c=new Container();Put(c.Inventory,name,count);ContainerRegistry.Stores.Add(c);return c;}
 static void Main(string[] args){
  if(args.Contains("absent")){Check(!ExternalSlotCompatibility.IsProtectedPlayerSlot(0,0),"Missing optional mod leaves ordinary slots usable");Check(!ExternalSlotCompatibility.IsProtectedPlayerSlot((ItemDrop.ItemData)null),"Null item is safe");Console.WriteLine($"PASS: {checks} absent optional API checks.");return;}
  var player=Reset();var bag=player.Inventory;
  Put(bag,"Wood",9,0,0);var small=Put(bag,"Wood",2,1,0);Put(bag,"Wood",4,2,0,2);Put(bag,"Wood",3,3,0,1,1);
  Check(InventoryTransfers.CountType(bag,"Wood")==18,"Count across qualities");
  Game.m_worldLevel=1;Check(InventoryTransfers.CountType(bag,"Wood",1,true)==3,"Quality and world filters agree");
  Check(InventoryTransfers.CountType(null,"Wood")==0&&InventoryTransfers.CountType(bag,"")==0,"Empty count arguments");
  Game.m_worldLevel=0;Check(InventoryTransfers.Remove(bag,"Wood",3,1,false)==3&&!bag.ContainsItem(small),"Small stacks consumed first");
  Check(InventoryTransfers.CountType(bag,"Wood")==15,"Exact removal amount");
  bag.RejectRemovals=true;Check(InventoryTransfers.Remove(bag,"Wood",8,-1,false)==0,"Rejected removal cannot count as paid");bag.RejectRemovals=false;
  Check(InventoryTransfers.HasType(bag,new ItemDrop.ItemData{m_shared=new(){m_name="Wood"}}),"Type lookup");
  player=Reset();var stock=Store("Stone",10);
  Store("Stone",100).Settings.CraftingSupply=false;var remote=Store("Stone",100);remote.Owned=false;
  Store("Stone",100).Accessible=false;Store("Stone",100).InUse=true;Store("Stone",100).Distance=20;
  Put(player.Inventory,"Stone",2);
  Check(WarehouseService.CountAvailableNearPlayer("Stone",-1,false)==110,"Remote-owned supply available without ownership handoff");
  stock.Reserved=remote.Reserved=true;
  Check(WarehouseService.CountAvailableNearPlayer("Stone",-1,false)==110,"Old saved leases no longer affect requirement counts");
  Check(CraftingPatches.CountItemsIncludingWarehouse(player.Inventory,"Stone",-1,false)==112,"All accessible stock plus carried materials remain available despite old markers");
  Check(CraftingPatches.FindIngredientIncludingWarehouse(player.Inventory,"Stone",2,false)==null,"Reserved quality cannot be selected");
  stock.Reserved=remote.Reserved=false;
  SharedOperations.Networked=false;
  Check(WarehouseService.CountAvailableNearPlayer("Stone",-1,false)==110,"Supply access does not depend on network mode");
  SharedOperations.Networked=true;
  SharedOperations.Executing=true;
  Check(WarehouseService.CountAvailableNearPlayer("Stone",-1,false)==110,"Execution does not require acquired ownership");
  SharedOperations.Executing=false;
  Plugin.CraftFromContainers.Value=false;
  Check(WarehouseService.CountAvailableNearPlayer("Stone",-1,false)==0,"Disabled supply contributes nothing");

  player=Reset();bag=player.Inventory;var chest=new Container{Inventory=new Inventory(1,1),InUse=true};
  var hotbar=Put(bag,"Wood",4,0,0);var equip=Put(bag,"Wood",4,0,1);equip.m_equipped=true;
  var quest=Put(bag,"Wood",4,1,1);quest.m_shared.m_questItem=true;
  var quick=Put(bag,"Wood",4,2,1);SlotApi.Protected.Add((2,1));
  var ordinary=Put(bag,"Wood",20,3,1);Put(chest.Inventory,"Wood",45);
  Check(WarehouseService.StoreAllInOpenChest(player,chest)==5&&ordinary.m_stack==15,"Partial deposit into open chest");
  Check(new[]{hotbar,equip,quest,quick}.All(i=>i.m_stack==4),"Hotbar, equipment, quest and quick slots preserved");
  Check(WarehouseService.StoreAllInOpenChest(player,chest)==0,"Full destination unchanged");
  chest.Accessible=false;Check(WarehouseService.StoreAllInOpenChest(player,chest)==0,"Inaccessible destination rejected");

  player=Reset();bag=player.Inventory;
  hotbar=Put(bag,"Z",1,0,0);equip=Put(bag,"Z",1,0,1);equip.m_equipped=true;
  SlotApi.Protected.Add((1,1));quick=Put(bag,"Z",1,1,1);
  var z=Put(bag,"Z",1,2,1);var a=Put(bag,"A",1,3,1);
  Check(WarehouseService.SortInventory(bag,true,true),"Ordinary cells sorted");
  Check(a.m_gridPos.x==2&&z.m_gridPos.x==3&&hotbar.m_gridPos.y==0&&equip.m_gridPos.x==0&&quick.m_gridPos.x==1,"Protected cells never move or get overwritten");
  Check(!WarehouseService.SortInventory(bag,true,true)&&bag.Notifications==1,"Repeated sorting is stable with one notification");
  var malformed=new Inventory(1,1);Put(malformed,"A",1);Put(malformed,"B",1);
  Check(!WarehouseService.SortInventory(malformed,false)&&malformed.Notifications==0,"Malformed positions left untouched");

  player=Reset();stock=Store("Wood",10);Put(player.Inventory,"Wood",2);
  Check(CraftingPatches.CountItemsIncludingWarehouse(player.Inventory,"Wood",-1,false)==12,"UI combines local and stored materials");
  CraftingPatches.LocalCountsOnly=true;
  Check(CraftingPatches.CountItemsIncludingWarehouse(player.Inventory,"Wood",-1,false)==2,"Native carried-only check cannot count warehouse stock");
  CraftingPatches.LocalCountsOnly=false;
  Check(CraftingPatches.CountItemsIncludingWarehouse(stock.Inventory,"Wood",-1,false)==10,"Foreign inventory never augmented");
  Plugin.Enabled.Value=false;Check(CraftingPatches.CountItemsIncludingWarehouse(player.Inventory,"Wood",-1,false)==2,"Disabled plugin uses native count");Plugin.Enabled.Value=true;
  Check(typeof(CraftingPatches).GetMethod("SpendRecipe",BindingFlags.Static|BindingFlags.NonPublic)==null
    &&typeof(CraftingPatches).GetMethod("CoverShortfall",BindingFlags.Static|BindingFlags.NonPublic)==null,"No unsafe post-output payment fallback remains");

  player=Reset();stock=Store("Fish",6);stock.Owned=false;
  Check(CraftingPatches.FindIngredientIncludingWarehouse(player.Inventory,"Fish",1,false)!=null,"Alternative ingredient selection includes remote-owned storage");
  SharedOperations.Executing=true;
  Check(CraftingPatches.FindIngredientIncludingWarehouse(player.Inventory,"Fish",1,false)!=null,"Alternative ingredient can use a remote-owned chest");
  stock.Owned=true;
  Check(CraftingPatches.FindIngredientIncludingWarehouse(player.Inventory,"Fish",1,false)!=null,"Alternative ingredient remains available after ownership changes");
  SharedOperations.Executing=false;
  var originalMethod=typeof(Inventory).GetMethod("CountItems");var il=new DynamicMethod("labels",typeof(void),Type.EmptyTypes).GetILGenerator();
  var countCall=new CodeInstruction(OpCodes.Callvirt,originalMethod);countCall.labels.Add(il.DefineLabel());countCall.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
  var untouched=new CodeInstruction(OpCodes.Nop);
  foreach(var route in new[]{"RecipeCounts","BuildingCounts","DisplayCounts"}){
   var result=((IEnumerable<CodeInstruction>)Call(route,new List<CodeInstruction>{untouched,countCall})).ToArray();
   Check(result[0].opcode==OpCodes.Nop&&result[1].opcode==OpCodes.Call&&((MethodInfo)result[1].operand).Name=="CountItemsIncludingWarehouse",route+" routes only item counts");
   Check(result[1].labels.SequenceEqual(countCall.labels)&&result[1].blocks.SequenceEqual(countCall.blocks)&&countCall.opcode==OpCodes.Callvirt,route+" preserves branch/exception metadata and input instructions");
  }
  Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0,3),"Hidden equipment rows protected");
  SlotApi.Throw=true;Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(0,1),"Broken optional API fails closed");int calls=SlotApi.Calls;
  Check(ExternalSlotCompatibility.IsProtectedPlayerSlot(1,1)&&SlotApi.Calls==calls&&Plugin.Log.Warnings==1,"Broken API is latched without repeated exceptions/logs");
  var rejectedSource=new Inventory(6,4);var rejectedTarget=new Inventory(6,4);var rejected=Put(rejectedSource,"Stone",5);
  InventoryTransfers.Admission=(inv,item)=>inv!=rejectedTarget;
  Check(InventoryTransfers.Move(rejectedSource,rejectedTarget,rejected,5,false)==0&&rejected.m_stack==5&&rejectedSource.ContainsItem(rejected)&&rejectedTarget.NrOfItems()==0,"Hard admission rejects transfers without consuming source");
  Check(InventoryTransfers.AddCopy(rejectedTarget,rejected,5,false)==0,"Hard admission rejects output copies");
  bool consumed=false;Check(!InventoryTransfers.ReceiveOne(rejectedTarget,rejected,()=>{consumed=true;return true;})&&!consumed,"Hard admission rejects world pickup before consuming item");
  InventoryTransfers.Admission=null;
  // A queued parcel is the sole source on every retry; delivered items are removed.
var mail=new Inventory(4,2);var inbox=new Inventory(1,1);var envelope=Put(mail,"Wood",70);envelope.m_shared.m_maxStackSize=50;
int first=InventoryTransfers.Move(mail,inbox,envelope,70,false);
Check(first==50&&mail.GetAllItems().Sum(i=>i.m_stack)==20,"partial mail delivery retains only remainder");
Check(InventoryTransfers.Move(mail,inbox,envelope,20,false)==0&&mail.GetAllItems().Sum(i=>i.m_stack)==20,"full destination retry does not duplicate or discard mail");
inbox.GetAllItems().Clear();var saved=mail.GetAllItems().Select(i=>i.Clone()).ToArray();var restarted=new Inventory(4,2);restarted.GetAllItems().AddRange(saved);
Check(InventoryTransfers.Move(restarted,inbox,saved[0],20,false)==20&&restarted.NrOfItems()==0,"reloaded parcel delivers its remaining cargo once");
Check(InventoryTransfers.Move(restarted,inbox,saved[0],20,false)==0,"completed retry is idempotent");
Check(MailRoute.Cancel(MailPhase.Waiting)==MailPhase.Returning&&MailRoute.Cancel(MailPhase.Delivered)==MailPhase.Delivered,"cancel cannot reactivate delivered mail");
Check(MailRoute.Finish(MailPhase.Returning,1)==MailPhase.Returning&&MailRoute.Finish(MailPhase.Returning,0)==MailPhase.Returned,"partial cancellation waits until all reserved cargo returns");
var absent=new ExternalStorageRules(null,null);
Check(!absent.Recognizes("dynamic_wood","dynamic_wood")&&absent.Allows("dynamic_wood","Iron"),"absent optional mods leave native storage unchanged");
var assembly=typeof(Program).Assembly;
var external=new ExternalStorageRules(assembly,assembly);
Check(external.Recognizes("OH_Berries","$barrel_berries")&&external.Recognizes("dynamic_wood","localized wood"),"barrel name and pile prefab recognition");
Check(!external.Recognizes("ship","boat_storage"),"unrelated containers not recognized");
Check(external.Assigned("modded_ore_pile","ore").Single()=="ModOre","modded piles use live metadata");
Check(external.Allows("$barrel_berries","Raspberry")&&!external.Allows("$barrel_berries","Iron"),"barrel hard restriction");
OdinsFoodBarrels.RestrictContainers.Items["$seed_bag"]=new(){"ModSeed"};
Check(external.Assigned("bag","$seed_bag").Single()=="ModSeed"&&!external.Allows("$seed_bag","CarrotSeeds"),"changed seed allowlist takes effect without restart");
var from=new Inventory(2,1);var pile=new Inventory(2,1);var iron=Put(from,"Iron",5);
InventoryTransfers.Admission=(inv,item)=>inv!=pile||external.Allows("dynamic_wood",InventoryTransfers.ItemId(item));
Check(InventoryTransfers.Move(from,pile,iron,5,false)==0&&iron.m_stack==5&&pile.NrOfItems()==0&&from.Notifications==0,"rejected pile move leaves source untouched");
DynamicStoragePiles.RestrictContainers.Restricted=false;
Check(external.Recognizes("dynamic_wood","dynamic_wood")&&external.Assigned("dynamic_wood","dynamic_wood").Single()=="Wood","unrestricted piles retain identity and default assignment");
Check(InventoryTransfers.Move(from,pile,iron,5,false)==5&&from.NrOfItems()==0&&pile.Notifications==1,"unrestricted move emits native visual and save notification");
DynamicStoragePiles.RestrictContainers.Restricted=true;
var wood=Put(from,"Wood",7);
Check(InventoryTransfers.Move(from,pile,wood,7,false)==7&&pile.Notifications==2,"matching material transfers with restriction enabled");
InventoryTransfers.Admission=null;
Console.WriteLine($"PASS: {checks} storage core, crafting hooks and equipment-slot regressions.");
 }
}
