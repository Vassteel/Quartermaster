using Quartermaster;
int checks=0;
void Check(bool c,string n){checks++;if(!c)throw new Exception(n);}
(Player,InventoryGui,Container) Setup(bool networked){CraftingPatches.LocalCountsOnly=false;ContainerRegistry.All.Clear();UnityEngine.Time.unscaledTime+=3;var p=new Player();Player.m_localPlayer=p;var c=new Container();c.View.Owner=!networked;c.Inventory.GetAllItems().Add(new(){m_shared=new(){m_name="Iron"},m_dropPrefab=new(){name="Iron"},m_stack=20});ContainerRegistry.All.Add(c);var gui=new InventoryGui{m_craftRecipe=new(){m_resources=new[]{new Piece.Requirement{m_resItem=new(){m_itemData=new(){m_shared=new(){m_name="Iron"}}},m_amount=3,m_amountPerLevel=5}}},m_craftUpgradeItem=new(){m_quality=1}};return(p,gui,c);}
foreach(bool networked in new[]{false,true})
{
 var (p,g,c)=Setup(networked);c.View.Unavailable=true;
 Check(CraftingPatches.CountItemsIncludingWarehouse(p.Inventory,"Iron",-1,true)==0,"unavailable iron excluded from displayed requirements");
 Check(!p.HaveRequirements(g.m_craftRecipe,false,2,1),"unavailable iron fails requirements before click");
 Check(CraftingPatches.FindIngredientIncludingWarehouse(p.Inventory,"Iron",1,false)==null,"unavailable ingredient cannot be selected");
 g.DoCrafting(p);
 Check(g.Crafted==0&&g.m_craftUpgradeItem.m_quality==1&&c.Inventory.GetAllItems()[0].m_stack==20,"unavailable iron cannot fund unpaid upgrade");
 Check(p.Messages.Count>0,"blocked upgrade explains busy materials");
 (p,g,c)=Setup(networked);g.DoCrafting(p);
 Check(g.Crafted==1&&g.m_craftUpgradeItem.m_quality==2&&InventoryTransfers.CountType(c.Inventory,"Iron")==15&&InventoryTransfers.CountType(p.Inventory,"Iron")==0,"upgrade pays all five iron before success");
 Check(!CraftingPatches.LocalCountsOnly,"craft scope restored");
 (p,g,c)=Setup(networked);g.m_craftUpgradeItem=null;g.m_multiCrafting=true;g.m_multiCraftAmount=3;g.DoCrafting(p);
 Check(g.Crafted==1&&InventoryTransfers.CountType(c.Inventory,"Iron")==11,"multicraft pays multiplied deficit");
 (p,g,c)=Setup(networked);p.Inventory=new("small",null,1,1);p.Inventory.GetAllItems().Add(new(){m_shared=new(){m_name="Other"},m_dropPrefab=new(){name="Other"},m_stack=1});g.DoCrafting(p);
 Check(g.Crafted==0&&InventoryTransfers.CountType(c.Inventory,"Iron")==20,"full inventory never upgrades or charges");
 (p,g,c)=Setup(networked);var piece=new Piece{m_resources=g.m_craftRecipe.m_resources};p.Selected=piece;c.View.Unavailable=true;
 Check(!p.HaveRequirements(piece,Player.RequirementMode.CanBuild),"unavailable building material fails ghost requirements");
 var placed=(bool)Harness.Call("TrySharedPlacement",new object[]{p,piece});
 Check(!placed&&p.Built==0&&p.Messages.Count>0,"unavailable building materials give visible failure without placing");
}
Console.WriteLine($"PASS: {checks} craft/build payment checks (actual prefixes, simulated native inventory).");

