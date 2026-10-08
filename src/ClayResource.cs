using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Quartermaster;

internal static class ClayResource
{
    internal const string ItemName="Quartermaster_RawClay";
    internal const string PickableName="Pickable_Quartermaster_Clay";
    private static bool registered;
    internal static void Initialize()=>PrefabManager.OnVanillaPrefabsAvailable+=Register;
    internal static void Shutdown()=>PrefabManager.OnVanillaPrefabsAvailable-=Register;
    private static void Register()
    {
        if(registered)return;
        try
        {
            LocalizationManager.Instance.GetLocalization().AddTranslation("English",new Dictionary<string,string>{
                {"item_quartermaster_rawclay","Raw Clay"},
                {"item_quartermaster_rawclay_description","Soft bank clay, ready to be shaped and fired."}
            });
            var item=PrefabManager.Instance.CreateClonedPrefab(ItemName,"Flint");
            if(!item)throw new InvalidOperationException("Missing native Flint item template");
            ClayModel.Replace(item,false);
            var data=item.GetComponent<ItemDrop>().m_itemData;
            data.m_dropPrefab=item;data.m_stack=1;
            data.m_shared.m_name="$item_quartermaster_rawclay";
            data.m_shared.m_description="$item_quartermaster_rawclay_description";
            data.m_shared.m_icons=new[]{ClayModel.Icon()};
            data.m_shared.m_weight=1f;data.m_shared.m_maxStackSize=50;
            data.m_shared.m_teleportable=true;data.m_shared.m_value=0;
            if(!ItemManager.Instance.AddItem(new CustomItem(item,false)))throw new InvalidOperationException("Cannot register Raw Clay");
            ItemStacks.Apply(data.m_shared);
            var patch=PrefabManager.Instance.CreateClonedPrefab(PickableName,"Pickable_Flint");
            if(!patch)throw new InvalidOperationException("Missing native Pickable_Flint template");
            patch.GetComponent<ZNetView>().m_syncInitialScale=true;
            var art=ClayModel.Replace(patch,true);
            Configure(patch.GetComponent<Pickable>(),item,art);
            // Native world-generation owns placement and persistent pickable IDs.
            // No per-frame spawning, rescanning terrain or retroactive zone rewriting.
            if(!ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(patch,false,Placement())))
                throw new InvalidOperationException("Cannot register clay shoreline vegetation");
            registered=true;Shutdown();
            Plugin.Log.LogInfo("Registered original Raw Clay and shoreline pickables (new Meadows/Black Forest terrain).");
        }
        catch(Exception error){Plugin.Log.LogError("Quartermaster clay registration failed: "+error);}
    }
    internal static void Configure(Pickable pickable,GameObject item,GameObject visual)
    {
        if(!pickable)throw new InvalidOperationException("Clay template has no Pickable");
        pickable.m_itemPrefab=item;pickable.m_hideWhenPicked=visual;
        pickable.m_overrideName="$item_quartermaster_rawclay";
        pickable.m_amount=3;pickable.m_minAmountScaled=1;pickable.m_dontScale=false;
        pickable.m_respawnTimeMinutes=240;pickable.m_respawnTimeInitMin=0;pickable.m_respawnTimeInitMax=0;
        pickable.m_spawnOffset=.2f;pickable.m_hoverOffset=.1f;
        pickable.m_defaultPicked=false;pickable.m_defaultEnabled=true;
        pickable.m_extraDrops=new DropTable();pickable.m_harvestable=false;
    }
    internal static VegetationConfig Placement()=>new VegetationConfig{
        Biome=Heightmap.Biome.Meadows|Heightmap.Biome.BlackForest,
        BiomeArea=Heightmap.BiomeArea.Everything,
        Min=0,Max=4,MinAltitude=.1f,MaxAltitude=2f,MinTilt=0,MaxTilt=22,
        GroupSizeMin=1,GroupSizeMax=2,GroupRadius=2f,
        ScaleMin=.9f,ScaleMax=1.15f,GroundOffset=0,
        BlockCheck=true,ForcePlacement=false
    };
}
