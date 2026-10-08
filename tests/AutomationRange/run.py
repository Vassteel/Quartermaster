"""Exercise production range, withdrawal and routing code against two loaded bases."""
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
source = (root / 'src/Automation.cs').read_text()
def section(start, end):
    return source[source.index(start):source.index(end, source.index(start))]
network = section('internal sealed class BaseNetwork', 'internal static partial class Automation')
take = section('    private static bool Take(', '    private static void Restore(')
lookup = section('    internal static BaseNetwork Network(', '    internal static void Tick(')
can_run = section('    private static bool CanRun(', '    private static void ProcessMachine(')
executor = section('    internal static bool Executor(', '    // Hives consume nothing')
routing = section('    private static List<Container> Destinations(', '    internal static int Route(')
code = r'''
using System;
using System.Collections.Generic;
using System.Linq;
class Vector3 {
 public float x,y,z; public Vector3(float x=0,float y=0,float z=0){this.x=x;this.y=y;this.z=z;}
 public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 public float sqrMagnitude=>x*x+y*y+z*z;
}
class Transform { public Vector3 position=new(); }
class Component { public Transform transform=new(); public bool Alive=true, Owned=true; public MachineSettings Settings=new(); public ContainerRegistry.View MachineView=new(); public static implicit operator bool(Component c)=>c!=null&&c.Alive; }
class Container:Component { public new ChestSettings Settings=new(); public Inventory Inventory=new(); public Inventory GetInventory()=>Inventory; public ContainerRegistry.View View=new(); }
class Inventory {
 public List<ItemDrop.ItemData> Items=new(); public List<ItemDrop.ItemData> GetAllItems()=>Items;
 public bool RemoveItem(ItemDrop.ItemData item,int amount){if(item.m_stack<amount)return false;item.m_stack-=amount;return true;}
 public int Room=100;
}
class ItemDrop { public string Id="Ore"; public class ItemData { public string Id="Ore"; public int m_stack=10,m_worldLevel=0; public ItemData Clone()=>(ItemData)MemberwiseClone(); } }
class MachineSettings { public string Group="Home"; public bool Paused; }
class ChestSettings { public string Group="Home"; public bool Deposit,Overflow,Preferred; public HashSet<string> Remembered=new(); }
static class Plugin { public static Setting Range=new(); public class Setting { public float Value=100; } }
static class NativeStorageAccess { public static bool CanWrite(object v)=>true; }
static class ContainerRegistry {
 public static List<Container> All=new(); public static bool Accessible(Container c)=>c;
 public static ChestSettings GetSettings(Container c)=>c.Settings;
 public static bool CanAutomate(Container c)=>c&&c.Owned&&c.View.IsOwner();
 public static View GetView(Container c)=>c.View;
 public class View { public bool Owner=true,Valid=true; public View GetZDO()=>this; public string m_uid="1"; public bool IsOwner()=>Owner; public bool IsValid()=>Valid; public static implicit operator bool(View v)=>v!=null; }
}
static class StorageObservation { public static Inventory Read(Container c)=>c.Inventory; }
static class ChestOwnership {
 public static List<(ContainerRegistry.View View,string Reason,bool OnlyUnserved)> Requests=new();
 public static bool Request(ContainerRegistry.View view,string reason,bool onlyUnserved=false){Requests.Add((view,reason,onlyUnserved));return view.IsOwner();}
 public static string Describe(ContainerRegistry.View view)=>view.IsOwner()?"local":"another player";
}
static class FurnitureAssignment { public static bool Accepts(Container c,ItemDrop.ItemData i)=>true; }
static class InventoryTransfers { public static string PrefabId(ItemDrop i)=>i.Id; public static string ItemId(ItemDrop.ItemData i)=>i.Id; public static int CapacityFor(Inventory inv,ItemDrop.ItemData item,bool matching)=>inv.Room; }
static class Policy { public static string Group(string s)=>s; public static bool SameGroup(string a,string b)=>a==b; }
static class PrivateArea { public static bool Allowed=true; public static bool CheckAccess(Vector3 point,float radius,bool a,bool b)=>Allowed; }
static class Game { public static int m_worldLevel=0; }
'''
code += network + '\nclass Program {\n' + take + routing + lookup + can_run + executor + r'''
 static List<BaseNetwork> networks=new();
 static List<Component> Devices=new();
 static Dictionary<Component,string> Statuses=new();
 static ContainerRegistry.View View(Component c)=>c is Container chest?chest.View:c.MachineView;
 static MachineSettings Settings(Component c)=>c.Settings;
 static bool Owned(Component c)=>c.Owned&&View(c).IsOwner();
 static string status;
 static void SetStatus(Component c,string value)=>status=value;
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static Container Chest(float x,bool hub=false){var c=new Container();c.transform.position=new(x);c.Settings.Deposit=hub; c.Inventory.Items.Add(new());return c;}
 static void Main(){
 var a=Chest(0,true);var b=Chest(1000,true);var ore=Chest(10);var remote=Chest(990);
 var n=new BaseNetwork{Group="Home",Hubs=new(){a,b},Chests=new(){ore,remote},Machines=new()};
 Check(n.Contains(new(1010)),"distant machine still has its own local hub coverage");
 Check(!n.CanTransfer(new(10),new(1010)),"same-name bases cannot share ore across 1000m");
 Check(n.CanTransfer(new(-100),new(100)),"radius is measured from the hub, not between endpoints");
 Check(!n.CanTransfer(new(),new(100.01f)),"outside hub radius denied");
 Check(!n.CanTransfer(new(),new(0,100.01f)),"vertical distance also bounded");
 Check(Take(n,new(1010),new(),s=>true,out var item,out var used)&&used==remote,"remote machine takes only local ore even when distant ore is first");
 Check(ore.Inventory.Items[0].m_stack==10&&remote.Inventory.Items[0].m_stack==9&&item.m_stack==1,"only exact local unit withdrawn");
 n.Chests.Remove(remote);
 Check(!Take(n,new(1010),new(),s=>true,out _,out _)&&ore.Inventory.Items[0].m_stack==10,"empty local base never falls back to distant ore");
 Check(Take(n,new(100),new(),s=>true,out _,out _),"machine exactly on radius can draw nearby ore");
 var before=ore.Inventory.Items[0].m_stack;
 Check(!Take(n,new(100.01f),new(),s=>true,out _,out _)&&ore.Inventory.Items[0].m_stack==before,"out-of-range machine cannot withdraw");
 n.Chests.Add(remote);
 var routes=Destinations(n,new(),new(5),null,true);
 Check(routes.Contains(ore)&&routes.Contains(a)&&!routes.Contains(remote)&&!routes.Contains(b),"output storage and deposit fallback both stay local");
 b.transform.position=new(150); remote.transform.position=new(200);
 Check(!n.CanTransfer(new(-50),new(200)),"overlapping coverage cannot relay between opposite ends");
 routes=Destinations(n,new(),new(),a,true);
 Check(!routes.Contains(remote)&&!routes.Contains(b),"deposit sorting stays within its own radius even with overlapping second hub");
 Check(n.CanTransfer(new(60),new(200)),"two endpoints inside the same second hub remain valid");
 Plugin.Range.Value=50;
 Check(!n.CanTransfer(new(),new(51))&&n.CanTransfer(new(),new(50)),"configured radius honored without cached stale range");
 a.Alive=false;b.Alive=false;
 Check(!n.CanTransfer(new(),new()),"removed hubs cannot authorize supply");
 Plugin.Range.Value=100;
 a.Alive=b.Alive=true;b.transform.position=new(5000);remote.transform.position=new(4990);
 a.Owned=false; b.Owned=false;
 var localMachine=new Component();localMachine.transform.position=new(16);
 var remoteMachine=new Component();remoteMachine.transform.position=new(5003);
 ContainerRegistry.All=new(){a,b,ore,remote};Devices=new(){localMachine,remoteMachine};
 RebuildNetworks();
 Check(networks.Count==2,"same-name bases 5km apart become separate networks");
 var local=Network(localMachine); var far=Network(remoteMachine);
 Check(local!=far&&local.Machines.SequenceEqual(new[]{localMachine}),"local ledger machine list excludes 5003m fire");
 Check(local.Chests.Contains(ore)&&!local.Chests.Contains(remote),"local stock excludes remote chests");
 Check(CanRun(localMachine,out var selected)&&selected==local,"owned local station runs even when neither hub is locally owned");
 localMachine.Owned=false;localMachine.MachineView.Owner=false;
 Check(!CanRun(localMachine,out _)&&Statuses[localMachine].StartsWith("Requesting station ownership"),"foreign station still cannot be mutated; ownership is requested instead");
 localMachine.Owned=true;localMachine.MachineView.Owner=true;localMachine.Settings.Paused=true;
 Check(!CanRun(localMachine,out _),"pause retained");localMachine.Settings.Paused=false;
 PrivateArea.Allowed=false;Check(!CanRun(localMachine,out _),"ward permission retained");PrivateArea.Allowed=true;
 localMachine.transform.position=new(101);Check(!CanRun(localMachine,out _),"station beyond any local hub is rejected");
 b.transform.position=new(200);RebuildNetworks();Check(networks.Count==1,"touching coverage remains connected");
 b.transform.position=new(201);RebuildNetworks();Check(networks.Count==2,"separated coverage is split");
 var bridge=Chest(150,true);ContainerRegistry.All.Add(bridge);b.transform.position=new(350);
 RebuildNetworks();Check(networks.Count==1&&networks[0].Hubs.Count==3,"overlap chains work independently of hub enumeration order");
 bridge.Alive=false;RebuildNetworks();Check(networks.Count==2,"removing bridge splits networks on next rebuild");
 ContainerRegistry.All.Clear();RebuildNetworks();Check(networks.Count==0,"removing all hubs leaves no stale networks");
 // Executor election and ownership requests (0.1.63 owner-routed automation).
 var h1=Chest(0,true);h1.View.m_uid="1";var h2=Chest(50,true);h2.View.m_uid="2";var store=Chest(20);store.View.m_uid="3";
 ContainerRegistry.All=new(){h2,h1,store};Devices=new();RebuildNetworks();
 var baseNet=networks[0];Check(baseNet.Hubs[0]==h1,"first hub is the lowest ZDO id regardless of registration order");
 ChestOwnership.Requests.Clear();
 Check(Executor(baseNet),"owner of the first Deposit Chest is the base executor");
 Check(ChestOwnership.Requests.Count==0,"an executing client sends no requests for its own hub");
 h1.View.Owner=false;h1.Owned=false;
 Check(!Executor(baseNet),"a client that does not own the first hub does not execute");
 Check(ChestOwnership.Requests.Count==1&&ChestOwnership.Requests[0].OnlyUnserved,"non-executor only takes over an unserved hub, never contests a connected player");
 Check(ExecutorStatus(baseNet).Contains("another player"),"status names the executor");
 h1.View.Owner=true;h1.Owned=true;
 store.View.Owner=false;store.Owned=false;ChestOwnership.Requests.Clear();
 var dest=Destinations(baseNet,new(),new(10),null,false);
 Check(!dest.Contains(store),"foreign-owned destination is not written this cycle");
 Check(ChestOwnership.Requests.Count==1&&ChestOwnership.Requests[0].View==store.View&&!ChestOwnership.Requests[0].OnlyUnserved,"foreign destination with room is requested from its owner");
 store.Inventory.Room=0;ChestOwnership.Requests.Clear();
 Check(!Destinations(baseNet,new(),new(10),null,false).Contains(store)&&ChestOwnership.Requests.Count==0,"a full foreign chest is never requested");
 store.Inventory.Room=100;ChestOwnership.Requests.Clear();
 Check(!Take(baseNet,new(10),new(),s=>true,out _,out _)&&ChestOwnership.Requests.Count==1,"foreign input chest holding the item is requested, not withdrawn");
 store.Inventory.Items[0].Id="Other";ChestOwnership.Requests.Clear();
 Check(!Take(baseNet,new(10),new(),s=>true,out _,out _)&&ChestOwnership.Requests.Count==0,"foreign chest without the input is never requested");
 store.Inventory.Items[0].Id="Ore";store.View.Owner=true;store.Owned=true;
 Check(Take(baseNet,new(10),new(),s=>true,out _,out var from)&&from==store,"once owned the same chest supplies normally");
 var station=new Component();station.transform.position=new(10);Devices=new(){station};RebuildNetworks();baseNet=networks[0];
 station.Owned=false;station.MachineView.Owner=false;ChestOwnership.Requests.Clear();
 Check(!CanRun(station,out _)&&ChestOwnership.Requests.Count==1&&ChestOwnership.Requests[0].Reason=="station","executor requests a foreign station instead of waiting forever");
 Check(Statuses[station].StartsWith("Requesting station ownership"),"station status explains the pending request");
 station.Owned=true;station.MachineView.Owner=true;Check(CanRun(station,out _),"owned station runs");
 h1.View.Owner=false;h1.Owned=false;ChestOwnership.Requests.Clear();
 Check(!CanRun(station,out _)&&Statuses[station].StartsWith("Base run by"),"non-executor never drives machines even when it owns them");
 Console.WriteLine($"Automation range: {checks} checks passed");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='qm-automation-range-') as tmp:
    p=Path(tmp)
    (p/'Range.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
    (p/'Program.cs').write_text(code)
    subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Range.csproj'),'-c','Release'],check=True)
