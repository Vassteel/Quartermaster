using System;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

public static partial class HelmsmanCargoAccess
{
    private sealed class UnloadRequest
    {
        internal Ship Ship;
        internal Container Hub;
        internal Player Requester;
        internal string Status;
        internal int Moved, Slots;
        internal bool Done;
        internal float Next;
    }
    // A request stays attached to this nearby base; it never follows the ship to another base.
    private static BaseNetwork Destinations(Container hub)
    {
        string group=ContainerRegistry.GetSettings(hub).Group;
        float radius=Plugin.Range.Value;
        return new BaseNetwork { Group=group,Hubs=new System.Collections.Generic.List<Container>{hub},
            Chests=ContainerRegistry.All.Where(c=>ContainerRegistry.Accessible(c) &&
                Policy.SameGroup(ContainerRegistry.GetSettings(c).Group,group) &&
                (c.transform.position-hub.transform.position).sqrMagnitude<=radius*radius).ToList() };
    }
    // Only the explicit gull-menu command creates this token. No automatic or saved jobs.
    public static object BeginUnload(Ship ship)
    {
        string reason=Check(ship);
        if(reason.Length>0) throw new InvalidOperationException(reason);
        return new UnloadRequest { Ship=ship,Hub=FindHub(ship),Requester=Player.m_localPlayer,Status="Unloading cargo into base storage…" };
    }
    public static bool Finished(object token) => !(token is UnloadRequest request) || request.Done;
    public static string Status(object token) => token is UnloadRequest request ? request.Status : "No cargo request.";
    public static void Cancel(object token)
    {
        if(token is UnloadRequest request && !request.Done) { request.Done=true;request.Status="Cargo request stopped."; }
    }
    public static ItemDrop.ItemData UnloadNext(object token)
    {
        if(!(token is UnloadRequest r) || r.Done) return null;
        void Stop(string reason) { r.Done=true;r.Status=reason+" Unloaded "+r.Moved+" items from "+r.Slots+" slots."; }
        string invalid=ValidateBoat(r.Ship,out var cargo);
        if(invalid.Length>0) { Stop(invalid);return null; }
        if(r.Requester!=Player.m_localPlayer || !HubInRange(r.Hub,r.Ship)) { Stop("The boat left its requested base or access changed.");return null; }
        if(Time.time<r.Next) return null;
        r.Next=Time.time+2f;
        var network=Destinations(r.Hub);
        ItemDrop.ItemData sample=null;
        var sorted=DepositSorting.OneSlot(cargo.GetInventory().GetAllItems(),(item,count)=>
        {
            var copy=item.Clone();
            int moved=Automation.Route(network,item,count,r.Ship.transform.position,cargo.GetInventory());
            if(moved>0) { copy.m_stack=moved;sample=copy; }
            return moved;
        });
        if(sorted.Moved>0)
        {
            r.Moved+=sorted.Moved;r.Slots++;
            r.Status="Unloaded "+r.Slots+" slots · "+r.Moved+" items.";
            return sample;
        }
        Stop(cargo.GetInventory().GetAllItems().Any(i=>i.m_stack>0)
            ? "Remaining cargo: "+Automation.ExplainUnsorted(network,cargo,sorted.BlockedItem ?? cargo.GetInventory().GetAllItems().First(i=>i.m_stack>0))
            : "Cargo hold emptied.");
        return null;
    }
}
