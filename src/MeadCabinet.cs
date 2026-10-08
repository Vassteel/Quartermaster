using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace Quartermaster;

[HarmonyPatch]
internal static class MeadCabinet
{
    internal const string Prefab="Quartermaster_MeadCabinet";
    [ThreadStatic] private static int loading;
    private static ZNetScene scene;
    private static int prefabCount=-1;
    private static readonly HashSet<string> fermented=new HashSet<string>(StringComparer.Ordinal);
    internal static bool IsMead(string id)
    {
        var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab(id):null;
        return prefab&&IsMead(prefab.GetComponent<ItemDrop>()?.m_itemData,id);
    }
    internal static bool IsMead(ItemDrop.ItemData item)=>IsMead(item,InventoryTransfers.ItemId(item));
    private static bool IsMead(ItemDrop.ItemData item,string id)
    {
        if(item?.m_shared==null)return false;
        var current=ZNetScene.instance;
        if(current!=scene||(current?current.m_prefabs.Count:0)!=prefabCount)
        {
            scene=current;prefabCount=current?current.m_prefabs.Count:0;fermented.Clear();
            if(current)foreach(var prefab in current.m_prefabs)
            {
                var f=prefab?prefab.GetComponent<Fermenter>():null;
                if(!f||f.m_conversion==null)continue;
                foreach(var conversion in f.m_conversion)if(conversion.m_to)fermented.Add(conversion.m_to.name);
            }
        }
        return MeadStoragePolicy.Accepts(id,item.m_shared.m_itemType==ItemDrop.ItemData.ItemType.Consumable,
            fermented.Contains(id)&&item.m_shared.m_consumeStatusEffect!=null);
    }
    internal static ItemDrop.ItemData[] DisplayItems(IEnumerable<ItemDrop.ItemData> items)=>items.Where(IsMead)
        .OrderBy(item=>item.m_gridPos.y).ThenBy(item=>item.m_gridPos.x)
        .GroupBy(item=>InventoryTransfers.ItemId(item),StringComparer.Ordinal).Select(group=>group.First()).Take(MeadStoragePolicy.DisplayCount).ToArray();
    internal static bool Restricted(Inventory inventory)
    {
        var owner=ContainerRegistry.OwnerOf(inventory);
        return owner&&ContainerRegistry.PrefabName(owner)==Prefab;
    }
    internal static bool Allows(Inventory inventory,ItemDrop.ItemData item)=>loading>0||!Restricted(inventory)||IsMead(item);
    internal static void Clear(){scene=null;prefabCount=-1;fermented.Clear();}

    // Check before native drag/swap removes either item. Also guard the reverse
    // direction when a bottle is swapped out for a non-mead from player inventory.
    [HarmonyPatch(typeof(InventoryGrid),"DropItem"),HarmonyPrefix]
    private static bool Drop(Inventory ___m_inventory,Inventory __0,ItemDrop.ItemData __1,int __2,Vector2i __3,ref bool __result)
    {
        if(!Allows(___m_inventory,__1)){__result=false;return false;}
        var other=___m_inventory.GetItemAt(__3.x,__3.y);
        bool swaps=other!=null&&other!=__1&&__2==__1.m_stack&&(other.m_shared.m_name!=__1.m_shared.m_name||
            (__1.m_shared.m_maxQuality>1&&other.m_quality!=__1.m_quality)||__1.m_shared.m_maxStackSize==1);
        if(swaps&&!Allows(__0,other)){__result=false;return false;}
        return true;
    }
    [HarmonyPatch(typeof(Inventory),"AddItem",new[]{typeof(ItemDrop.ItemData)}),HarmonyPrefix]
    private static bool Add(Inventory __instance,ItemDrop.ItemData __0,ref bool __result)
    {if(Allows(__instance,__0))return true;__result=false;return false;}
    [HarmonyPatch(typeof(Inventory),"AddItem",new[]{typeof(ItemDrop.ItemData),typeof(Vector2i)}),HarmonyPrefix]
    private static bool AddAt(Inventory __instance,ItemDrop.ItemData __0,ref bool __result)=>Add(__instance,__0,ref __result);
    [HarmonyPatch(typeof(Inventory),"AddItem",new[]{typeof(ItemDrop.ItemData),typeof(int),typeof(int),typeof(int),typeof(bool)}),HarmonyPrefix]
    private static bool AddAmount(Inventory __instance,ItemDrop.ItemData __0,ref bool __result)=>Add(__instance,__0,ref __result);
    // Loading must never discard persisted items, even if another mod previously
    // inserted an unsupported item. Such items remain removable by the player.
    [HarmonyPatch(typeof(Inventory),"Load",new[]{typeof(ZPackage),typeof(bool)}),HarmonyPrefix]
    private static void Loading()=>loading++;
    [HarmonyPatch(typeof(Inventory),"Load",new[]{typeof(ZPackage),typeof(bool)}),HarmonyFinalizer]
    private static Exception Loaded(Exception __exception){loading=Math.Max(0,loading-1);return __exception;}
}
