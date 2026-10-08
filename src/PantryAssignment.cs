using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster;

// Secondary food families preserve the established ingredient/apothecary assignments.
internal static class PantryAssignment
{
    internal static Dictionary<string,string> Build(ObjectDB db,ZNetScene scene)
    {
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        var prepared=new HashSet<string>(StringComparer.Ordinal);
        foreach(var recipe in db.m_recipes)
            if(recipe&&recipe.m_item&&Food(recipe.m_item))prepared.Add(recipe.m_item.name);
        if(scene)
        foreach(var prefab in scene.m_prefabs)
        {
            if(!prefab)continue;var cooker=prefab.GetComponent<CookingStation>();
            if(!cooker||cooker.m_conversion==null)continue;
            foreach(var conversion in cooker.m_conversion)
                if(conversion.m_to&&Food(conversion.m_to))prepared.Add(conversion.m_to.name);
        }
        foreach(var prefab in db.m_items)
        {
            if(!prefab)continue;var item=prefab.GetComponent<ItemDrop>();if(!item)continue;
            var data=item.m_itemData.m_shared;string id=prefab.name;
            if(data.m_itemType==ItemDrop.ItemData.ItemType.Fish){result[id]="fish";continue;}
            if(data.m_itemType!=ItemDrop.ItemData.ItemType.Material&&data.m_itemType!=ItemDrop.ItemData.ItemType.Consumable)continue;
            if(data.m_consumeStatusEffect!=null)continue;
            if(id=="Barley"||id.EndsWith("Flour",StringComparison.Ordinal)){result[id]="grain";continue;}
            if(id=="FishRaw"){result[id]="fish";continue;}
            if(!Food(item))continue;
            result[id]=prepared.Contains(id)?"food":id.EndsWith("Meat",StringComparison.Ordinal)?"meat":"produce";
        }
        return result;
    }
    private static bool Food(ItemDrop item)
    {
        var s=item.m_itemData.m_shared;
        return (s.m_itemType==ItemDrop.ItemData.ItemType.Material||s.m_itemType==ItemDrop.ItemData.ItemType.Consumable)&&
            s.m_consumeStatusEffect==null&&(s.m_food>0||s.m_foodStamina>0||s.m_foodEitr>0);
    }
}
