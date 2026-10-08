using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
namespace Quartermaster;

internal static class PlantEasilyCompatibility
{
    internal const string Guid="advize.PlantEasily";
    private static bool installed, ready;
    private static FieldInfo config, ghosts, statuses;
    private static PropertyInfo active, partial, preventInvalid, holding, planting, maxGhosts;
    private static MethodInfo evaluate;
    private static object healthy, lackResources;

    internal static void Initialize(Harmony harmony)
    {
        if (!Chainloader.PluginInfos.TryGetValue(Guid,out var plugin)) return;
        installed=true;
        try
        {
            var assembly=plugin.Instance.GetType().Assembly;
            Type Type(string n)=>assembly.GetType("Advize_PlantEasily."+n,true);
            config=AccessTools.Field(Type("ModContext"),"config");
            var cfg=Type("ModConfig");
            active=AccessTools.Property(cfg,"ModActive");
            partial=AccessTools.Property(cfg,"PreventPartialPlanting");
            preventInvalid=AccessTools.Property(cfg,"PreventInvalidPlanting");
            holding=AccessTools.Property(Type("ModUtils"),"HoldingCultivator");
            planting=AccessTools.Property(Type("PlacementController"),"IsPlanting");
            ghosts=AccessTools.Field(Type("GhostGrid"),"ExtraGhosts");
            statuses=AccessTools.Field(Type("GhostGrid"),"GhostPlacementStatus");
            maxGhosts=AccessTools.Property(Type("GhostGrid"),"MaxActiveGhosts");
            evaluate=AccessTools.Method(Type("GhostStatus"),"EvaluateStatus");
            healthy=Enum.Parse(Type("Status"),"Healthy");
            lackResources=Enum.Parse(Type("Status"),"LackResources");
            if(new MemberInfo[]{config,active,partial,preventInvalid,holding,planting,ghosts,statuses,maxGhosts,evaluate}.Any(m=>m==null))
                throw new MissingMemberException("Plant Easily batch interface changed");
            var replant=AccessTools.Method(Type("InteractPatches"),"Prefix",new[]{typeof(Pickable),typeof(bool)});
            if(replant==null)throw new MissingMethodException("Plant Easily replant hook missing");
            harmony.Patch(replant,transpiler:new HarmonyMethod(typeof(PlantEasilyCompatibility),nameof(ReplantRequirements)));
            ready=true;
            Plugin.Log.LogInfo("Plant Easily batch funding enabled");
        }
        catch(Exception error)
        { Plugin.Log.LogWarning("Plant Easily compatibility unavailable; planting from storage is paused: "+error.Message); }
    }

    internal static bool TryPrepare(Player player,Piece piece,GameObject root,out bool funded)
    {
        funded=false;
        if(!installed || !piece || (!piece.GetComponent<Plant>()&&!piece.GetComponent<Pickable>()))return false;
        if(!ready) { player.Message(MessageHud.MessageType.TopLeft,"Plant Easily compatibility unavailable; disable crafting from chests to plant"); return true; }
        var cfg=config.GetValue(null);
        if(cfg==null || !(bool)active.GetValue(cfg) || !(bool)holding.GetValue(null))return false;
        try
        {
            if(!root || (bool)planting.GetValue(null))return true;
            var extra=(IList)ghosts.GetValue(null);
            var state=(IList)statuses.GetValue(null);
            int count=1+(int)maxGhosts.GetValue(null);
            if(count<1 || count>extra.Count+1 || state.Count<count)return true;
            bool strict=(bool)preventInvalid.GetValue(cfg);
            var valid=new bool[count];var geometry=new object[count];
            for(int i=0;i<count;i++)
            {
                // Ask Plant Easily itself about its existing ghosts; never move them.
                geometry[i]=evaluate.Invoke(null,new[]{i==0?(object)root:extra[i-1],healthy});
                valid[i]=Equals(geometry[i],healthy)||(!strict&&Convert.ToInt32(geometry[i])>Convert.ToInt32(lackResources));
            }
            int affordable=SharedCrafting.AffordablePlants(player,piece,count);
            var selected=PlantBatchPlan.Select(valid,affordable,(bool)partial.GetValue(cfg));
            if(selected.Length==0)
            { player.Message(MessageHud.MessageType.TopLeft,"Planting group is invalid or needs more materials");return true; }
            if(!SharedCrafting.FundPlants(player,piece,selected.Length))return true;
            var chosen=new HashSet<int>(selected);
            for(int i=0;i<count;i++)state[i]=chosen.Contains(i)?geometry[i]:lackResources;
            funded=true;
            return true;
        }
        catch(Exception error)
        {Plugin.Log.LogError("Plant Easily funding failed: "+error);return true;}
    }

    private static IEnumerable<CodeInstruction> ReplantRequirements(IEnumerable<CodeInstruction> instructions)
    {
        var original=AccessTools.Method(typeof(Player),"HaveRequirements",new[]{typeof(Piece),typeof(Player.RequirementMode)});
        var replacement=AccessTools.Method(typeof(PlantEasilyCompatibility),nameof(FundedReplant));
        int replaced=0;
        foreach(var instruction in instructions)
        {
            var copy=new CodeInstruction(instruction);
            if(copy.Calls(original)){copy.opcode=OpCodes.Call;copy.operand=replacement;replaced++;}
            yield return copy;
        }
        if(replaced!=1)throw new InvalidOperationException("Plant Easily replant payment call changed");
    }
    private static bool FundedReplant(Player player,Piece piece,Player.RequirementMode mode)
    {
        if(!Plugin.Enabled.Value || !Plugin.CraftFromContainers.Value || player!=Player.m_localPlayer)
            return player.HaveRequirements(piece,mode);
        if(player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))return true;
        // The caller creates one plant then consumes once. Fund it before permitting creation.
        if(!SharedCrafting.FundPlants(player,piece,1))return false;
        bool before=CraftingPatches.LocalCountsOnly;CraftingPatches.LocalCountsOnly=true;
        try{return player.HaveRequirements(piece,mode);}
        finally{CraftingPatches.LocalCountsOnly=before;}
    }
}
