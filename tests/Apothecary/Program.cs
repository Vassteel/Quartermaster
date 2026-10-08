using Quartermaster;
using UnityEngine;
using Jotunn.Managers;
int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
Apothecary.Initialize();PrefabManager.Fire();
Check(Plugin.Log.Errors.Count==0,string.Join("\n",Plugin.Log.Errors));
var items=ItemManager.Instance.Items;var pieces=PieceManager.Instance.Pieces;
Check(items.Count==4&&pieces.Count==Apothecary.All.Count()+1,"four components, one kiln and the complete cabinet catalog");
Check(pieces.Where(p=>p.Config.Enabled).Select(p=>p.Prefab.name).OrderBy(x=>x).SequenceEqual(Apothecary.All.Select(d=>d.Prefab).Append(Apothecary.Kiln).OrderBy(x=>x)),"All released furniture and the pottery kiln are buildable");
foreach(var def in Apothecary.All.Except(Apothecary.Cabinets)){
 var restored=pieces.Single(p=>p.Prefab.name==def.Prefab);
 Check(restored.Config.Enabled&&BuildMenuCategory.Registered.Contains(def.Prefab),"Restored furniture has a build recipe and Quartermaster category");
 Check(restored.Config.Requirements.All(r=>r.Recover),"Restored furniture retains dismantle refunds");
 Check(restored.Prefab.GetComponent<Container>().m_width*restored.Prefab.GetComponent<Container>().m_height==def.Capacity,"Restored saved containers keep their slot count");
}
Check(items.Select(x=>x.Prefab.name).Distinct().Count()==4,"all item identities unique");
foreach(var item in items){Check(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab==item.Prefab,"routing/drop identity is own prefab");Check(item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize==500,"each component honors stack policy");}
Check(items.Single(i=>i.Prefab.name==Apothecary.Unfired).Config.Requirements.Single().Item==ClayResource.ItemName,"unfired jar consumes original raw clay");
Check(items.Single(i=>i.Prefab.name==Apothecary.Blank).Config.Requirements.Single().Item=="Crystal","flask blank consumes native crystal");
Check(items.Count(i=>i.Config!=null)==2,"finished jars and flasks require kiln conversion, no bypass craft recipe");
var kiln=pieces.Single(p=>p.Prefab.name==Apothecary.Kiln).Prefab.GetComponent<Smelter>();
Check(kiln.m_conversion.Count==2,"kiln contains only pottery conversions");
Check(kiln.m_conversion[0].m_from.name==Apothecary.Unfired&&kiln.m_conversion[0].m_to.name==Apothecary.Jar,"correct clay chain");
Check(kiln.m_conversion[1].m_from.name==Apothecary.Blank&&kiln.m_conversion[1].m_to.name==Apothecary.Flask,"correct crystal chain");
Check(kiln.m_maxFuel==10&&kiln.m_fuelPerProduct==2&&kiln.m_secPerProduct==45,"bounded native fuel and timer parameters");
foreach(var def in Apothecary.Cabinets){var p=pieces.Single(p=>p.Prefab.name==def.Prefab);var c=p.Prefab.GetComponent<Container>();Check(c.m_width==def.Columns&&c.m_height==def.Rows,"native inventory size matches visual cells");Check(p.Config.Requirements.Single(r=>r.Item==def.Jar).Amount==def.Capacity,"recipe supplies all physical jars");Check(p.Config.Requirements.All(r=>r.Recover),"native dismantling refunds components");Check(BuildMenuCategory.Registered.Contains(def.Prefab),"current usage-based Hammer category includes cabinet");}
foreach(var def in PantryStorage.Pieces.Where(d=>d.Handling==FurnitureHandling.Hanging))
{
    var p=pieces.Single(p=>p.Prefab.name==def.Prefab);
    Check(def.Capacity==1&&def.Mount==CabinetMount.Ceiling,"individual ceiling hook holds one native stack");
    Check(p.Config.CraftingStation=="forge","bronze hook requires forge");
    Check(p.Config.Requirements.Length==1&&p.Config.Requirements[0].Item=="Bronze"&&p.Config.Requirements[0].Amount==2&&p.Config.Requirements[0].Recover,"hook recipe uses recoverable bronze without wood rails");
}
Check(Apothecary.Cabinets.Length==7,"five approved variations registered alongside the first pair");
Check(Apothecary.Cabinets.Select(d=>d.Prefab).Distinct().Count()==7,"stable distinct prefab IDs for saved variants");
Check(!pieces.Any(p=>p.Prefab.name.Contains("Stair"))&&!BuildMenuCategory.Registered.Any(id=>id.Contains("Stair")),"deferred under-stair prototypes are absent from registration and Hammer category");
var assetPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../assets/apothecary/model.json"));
using(var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(assetPath)))
foreach(var def in Apothecary.All.Except(Apothecary.MeadShelves).Where(d=>!d.Model.StartsWith("modular_")))
{
    var layout=document.RootElement.GetProperty("layouts").GetProperty(def.Model);
    Check(def.Columns==layout.GetProperty("columns").GetInt32()&&def.Rows==layout.GetProperty("rows").GetInt32(),"native inventory coordinates match authored model layout");
    Check(def.Capacity==document.RootElement.GetProperty("slots").GetProperty(def.Model).GetArrayLength(),"exactly one display socket for each native inventory cell");
    if(FurnitureMotion.IsRack(def.Handling))
    {
        var fit=layout.GetProperty("fit");
        Check(Math.Abs(fit[0].GetSingle()-def.DisplayWidth)<.00001f&&Math.Abs(fit[1].GetSingle()-def.DisplayHeight)<.00001f&&Math.Abs(fit[2].GetSingle()-def.DisplayDepth)<.00001f,"runtime equipment fit matches authored clearances");
        Check(def.HorizontalDisplay==layout.GetProperty("horizontal").GetBoolean(),"runtime orientation matches rack geometry");
    }
    Check(def.Capacity<=8&&def.Capacity>=(def.Handling==FurnitureHandling.Hanging||FurnitureMotion.IsRack(def.Handling)||FurnitureMotion.IsDisplay(def.Handling)?1:3),"bounded display count across small and large variants");
    Check(Math.Abs(def.Width-layout.GetProperty("width").GetSingle())<.00001f&&Math.Abs(def.Height-layout.GetProperty("height").GetSingle())<.00001f&&Math.Abs(def.Depth-layout.GetProperty("depth").GetSingle())<.00001f,"authored dimensions match runtime placement bounds");
    var piece=pieces.Single(p=>p.Prefab.name==def.Prefab).Prefab.GetComponent<Piece>();
    if(def.Mount==CabinetMount.Wall||def.Mount==CabinetMount.Ceiling)
    {
        Check(!piece.m_groundPiece&&!piece.m_groundOnly&&!piece.m_notOnWood&&!piece.m_waterPiece&&!piece.m_notOnTiltingSurface,"mounted furniture does not inherit floor/soil-only placement restrictions");
        Check(piece.m_notOnFloor&&piece.m_inCeilingOnly==(def.Mount==CabinetMount.Ceiling),"wall and rafter mounting remain distinct");
    }
    else Check(piece.m_groundOnly&&!piece.m_notOnFloor,"floor-standing styles retain template placement settings");
    if(def.Mount==CabinetMount.Slope)Check(def.DrawDistance>=.4f,"sloped roof jar pulls fully outside before lid lifts");
}
Check(Apothecary.Definition("Quartermaster_ClayCabinet").Columns==4&&Apothecary.Definition("Quartermaster_ClayCabinet").Rows==3,"clay cabinet identity retained and capacity expanded without reordering old cells");
Check(Apothecary.Definition("Quartermaster_CrystalCabinet").Columns==4&&Apothecary.Definition("Quartermaster_CrystalCabinet").Rows==3,"crystal cabinet identity retained and capacity expanded without discarding old cells");
Check(BuildMenuCategory.Registered.Contains(Apothecary.Kiln),"kiln is in Quartermaster tab");
Check(PrefabManager.Instance.Clones.All(p=>new[]{"Flint","smelter","piece_chest_wood"}.Contains(p.source)),"only native behavior templates, no external mod assets");
PrefabManager.Fire();Check(items.Count==4&&pieces.Count==Apothecary.All.Count()+1,"repeat registration cannot duplicate pieces or conversions");
Check(typeof(ChestSettings).GetMethod("Accepts",new[]{typeof(string)})!=null,"preserve original public one-argument acceptance API");
var ordinary=new GameObject("piece_chest_wood").AddComponent<Container>();
var cabinet=new GameObject(Apothecary.Cabinets[0].Prefab).AddComponent<Container>();
ItemDrop Make(string id,ItemDrop.ItemData.ItemType type=ItemDrop.ItemData.ItemType.Material){var p=new GameObject(id);var d=p.AddComponent<ItemDrop>();d.m_itemData.m_dropPrefab=p;d.m_itemData.m_shared.m_itemType=type;return d;}
var herb=Make("ModdedHerb");var meal=Make("ModdedMeal");meal.m_itemData.m_shared.m_food=20;
var sword=Make("ModdedSword",ItemDrop.ItemData.ItemType.OneHandedWeapon);
ObjectDB.instance=new();ObjectDB.instance.m_recipes.Add(new(){m_item=meal,m_resources=new[]{new Recipe.Requirement{m_resItem=herb},new Recipe.Requirement{m_resItem=sword}}});
Check(!FurnitureAssignment.Accepts(ordinary,herb.m_itemData),"ordinary empty chest retains learning defaults");
Check(FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"modded ingredient classified through recipe metadata");
Check(!FurnitureAssignment.Accepts(cabinet,sword.m_itemData),"equipment is not guessed into an apothecary cabinet");
cabinet.Settings.Forget("ModdedHerb");Check(!FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"explicit exclusion overrides category assignment");
cabinet.Settings.Restore("ModdedHerb");cabinet.Settings.AutoAssign=false;Check(FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"manual inclusion survives disabled auto assignment");
cabinet.Settings.Remembered.Clear();Check(!FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"disabled assignment does not silently route unmatched items");
cabinet.Settings.AutoAssign=true;cabinet.Settings.AutoCategories="reagents";Check(!FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"per-cabinet category restriction applies");
cabinet.Settings.AutoCategories="ingredients";Check(FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"category can be restored");
cabinet.Settings.AcceptStorage=false;Check(!FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"receive gate remains authoritative");
cabinet.Settings.AcceptStorage=true;cabinet.Settings.Deposit=true;Check(!FurnitureAssignment.Accepts(cabinet,herb.m_itemData),"deposit role remains authoritative");
ObjectDB.instance=new();Check(FurnitureAssignment.Classify("ModdedHerb")=="","classification cache resets with world database");
Plugin.StorageCategoryOverrides.Value="CustomPowder=reagents;Resin=none;BadType=unrecognized";
Check(FurnitureAssignment.Classify("CustomPowder")=="reagents"&&FurnitureAssignment.Classify("Resin")=="","explicit server rules can add modded items or remove defaults");
Check(FurnitureAssignment.Classify("BadType")=="","unknown category values do not become matches");
Plugin.StorageCategoryOverrides.Value="CustomPowder=ingredients";
Check(FurnitureAssignment.Classify("CustomPowder")=="ingredients","server config change invalidates the cache");
// Bulk categories use native material relationships without changing legacy chest acceptance.
Plugin.StorageCategoryOverrides.Value="";ZNetScene.instance=new();
var coal=Make("Coal");var ore=Make("CopperOre");var copper=Make("Copper");
var metalFurnace=new GameObject("smelter").AddComponent<Smelter>();metalFurnace.m_fuelItem=coal;
metalFurnace.m_conversion.Add(new(){m_from=ore,m_to=copper});
metalFurnace.m_conversion.Add(new(){m_from=Make("ModMetalOre"),m_to=Make("ModMetal")});
metalFurnace.m_conversion.Add(new(){m_from=Make("UnfiredPot"),m_to=Make("FiredPot")});
metalFurnace.m_conversion.Add(new(){m_from=Make("SwordOre"),m_to=Make("Sword",ItemDrop.ItemData.ItemType.OneHandedWeapon)});
ZNetScene.instance.m_prefabs.Add(metalFurnace.gameObject);
Check(FurnitureAssignment.Classify("CopperOre")=="ores"&&FurnitureAssignment.Classify("Copper")=="ingots","native metal conversion separates raw and finished materials");
Check(FurnitureAssignment.Classify("ModMetalOre")=="ores"&&FurnitureAssignment.Classify("ModMetal")=="ingots","exact modded ore pair supports conservative metal inference");
Check(FurnitureAssignment.Classify("FiredPot")==""&&FurnitureAssignment.Classify("SwordOre")=="","pottery and weapon conversion cannot become metals");
Check(!FurnitureAssignment.Accepts(ordinary,coal.m_itemData),"ordinary chest remains opt-in for coal");
foreach(var def in BulkStorage.Pieces)
{
    var piece=pieces.Single(p=>p.Prefab.name==def.Prefab);var chest=piece.Prefab.GetComponent<Container>();
    Check(BuildMenuCategory.Registered.Contains(def.Prefab)&&chest.m_width*chest.m_height==def.Capacity,"bulk furniture uses the native inventory and Hammer registration");
    Check(piece.Config.Requirements.All(r=>r.Amount>0&&r.Recover)&&piece.Config.Requirements.All(r=>r.Item=="Wood"||r.Item=="FineWood"),"bulk recipes have positive native wood costs and refunds");
    var item=def.Category=="lumber"?Make("Wood"):def.Category=="ingots"?copper:def.Category=="ores"?ore:coal;
    Check(FurnitureAssignment.Accepts(chest,item.m_itemData),"empty furniture immediately accepts its intended material");
    Check(!FurnitureAssignment.Accepts(chest,herb.m_itemData),"bulk category does not inherit apothecary defaults");
    FurnitureAssignment.Cycle(chest,chest.Settings);Check(!chest.Settings.AutoAssign&&!FurnitureAssignment.Accepts(chest,item.m_itemData),"bulk auto-assignment can be disabled");
    chest.Settings.Remembered.Add(item.name);Check(FurnitureAssignment.Accepts(chest,item.m_itemData),"learning remains authoritative with bulk auto-assignment off");
    FurnitureAssignment.Cycle(chest,chest.Settings);chest.Settings.Forget(item.name);Check(!FurnitureAssignment.Accepts(chest,item.m_itemData),"bulk ignored item defeats default category");
}
Plugin.StorageCategoryOverrides.Value="ModLog=lumber;ModBar=ingots;ModChunk=ores;ModFuel=coal;CopperOre=none";
Check(FurnitureAssignment.Classify("ModLog")=="lumber"&&FurnitureAssignment.Classify("ModBar")=="ingots"&&FurnitureAssignment.Classify("ModChunk")=="ores"&&FurnitureAssignment.Classify("ModFuel")=="coal","server overrides support each bulk family");
Check(FurnitureAssignment.Classify("CopperOre")=="","explicit none overrides inferred ore category");
Check(BulkPresentation.Model("ores","IronScrap",2)=="scrap_load_2"&&BulkPresentation.Model("ores","CopperOre",2)=="ores_load_2","scrap and raw ore have distinct representative silhouettes");
Check(BulkPresentation.Level(0,500)==0&&BulkPresentation.Level(1,500)==1&&BulkPresentation.Level(125,500)==1&&BulkPresentation.Level(126,500)==2&&BulkPresentation.Level(331,500)==3,"empty and threshold boundaries stay bounded at custom stack sizes");
Check(BulkPresentation.Level(int.MaxValue,1)==3&&BulkPresentation.Level(1,0)==3,"overflow and invalid maximum cannot exceed display budget");
foreach(var style in new[]{FurnitureHandling.Lumber,FurnitureHandling.Ingot,FurnitureHandling.Bin})
{
    Check(FurnitureMotion.Contact(style)>0&&FurnitureMotion.Contact(style)<FurnitureMotion.Duration(style)&&FurnitureMotion.Duration(style)<5.2f,"bulk gestures contact before finishing and are shorter than jars");
    Check(FurnitureMotion.Lean(style,0)==0&&FurnitureMotion.Lean(style,FurnitureMotion.Duration(style))==0,"bulk handling resets pose at both ends");
}
// Soft materials require both material metadata and conservative family naming.
Plugin.StorageCategoryOverrides.Value="";
var softItems=new[]{Make("DeerHide"),Make("BjornHide"),Make("ModdedPelt"),Make("LeatherScraps"),Make("Leatherstraps"),Make("LinenThread"),Make("ModdedCloth"),Make("JuteRed"),Make("Feathers"),Make("CelestialFeather"),Make("BoneFragments"),Make("CharredBone"),Make("ModdedBone")};
ObjectDB.instance.m_items.AddRange(softItems.Select(i=>i.gameObject));
foreach(var id in new[]{"DeerHide","BjornHide","ModdedPelt","LeatherScraps","Leatherstraps"})Check(FurnitureAssignment.Classify(id)=="hides","native and modded material skins classified without listing every species");
foreach(var id in new[]{"LinenThread","ModdedCloth","JuteRed"})Check(FurnitureAssignment.Classify(id)=="textiles","textile materials classified");
Check(FurnitureAssignment.Classify("Feathers")=="feathers"&&FurnitureAssignment.Classify("CelestialFeather")=="feathers","both feather resources classified");
foreach(var id in new[]{"BoneFragments","CharredBone","ModdedBone"})Check(FurnitureAssignment.Classify(id)=="bones","bone materials classified");
foreach(var id in new[]{"WeaponPelt","ShieldHide","BowThread","SpearFeather","SwordBone"})ObjectDB.instance.m_items.Add(Make(id,ItemDrop.ItemData.ItemType.OneHandedWeapon).gameObject);
foreach(var id in new[]{"WeaponPelt","ShieldHide","BowThread","SpearFeather","SwordBone"})Check(FurnitureAssignment.Classify(id)=="weapons","equipment names cannot trigger soft-material routing");
ObjectDB.instance.m_items.Add(Make("LateModHide").gameObject);Check(FurnitureAssignment.Classify("LateModHide")=="hides","late item registrations invalidate family cache");
Check(SoftStorage.Pieces.Length==6&&Apothecary.All.Select(d=>d.Prefab).Distinct().Count()==Apothecary.All.Count(),"six distinct new pieces without replacing approved IDs");
foreach(var def in SoftStorage.Pieces)
{
 var p=pieces.Single(p=>p.Prefab.name==def.Prefab);var chest=p.Prefab.GetComponent<Container>();
 Check(BuildMenuCategory.Registered.Contains(def.Prefab)&&chest.m_width*chest.m_height==def.Capacity,"soft furniture has Hammer registration and native inventory dimensions");
 Check(p.Config.Requirements.All(r=>r.Amount>0&&r.Recover),"soft furniture has refundable positive crafting requirements");
 var item=softItems.First(i=>FurnitureAssignment.Classify(i.name)==def.Category);
 Check(FurnitureAssignment.Accepts(chest,item.m_itemData),"new furniture auto-assigns matching soft materials");
 chest.Settings.Forget(item.name);Check(!FurnitureAssignment.Accepts(chest,item.m_itemData),"ignored soft material defeats auto assignment");
 chest.Settings.Forgotten.Clear();FurnitureAssignment.Cycle(chest,chest.Settings);Check(!FurnitureAssignment.Accepts(chest,item.m_itemData),"soft furniture auto-assignment can be disabled");
 chest.Settings.Remembered.Add(item.name);Check(FurnitureAssignment.Accepts(chest,item.m_itemData),"manual teaching remains available with auto assignment off");
}
Check(BulkPresentation.Model("hides","DeerHide",3,"hide_rail")=="hanging_hides_load_3"&&BulkPresentation.Model("hides","DeerHide",3,"hide_shelf")=="hides_load_3","hanging and rolled hides use furniture-specific load geometry");
Check(BulkPresentation.Model("textiles","LinenThread",2)=="thread_load_2"&&BulkPresentation.Model("textiles","JuteRed",2)=="textiles_load_2","thread hanks differ from folded cloth");
Plugin.StorageCategoryOverrides.Value="LateModHide=none;LeatherScraps=textiles;BoneFragments=reagents";
Check(FurnitureAssignment.Classify("LateModHide")==""&&FurnitureAssignment.Classify("LeatherScraps")=="textiles"&&FurnitureAssignment.Classify("BoneFragments")=="reagents","server overrides win over inferred soft-material families");
for(float t=-.5f;t<5;t+=.05f)
{
 Check(FurnitureMotion.Cover(t)>=0&&FurnitureMotion.Cover(t)<=68.001f,"coffer hinge is bounded");
 foreach(var style in new[]{FurnitureHandling.Hide,FurnitureHandling.Textile,FurnitureHandling.Feather,FurnitureHandling.Bone})Check(FurnitureMotion.Press(style,t)>=0&&FurnitureMotion.Press(style,t)<=.0751f,"soft material deformation remains subtle and bounded");
}
Check(FurnitureMotion.Cover(0)==0&&FurnitureMotion.Cover(FurnitureMotion.Duration(FurnitureHandling.Feather))==0,"coffer starts and finishes closed");
Check(FurnitureMotion.Cover(FurnitureMotion.Contact(FurnitureHandling.Feather))>60,"coffer lid is open before a feather is placed");
for(float t=-1;t<7;t+=.02f){var p=ApothecaryMotion.Sample(t);Check(float.IsFinite(p.Draw)&&p.Draw>=0&&p.Draw<=.231f&&p.Lid>=0&&p.Lid<=.141f,"bounded finite motion");}
Check(ApothecaryMotion.Sample(0).Draw==0&&ApothecaryMotion.Sample(5.2f).Draw==0&&ApothecaryMotion.Sample(5.2f).Lid==0,"interaction starts and ends closed");
// Masonry uses a small explicit native set; arbitrary stone-named valuables stay out.
Plugin.StorageCategoryOverrides.Value="";
foreach(var id in new[]{"Stone","BlackMarble","Grausten"})Check(FurnitureAssignment.Classify(id)=="masonry","native masonry exception classifies predictably");
foreach(var id in new[]{"SharpeningStone","Thunderstone","StoneGolemTrophy","Crystal","ModStoneSword"})Check(FurnitureAssignment.Classify(id)!="masonry","stone-like names and minerals are not guessed into masonry");
foreach(var def in MasonryStorage.Pieces)
{
 var p=pieces.Single(p=>p.Prefab.name==def.Prefab);var chest=p.Prefab.GetComponent<Container>();
 Check(BuildMenuCategory.Registered.Contains(def.Prefab)&&chest.m_width*chest.m_height==def.Capacity,"new masonry/rafter pieces appear in Hammer with intended native inventory");
 Check(p.Config.Requirements.All(r=>r.Amount>0&&r.Recover),"construction materials remain refundable");
 var item=Make(def.Category=="masonry"?"BlackMarble":"Wood");
 Check(FurnitureAssignment.Accepts(chest,item.m_itemData)&&!FurnitureAssignment.Accepts(chest,herb.m_itemData),"specialized default routes only its own category");
 chest.Settings.Forget(item.name);Check(!FurnitureAssignment.Accepts(chest,item.m_itemData),"explicit ignore defeats masonry and ceiling category");
 chest.Settings.Forgotten.Clear();FurnitureAssignment.Cycle(chest,chest.Settings);
 Check(!FurnitureAssignment.Accepts(chest,item.m_itemData),"automatic assignment remains optional");
 chest.Settings.Remembered.Add(item.name);Check(FurnitureAssignment.Accepts(chest,item.m_itemData),"manual assignment survives disabled auto category");
}
Check(!FurnitureAssignment.Accepts(ordinary,Make("Stone").m_itemData),"ordinary unlearned chests do not acquire masonry defaults");
Plugin.StorageCategoryOverrides.Value="ModBuildingRock=masonry;Stone=none;BlackMarble=reagents";
Check(FurnitureAssignment.Classify("ModBuildingRock")=="masonry"&&FurnitureAssignment.Classify("Stone")==""&&FurnitureAssignment.Classify("BlackMarble")=="reagents","synchronized override adds removes and reassigns masonry");
Plugin.StorageCategoryOverrides.Value="";
for(int level=1;level<=3;level++)
{
 Check(BulkPresentation.Model("masonry","BlackMarble",level,"masonry_crib")=="marble_crib_load_"+level,"deep masonry crib uses taller matching piles");
 Check(BulkPresentation.Model("masonry","Stone",level)=="masonry_load_"+level,"fieldstone has its own shape");
 Check(BulkPresentation.Model("masonry","BlackMarble",level)=="marble_load_"+level,"marble uses chipped block shape");
 Check(BulkPresentation.Model("masonry","Grausten",level)=="grausten_load_"+level,"grausten uses rough fractured shape");
 Check(BulkPresentation.Model("masonry","ModBuildingRock",level)=="masonry_load_"+level,"unknown masonry uses family-neutral representative");
}
Check(FurnitureMotion.Carries(FurnitureHandling.Masonry)&&FurnitureMotion.Contact(FurnitureHandling.Masonry)<FurnitureMotion.Duration(FurnitureHandling.Masonry),"stone is held then placed inside a bounded performance");
Check(FurnitureMotion.Duration(FurnitureHandling.Masonry)<4&&FurnitureMotion.Lean(FurnitureHandling.Masonry,0)==0&&FurnitureMotion.Lean(FurnitureHandling.Masonry,FurnitureMotion.Duration(FurnitureHandling.Masonry))==0,"masonry motion ends promptly in neutral pose");
Check(BulkPresentation.ContactHeight("masonry_crib",1)<BulkPresentation.ContactHeight("masonry_crib",2)&&BulkPresentation.ContactHeight("masonry_crib",2)<BulkPresentation.ContactHeight("masonry_crib",3),"owl target follows the visible pile height");
Check(BulkPresentation.ContactHeight("hide_rail",3)==.70f&&BulkPresentation.ContactHeight("lumber_rafter",3)==.15f,"existing hide and timber contact positions remain unchanged");
// Food families are secondary: existing ingredient assignments remain valid.
Plugin.StorageCategoryOverrides.Value="";ObjectDB.instance=new();ZNetScene.instance=new();
ItemDrop Food(string id){var d=Make(id,ItemDrop.ItemData.ItemType.Consumable);d.m_itemData.m_shared.m_food=20;ObjectDB.instance.m_items.Add(d.gameObject);return d;}
var carrot=Food("Carrot");var meat=Food("DeerMeat");var cooked=Food("CookedDeerMeat");var meal2=Food("VegetableStew");
var mushroom=Food("MushroomYellow");var berries=Food("Blueberries");
var barley=Make("Barley");var flour=Make("BarleyFlour");var modflour=Make("ModFlour");var flax=Make("Flax");
var fish=Make("ModFish",ItemDrop.ItemData.ItemType.Fish);var cuts=Make("FishRaw");
ObjectDB.instance.m_items.AddRange(new[]{barley,flour,modflour,flax,fish,cuts}.Select(i=>i.gameObject));
ObjectDB.instance.m_recipes.Add(new(){m_item=meal2,m_resources=new[]{new Recipe.Requirement{m_resItem=carrot},new Recipe.Requirement{m_resItem=meat},new Recipe.Requirement{m_resItem=flour}}});
var cooker=new GameObject("cooking_station").AddComponent<CookingStation>();cooker.m_conversion.Add(new(){m_from=meat,m_to=cooked});ZNetScene.instance.m_prefabs.Add(cooker.gameObject);
Check(FurnitureAssignment.Classify("Carrot")=="ingredients"&&FurnitureAssignment.Classify("Carrot","produce")=="produce","adding produce preserves recipe-derived ingredient classification");
Check(FurnitureAssignment.Classify("DeerMeat")=="ingredients"&&FurnitureAssignment.Classify("DeerMeat","meat")=="meat","raw recipe meat retains apothecary assignment and gains hanging storage");
Check(FurnitureAssignment.Classify("CookedDeerMeat","food")=="food"&&FurnitureAssignment.Classify("CookedDeerMeat","meat")!="meat","cooking product takes precedence over meat suffix");
Check(FurnitureAssignment.Classify("VegetableStew","food")=="food","prepared recipe food routes to pantry");
Check(FurnitureAssignment.Classify("ModFish","fish")=="fish"&&FurnitureAssignment.Classify("FishRaw","fish")=="fish","native Fish type works for modded names and raw cuts exception");
foreach(var id in new[]{"Barley","BarleyFlour","ModFlour"})Check(FurnitureAssignment.Classify(id,"grain")=="grain","grain and material flour category");
Check(FurnitureAssignment.Classify("Flax","grain")!="grain","flax remains a fiber ingredient");
var potion=Food("HealingDrink");potion.m_itemData.m_shared.m_consumeStatusEffect=new();
var swordFlour=Make("SwordFlour",ItemDrop.ItemData.ItemType.OneHandedWeapon);swordFlour.m_itemData.m_shared.m_food=99;ObjectDB.instance.m_items.Add(swordFlour.gameObject);
Check(FurnitureAssignment.Classify("HealingDrink","produce")!="produce"&&FurnitureAssignment.Classify("SwordFlour","grain")!="grain","potions and equipment do not become edible or grain supplies");
foreach(var def in PantryStorage.Pieces)
{
 var p=pieces.Single(p=>p.Prefab.name==def.Prefab);var c=p.Prefab.GetComponent<Container>();
 var item=def.Category=="food"?meal2:def.Category=="produce"?carrot:def.Category=="meat"?meat:def.Category=="fish"?fish:flour;
 Check(BuildMenuCategory.Registered.Contains(def.Prefab)&&c.m_width*c.m_height==def.Capacity,"restored pantry piece is buildable with correct native inventory");
 Check(p.Config.Requirements.All(r=>r.Amount>0&&r.Recover),"pantry construction costs remain refundable");
 Check(FurnitureAssignment.Accepts(c,item.m_itemData)&&FurnitureAssignment.Types(c).Contains(item.name),"category assignment and searchable types include secondary food families");
 Check(!FurnitureAssignment.Accepts(c,swordFlour.m_itemData),"equipment cannot route through pantry classification");
 c.Settings.Forget(item.name);Check(!FurnitureAssignment.Accepts(c,item.m_itemData),"ignored food defeats secondary assignment");
 c.Settings.Forgotten.Clear();FurnitureAssignment.Cycle(c,c.Settings);Check(!FurnitureAssignment.Accepts(c,item.m_itemData),"pantry auto-assignment disable applies to secondary families");
 c.Settings.Remembered.Add(item.name);Check(FurnitureAssignment.Accepts(c,item.m_itemData),"learned item works when pantry auto-assignment is disabled");
}
var malformed=new GameObject(PantryStorage.Pieces[0].Prefab).AddComponent<Container>();malformed.Settings.AutoCategories=",";
Check(!FurnitureAssignment.Accepts(malformed,swordFlour.m_itemData),"empty category tokens cannot accept unclassified items");
var freshJar=new GameObject(Apothecary.Cabinets[0].Prefab).AddComponent<Container>();
Check(FurnitureAssignment.Accepts(freshJar,carrot.m_itemData)&&FurnitureAssignment.Accepts(freshJar,meat.m_itemData),"existing jar routing remains available after food expansion");
Plugin.StorageCategoryOverrides.Value="Carrot=none;DeerMeat=reagents;BarleyFlour=food;MushroomYellow=produce;Blueberries=invalid";
Check(FurnitureAssignment.Classify("Carrot","produce")==""&&FurnitureAssignment.Classify("DeerMeat","meat")=="reagents","explicit remove or reassignment clears inferred secondary family");
Check(FurnitureAssignment.Classify("BarleyFlour","grain")=="food"&&FurnitureAssignment.Classify("MushroomYellow","produce")=="produce","explicit food categories are accepted");
Check(FurnitureAssignment.Classify("Blueberries","produce")=="produce","invalid override does not erase a valid inferred family");
Plugin.StorageCategoryOverrides.Value="";ObjectDB.instance=new();
Check(FurnitureAssignment.Classify("Carrot","produce")=="","world change discards secondary classifications");
foreach(var style in new[]{FurnitureHandling.Pantry,FurnitureHandling.Hanging,FurnitureHandling.Grain})
 Check(FurnitureMotion.Contact(style)>0&&FurnitureMotion.Contact(style)+.65f<FurnitureMotion.Duration(style),"pantry contact finishes before performance ends");
