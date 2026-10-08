using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

// Recipe relationships classify modded ingredients without a list of every berry/mead.
internal static class FurnitureAssignment
{
    private static ObjectDB database;
    private static int recipeCount=-1,prefabCount=-1,itemCount=-1;
    private static ZNetScene scene;
    private static string lastOverrides="";
    private static Dictionary<string,string> pantry=new Dictionary<string,string>(StringComparer.Ordinal);
    private static readonly Dictionary<string,string> categories=new Dictionary<string,string>(StringComparer.Ordinal);
    private static readonly Dictionary<string,string> exceptions=new Dictionary<string,string>(StringComparer.Ordinal){
        {"Resin","reagents"},{"GreydwarfEye","reagents"},{"YmirRemains","reagents"},{"Eitr","reagents"},{"Softtissue","reagents"},
        {"Wood","lumber"},{"FineWood","lumber"},{"RoundLog","lumber"},{"ElderBark","lumber"},{"YggdrasilWood","lumber"},{"Blackwood","lumber"},
        {"Stone","masonry"},{"BlackMarble","masonry"},{"Grausten","masonry"},
        {"Coal","coal"},{"Copper","ingots"},{"Tin","ingots"},{"Bronze","ingots"},{"Iron","ingots"},{"Silver","ingots"},{"BlackMetal","ingots"},{"Flametal","ingots"},{"FlametalNew","ingots"},
        {"Dandelion","ingredients"},{"Thistle","ingredients"},{"Barley","ingredients"},{"Flax","ingredients"}
    };
    internal static bool IsFurniture(Container c)=>c&&(Apothecary.Definition(ContainerRegistry.PrefabName(c))!=null||ExternalStorageCompatibility.Recognizes(c));
    internal static string Mode(ChestSettings s)=>Mode(s,null);
    internal static string Mode(ChestSettings s,Container c)=>ExternalStorageCompatibility.Recognizes(c)?"Container's item types":string.IsNullOrEmpty(s.AutoCategories)?(c?Apothecary.Definition(ContainerRegistry.PrefabName(c))?.Category:null)??"ingredients,reagents":s.AutoCategories;
    internal static void Cycle(Container c,ChestSettings s)
    {
        if(!s.AutoAssign){s.AutoAssign=true;s.AutoCategories="";return;}
        if(Apothecary.Definition(ContainerRegistry.PrefabName(c))?.Handling!=FurnitureHandling.Jar){s.AutoAssign=false;return;}
        if(Mode(s,c)=="ingredients,reagents")s.AutoCategories="ingredients";
        else if(s.AutoCategories=="ingredients")s.AutoCategories="reagents";
        else s.AutoAssign=false;
    }
    internal static IEnumerable<string> Types(Container c)
    {if(ExternalStorageCompatibility.Recognizes(c))return ExternalStorageCompatibility.Assigned(c);Refresh();return IsFurniture(c)?categories.Keys.Concat(ContainerRegistry.PrefabName(c)==MeadCabinet.Prefab&&ObjectDB.instance?ObjectDB.instance.m_items.Where(p=>p).Select(p=>p.name):Enumerable.Empty<string>()).Distinct().Where(id=>Matches(ContainerRegistry.GetSettings(c),id,c)):Enumerable.Empty<string>();}
    internal static bool Accepts(Container c,ItemDrop.ItemData item)
    {
        if(!ExternalStorageCompatibility.Allows(c.GetInventory(),item))return false;
        if(ContainerRegistry.PrefabName(c)=="Quartermaster_MeadCabinet"&&!MeadCabinet.IsMead(item))return false;
        var s=ContainerRegistry.GetSettings(c);string id=InventoryTransfers.ItemId(item);
        return s.Accepts(id,IsFurniture(c)&&Matches(s,id,c));
    }
    private static bool Matches(ChestSettings settings,string id,Container c)
    {if(ExternalStorageCompatibility.Recognizes(c))return settings.AutoAssign&&ExternalStorageCompatibility.Assigned(c).Contains(id);Refresh();return settings.AutoAssign&&Mode(settings,c).Split(',').Any(kind=>kind.Length>0&&Classify(id,kind)==kind);}
    internal static string Classify(string id){Refresh();return categories.TryGetValue(id,out var value)?value:"";}
    internal static string Classify(string id,string preferred)
    {Refresh();if(preferred=="meads")return MeadCabinet.IsMead(id)?"meads":"";return pantry.TryGetValue(id,out var kind)&&kind==preferred?kind:categories.TryGetValue(id,out var value)?value:"";}
    private static void Refresh()
    {
        var db=ObjectDB.instance;if(!db)return;
        // ObjectDB changes on scene load; recipe count catches late mod registrations.
        string overrides=Plugin.StorageCategoryOverrides?.Value??"";
        var currentScene=ZNetScene.instance;int count=currentScene?currentScene.m_prefabs.Count:0;
        if(db==database&&recipeCount==db.m_recipes.Count&&lastOverrides==overrides&&scene==currentScene&&prefabCount==count&&itemCount==db.m_items.Count)return;
        scene=currentScene;prefabCount=count;itemCount=db.m_items.Count;
        lastOverrides=overrides;
        database=db;recipeCount=db.m_recipes.Count;categories.Clear();
        foreach(var pair in exceptions)categories[pair.Key]=pair.Value;
        foreach(var prefab in db.m_items)
        {
            if(!prefab)continue;var drop=prefab.GetComponent<ItemDrop>();
            if(!drop||drop.m_itemData.m_shared.m_itemType!=ItemDrop.ItemData.ItemType.Material)continue;
            string id=prefab.name,category=MaterialCategory(id);
            if(category.Length>0&&!categories.ContainsKey(id))categories[id]=category;
        }
        foreach(var recipe in db.m_recipes)
        {
            if(!recipe||!recipe.m_item||recipe.m_resources==null)continue;
            var product=recipe.m_item.m_itemData.m_shared;
            if(product.m_food<=0&&product.m_foodStamina<=0&&product.m_foodEitr<=0&&product.m_consumeStatusEffect==null)continue;
            foreach(var requirement in recipe.m_resources)
            {
                if(!requirement.m_resItem)continue;
                var ingredient=requirement.m_resItem.m_itemData.m_shared;
                // Equipment is never inferred as an ingredient from its recipe relationships.
                if(ingredient.m_itemType!=ItemDrop.ItemData.ItemType.Material&&ingredient.m_itemType!=ItemDrop.ItemData.ItemType.Consumable)continue;
                string id=requirement.m_resItem.name;
                if(!categories.ContainsKey(id))categories[id]="ingredients";
            }
        }
        // Smelting relationships identify ore/scrap without a list of every raw resource.
        // Known metals seed native chains; exact XOre -> X material pairs support modded metals.
        if(currentScene)
        foreach(var prefab in currentScene.m_prefabs)
        {
            if(!prefab)continue;var smelter=prefab.GetComponent<Smelter>();
            if(!smelter||smelter.m_conversion==null||!smelter.m_fuelItem||smelter.m_fuelItem.name!="Coal")continue;
            foreach(var conversion in smelter.m_conversion)
            {
                if(!conversion.m_from||!conversion.m_to)continue;
                string from=conversion.m_from.name,to=conversion.m_to.name;
                if(conversion.m_from.m_itemData.m_shared.m_itemType!=ItemDrop.ItemData.ItemType.Material||
                    conversion.m_to.m_itemData.m_shared.m_itemType!=ItemDrop.ItemData.ItemType.Material)continue;
                bool known=categories.TryGetValue(to,out var kind)&&kind=="ingots";
                if(!known&&from!=to+"Ore")continue;
                // Never steal a seeded wood/fuel/metal category through an unusual conversion.
                if(!categories.ContainsKey(from))categories[from]="ores";
                if(!categories.ContainsKey(to))categories[to]="ingots";
            }
        }
        foreach(var pair in ArmoryAssignment.Build(db))categories[pair.Key]=pair.Value;
        pantry=PantryAssignment.Build(db,currentScene);
        foreach(var pair in pantry)if(!categories.ContainsKey(pair.Key))categories[pair.Key]=pair.Value;
        DisplayStorage.Assign(db,categories);
        foreach(var rule in overrides.Split(';'))
        {
            var pair=rule.Split('=');if(pair.Length!=2)continue;
            string id=pair[0].Trim(),category=pair[1].Trim().ToLowerInvariant();
            if(id.Length==0)continue;
            if(category=="none"){categories.Remove(id);pantry.Remove(id);}
            else if(new[]{"ingredients","reagents","lumber","ingots","ores","coal","hides","textiles","feathers","bones","masonry","food","produce","meat","fish","grain","arrows","bolts","weapons","longweapons","bows","crossbows","shields","armor","trophies","gems","valuables"}.Contains(category)&&(DisplayStorage.Enabled||(category!="trophies"&&category!="gems"&&category!="valuables"))){categories[id]=category;pantry.Remove(id);}
        }
    }
    private static string MaterialCategory(string id)
    {
        if(id.EndsWith("Hide",StringComparison.Ordinal)||id.EndsWith("Pelt",StringComparison.Ordinal)||id=="LeatherScraps"||id=="Leatherstraps")return "hides";
        if(id.EndsWith("Thread",StringComparison.Ordinal)||id.EndsWith("Cloth",StringComparison.Ordinal)||id=="JuteRed"||id=="JuteBlue")return "textiles";
        if(id.EndsWith("Feather",StringComparison.Ordinal)||id.EndsWith("Feathers",StringComparison.Ordinal))return "feathers";
        if(id.EndsWith("Bone",StringComparison.Ordinal)||id=="BoneFragments")return "bones";
        return "";
    }
    internal static void Clear(){database=null;scene=null;recipeCount=prefabCount=itemCount=-1;categories.Clear();pantry.Clear();}
}
