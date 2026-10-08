using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Quartermaster;

internal enum CabinetMount { Floor, Wall, Ceiling, Corner, Slope }

internal sealed class ApothecaryDefinition
{
    internal string Prefab,Name,Model,Jar;
    internal string Category="ingredients,reagents";
    internal FurnitureHandling Handling;
    internal int Columns,Rows=2,Wood=20,FineWood=5,Bronze,CoreWood;
    internal string Station="piece_workbench";
    internal float Width,Height,Depth=.55f,DrawDistance=.23f;
    internal bool Glass;
    internal float DisplayWidth,DisplayHeight,DisplayDepth;
    internal bool HorizontalDisplay;
    internal bool PlayerLid;
    internal float LidHeight,LidBack;
    internal int Doors;
    internal float DoorHingeX,DoorFront;
    internal CabinetMount Mount;
    internal int Capacity=>Columns*Rows;
}
internal static class Apothecary
{
    internal const string Unfired="Quartermaster_UnfiredJar",Jar="Quartermaster_ClayJar",Blank="Quartermaster_CrystalFlaskBlank",Flask="Quartermaster_CrystalFlask",Kiln="Quartermaster_PotteryKiln";
    // Under-stair prototypes are deferred until native stair geometry is measured.
    internal static readonly ApothecaryDefinition[] Cabinets={
        new ApothecaryDefinition{Prefab="Quartermaster_ClayCabinet",Name="Clay Jar Cabinet",Model="modular_clay",Jar=Jar,Columns=4,Rows=3,Width=2,Height=2,Depth=.6f},
        new ApothecaryDefinition{Prefab="Quartermaster_CrystalCabinet",Name="Crystal Flask Cabinet",Model="modular_crystal",Jar=Flask,Columns=4,Rows=3,Width=2,Height=2,Depth=.6f,Glass=true},
        new ApothecaryDefinition{Prefab="Quartermaster_ClayCabinetNarrow",Name="Narrow Clay Cabinet",Model="clay_narrow",Jar=Jar,Columns=2,Rows=3,Width=.94f,Height=2.05f,Wood=16,FineWood=4},
        new ApothecaryDefinition{Prefab="Quartermaster_ClayCabinetLow",Name="Low Clay Cabinet",Model="clay_low",Jar=Jar,Columns=4,Rows=1,Width=1.70f,Height=.93f,Wood=12,FineWood=3},
        new ApothecaryDefinition{Prefab="Quartermaster_CrystalCabinetWall",Name="Wall Flask Cabinet",Model="crystal_wall",Jar=Flask,Columns=3,Rows=1,Width=1.36f,Height=.73f,Depth=.46f,Glass=true,Mount=CabinetMount.Wall,Wood=8,FineWood=4},
        new ApothecaryDefinition{Prefab="Quartermaster_ClayCabinetCorner",Name="Corner Clay Cabinet",Model="clay_corner",Jar=Jar,Columns=2,Rows=2,Width=1.38f,Height=1.50f,Depth=.80f,Mount=CabinetMount.Corner,Wood=14,FineWood=4},
        new ApothecaryDefinition{Prefab="Quartermaster_CrystalCabinetRafter",Name="Rafter Flask Rack",Model="crystal_rafter",Jar=Flask,Columns=4,Rows=1,Width=1.80f,Height=1.15f,Glass=true,Mount=CabinetMount.Ceiling,Wood=14,FineWood=4}
    };
    internal static readonly ApothecaryDefinition[] MeadShelves={
        new ApothecaryDefinition{Prefab="Quartermaster_MeadCabinet",Name="Mead Cabinet",Model="mead_cabinet",Columns=6,Rows=4,Width=2,Height=2,Depth=.60f,Wood=10,FineWood=4,CoreWood=2,Category="meads",Handling=FurnitureHandling.Mead,DisplayWidth=.13f,DisplayHeight=.20f,DisplayDepth=.12f},
    };
    // Keep stable prefab identities and inventory layouts when restoring build recipes.
    internal static IEnumerable<ApothecaryDefinition> All=>Cabinets.Concat(MeadShelves).Concat(BulkStorage.Pieces).Concat(SoftStorage.Pieces).Concat(MasonryStorage.Pieces).Concat(PantryStorage.Pieces).Concat(ArmoryStorage.Pieces).Concat(WardrobeStorage.Pieces).Concat(DisplayStorage.Enabled?DisplayStorage.Pieces:Array.Empty<ApothecaryDefinition>());
    internal static ApothecaryDefinition Definition(string name)=>name==DrawerFurniture.DrawerPrefab?DrawerFurniture.Definition:All.FirstOrDefault(d=>d.Prefab==name);
    private static bool registered;
    internal static void Initialize()=>PrefabManager.OnVanillaPrefabsAvailable+=Register;
    internal static void Shutdown()=>PrefabManager.OnVanillaPrefabsAvailable-=Register;
    private static void Register()
    {
        if(registered)return;
        try
        {
            var native=PrefabManager.Instance.GetPrefab("piece_chest_wood");ApothecaryArt.Initialize(native);
            Item(Unfired,"Unfired Jar","Soft clay shaped at a workbench. Fire in a Pottery Kiln.","unfired_jar",new[]{new RequirementConfig(ClayResource.ItemName,3)});
            Item(Jar,"Clay Jar","A fired, lidded jar for an apothecary cabinet.","clay_jar",null);
            Item(Blank,"Crystal Flask Blank","Prepared crystal for a flask. Melt and form in a Pottery Kiln.","flask_blank",new[]{new RequirementConfig("Crystal",2)});
            Item(Flask,"Crystal Flask","A stoppered crystal flask for an apothecary cabinet.","crystal_flask",null);
            var kiln=PrefabManager.Instance.CreateClonedPrefab(Kiln,"smelter");
            var smelter=kiln.GetComponent<Smelter>();smelter.m_name="Pottery Kiln";smelter.m_addOreTooltip="Add jar or flask blank";
            smelter.m_maxOre=10;smelter.m_maxFuel=10;smelter.m_fuelPerProduct=2;smelter.m_secPerProduct=45;
            smelter.m_conversion=new List<Smelter.ItemConversion>{Conversion(Unfired,Jar),Conversion(Blank,Flask)};
            AddPiece(kiln,new PieceConfig{Name="Pottery Kiln",Description="Fire clay jars and crystal flasks with coal. Uses the native smelter body.",PieceTable="Hammer",Category="Quartermaster",CraftingStation="piece_workbench",Requirements=new[]{new RequirementConfig("Stone",20,0,true),new RequirementConfig("SurtlingCore",2,0,true),new RequirementConfig(ClayResource.ItemName,10,0,true)}});
            foreach(var def in All)
            {
                var prefab=PrefabManager.Instance.CreateClonedPrefab(def.Prefab,"piece_chest_wood");
                ConfigurePlacement(prefab.GetComponent<Piece>(),def);
                ApothecaryArt.Cabinet(prefab,def);
                var c=prefab.GetComponent<Container>();c.m_name=def.Name;c.m_width=def.Columns;c.m_height=def.Rows;c.m_defaultItems=new DropTable();
                prefab.AddComponent<ApothecaryDisplay>();
                AddPiece(prefab,new PieceConfig{Enabled=true,Name=def.Name,Description=def.Category=="meads"?"Stores finished meads only. 24 inventory slots; displays stored bottles on the shelves.":"Automatically stores "+def.Category.Replace(",", " and ")+". Configure exceptions in Chest Config.",Icon=ApothecaryArt.Icon(def.Model),PieceTable="Hammer",Category="Quartermaster",CraftingStation=def.Station,Requirements=Requirements(def)});
            }
            registered=true;Shutdown();Plugin.Log.LogInfo("Registered pottery chain, native-body Pottery Kiln and "+All.Count()+" buildable storage furniture pieces.");
        }
        catch(Exception error){Plugin.Log.LogError("Quartermaster apothecary registration failed: "+error);}
    }
    internal static RequirementConfig[] Requirements(ApothecaryDefinition def)
    {
        var costs=new List<RequirementConfig>();
        if(def.CoreWood>0)costs.Add(new RequirementConfig("RoundLog",def.CoreWood,0,true));
        if(def.Wood>0)costs.Add(new RequirementConfig("Wood",def.Wood,0,true));
        if(def.Bronze>0)costs.Add(new RequirementConfig("Bronze",def.Bronze,0,true));
        if(def.FineWood>0)costs.Add(new RequirementConfig("FineWood",def.FineWood,0,true));
        if(!string.IsNullOrEmpty(def.Jar))costs.Add(new RequirementConfig(def.Jar,def.Capacity,0,true));
        return costs.ToArray();
    }
    internal static void ConfigurePlacement(Piece piece,ApothecaryDefinition def)
    {
        // Only these two mount styles override native chest surface restrictions.
        if(def.Mount!=CabinetMount.Wall&&def.Mount!=CabinetMount.Ceiling)return;
        piece.m_groundPiece=false;piece.m_groundOnly=false;piece.m_cultivatedGroundOnly=false;
        piece.m_waterPiece=false;piece.m_notOnWood=false;piece.m_notOnTiltingSurface=false;
        piece.m_inCeilingOnly=def.Mount==CabinetMount.Ceiling;piece.m_notOnFloor=true;
    }
    private static void Item(string id,string name,string description,string art,RequirementConfig[] requirements)
    {
        var prefab=PrefabManager.Instance.CreateClonedPrefab(id,"Flint");ApothecaryArt.Item(prefab,art);
        var data=prefab.GetComponent<ItemDrop>().m_itemData;data.m_dropPrefab=prefab;data.m_stack=1;
        data.m_shared.m_name=name;data.m_shared.m_description=description;data.m_shared.m_icons=new[]{ApothecaryArt.Icon(art)};
        data.m_shared.m_maxStackSize=20;data.m_shared.m_weight=.5f;data.m_shared.m_value=0;
        var custom=requirements==null?new CustomItem(prefab,false):new CustomItem(prefab,false,new ItemConfig{Name=name,Description=description,CraftingStation="piece_workbench",MinStationLevel=1,Amount=1,Requirements=requirements});
        if(!ItemManager.Instance.AddItem(custom))throw new InvalidOperationException("Cannot register "+id);
        ItemStacks.Apply(data.m_shared);
    }
    private static Smelter.ItemConversion Conversion(string from,string to)=>new Smelter.ItemConversion{m_from=PrefabManager.Instance.GetPrefab(from).GetComponent<ItemDrop>(),m_to=PrefabManager.Instance.GetPrefab(to).GetComponent<ItemDrop>()};
    private static void AddPiece(GameObject prefab,PieceConfig config)
    {if(!PieceManager.Instance.AddPiece(new CustomPiece(prefab,false,config)))throw new InvalidOperationException("Cannot register "+prefab.name);if(config.Enabled)BuildMenuCategory.Register(prefab.name);}
}