Check(FurnitureMotion.Cover(FurnitureMotion.Contact(FurnitureHandling.Grain))>60&&FurnitureMotion.Cover(FurnitureMotion.Duration(FurnitureHandling.Grain))==0,"grain lid opens before filling and finishes shut");
for(float time=-1;time<5;time+=.025f)Check(Math.Abs(FurnitureMotion.HangingSway(time))<=3.001f,"hanging food settles within a bounded sway");
Check(FurnitureMotion.HangingSway(0)==0&&FurnitureMotion.HangingSway(FurnitureMotion.Duration(FurnitureHandling.Hanging))==0,"hanging loads start and finish at their authored ties");
Check(BulkPresentation.Model("fish","FishRaw",2)=="fish_cuts_load_2"&&BulkPresentation.Model("fish","ModFish",3)=="fish_load_3","raw fillets and whole fish have distinct displays");
Check(BulkPresentation.Model("produce","Blueberries",2)=="berries_load_2"&&BulkPresentation.Model("produce","MushroomYellow",3)=="mushrooms_load_3","berries and mushrooms use appropriate silhouettes");
Check(BulkPresentation.Model("grain","BarleyFlour",3)=="flour_load_3"&&BulkPresentation.Model("grain","Barley",2)=="grain_load_2","grain differs from smooth flour");
// Armory metadata, ambiguity and player exception precedence.
Plugin.StorageCategoryOverrides.Value="";ObjectDB.instance=new();ZNetScene.instance=new();
ItemDrop Gear(string id,ItemDrop.ItemData.ItemType type,Skills.SkillType skill=Skills.SkillType.None,string ammo="")
{var item=Make(id,type);item.m_itemData.m_shared.m_skillType=skill;item.m_itemData.m_shared.m_ammoType=ammo;ObjectDB.instance.m_items.Add(item.gameObject);return item;}
var bow=Gear("A_NameWithoutBow",ItemDrop.ItemData.ItemType.Bow,Skills.SkillType.Bows,"custom-shaft");
var crossbow=Gear("AnotherModRanged",ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft,Skills.SkillType.Crossbows,"custom-short-shaft");
var arrows=Gear("UntranslatedAmmoOne",ItemDrop.ItemData.ItemType.Ammo,Skills.SkillType.None,"custom-shaft");
var bolts=Gear("UntranslatedAmmoTwo",ItemDrop.ItemData.ItemType.Ammo,Skills.SkillType.None,"custom-short-shaft");
var axe=Gear("SmallAxe",ItemDrop.ItemData.ItemType.OneHandedWeapon,Skills.SkillType.Axes);
var spear=Gear("LongShaft",ItemDrop.ItemData.ItemType.OneHandedWeapon,Skills.SkillType.Spears);
var greatSword=Gear("LargeBlade",ItemDrop.ItemData.ItemType.TwoHandedWeapon,Skills.SkillType.Swords);
var shield=Gear("Protection",ItemDrop.ItemData.ItemType.Shield);
var magic=Gear("Staff",ItemDrop.ItemData.ItemType.TwoHandedWeapon,Skills.SkillType.ElementalMagic);
var tool=Gear("ArrowBoltSwordTool",ItemDrop.ItemData.ItemType.Tool,Skills.SkillType.Crossbows);
var bait=Gear("Bait",ItemDrop.ItemData.ItemType.Ammo,Skills.SkillType.Fishing,"fishing");
Gear("Armor",ItemDrop.ItemData.ItemType.Chest);
Check(FurnitureAssignment.Classify(bow.name)=="bows"&&FurnitureAssignment.Classify(crossbow.name)=="crossbows","bow and crossbow use actual equipment metadata");
Check(FurnitureAssignment.Classify(arrows.name)=="arrows"&&FurnitureAssignment.Classify(bolts.name)=="bolts","ammo follows compatible weapon token without English item names");
Check(FurnitureAssignment.Classify(axe.name)=="weapons"&&FurnitureAssignment.Classify(spear.name)=="longweapons"&&FurnitureAssignment.Classify(greatSword.name)=="longweapons","spears and two-handed weapons route to taller racks");
Check(FurnitureAssignment.Classify(shield.name)=="shields","native shields have their own furniture");
foreach(var id in new[]{magic.name,tool.name,bait.name})Check(FurnitureAssignment.Classify(id)=="","staffs, tools and bait are not guessed into weapon racks");
var otherBow=Gear("SharedBow",ItemDrop.ItemData.ItemType.Bow,Skills.SkillType.Bows,"shared");
Gear("SharedCrossbow",ItemDrop.ItemData.ItemType.Bow,Skills.SkillType.Crossbows,"shared");
var ambiguous=Gear("SharedAmmo",ItemDrop.ItemData.ItemType.Ammo,Skills.SkillType.Bows,"shared");
Check(FurnitureAssignment.Classify(ambiguous.name)=="","ambiguous multi-weapon token stays unassigned even with conflicting skill metadata");
var bowRack=new GameObject("Quartermaster_BowRack").AddComponent<Container>();
Check(FurnitureAssignment.Accepts(bowRack,bow.m_itemData)&&!FurnitureAssignment.Accepts(bowRack,crossbow.m_itemData),"new furniture starts with its intended auto category");
bowRack.Settings.Forgotten.Add(bow.name);Check(!FurnitureAssignment.Accepts(bowRack,bow.m_itemData),"per-chest exclusion wins over automatic category");
Plugin.StorageCategoryOverrides.Value="SharedAmmo=bolts;SmallAxe=longweapons;Protection=none";
Check(FurnitureAssignment.Classify(ambiguous.name)=="bolts"&&FurnitureAssignment.Classify(axe.name)=="longweapons"&&FurnitureAssignment.Classify(shield.name)=="","armory exceptions can resolve ambiguity, move or suppress assignments");
foreach(var def in ArmoryStorage.Pieces)
{
 var built=pieces.Single(p=>p.Prefab.name==def.Prefab);var c=built.Prefab.GetComponent<Container>();
 Check(c.m_width==def.Columns&&c.m_height==def.Rows&&BuildMenuCategory.Registered.Contains(def.Prefab),"armory uses registered native containers with authored cells");
 Check(built.Config.Requirements.All(r=>r.Recover)&&def.Capacity<=5,"bounded equipment displays and native refunds");
 Check(def.DisplayWidth>0&&def.DisplayHeight>0&&def.DisplayDepth>0,"every rack has finite positive display bounds");
 Check(FurnitureMotion.Carries(def.Handling)&&FurnitureMotion.Contact(def.Handling)+.9f<FurnitureMotion.Duration(def.Handling),"rack placement has time to settle without delaying automation");
 Check(FurnitureMotion.RackLift(def.Handling,0)==0&&FurnitureMotion.RackLift(def.Handling,FurnitureMotion.Duration(def.Handling))==0,"rack gesture starts and ends seated");
 for(float t=0;t<4;t+=.025f)Check(FurnitureMotion.RackLift(def.Handling,t)>=0&&FurnitureMotion.RackLift(def.Handling,t)<=.0651f,"equipment lift is bounded");
}
// Armor type metadata and door interruptions reuse the same real storage policy.
Check(FurnitureAssignment.Classify("Armor")=="armor","native chest armor classified for wardrobes");
foreach(var type in new[]{ItemDrop.ItemData.ItemType.Helmet,ItemDrop.ItemData.ItemType.Chest,ItemDrop.ItemData.ItemType.Legs,ItemDrop.ItemData.ItemType.Shoulder,ItemDrop.ItemData.ItemType.Hands})
{
 var armorItem=Gear("ModArmor"+type,type);
 Check(FurnitureAssignment.Classify(armorItem.name)=="armor","modded armor classified by equipment type");
 var wardrobe=new GameObject(WardrobeStorage.Pieces[0].Prefab).AddComponent<Container>();
 Check(FurnitureAssignment.Accepts(wardrobe,armorItem.m_itemData),"wardrobe accepts armor automatically");
 Check(!FurnitureAssignment.Accepts(wardrobe,axe.m_itemData),"armor wardrobe does not infer weapon acceptance");
 wardrobe.Settings.Forget(armorItem.name);Check(!FurnitureAssignment.Accepts(wardrobe,armorItem.m_itemData),"explicit armor exclusion wins");
}
Plugin.StorageCategoryOverrides.Value="Armor=none;Staff=armor";
Check(FurnitureAssignment.Classify("Armor")==""&&FurnitureAssignment.Classify("Staff")=="armor","armor override supports exclusion and ambiguous custom equipment");
using(var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(assetPath)))
foreach(var def in WardrobeStorage.Pieces)
{
 var layout=document.RootElement.GetProperty("layouts").GetProperty(def.Model);
 Check(def.Doors==layout.GetProperty("doors").GetInt32()&&Math.Abs(def.DoorHingeX-layout.GetProperty("doorHingeX").GetSingle())<.00001f&&Math.Abs(def.DoorFront-layout.GetProperty("doorFront").GetSingle())<.00001f,"door count and runtime pivots match authored geometry");
 var fit=layout.GetProperty("fit");Check(Math.Abs(def.DisplayWidth-fit[0].GetSingle())<.00001f&&Math.Abs(def.DisplayHeight-fit[1].GetSingle())<.00001f&&Math.Abs(def.DisplayDepth-fit[2].GetSingle())<.00001f,"armor fit matches shelf clearance");
}
var door=new FurnitureDoors();int opens=0,closes=0;
for(int i=0;i<120;i++){door.Request(FurnitureDoors.OwlOpening(i*.04f));int sound=door.Step(.04f,false);if(sound>0)opens++;if(sound<0)closes++;Check(door.Amount>=0&&door.Amount<=1,"bounded animated door state");if(i==50)Check(door.Amount>.99f,"doors fully open before owl places equipment");}
Check(opens==1&&closes==1&&door.Amount==0,"complete owl visit opens and latches once");
door.Request(1);for(int i=0;i<10;i++)door.Step(.05f,false);door.Request(0);for(int i=0;i<10;i++)door.Step(.05f,true);
Check(door.Amount==1,"player inventory keeps doors open after owl task cancels");
for(int i=0;i<11;i++)door.Step(.05f,false);Check(door.Amount==0,"closing inventory releases wardrobe doors");
door.Request(1);door.Step(.1f,false);door.Reset();Check(door.Amount==0&&door.Step(.1f,false)==0,"disable/distance reset clears partial motion and stale request without audio");
door.Request(float.NaN);Check(door.Step(.1f,false)==0&&door.Step(float.NaN,true)==0,"invalid timing/request cannot poison animation state");
Check(FurnitureMotion.Carries(FurnitureHandling.Wardrobe)&&FurnitureMotion.Duration(FurnitureHandling.Wardrobe)>4.25f,"wardrobe performance allows door closure before owl departure");
// Keep future display-family checks ready for re-enabling the shelved batch.
if(DisplayStorage.Enabled)
{
// Display families preserve known routes and allow explicit ambiguity resolution.
Plugin.StorageCategoryOverrides.Value="";ObjectDB.instance=new();ZNetScene.instance=new();
var trophy=Gear("CustomCreatureHead",ItemDrop.ItemData.ItemType.Trophy);trophy.m_itemData.m_shared.m_value=20;
var trinket=Gear("ModdedSaleTrinket",ItemDrop.ItemData.ItemType.Material);trinket.m_itemData.m_shared.m_value=12;
var cheap=Gear("ModdedUnsoldMaterial",ItemDrop.ItemData.ItemType.Material);
var valuableSword=Gear("ExpensiveBlade",ItemDrop.ItemData.ItemType.OneHandedWeapon);valuableSword.m_itemData.m_shared.m_value=800;
var valuableCopper=Gear("Copper",ItemDrop.ItemData.ItemType.Material);valuableCopper.m_itemData.m_shared.m_value=200;
var ruby=Gear("Ruby",ItemDrop.ItemData.ItemType.Material);ruby.m_itemData.m_shared.m_value=20;
var mealForSale=Gear("SaleMeal",ItemDrop.ItemData.ItemType.Consumable);mealForSale.m_itemData.m_shared.m_value=100;mealForSale.m_itemData.m_shared.m_food=20;
var saleHerb=Gear("SaleHerb",ItemDrop.ItemData.ItemType.Material);saleHerb.m_itemData.m_shared.m_value=20;
ObjectDB.instance.m_recipes.Add(new(){m_item=mealForSale,m_resources=new[]{new Recipe.Requirement{m_resItem=saleHerb}}});
Check(FurnitureAssignment.Classify(trophy.name)=="trophies","modded trophy type wins over sale value");
Check(FurnitureAssignment.Classify(trinket.name)=="valuables"&&FurnitureAssignment.Classify(cheap.name)=="","only otherwise unclassified sale materials inferred as valuables");
Check(FurnitureAssignment.Classify(valuableSword.name)=="weapons"&&FurnitureAssignment.Classify(valuableCopper.name)=="ingots","sale value never steals equipment or classified metal");
Check(FurnitureAssignment.Classify(saleHerb.name)=="ingredients"&&FurnitureAssignment.Classify(mealForSale.name)!="valuables","sale value preserves food and ingredient routes");
foreach(var id in new[]{"Ruby","Amber","AmberPearl","Crystal","GemstoneRed","GemstoneGreen","GemstoneBlue"})Check(FurnitureAssignment.Classify(id)=="gems","small explicit gem exception set");
Check(FurnitureAssignment.Classify("Coins")=="valuables","coin default is explicit even without loaded item metadata");
var trophyShelf=new GameObject("Quartermaster_TrophyShelf").AddComponent<Container>();
var gemShelf=new GameObject("Quartermaster_GemShelf").AddComponent<Container>();
var treasure=new GameObject("Quartermaster_TreasureCoffer").AddComponent<Container>();
Check(FurnitureAssignment.Accepts(trophyShelf,trophy.m_itemData)&&!FurnitureAssignment.Accepts(trophyShelf,trinket.m_itemData),"trophy shelf default is selective");
Check(FurnitureAssignment.Accepts(gemShelf,ruby.m_itemData)&&!FurnitureAssignment.Accepts(treasure,ruby.m_itemData),"gem and treasure routes remain distinct");
Check(FurnitureAssignment.Accepts(treasure,trinket.m_itemData),"coffer accepts modded sale material");
trophyShelf.Settings.Forget(trophy.name);Check(!FurnitureAssignment.Accepts(trophyShelf,trophy.m_itemData),"explicit trophy exclusion wins");
treasure.Settings.AutoAssign=false;Check(!FurnitureAssignment.Accepts(treasure,trinket.m_itemData),"disabling coffer assignment stops inferred routes");
Plugin.StorageCategoryOverrides.Value="Ruby=valuables;ModdedUnsoldMaterial=gems;CustomCreatureHead=none";
Check(FurnitureAssignment.Accepts(treasure,ruby.m_itemData)==false,"disabled assignment stays disabled after override");
treasure.Settings.AutoAssign=true;
Check(FurnitureAssignment.Accepts(treasure,ruby.m_itemData)&&!FurnitureAssignment.Accepts(gemShelf,ruby.m_itemData),"override reassigns gem without leaving duplicate inferred routes");
Check(FurnitureAssignment.Classify(cheap.name)=="gems"&&FurnitureAssignment.Classify(trophy.name)=="","override includes ambiguous gems and excludes trophies");
using(var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(assetPath)))
foreach(var def in DisplayStorage.Pieces)
{
 var layout=document.RootElement.GetProperty("layouts").GetProperty(def.Model);var fit=layout.GetProperty("fit");
 Check(Math.Abs(def.DisplayWidth-fit[0].GetSingle())<.00001f&&Math.Abs(def.DisplayHeight-fit[1].GetSingle())<.00001f&&Math.Abs(def.DisplayDepth-fit[2].GetSingle())<.00001f,"runtime display envelope matches authored shelf clearances");
 var built=pieces.Single(p=>p.Prefab.name==def.Prefab);
 Check(BuildMenuCategory.Registered.Contains(def.Prefab)&&built.Config.Requirements.All(r=>r.Recover)&&def.Capacity<=6,"restored pieces have menu registration and recoverable bounded storage");
 Check(FurnitureMotion.Carries(def.Handling)&&FurnitureMotion.Contact(def.Handling)+.9f<FurnitureMotion.Duration(def.Handling),"display owl contact fits within independent cosmetic duration");
 Check(FurnitureMotion.DisplayLift(def.Handling,0)==0&&FurnitureMotion.DisplayLift(def.Handling,FurnitureMotion.Duration(def.Handling))==0,"display placement starts and finishes on its shelf");
 for(float t=0;t<5;t+=.04f)Check(FurnitureMotion.DisplayLift(def.Handling,t)>=0&&FurnitureMotion.DisplayLift(def.Handling,t)<=.0451f,"display gestures stay within bounded clearances");
 if(def.PlayerLid)
 {
  var pivot=layout.GetProperty("lidPivot");Check(Math.Abs(def.LidHeight-pivot[1].GetSingle())<.00001f&&Math.Abs(def.LidBack-pivot[2].GetSingle())<.00001f,"coffer lid uses exact authored hinge");
  Check(def.Doors==0&&FurnitureMotion.Duration(def.Handling)>4.25f,"coffer uses one cover and allows closure before departure");
 }
}
}
else
{
 Check(Apothecary.All.Count()==41,"shelving display batch preserves forty approved storage definitions");
 foreach(var def in DisplayStorage.Pieces)
 {
  Check(Apothecary.Definition(def.Prefab)==null,"shelved furniture is absent from active definitions");
  Check(!pieces.Any(p=>p.Prefab.name==def.Prefab)&&!BuildMenuCategory.Registered.Contains(def.Prefab),"shelved furniture is not registered or shown in Hammer");
 }
 Plugin.StorageCategoryOverrides.Value="";ObjectDB.instance=new();ZNetScene.instance=new();
 var futureTrophy=Gear("ShelvedTrophy",ItemDrop.ItemData.ItemType.Trophy);
 var futureTreasure=Gear("ShelvedTrinket",ItemDrop.ItemData.ItemType.Material);futureTreasure.m_itemData.m_shared.m_value=50;
 foreach(var id in new[]{futureTrophy.name,futureTreasure.name,"Ruby","Crystal","Coins"})Check(FurnitureAssignment.Classify(id)=="","shelved default classifications do not affect current routes");
 Plugin.StorageCategoryOverrides.Value="Ruby=gems;ShelvedTrinket=valuables;ShelvedTrophy=trophies";
 foreach(var id in new[]{"Ruby",futureTreasure.name,futureTrophy.name})Check(FurnitureAssignment.Classify(id)=="","shelved category overrides remain inactive");
}
Console.WriteLine($"PASS: {checks} apothecary registration, crafting, assignment, menu and articulated motion checks.");

