using Quartermaster;
using UnityEngine;

// Execute the shipped request loop and transfer code. The host supplies access state and routing.
public class Ship : MonoBehaviour { public Container Cargo; public string Invalid=""; }
namespace UnityEngine { public static class Time { public static float time; } }
namespace Quartermaster
{
    public static partial class HelmsmanCargoAccess
    {
        internal static Container Hub;
        internal static bool Enabled=true;
        internal static string ValidateBoat(Ship s,out Container c) { c=s.Cargo;return Enabled?s.Invalid:"Missing mod"; }
        internal static bool HubInRange(Container h,Ship s)=>h && h.Accessible && h.Settings.Deposit &&
            (h.transform.position-s.transform.position).sqrMagnitude<=Plugin.Range.Value*Plugin.Range.Value;
        internal static Container FindHub(Ship s)=>HubInRange(Hub,s)?Hub:null;
        public static string Check(Ship s) { var invalid=ValidateBoat(s,out _);return invalid.Length>0?invalid:FindHub(s)?"":"No base"; }
    }
    internal class BaseNetwork { internal string Group; internal List<Container> Hubs,Chests; }
    internal static class Automation
    {
        internal static string ExplainUnsorted(BaseNetwork n,Container source,ItemDrop.ItemData item)=>"Where should "+item.m_shared.m_name+" go?";
        internal static int Route(BaseNetwork n,ItemDrop.ItemData item,int count,Vector3 point,Inventory source)
        {
            int left=count;
            foreach(var c in n.Chests.Where(c=>ContainerRegistry.IsUsable(c,false)&&c.Settings.Accepts(InventoryTransfers.ItemId(item))))
            { left-=InventoryTransfers.Move(source,c.Inventory,item,left,false);if(left==0)break; }
            return count-left;
        }
    }
}
internal static class CargoUnloadingTests
{
    internal static void Run(Action<bool,string> check)
    {
        ItemDrop.ItemData Item(string id,int count,int slot=0)=>new() {m_dropPrefab=new Prefab{name=id},m_shared=new ItemDrop.SharedData{m_name=id},m_stack=count,m_gridPos=new Vector2i(slot,0)};
        Container Chest(bool deposit=false)=>new(){Inventory=new(4,1),Settings=new(){Deposit=deposit,Group="home"}};
        var hub=Chest(true);var dest=Chest();dest.Settings.Remembered.AddRange(new[]{"Wood","Stone"});
        var cargo=Chest();var ship=new Ship{Cargo=cargo};
        ContainerRegistry.All.Clear();ContainerRegistry.All.AddRange(new[]{hub,dest});
        HelmsmanCargoAccess.Hub=hub;HelmsmanCargoAccess.Enabled=true;
        Time.time=0;cargo.Inventory.GetAllItems().AddRange(new[]{Item("Wood",20),Item("Stone",30,1)});
        check(HelmsmanCargoAccess.Check(ship)==""&&cargo.Inventory.GetAllItems().Count==2,"availability checks never unload");
        var request=HelmsmanCargoAccess.BeginUnload(ship);
        check(cargo.Inventory.GetAllItems().Count==2&&dest.Inventory.GetAllItems().Count==0,"request creation waits for a sorting step");
        var sample=HelmsmanCargoAccess.UnloadNext(request);
        check(sample.m_stack==20&&sample.m_dropPrefab.name=="Wood"&&cargo.Inventory.GetAllItems().Count==1,"unload moves one boat slot toward base, returns an intact visual sample");
        check(HelmsmanCargoAccess.UnloadNext(request)==null&&cargo.Inventory.GetAllItems().Count==1,"same-frame polling cannot unload a second slot");
        Time.time=2;HelmsmanCargoAccess.UnloadNext(request);
        check(cargo.Inventory.GetAllItems().Count==0&&dest.Inventory.GetAllItems().Sum(i=>i.m_stack)==50,"two steps conserve boat plus base quantities");
        Time.time=4;HelmsmanCargoAccess.UnloadNext(request);
        check(HelmsmanCargoAccess.Finished(request)&&HelmsmanCargoAccess.Status(request).Contains("emptied"),"empty hold completes request");
        cargo.Inventory.GetAllItems().Add(Item("Wood",1));Time.time=6;
        check(HelmsmanCargoAccess.UnloadNext(request)==null&&cargo.Inventory.GetAllItems().Count==1,"completed request never resumes for new cargo");
        request=HelmsmanCargoAccess.BeginUnload(ship);HelmsmanCargoAccess.Cancel(request);
        check(HelmsmanCargoAccess.UnloadNext(request)==null,"cancelled request performs no transfer");
        cargo.Inventory.GetAllItems().Clear();cargo.Inventory.GetAllItems().AddRange(new[]{Item("Unknown",4),Item("Wood",2,1)});
        request=HelmsmanCargoAccess.BeginUnload(ship);sample=HelmsmanCargoAccess.UnloadNext(request);
        check(sample.m_stack==2&&cargo.Inventory.GetAllItems().Single().m_dropPrefab.name=="Unknown","unmatched cargo remains aboard without blocking a later match");
        Time.time+=2;HelmsmanCargoAccess.UnloadNext(request);
        check(HelmsmanCargoAccess.Finished(request)&&HelmsmanCargoAccess.Status(request).Contains("Remaining cargo"),"blocked leftovers finish with an explanation");
        foreach(var why in new[]{"Hold open","Helm occupied","Boat moving","Ownership lost","Ward denied"})
        {
            request=HelmsmanCargoAccess.BeginUnload(ship);ship.Invalid=why;
            check(HelmsmanCargoAccess.UnloadNext(request)==null&&HelmsmanCargoAccess.Finished(request),"access revalidation stops: "+why);ship.Invalid="";
        }
        request=HelmsmanCargoAccess.BeginUnload(ship);ship.transform.position=new(101,0,0);
        check(HelmsmanCargoAccess.UnloadNext(request)==null&&HelmsmanCargoAccess.Finished(request),"leaving pinned base stops request");ship.transform.position=new();
        request=HelmsmanCargoAccess.BeginUnload(ship);Player.m_localPlayer=new();
        check(HelmsmanCargoAccess.UnloadNext(request)==null&&HelmsmanCargoAccess.Finished(request),"changing player stops request");
        request=HelmsmanCargoAccess.BeginUnload(ship);HelmsmanCargoAccess.Enabled=false;
        check(HelmsmanCargoAccess.UnloadNext(request)==null&&HelmsmanCargoAccess.Finished(request),"disabling integration stops request");HelmsmanCargoAccess.Enabled=true;
        cargo.Inventory.GetAllItems().Clear();cargo.Inventory.GetAllItems().Add(Item("Wood",20));
        dest.Inventory.GetAllItems().Clear();dest.Inventory=new Inventory(1,1);dest.Inventory.GetAllItems().Add(Item("Wood",45));
        request=HelmsmanCargoAccess.BeginUnload(ship);sample=HelmsmanCargoAccess.UnloadNext(request);
        check(sample.m_stack==5&&cargo.Inventory.GetAllItems().Single().m_stack==15,"partial room preserves the boat remainder");
        Time.time+=2;HelmsmanCargoAccess.UnloadNext(request);check(HelmsmanCargoAccess.Finished(request),"full storage stops without dropping remainder");
        foreach(var mode in new[]{"range","group","access","busy"})
        {
            dest.Inventory.GetAllItems().Clear();dest.transform.position=mode=="range"?new(101,0,0):new();
            dest.Settings.Group=mode=="group"?"other":"home";dest.Accessible=mode!="access";dest.InUse=mode=="busy";
            request=HelmsmanCargoAccess.BeginUnload(ship);
            check(HelmsmanCargoAccess.UnloadNext(request)==null&&cargo.Inventory.GetAllItems().Single().m_stack==15,"destination exclusion: "+mode);
        }
        ContainerRegistry.All.Clear();
    }
}
