"""Exercise owner-routed ownership requests, grants, claims and settle delays with simulated peers.

Compiles the production src/ChestOwnership.cs against minimal network stubs. Two sessions
share one ZDO store; the active session id is switched to play either side of a handoff.
This proves protocol ordering and gating logic, not Valheim transport timing.
"""
import os,subprocess,tempfile
from xml.sax.saxutils import escape
from pathlib import Path
root=Path(__file__).resolve().parents[2]
stubs=r'''
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public static class Time { public static float unscaledTime; }
 public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Pow(float a,float b)=>(float)Math.Pow(a,b); }
}
public struct ZDOID { public long Id; public ZDOID(long id){Id=id;} public override string ToString()=>Id.ToString(); }
public static class ZDOVars { public static int s_inUse=7; }
public class ZDO {
 public ZDOID m_uid; long owner; public uint DataRevision; public ushort OwnerRevision; public Dictionary<int,int> Ints=new();
 public long GetOwner()=>owner; public bool HasOwner()=>owner!=0;
 public void SetOwner(long uid){ if(owner!=uid){owner=uid;OwnerRevision++;Net.Log.Add("setowner "+m_uid+" -> "+uid);} }
 public int GetInt(int hash,int d=0)=>Ints.TryGetValue(hash,out var v)?v:d;
}
public class Container { public bool InUse; public bool IsInUse()=>InUse; public static implicit operator bool(Container c)=>c!=null; }
public class ZNetView {
 public string name="chest"; public ZDO Zdo=new(); public Container Chest; public bool Valid=true;
 public Dictionary<string,Delegate> Rpcs=new();
 public ZDO GetZDO()=>Zdo; public bool IsValid()=>Valid; public bool IsOwner()=>Zdo.GetOwner()==ZDOMan.GetSessionID();
 public void Register<T>(string name,Action<long,T> f)=>Rpcs.Add(name,f);
 public void Register(string name,Action<long> f)=>Rpcs.Add(name,f);
 public void InvokeRPC(long target,string method,params object[] args){Net.Sent.Add((target,method,this,args,ZDOMan.GetSessionID()));}
 public void ClaimOwnership(){ if(!IsOwner()) Zdo.SetOwner(ZDOMan.GetSessionID()); Net.Log.Add("claim "+Zdo.m_uid); }
 public T GetComponent<T>() where T:class => typeof(T)==typeof(Container)?Chest as T:null;
 public static implicit operator bool(ZNetView v)=>v!=null;
}
public class ZNetPeer { public long m_uid; }
public class ZNet {
 public static ZNet instance=new(); public bool Server; public ZNetPeer ServerPeer=new(){m_uid=1}; public List<ZNetPeer> Peers=new();
 public bool IsServer()=>Server; public ZNetPeer GetServerPeer()=>ServerPeer; public List<ZNetPeer> GetPeers()=>Peers;
}
public class ZDOMan {
 public static ZDOMan instance=new(); public static long Session=100; public static long GetSessionID()=>Session;
 public void ForceSendZDO(long peer,ZDOID id)=>Net.Log.Add("forcesend "+id+" -> "+peer);
}
public class Player { public static Player m_localPlayer=new(); public static implicit operator bool(Player p)=>p!=null; }
public static class Net { public static List<(long Target,string Method,ZNetView View,object[] Args,long Sender)> Sent=new(); public static List<string> Log=new(); }
namespace Quartermaster {
 public class Entry<T> { public T Value; }
 public static class Plugin {
  public static Logger Log=new(); public static Container OpenContainer; public static Entry<bool> ClaimUnansweredOwnership=new(){Value=true}, OwnershipTrace=new(){Value=true};
  public class Logger { public List<string> Lines=new(); public void LogInfo(string s)=>Lines.Add(s); public void LogWarning(string s)=>Lines.Add("WARN "+s); }
 }
 public static class NativeStorageAccess { public static bool Networked=true; }
 public static class SharedChests { public static HashSet<Container> ViewedBy=new(); public static bool Viewed(Container c)=>ViewedBy.Contains(c); }
 public static class ContainerRegistry {
  public static readonly Dictionary<Container,ZNetView> Views=new();
  public static ZNetView GetView(Container c)=>c!=null&&Views.TryGetValue(c,out var v)?v:null;
  static readonly Dictionary<Container,float> hold=new();
  public static void Yield(Container c,float seconds){hold[c]=UnityEngine.Time.unscaledTime+seconds;}
  public static bool Yielding(Container c)=>hold.TryGetValue(c,out var u)&&UnityEngine.Time.unscaledTime<u;
 }
}
'''
program=r'''
using System;
using System.Linq;
using Quartermaster;
class Program {
 static int checks; static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static ZNetView View(long id,long owner,bool chest=true){var v=new ZNetView{Zdo=new ZDO{m_uid=new ZDOID(id)},Chest=chest?new Container():null};v.Zdo.SetOwner(owner);if(v.Chest!=null)ContainerRegistry.Views[v.Chest]=v;ChestOwnership.Register(v);Net.Log.Clear();return v;}
 static int Rpcs(string method)=>Net.Sent.Count(s=>s.Method==method);
 // Delivers with the sender identity captured at send time, as the routed transport does.
 static void Deliver(){ foreach(var s in Net.Sent.ToArray()){ if(s.View.Rpcs.TryGetValue(s.Method,out var f)){ if(f is Action<long,long> g)g(s.Sender,(long)s.Args[0]); else if(f is Action<long> h)h(s.Sender);} } Net.Sent.Clear(); }
 static void Main(){
  const long A=100,B=200,Server=1;
  ZNet.instance.Peers.Add(new ZNetPeer{m_uid=B});ZNet.instance.Peers.Add(new ZNetPeer{m_uid=A});
  ZDOMan.Session=A; UnityEngine.Time.unscaledTime=1000;
  // Registration
  var v=View(1,A); Check(v.Rpcs.ContainsKey(ChestOwnership.RequestRpc)&&v.Rpcs.ContainsKey(ChestOwnership.TouchRpc),"request and touch RPCs registered on the view");
  ChestOwnership.Register(v); Check(v.Rpcs.Count==2,"duplicate registration is ignored");
  // Owned already
  Check(ChestOwnership.Request(v,"t"),"owned, unclaimed object is ready immediately"); Check(Net.Sent.Count==0,"no RPC for an owned object");
  // Not networked
  var foreign=View(2,B); NativeStorageAccess.Networked=false;
  Check(!ChestOwnership.Request(foreign,"t")&&Net.Sent.Count==0,"offline: no request, no write");
  NativeStorageAccess.Networked=true;
  // In use is never requested
  foreign.Zdo.Ints[ZDOVars.s_inUse]=1; Check(!ChestOwnership.Request(foreign,"t")&&Net.Sent.Count==0,"replicated in-use chest is not requested"); foreign.Zdo.Ints.Remove(ZDOVars.s_inUse);
  foreign.Chest.InUse=true; Check(!ChestOwnership.Request(foreign,"t")&&Net.Sent.Count==0,"locally in-use chest is not requested"); foreign.Chest.InUse=false;
  // Request to a connected owner with backoff
  Check(!ChestOwnership.Request(foreign,"t")&&Rpcs(ChestOwnership.RequestRpc)==1,"first request sent to the connected owner");
  Check(Net.Sent[0].Target==B&&(long)Net.Sent[0].Args[0]==A,"request targets the owner and carries the requester id");
  Check(!ChestOwnership.Request(foreign,"t")&&Rpcs(ChestOwnership.RequestRpc)==1,"repeat within backoff is suppressed");
  UnityEngine.Time.unscaledTime+=2.1f; ChestOwnership.Request(foreign,"t"); Check(Rpcs(ChestOwnership.RequestRpc)==2,"second request after 2s");
  UnityEngine.Time.unscaledTime+=2.1f; ChestOwnership.Request(foreign,"t"); Check(Rpcs(ChestOwnership.RequestRpc)==2,"backoff doubled: no third request after 2s");
  UnityEngine.Time.unscaledTime+=2.1f; ChestOwnership.Request(foreign,"t"); Check(Rpcs(ChestOwnership.RequestRpc)==3,"third request after 4s");
  Check(!Net.Log.Any(l=>l.StartsWith("claim")),"no claim while requests are still being sent");
  UnityEngine.Time.unscaledTime+=8.1f; ChestOwnership.Request(foreign,"t");
  Check(Net.Log.Any(l=>l.StartsWith("claim"))&&foreign.IsOwner(),"three unanswered requests fall back to the game's claim");
  Check(!ChestOwnership.Request(foreign,"t"),"a just-claimed object is not writable yet");
  Check(!ChestOwnership.Settled(foreign),"settle delay after a unilateral claim");
  UnityEngine.Time.unscaledTime+=ChestOwnership.ClaimSettleSeconds; Check(ChestOwnership.Settled(foreign)&&ChestOwnership.Request(foreign,"t"),"claim settles after one cycle");
  // Claim disabled by config
  Plugin.ClaimUnansweredOwnership.Value=false; Net.Sent.Clear(); Net.Log.Clear();
  var stubborn=View(3,B);
  for(int i=0;i<6;i++){ChestOwnership.BeginCycle();ChestOwnership.Request(stubborn,"t");UnityEngine.Time.unscaledTime+=40;}
  Check(!stubborn.IsOwner()&&!Net.Log.Any(l=>l.StartsWith("claim")),"with claims disabled a silent connected owner is never overridden");
  Check(Rpcs(ChestOwnership.RequestRpc)==6,"requests keep going at the 30s backoff cap");
  Plugin.ClaimUnansweredOwnership.Value=true; Net.Sent.Clear(); Net.Log.Clear();
  // Unserved owners: unowned, server, disconnected
  var unowned=View(4,0); ChestOwnership.Request(unowned,"t"); Check(unowned.IsOwner()&&Rpcs(ChestOwnership.RequestRpc)==0,"unowned object is claimed without any RPC");
  var serverHeld=View(5,Server); ChestOwnership.Request(serverHeld,"t"); Check(serverHeld.IsOwner()&&Rpcs(ChestOwnership.RequestRpc)==0,"server-held object is claimed without any RPC");
  ZNet.instance.Server=true;
  var gone=View(6,999); ChestOwnership.Request(gone,"t"); Check(gone.IsOwner()&&Rpcs(ChestOwnership.RequestRpc)==0,"server can establish a disconnected owner and claim without RPC");
  ZNet.instance.Server=false;
  Check(ChestOwnership.Describe(View(7,B))=="another player"&&ChestOwnership.Describe(View(8,Server))=="server"&&ChestOwnership.Describe(View(9,0))=="unowned","owner descriptions");
  // onlyUnserved never contests a connected peer but does take unserved hubs
  Net.Sent.Clear(); Net.Log.Clear(); var hub=View(10,B);
  Check(!ChestOwnership.Request(hub,"base executor",onlyUnserved:true)&&Net.Sent.Count==0&&!hub.IsOwner(),"another connected player's hub is never contested");
  var orphanHub=View(11,0); ChestOwnership.Request(orphanHub,"base executor",onlyUnserved:true); Check(orphanHub.IsOwner(),"an unowned hub is taken so the base keeps running");
  // Per-cycle request cap
  Net.Sent.Clear(); ChestOwnership.BeginCycle();
  for(int i=0;i<9;i++)ChestOwnership.Request(View(20+i,B),"t");
  Check(Rpcs(ChestOwnership.RequestRpc)==6,"at most six requests per cycle");
  ChestOwnership.BeginCycle(); UnityEngine.Time.unscaledTime+=0.1f;
  // Grant side: play B receiving A's request
  Net.Sent.Clear(); Net.Log.Clear(); var shared=View(30,B);
  ChestOwnership.Request(shared,"t"); Check(Rpcs(ChestOwnership.RequestRpc)==1,"request sent");
  ZDOMan.Session=B; Deliver();
  Check(shared.Zdo.GetOwner()==A,"owner grants a closed chest to the requester");
  int fs=Net.Log.FindIndex(l=>l.StartsWith("forcesend")), so=Net.Log.FindIndex(l=>l.StartsWith("setowner"));
  Check(fs>=0&&so>fs&&Net.Log[fs].EndsWith("-> "+A),"latest ZDO is force-sent to the requester before ownership changes");
  // Granted ownership settles briefly before the first write.
  ZDOMan.Session=A;
  Check(!ChestOwnership.Request(shared,"t"),"a just-granted chest is not writable until the handoff settles");
  UnityEngine.Time.unscaledTime+=ChestOwnership.GrantSettleSeconds;
  Check(ChestOwnership.Request(shared,"t"),"a granted handoff settles after a short wait, not the full claim cycle");
  // Forged requester: the payload must match the routed sender.
  Net.Sent.Clear(); var forged=View(37,B);
  forged.Rpcs.TryGetValue(ChestOwnership.RequestRpc,out var forgedHandler);
  ZDOMan.Session=B; ((Action<long,long>)forgedHandler)(A,999);
  Check(forged.Zdo.GetOwner()==B,"a request whose requester does not match the sender is refused");
  ((Action<long,long>)forgedHandler)(999,999);
  Check(forged.Zdo.GetOwner()==999,"a matching requester and sender is still granted");
  forged.Zdo.SetOwner(B); ZDOMan.Session=A;
  // Refusals
  ZDOMan.Session=A; var busy=View(31,B); ChestOwnership.Request(busy,"t"); ZDOMan.Session=B; busy.Chest.InUse=true; Deliver();
  Check(busy.Zdo.GetOwner()==B,"an open chest is never handed over"); busy.Chest.InUse=false;
  ZDOMan.Session=A; var viewed=View(38,B); ChestOwnership.Request(viewed,"t"); ZDOMan.Session=B; SharedChests.ViewedBy.Add(viewed.Chest); Deliver();
  Check(viewed.Zdo.GetOwner()==B,"a chest another player is viewing (shared chest) is never handed over"); SharedChests.ViewedBy.Clear(); ChestOwnership.BeginCycle(); Net.Sent.Clear();
  ZDOMan.Session=A; var mine=View(32,B); ChestOwnership.Request(mine,"t"); ZDOMan.Session=B; Plugin.OpenContainer=mine.Chest; Deliver();
  Check(mine.Zdo.GetOwner()==B,"the chest the owner has open in the UI is never handed over"); Plugin.OpenContainer=null;
  ZDOMan.Session=A; var stale=View(33,999); UnityEngine.Time.unscaledTime+=1; Net.Sent.Clear(); stale.Zdo.SetOwner(B); Net.Log.Clear();
  ChestOwnership.Request(stale,"t"); ZDOMan.Session=A; Deliver(); Check(stale.Zdo.GetOwner()==B,"a non-owner receiving a request does nothing");
  // Touch: crafting hint yields and blocks handoff
  ZDOMan.Session=A; Net.Sent.Clear(); Net.Log.Clear(); var supply=View(34,B); var own=View(35,A);
  ChestOwnership.Touch(new[]{supply.Chest,own.Chest,new Container()});
  Check(Rpcs(ChestOwnership.TouchRpc)==1&&Net.Sent[0].Target==B&&Net.Sent[0].View==supply,"touch goes only to the connected owner of a foreign source");
  ChestOwnership.Request(supply,"t"); ZDOMan.Session=B; Deliver();
  Check(ContainerRegistry.Yielding(supply.Chest),"touched owner yields automation on that chest");
  Check(supply.Zdo.GetOwner()==B,"no handoff while another client's withdrawal is in flight");
  UnityEngine.Time.unscaledTime+=1.6f; ZDOMan.Session=A; UnityEngine.Time.unscaledTime+=2.1f; ChestOwnership.Request(supply,"t"); ZDOMan.Session=B; Deliver();
  Check(supply.Zdo.GetOwner()==A,"after the yield expires the chest is handed over normally");
  // Owner change resets pending state
  ZDOMan.Session=A; Net.Sent.Clear(); ChestOwnership.BeginCycle(); var moving=View(36,B); ChestOwnership.Request(moving,"t"); moving.Zdo.SetOwner(300); ZNet.instance.Peers.Add(new ZNetPeer{m_uid=300});
  ChestOwnership.Request(moving,"t"); Check(Rpcs(ChestOwnership.RequestRpc)==2&&Net.Sent[1].Target==300,"a new owner gets a fresh request without waiting out the old backoff");
  // A real client only has a direct peer for the server; routed owners are not absent.
  ChestOwnership.Clear(); Net.Sent.Clear(); ZDOMan.Session=A; ZNet.instance.Peers.Clear(); ZNet.instance.Peers.Add(new ZNetPeer{m_uid=Server});
  var routed=View(40,B); ChestOwnership.Request(routed,"routed owner");
  Check(!routed.IsOwner()&&Rpcs(ChestOwnership.RequestRpc)==1&&Net.Sent[0].Target==B,"server-only client peer list still requests the remote owner");
  Net.Sent.Clear(); var routedHub=View(41,B); ChestOwnership.Request(routedHub,"base executor",onlyUnserved:true);
  Check(!routedHub.IsOwner()&&Net.Sent.Count==0,"server-only client peer list never steals a remote executor hub");
  Console.WriteLine($"PASS: {checks} owner-routed ownership checks.");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='qm-ownership-') as tmp:
    p=Path(tmp)
    (p/'Ownership.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel><NoWarn>CS8632</NoWarn></PropertyGroup><ItemGroup><Compile Include="{escape((root/"src/ChestOwnership.cs").as_posix())}" Link="ChestOwnership.cs"/></ItemGroup></Project>')
    (p/'Stubs.cs').write_text(stubs)
    (p/'Program.cs').write_text(program)
    subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Ownership.csproj'),'-c','Release'],check=True)