// Two client-side container copies sharing synchronized serialized state.
// This models delivered network updates; it does not simulate concurrent writes.
{
 var (a,ga,ca)=Setup(true);
 ca.View.z.Saved=ca.Inventory.GetAllItems().Select(i=>i.Clone()).ToList();
 var b=new Player();
 var cb=new Container();cb.View.z=ca.View.z;cb.View.Owner=false;
 cb.Inventory.GetAllItems().Clear();
 Player.m_localPlayer=b;ContainerRegistry.All.Clear();ContainerRegistry.All.Add(cb);
 Check(CraftingPatches.CountItemsIncludingWarehouse(b.Inventory,"Iron",-1,true)==20,"B counts A-owned chest without opening it");
 ga.DoCrafting(b);
 Check(ga.Crafted==1&&cb.Saves==1&&ca.View.z.Saved.Sum(i=>i.m_stack)==15,"B upgrade explicitly saves five iron withdrawn from A-owned chest");
 Check(!cb.View.Owner,"crafting does not claim chest ownership");
 ca.View.Owner=false;cb.View.Owner=true; // B opens/closes; A is now non-owner.
 Player.m_localPlayer=a;ContainerRegistry.All.Clear();ContainerRegistry.All.Add(ca);
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==15,"A sees remaining stock after ownership switches to B");
 ga.m_craftUpgradeItem.m_quality=1;ga.DoCrafting(a);
 Check(ga.Crafted==2&&ca.View.z.Saved.Sum(i=>i.m_stack)==10,"A can also upgrade after B opens chest, paying five iron");
 ca.Open=true;
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"open chest excluded");
 ca.Open=false;ca.View.z.InUse=1;
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"remote open marker excluded");
 ca.View.z.InUse=0;SharedChests.ViewedBy.Add(ca);
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"chest shared-open by other players excluded");
 SharedChests.ViewedBy.Clear();ca.Denied=true;
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"access denied chest excluded");
 ca.Denied=false;ca.Supply=false;
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"supply disabled chest excluded");
 ca.Supply=true;ca.transform.position.x=101;
 Check(CraftingPatches.CountItemsIncludingWarehouse(a.Inventory,"Iron",-1,true)==0,"out of range chest excluded");
}
{
 var(p,g,c)=Setup(true);c.View.Owner=false;
 var piece=new Piece{m_resources=g.m_craftRecipe.m_resources};p.Selected=piece;
 var placed=(bool)Harness.Call("TrySharedPlacement",new object[]{p,piece});
 Check(placed&&p.Built==1,"foreign-owned materials fund placement on first click");
 Check(c.Saves==1&&c.View.z.Saved.Sum(i=>i.m_stack)==17,"building withdrawal saved before native placement");
 Check(CraftingPatches.LocalCountsOnly,"native placement payment remains player-only");
 // Simulate the rest of native UpdatePlacement's consumption and finalizer.
 p.Inventory.RemoveItem("Iron",3,-1,true);
 Harness.Call("ReleasePlacement",new object[]{null,false});
 Check(InventoryTransfers.CountType(p.Inventory,"Iron")==0&&!CraftingPatches.LocalCountsOnly,"build payment consumes exact fetched amount and restores scope");
}
{
 var(p,g,c)=Setup(true);
 Check(CraftingPatches.CountItemsIncludingWarehouse(p.Inventory,"Iron",-1,true)==20,"initial stock visible");
 c.View.z.Saved=c.Inventory.GetAllItems().Select(i=>i.Clone()).ToList();
 c.View.z.Saved[0].m_stack=2;
 g.DoCrafting(p);
 Check(g.Crafted==0&&c.Saves==0&&InventoryTransfers.CountType(p.Inventory,"Iron")==0,"new synchronized shortage prevents upgrade before any withdrawal");
 (p,g,c)=Setup(true);
 var carried=c.Inventory.GetAllItems()[0].Clone();carried.m_stack=2;p.Inventory.GetAllItems().Add(carried);
 g.DoCrafting(p);
 Check(g.Crafted==1&&c.View.z.Saved.Sum(i=>i.m_stack)==17&&InventoryTransfers.CountType(p.Inventory,"Iron")==0,"player inventory pays first; only deficit saved to remote chest");
}
Console.WriteLine($"PASS: {checks} total craft/build checks, including two-client synchronized-state regression.");

