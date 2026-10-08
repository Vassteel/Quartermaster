using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

internal sealed class OwlWork
{
    internal Component Machine;
    internal ApothecaryDisplay Furniture;
    internal int Slot;
    internal bool Retrieving;
    internal ItemDrop Input,Fuel;
    internal Vector3 InputPoint,FuelPoint;
    internal Vector3 Aim(bool fuel)=>fuel&&Fuel?FuelPoint:InputPoint;
    internal ItemDrop Prop(bool fuel)=>fuel&&Fuel?Fuel:Input;

    internal static bool Belongs(Container home,Component machine)
    {
        if(!home||!machine||!Plugin.Enabled.Value||!ContainerRegistry.GetSettings(home).Deposit)return false;
        var n=Automation.Network(home);
        if(n==null||!n.Contains(machine.transform.position)||!Policy.SameGroup(n.Group,Automation.Settings(machine).Group)||
            Automation.Settings(machine).Paused||!PrivateArea.CheckAccess(machine.transform.position,0,false,true))return false;
        // One nearby owl per workstation, including overlapping Deposit Chests.
        var nearest=n.Hubs.OrderBy(h=>(h.transform.position-machine.transform.position).sqrMagnitude)
            .ThenBy(h=>ContainerRegistry.GetView(h).GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
        return nearest==home;
    }
    internal static OwlWork Read(Container home,Component machine)
    {
        if(machine is ApothecaryDisplay cabinet)return cabinet.Work(home);
        if(!Belongs(home,machine))return null;
        var view=Automation.View(machine);if(!view||!view.IsValid())return null;
        var z=view.GetZDO();
        var job=new OwlWork{Machine=machine,InputPoint=machine.transform.position+Vector3.up*.8f};
        if(machine is Beehive hive)
        {
            long age=ZNet.instance.GetTime().Ticks-z.GetLong("Quartermaster.honeyVisit",0L);
            if(Automation.HoneyLevel(hive)<=0&&(age<0||age>TimeSpan.FromMinutes(2).Ticks))return null;
            var network=Automation.Network(home);
            if(!hive.m_honeyItem||network==null)return null;
            // An unset honey limit means unlimited collection; a set limit of 0 disables the visit.
            bool limited=Automation.HasProductionLimit(network,InventoryTransfers.PrefabId(hive.m_honeyItem));
            if(limited&&Automation.EffectiveCap(network,InventoryTransfers.PrefabId(hive.m_honeyItem))<=0)return null;
            job.Input=hive.m_honeyItem;
            job.InputPoint=hive.m_spawnPoint?hive.m_spawnPoint.position:hive.transform.position+Vector3.up*.6f;
        }
        else if(machine is Smelter s)
        {
            if(z.GetInt(ZDOVars.s_queued)<=0||s.m_secPerProduct<=0||
                (s.m_maxFuel>0&&z.GetFloat(ZDOVars.s_fuel)<=0)||
                Traverse.Create(s).Field<bool>("m_blockedSmoke").Value||
                (s.m_requiresRoof&&!Traverse.Create(s).Field<bool>("m_haveRoof").Value)||
                (s.m_windmill&&s.m_windmill.GetPowerOutput()<=0))return null;
            string input=z.GetString("item0");
            job.Input=s.m_conversion.FirstOrDefault(c=>c.m_from&&c.m_from.name==input)?.m_from;
            if(!job.Input)return null;
            job.Fuel=s.m_fuelItem;
            if(s.m_addOreSwitch)job.InputPoint=s.m_addOreSwitch.transform.position;
            job.FuelPoint=s.m_addWoodSwitch?s.m_addWoodSwitch.transform.position:job.InputPoint;
        }
        else if(machine is Fermenter f)
        {
            if(FermenterCompatibility.External)return null;
            int content=z.GetInt(ZDOVars.s_content);
            job.Input=f.m_conversion.FirstOrDefault(c=>c.m_from&&c.m_from.name.GetStableHashCode()==content)?.m_from;
            if(!job.Input||!Traverse.Create(f).Field<bool>("m_hasRoof").Value||Traverse.Create(f).Field<bool>("m_exposed").Value||
                (double)AccessTools.Method(typeof(Fermenter),"GetFermentationTime").Invoke(f,null)>f.m_fermentationDuration)return null;
            if(f.m_addSwitch)job.InputPoint=f.m_addSwitch.transform.position;
        }
        else if(machine is CookingStation rack)
        {
            if(!(bool)AccessTools.Method(typeof(CookingStation),"IsFireLit").Invoke(rack,null))return null;
            for(int i=0;i<rack.m_slots.Length;i++)
            {
                if(z.GetInt("slotstatus"+i)!=0)continue;
                string id=z.GetString("slot"+i);
                var recipe=rack.m_conversion.FirstOrDefault(c=>c.m_from&&c.m_from.name==id);
                if(recipe==null)continue;
                job.Input=recipe.m_from;job.InputPoint=rack.m_slots[i].position;break;
            }
            if(!job.Input)return null;
        }
        else return null;
        return job;
    }
    internal static OwlWork Next(Container home,IDictionary<int,float> visited,ISet<int> tour,Vector3 from)
    {
        var n=Automation.Network(home);if(n==null)return null;
        // Oldest visit first, then distance. A busy smelter cannot monopolize him.
        foreach(var machine in n.Machines.Concat(n.Chests.Where(c=>c).Select(c=>(Component)c.GetComponent<ApothecaryDisplay>())).Where(m=>m&&!tour.Contains(m.GetInstanceID())).OrderBy(m=>visited.TryGetValue(m.GetInstanceID(),out var t)?t:float.NegativeInfinity)
            .ThenBy(m=>(m.transform.position-from).sqrMagnitude))
        {
            if(visited.TryGetValue(machine.GetInstanceID(),out var last)&&Time.time-last<(machine is Beehive?120:12))continue;
            var job=Read(home,machine);if(job!=null)return job;
        }
        return null;
    }
}
