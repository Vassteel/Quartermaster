using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[HarmonyPatch]
internal static class WardDeterrence
{
    private static readonly MethodInfo Flee=AccessTools.Method(typeof(BaseAI),"Flee",new[]{typeof(float),typeof(Vector3)});
    private static readonly MethodInfo Wake=AccessTools.Method(typeof(MonsterAI),"Wakeup");
    private static readonly FieldInfo Areas=AccessTools.Field(typeof(PrivateArea),"m_allAreas");
    private static readonly FieldInfo Creature=AccessTools.Field(typeof(MonsterAI),"m_targetCreature");
    private static readonly FieldInfo Structure=AccessTools.Field(typeof(MonsterAI),"m_targetStatic");

    // Native BaseAI maintenance and ownership checks have already run. Returning false
    // here stops MonsterAI from acquiring targets or attacking during the retreat.
    [HarmonyPostfix, HarmonyPatch(typeof(BaseAI),nameof(BaseAI.UpdateAI))]
    private static void AfterBaseAI(BaseAI __instance,float dt,ref bool __result)
    {
        if(!__result||!Plugin.Enabled.Value||!Plugin.WardRepelsMonsters.Value||!(__instance is MonsterAI monster))return;
        var body=monster.GetComponent<Character>();
        if(!body||body.IsTamed()||body.IsDead())return;
        var faction=body.GetFaction();
        if(faction==Character.Faction.Players||faction==Character.Faction.AnimalsVeg||
            faction==Character.Faction.Dverger&&!monster.IsAggravated())return;
        var position=body.transform.position;
        PrivateArea closest=null;float penetration=0;
        foreach(var ward in (List<PrivateArea>)Areas.GetValue(null))
        {
            if(!ward||!ward.isActiveAndEnabled)continue;
            var view=ward.GetComponent<ZNetView>();
            if(!view||!view.IsValid()||!view.GetZDO().GetBool(ZDOVars.s_enabled))continue;
            float depth=ward.m_radius+4f-Vector3.Distance(position,ward.transform.position);
            if(depth>penetration){penetration=depth;closest=ward;}
        }
        if(!closest)return;
        if(monster.IsSleeping())Wake.Invoke(monster,null);
        Creature.SetValue(monster,null);Structure.SetValue(monster,null);
        var source=closest.transform.position;
        // Avoid a zero flee direction for a creature spawned directly on the ward.
        if(Utils.DistanceXZ(position,source)<.1f)source=position-body.transform.forward;
        Flee.Invoke(monster,new object[]{dt,source});
        __result=false;
    }
}