BepInEx.Bootstrap.Chainloader.PluginInfos[PlantEasilyCompatibility.Guid]=new(){Instance=new Advize_PlantEasily.PlantEasily()};
PlantEasilyCompatibility.Initialize(new HarmonyLib.Harmony());
(Player,Piece,Container) PlantSetup(int stock,int cells,bool whole=false)
{
 var(p,g,c)=Setup(true);c.Inventory.GetAllItems()[0].m_stack=stock;
 var piece=new Piece{IsPlant=true,m_resources=g.m_craftRecipe.m_resources};p.Selected=piece;
 Advize_PlantEasily.ModContext.config=new(){PreventPartialPlanting=whole};
 Advize_PlantEasily.GhostGrid.ExtraGhosts=Enumerable.Range(1,cells-1).Select(i=>new UnityEngine.GameObject{transform=new(){position=new(){x=i}}}).ToList();
 Advize_PlantEasily.GhostGrid.GhostPlacementStatus=Enumerable.Repeat(Advize_PlantEasily.Status.Healthy,cells).ToList();
 Advize_PlantEasily.GhostStatus.Geometry.Clear();
 return(p,piece,c);
}
int SelectedPlants()=>Advize_PlantEasily.GhostGrid.GhostPlacementStatus.Count(s=>s==Advize_PlantEasily.Status.Healthy);
{
 var(p,piece,c)=PlantSetup(6,1000);
 var positions=Advize_PlantEasily.GhostGrid.ExtraGhosts.Select(g=>g.transform.position.x).ToArray();
 Check(PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out var funded)&&funded,"1000-cell group can fund partial batch");
 Check(SelectedPlants()==2,"two plants of resources never enables 1000 plants");
 Check(InventoryTransfers.CountType(p.Inventory,"Iron")==6&&InventoryTransfers.CountType(c.Inventory,"Iron")==0&&c.Saves==1,"whole affordable batch fetched and saved once");
 int planted=SelectedPlants();for(int i=0;i<planted;i++)p.Inventory.RemoveItem("Iron",3,-1,true);
 Check(planted==2&&InventoryTransfers.CountType(p.Inventory,"Iron")==0,"planting mod consumption spends exactly two plants, without double charge");
 Check(positions.SequenceEqual(Advize_PlantEasily.GhostGrid.ExtraGhosts.Select(g=>g.transform.position.x)),"funding never moves planting ghosts");
}
{
 var(p,piece,c)=PlantSetup(6,1000,true);
 Check(PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out var funded)&&!funded,"whole-grid setting rejects underfunded group");
 Check(c.Saves==0&&InventoryTransfers.CountType(c.Inventory,"Iron")==6&&InventoryTransfers.CountType(p.Inventory,"Iron")==0,"rejected whole group does not withdraw or charge");
 (p,piece,c)=PlantSetup(6,2,true);
 Check(PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out funded)&&funded&&SelectedPlants()==2,"fully funded all-or-nothing batch succeeds");
}
{
 var(p,piece,c)=PlantSetup(6,4);
 Advize_PlantEasily.GhostStatus.Geometry[Advize_PlantEasily.GhostGrid.ExtraGhosts[0]]=Advize_PlantEasily.Status.NoSpace;
 PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out var funded);
 Check(funded&&SelectedPlants()==2&&Advize_PlantEasily.GhostGrid.GhostPlacementStatus[1]!=Advize_PlantEasily.Status.Healthy&&Advize_PlantEasily.GhostGrid.GhostPlacementStatus[2]==Advize_PlantEasily.Status.Healthy,"Plant Easily geometry excludes blocked positions without spending their cost");
 (p,piece,c)=PlantSetup(12,4,true);
 Advize_PlantEasily.GhostStatus.Geometry[Advize_PlantEasily.GhostGrid.ExtraGhosts[0]]=Advize_PlantEasily.Status.NoSpace;
 PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out funded);
 Check(!funded&&c.Saves==0,"whole-grid setting respects Plant Easily invalid geometry");
 (p,piece,c)=PlantSetup(12,4);
 Advize_PlantEasily.ModContext.config.PreventInvalidPlanting=false;
 Advize_PlantEasily.GhostStatus.Geometry[Advize_PlantEasily.GhostGrid.ExtraGhosts[0]]=Advize_PlantEasily.Status.NoSpace;
 PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out funded);
 Check(funded&&InventoryTransfers.CountType(p.Inventory,"Iron")==12,"allow-invalid mode still funds each requested position");
}
{
 var(p,piece,c)=PlantSetup(2,1000);
 PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out var funded);
 Check(!funded&&c.Saves==0,"not enough for one plant produces no funded batch");
 (p,piece,c)=PlantSetup(6,2);p.Inventory=new("full",null,1,1);p.Inventory.GetAllItems().Add(new(){m_shared=new(){m_name="Other"},m_stack=1});
 PlantEasilyCompatibility.TryPrepare(p,piece,p.m_placementGhost,out funded);
 Check(!funded&&c.Saves==0,"full player inventory prevents bulk planting and withdrawal");
}
{
 var(p,piece,c)=PlantSetup(6,2);
 var replant=typeof(PlantEasilyCompatibility).GetMethod("FundedReplant",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
 Check((bool)replant.Invoke(null,new object[]{p,piece,Player.RequirementMode.CanBuild}),"replant funded before allowing spawn");
 Check(InventoryTransfers.CountType(p.Inventory,"Iron")==3&&InventoryTransfers.CountType(c.Inventory,"Iron")==3,"replant fetches only one plant cost");
 p.Inventory.RemoveItem("Iron",3,-1,true);
 c.Inventory.GetAllItems().Clear();c.View.z.Saved.Clear();
 Check(!(bool)replant.Invoke(null,new object[]{p,piece,Player.RequirementMode.CanBuild}),"unfunded replant cannot spawn");
 Check(!CraftingPatches.LocalCountsOnly,"replant restores counting scope");
}
Console.WriteLine($"PASS: {checks} total checks including Plant Easily batch and replant contracts.");

{
 var(p,piece,c)=PlantSetup(6,1000);
 p.PlaceExtras=placedPiece=>{for(int i=1;i<SelectedPlants();i++){p.Built++;p.Inventory.RemoveItem("Iron",3,-1,true);}};
 Check((bool)Harness.Call("TrySharedPlacement",new object[]{p,piece}),"real Quartermaster placement hook routes to batch adapter");
 p.Inventory.RemoveItem("Iron",3,-1,true); // Native root payment follows PlacePiece's postfix.
 Harness.Call("ReleasePlacement",new object[]{null,false});
 Check(p.Built==2&&InventoryTransfers.CountType(p.Inventory,"Iron")==0&&InventoryTransfers.CountType(c.Inventory,"Iron")==0,"root plus extras consume funded batch once through placement scope");
 (p,piece,c)=PlantSetup(6,1000,true);
 Check(!(bool)Harness.Call("TrySharedPlacement",new object[]{p,piece})&&p.Built==0&&c.Saves==0,"unfunded whole batch never reaches root placement");
}
Console.WriteLine($"PASS: {checks} final craft, build and planting checks.");
