using System;
using System.Collections.Generic;
using UnityEngine;
namespace Quartermaster;

// Ammunition follows weapon compatibility tokens, not item names or translated labels.
internal static class ArmoryAssignment
{
    internal static Dictionary<string,string> Build(ObjectDB db)
    {
        var ammo=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var prefab in db.m_items)
        {
            var drop=prefab?prefab.GetComponent<ItemDrop>():null;if(!drop)continue;
            var shared=drop.m_itemData.m_shared;string kind=Equipment(shared);
            int flag=kind=="bows"?1:kind=="crossbows"?2:0;
            if(flag==0||string.IsNullOrEmpty(shared.m_ammoType))continue;
            ammo.TryGetValue(shared.m_ammoType,out var previous);ammo[shared.m_ammoType]=previous|flag;
        }
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var prefab in db.m_items)
        {
            var drop=prefab?prefab.GetComponent<ItemDrop>():null;if(!drop)continue;
            var shared=drop.m_itemData.m_shared;string category=Equipment(shared);
            if(shared.m_itemType==ItemDrop.ItemData.ItemType.Ammo)
            {
                int flags=0;
                if(!string.IsNullOrEmpty(shared.m_ammoType))ammo.TryGetValue(shared.m_ammoType,out flags);
                // Shared/ambiguous modded ammunition stays unassigned until overridden.
                if(flags==1)category="arrows";
                else if(flags==2)category="bolts";
                else if(flags==0)category=shared.m_skillType==Skills.SkillType.Bows?"arrows":shared.m_skillType==Skills.SkillType.Crossbows?"bolts":"";
            }
            if(category.Length>0)result[prefab.name]=category;
        }
        return result;
    }
    private static string Equipment(ItemDrop.ItemData.SharedData shared)
    {
        var type=shared.m_itemType;var skill=shared.m_skillType;
        if(type==ItemDrop.ItemData.ItemType.Helmet||type==ItemDrop.ItemData.ItemType.Chest||type==ItemDrop.ItemData.ItemType.Legs||type==ItemDrop.ItemData.ItemType.Shoulder||type==ItemDrop.ItemData.ItemType.Hands)return "armor";
        if(type==ItemDrop.ItemData.ItemType.Shield)return "shields";
        bool weapon=type==ItemDrop.ItemData.ItemType.OneHandedWeapon||type==ItemDrop.ItemData.ItemType.TwoHandedWeapon||type==ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft||type==ItemDrop.ItemData.ItemType.Bow;
        if(!weapon)return "";
        if(skill==Skills.SkillType.Crossbows)return "crossbows";
        if(type==ItemDrop.ItemData.ItemType.Bow||skill==Skills.SkillType.Bows)return "bows";
        if(skill==Skills.SkillType.ElementalMagic||skill==Skills.SkillType.BloodMagic||skill==Skills.SkillType.Fishing)return "";
        if(skill==Skills.SkillType.Polearms||skill==Skills.SkillType.Spears||type==ItemDrop.ItemData.ItemType.TwoHandedWeapon||type==ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)return "longweapons";
        return "weapons";
    }
}