var mead=Apothecary.MeadShelves.Single();
Check(mead.Columns==6&&mead.Rows==4&&mead.Category=="meads","mead cabinet has 24 restricted slots");
Check(pieces.Single(p=>p.Prefab.name==mead.Prefab).Config.Requirements.Any(r=>r.Item=="RoundLog"&&r.Amount==2),"native core wood recipe cost");

ExternalStorageCompatibility.Rules=new(typeof(ExternalStorageCompatibility).Assembly,typeof(ExternalStorageCompatibility).Assembly);
var barrel=new GameObject("OH_Berries").AddComponent<Container>();barrel.m_name="$barrel_berries";
var berry=new ItemDrop.ItemData{m_dropPrefab=new GameObject("Raspberry")};
Check(FurnitureAssignment.Accepts(barrel,berry),"empty food barrel automatically receives matching food");
barrel.Settings.Forget("Raspberry");Check(!FurnitureAssignment.Accepts(barrel,berry),"forgotten items override automatic barrel assignment");
barrel.Settings.Restore("Raspberry");barrel.Settings.Overflow=true;
Check(!FurnitureAssignment.Accepts(barrel,new ItemDrop.ItemData{m_dropPrefab=new GameObject("Iron")}),"overflow cannot bypass barrel restriction");
barrel.Settings.Overflow=false;barrel.Settings.Remembered.Clear();FurnitureAssignment.Cycle(barrel,barrel.Settings);
Check(!barrel.Settings.AutoAssign&&!FurnitureAssignment.Accepts(barrel,berry),"autoassignment can be disabled in Chest Config");
FurnitureAssignment.Cycle(barrel,barrel.Settings);Check(FurnitureAssignment.Accepts(barrel,berry),"autoassignment can be restored");
Console.WriteLine("PASS: external furniture assignment, exclusions and hard restrictions.");
