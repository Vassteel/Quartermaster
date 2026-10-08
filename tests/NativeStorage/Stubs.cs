using System.Reflection;
using Quartermaster;
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null; }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 public class Transform {public Vector3 position;}
 public class GameObject:Object {public Transform transform=new();public List<Component> Components=new();public T GetComponent<T>() where T:class=>Components.OfType<T>().FirstOrDefault();public T Add<T>(T c) where T:Component{c.gameObject=this;Components.Add(c);return c;}}
 public struct Vector3 {public float x,y,z;public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(){x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};}
 public static class Mathf {public static float Pow(float a,float b)=>MathF.Pow(a,b);}
 public static class Time {public static float unscaledTime;}
}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true)]public class HarmonyPatch:Attribute{public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}}
 public class HarmonyPrefix:Attribute{}
 public static class AccessTools{public static MethodInfo Method(Type t,string n)=>t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);public static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);}
}
public readonly record struct ZDOID(int Id);
public class ZDO {
 public ZDOID m_uid;public long Owner;public UnityEngine.Vector3 Position;readonly Dictionary<string,object> data=new();
 public long GetLong(string k)=>data.TryGetValue(k,out var v)?(long)v:0;
 public long GetLong(int k,long fallback=0)=>data.TryGetValue(k.ToString(),out var v)?(long)v:fallback; public void Set(int k,long v)=>data[k.ToString()]=v;
 public int GetInt(int k)=>0;public void Set(string k,long v)=>data[k]=v;
 public long GetOwner()=>Owner;public void SetOwner(long p)=>Owner=p;public UnityEngine.Vector3 GetPosition()=>Position;
}
public class ZNetView:UnityEngine.Component {
 public ZDO Data=new();public bool Valid=true;public HashSet<long> Hidden=new();
 public void InvokeRPC(long peer,string name,params object[] args){}
 public bool IsValid()=>Valid;public bool IsOwner()=>Data.Owner==ZDOMan.Session&&!Hidden.Contains(ZDOMan.Session);public ZDO GetZDO()=>Data;
}
public static class ZDOVars {public const int s_inUse=1,s_playerID=2;}
public class ZDOMan {public static ZDOMan instance=new();public static long Session;public static long GetSessionID()=>Session;public Dictionary<ZDOID,ZDO> Data=new();public ZDO GetZDO(ZDOID id)=>Data.GetValueOrDefault(id);public void ForceSendZDO(long peer,ZDOID id){}}
public class ZNetScene:UnityEngine.Object {public static ZNetScene instance=new();public Dictionary<ZDOID,UnityEngine.GameObject> Objects=new();public HashSet<long> HiddenPeers=new();public UnityEngine.GameObject FindInstance(ZDOID id)=>HiddenPeers.Contains(ZDOMan.Session)?null:Objects.GetValueOrDefault(id);}
public class Container:UnityEngine.Component {public bool Busy,Allowed=true;public int Saves,Loads;public bool m_checkGuardStone=true;public bool IsInUse()=>Busy;private bool CheckAccess(long player)=>Allowed;private void Save()=>Saves++;}
public class Piece:UnityEngine.Component {public long Creator;public long GetCreator()=>Creator;}
public class PrivateArea:UnityEngine.Component {private static List<PrivateArea> m_allAreas=new();public bool Enabled,Inside,Permitted;private bool IsEnabled()=>Enabled;private bool IsInside(UnityEngine.Vector3 p,float r)=>Inside;private bool IsPermitted(long id)=>Permitted;public static void Add(PrivateArea a)=>m_allAreas.Add(a);public static void Clear()=>m_allAreas.Clear();}
public class Player:UnityEngine.Component {public static Player m_localPlayer;public long ID;public bool Dead,Teleport;public long GetPlayerID()=>ID;public bool IsDead()=>Dead;public bool IsTeleporting()=>Teleport;}
public class ZNetPeer{public ZDOID m_characterID;public long m_uid,m_playerID;public UnityEngine.Vector3 m_refPos;}
public class ZNet:UnityEngine.Object {
 public static ZNet instance;public long Peer;public bool IsServer()=>Peer==1;
 public List<ZNetPeer> GetPeers()=>World.Nodes.Keys.Where(id=>id!=Peer).Select(id=>new ZNetPeer{m_uid=id,m_playerID=World.Nodes[id].SendPlayerId?id*10:0,m_characterID=World.Nodes[id].Character}).ToList();
 public ZNetPeer GetPeer(long p)=>GetPeers().FirstOrDefault(n=>n.m_uid==p);public ZNetPeer GetServerPeer()=>new(){m_uid=1,m_playerID=10};public double GetTimeSeconds()=>UnityEngine.Time.unscaledTime;
}
public class ZPackage {
 readonly List<object> data;int at;public ZPackage(){data=new();}private ZPackage(List<object> d){data=new(d);}public ZPackage Copy()=>new(data);public void SetPos(int n)=>at=n;
 public void Write(long v)=>data.Add(v);public void Write(int v)=>data.Add(v);public void Write(double v)=>data.Add(v);public void Write(bool v)=>data.Add(v);public void Write(string v)=>data.Add(v);public void Write(ZDOID v)=>data.Add(v);
 public long ReadLong()=>(long)data[at++];public int ReadInt()=>(int)data[at++];public bool ReadBool()=>(bool)data[at++];public string ReadString()=>(string)data[at++];public double ReadDouble()=>(double)data[at++];public ZDOID ReadZDOID()=>(ZDOID)data[at++];
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance;public Dictionary<string,Action<long,ZPackage>> Handlers=new();
 public void Register<T>(string n,Action<long,T> h)=>Handlers.Add(n,(s,p)=>h(s,(T)(object)p));
 public void InvokeRoutedRPC(long peer,string n,ZPackage p)=>World.Messages.Add(new(World.Current,peer,n,p.Copy()));
}
namespace Quartermaster {
 static class Plugin {public static Setting<bool> Enabled=new(true);public static Setting<float> Range=new(100),CraftRange=new(100);public static Logger Log=new();}
 class Setting<T>{public T Value;public Setting(T v){Value=v;}}
 class Logger {public List<string> Errors=new(),Infos=new(),Debugs=new();public void LogError(string s)=>Errors.Add(s);public void LogInfo(string s)=>Infos.Add(s);public void LogDebug(string s)=>Debugs.Add(s);public void LogWarning(string s){}}
 static class ContainerRegistry {public static List<Container> All=new();public static ZNetView GetView(Container c)=>c.GetComponent<ZNetView>();public static void Refresh(Container c)=>c.Loads++;}
 static class Automation {public static List<UnityEngine.Component> Devices=new();public static ZNetView View(UnityEngine.Component c)=>c.GetComponent<ZNetView>();}
}
static class World {
 public record Message(long From,long To,string Name,ZPackage Data);
 public class Node {public bool SendPlayerId=true;public ZDOID Character=default;public object Service=new();public ZRoutedRpc Rpc=new();public ZNet Net;public Player Player;public Node(long id){Net=new(){Peer=id};Player=new UnityEngine.GameObject().Add(new Player{ID=id*10});}}
 public static Dictionary<long,Node> Nodes=new();public static List<Message> Messages=new();public static long Current;
 public static void Select(long id){Current=id;var n=Nodes[id];ZNet.instance=n.Net;ZRoutedRpc.instance=n.Rpc;ZDOMan.Session=id;Player.m_localPlayer=n.Player;}
 public static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 public static void Reset(){Messages.Clear();Nodes.Clear();ContainerRegistry.All.Clear();Automation.Devices.Clear();PrivateArea.Clear();ZDOMan.instance=new();ZNetScene.instance=new();UnityEngine.Time.unscaledTime=1;Plugin.Enabled.Value=true;Plugin.Log.Errors.Clear();foreach(long id in new[]{1L,2L})Nodes.Add(id,new(id));foreach(var id in Nodes.Keys){Select(id);}}
 public static void Tick(float dt=.1f){UnityEngine.Time.unscaledTime+=dt;foreach(var id in Nodes.Keys.ToArray()){Select(id);}}
 public static void Deliver(int index=0){var msg=Messages[index];Messages.RemoveAt(index);if(!Nodes.ContainsKey(msg.To))return;Select(msg.To);Nodes[msg.To].Rpc.Handlers[msg.Name](msg.From,msg.Data.Copy());}
 public static void Flush(){int limit=1000;while(Messages.Count>0&&limit-->0)Deliver();if(limit==0)throw new Exception("message loop");}
 public static ZNetView Chest(int id,long owner){var go=new UnityEngine.GameObject();var v=go.Add(new ZNetView{Data=new(){m_uid=new(id),Owner=owner}});var c=go.Add(new Container());ContainerRegistry.All.Add(c);ZDOMan.instance.Data.Add(v.Data.m_uid,v.Data);ZNetScene.instance.Objects.Add(v.Data.m_uid,go);return v;}
}
