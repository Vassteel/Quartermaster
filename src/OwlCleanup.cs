using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Items are secured in the real chest at pickup; carried meshes are purely cosmetic.
// No transient NPC inventory can be lost on disconnect, culling or route recovery.
internal static class OwlCleanup
{
    private static readonly Dictionary<ItemDrop,float> Drops=new Dictionary<ItemDrop,float>();
    private static readonly System.Reflection.FieldInfo Instances=AccessTools.Field(typeof(ItemDrop),"s_instances");
    private static bool recoverRegistry=true;
    internal static void Track(ItemDrop drop) { if(drop&&!Drops.ContainsKey(drop))Drops.Add(drop,Time.time); }
    internal static void Clear(){Drops.Clear();recoverRegistry=true;}
    internal static bool Requested(Container home) => home && ContainerRegistry.GetSettings(home).PickupRequestedUntil > DateTime.UtcNow.Ticks;
    internal static bool CanStart(Container home) => Requested(home) && ContainerRegistry.CanAutomate(home);
    internal static float Radius(Container home) => Math.Min(Plugin.Range.Value, Math.Max(1f, Math.Min(100f, ContainerRegistry.GetSettings(home).PickupRange > 0 ? ContainerRegistry.GetSettings(home).PickupRange : 25f)));
    internal static void Complete(Container home)
    {
        if(!home||!ContainerRegistry.CanAutomate(home))return;
        var settings=JsonUtility.FromJson<ChestSettings>(JsonUtility.ToJson(ContainerRegistry.GetSettings(home)));
        settings.PickupRequestedUntil=0;ContainerRegistry.SaveSettings(home,settings);
    }
    internal static bool Eligible(Container home,ItemDrop drop)
    {
        if(!home||!drop||!Plugin.Enabled.Value||(!Plugin.OwlCollectDroppedItems.Value&&!Requested(home))||
            !ContainerRegistry.GetSettings(home).Deposit||!ContainerRegistry.CanAutomate(home)||
            !Automation.Owned(drop)||!drop.CanPickup()||drop.GetComponent<Piece>()||
            drop.m_itemData?.m_shared==null||drop.m_itemData.m_stack<=0||!drop.m_autoPickup||
            !PrivateArea.CheckAccess(drop.transform.position,0,false,true)||
            PickupFilter.IsIgnored(Player.m_localPlayer,InventoryTransfers.ItemId(drop.m_itemData)))return false;
        if(!Drops.TryGetValue(drop,out var seen)||Time.time-seen<15)return false;
        float range=Requested(home)?Radius(home):Mathf.Min(Plugin.Range.Value,25);
        if((drop.transform.position-home.transform.position).sqrMagnitude>range*range)return false;
        var n=Automation.Network(home);
        if(n==null||!n.Contains(drop.transform.position))return false;
        if(!Requested(home)&&n.Hubs.OrderBy(h=>(h.transform.position-drop.transform.position).sqrMagnitude)
            .ThenBy(h=>ContainerRegistry.GetView(h).GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault()!=home)return false;
        var z=Automation.View(drop).GetZDO();if(z.GetBool("Quartermaster.animalFood",false))return false;string outputGroup=z.GetString(ProductionOutput.GroupKey,"");
        if(outputGroup.Length>0&&(!Policy.SameGroup(outputGroup,n.Group)||!ProductionOutput.Ready(drop)))return false;
        return InventoryTransfers.CapacityFor(home.GetInventory(),drop.m_itemData,false)>0;
    }
    internal static ItemDrop Find(Container home,ISet<int> skipped)
    {
        // Character changes can clear local registries while scene objects survive.
        if(recoverRegistry)
        {
            recoverRegistry=false;
            if(Instances.GetValue(null) is List<ItemDrop> existing)foreach(var drop in existing)Track(drop);
        }
        foreach(var drop in Drops.Keys.Where(d=>!d).ToArray())Drops.Remove(drop);
        return Drops.Keys.Where(d=>d&&!skipped.Contains(d.GetInstanceID())&&Eligible(home,d))
            .OrderBy(d=>(d.transform.position-home.transform.position).sqrMagnitude).FirstOrDefault();
    }
    internal static ItemDrop.ItemData TakeOne(Container home,ItemDrop drop)
    {
        if(!Eligible(home,drop))return null;
        var sample=drop.m_itemData.Clone();sample.m_stack=1;
        if(!InventoryTransfers.ReceiveOne(home.GetInventory(),sample,()=>drop&&Automation.Owned(drop)&&drop.RemoveOne()))return null;
        ChestVisual.Pulse(home);return sample;
    }
}
