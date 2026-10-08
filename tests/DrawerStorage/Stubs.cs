using UnityEngine;
#pragma warning disable CS0067 // Test event exposes the native registration API.
namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;public static void DestroyImmediate(Object o){}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {public void InvokeRepeating(string n,float delay,float rate){}}
 public class GameObject:Object {public Transform transform=new();private Dictionary<Type,Component> c=new();public T GetComponent<T>() where T:Component=>c.TryGetValue(typeof(T),out var o)?(T)o:null;public T AddComponent<T>() where T:Component,new(){var o=new T{gameObject=this};c[typeof(T)]=o;return o;}public T[] GetComponentsInChildren<T>(bool b) where T:Component=>Array.Empty<T>();}
 public class Transform:Component {public Vector3 position,localPosition,localScale;public Quaternion rotation;public Transform Find(string n)=>null;public bool CompareTag(string n)=>false;}
 public struct Vector3 {public static Vector3 one=>new(1,1,1);public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 operator *(Vector3 v,float n)=>new(v.x*n,v.y*n,v.z*n);public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Quaternion{}
 public class Renderer:Component{public bool enabled,forceRenderingOff;}
 public class Collider:Component{}
 public class BoxCollider:Collider{public Vector3 center,size;}
 public class LODGroup:Component{}
 public static class Time {public static float time;}
}
namespace HarmonyLib { public static class AccessTools {public static System.Reflection.FieldInfo Field(Type t,string n)=>t.GetField(n);public static System.Reflection.MethodInfo Method(Type t,string n,Type[] a)=>t.GetMethod(n,System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,null,a,null);}}
namespace Jotunn.Configs {public class RequirementConfig{public RequirementConfig(string s,int a,int b,bool c){}}public class PieceConfig{public bool Enabled;public string Name,Description,PieceTable,Category,CraftingStation;public object Icon;public RequirementConfig[] Requirements;}}
namespace Jotunn.Entities {public class CustomPrefab{public CustomPrefab(GameObject p,bool b){}}public class CustomPiece{public CustomPiece(GameObject p,bool b,Jotunn.Configs.PieceConfig c){}}}
namespace Jotunn.Managers {
 public class PrefabManager {public static PrefabManager Instance=new();public static event Action OnVanillaPrefabsAvailable;public GameObject CreateClonedPrefab(string n,string s)=>new();public void AddPrefab(Jotunn.Entities.CustomPrefab p){}public GameObject GetPrefab(string id)=>null;}
 public class PieceManager{public static PieceManager Instance=new();public bool AddPiece(Jotunn.Entities.CustomPiece p)=>true;}
}
public class Piece:Component {public class Requirement{}public bool m_groundOnly,m_groundPiece,m_cultivatedGroundOnly,m_notOnWood,m_notOnFloor,m_inCeilingOnly,m_notOnTiltingSurface,m_noClipping,m_clipEverything,m_canBeRemoved;public Requirement[] m_resources;public long GetCreator()=>123;}
public class WearNTear:Component{public bool m_supports;public Action m_onDestroyed;}
public class Container:Component {public int m_width,m_height,Drops,Items;public string m_name;public DropTable m_defaultItems;public GameObject m_open,m_closed,m_destroyedLootPrefab;private void DropAllItems(){Drops+=Items;Items=0;}}
public class DropTable{}
public readonly record struct ZDOID(int Value){public static ZDOID None=>default;}
public class ZDO {public ZDOID m_uid;public bool Persistent;public long Owner;public int Prefab;public Dictionary<string,object> Data=new();public void SetPrefab(int f)=>Prefab=f;public int GetPrefab()=>Prefab;public void SetRotation(Quaternion q){}public void SetOwner(long v)=>Owner=v;public long GetOwner()=>Owner;public void Set(string k,object v)=>Data[k]=v;public ZDOID GetZDOID(string k)=>Data.TryGetValue(k,out var o)?(ZDOID)o:ZDOID.None;public int GetInt(string k,int d)=>Data.TryGetValue(k,out var o)?(int)o:d;}
public class ZDOMan {public static ZDOMan instance=new();public Dictionary<ZDOID,ZDO> All=new();private int next;public static long GetSessionID()=>10;public ZDO CreateNewZDO(Vector3 p,int f){var z=new ZDO{m_uid=new ZDOID(++next)};All[z.m_uid]=z;return z;}public ZDO GetZDO(ZDOID id)=>All.GetValueOrDefault(id);}
public class ZNetView:Component {public ZDO Data;public bool m_ghost;public long Peer=10;public bool IsValid()=>Data!=null;public bool IsOwner()=>Data.Owner==Peer;public ZDO GetZDO()=>Data;public void ClaimOwnership()=>Data.Owner=Peer;public void InvokeRPC(string s){}public void Register(string s,Action<long> a){}}
public class ZNetScene:UnityEngine.Object {public static ZNetScene instance=new();public Dictionary<ZDOID,GameObject> Loaded=new();public List<GameObject> Destroyed=new();public GameObject FindInstance(ZDOID id)=>Loaded.GetValueOrDefault(id);public void Destroy(GameObject g)=>Destroyed.Add(g);}
public class ZNet:UnityEngine.Object {public static ZNet instance=new();public bool Server=true;public bool IsServer()=>Server;public Peer GetServerPeer()=>new Peer();}
public static class ZDOVars {public const string s_creator="creator";}
public static class Strings {public static int GetStableHashCode(this string s)=>s.GetHashCode();}
namespace Quartermaster {
 internal class ApothecaryDefinition{public string Prefab,Name,Category;public int Columns,Rows;}
 internal static class Plugin{public static Logger Log=new();internal class Logger {public void LogError(string s){}}}
 internal static class DrawerCabinetArt{public static void Build(GameObject g,int i){}public static Vector3 Position(int i)=>ModularCabinetPlacement.DrawerPosition(i);}
 internal static class ApothecaryArt{public static object Icon(string s)=>null;}
 internal static class BuildMenuCategory{public static void Register(string s){}}
 internal static class ContainerRegistry{public static void Refresh(Container c){}}

}



public class Peer {public long m_uid=10;public bool IsReady()=>true;}
public class ZRoutedRpc {
 public static ZRoutedRpc instance=new();public int Requests;public string LastMethod;
 public void Register<T>(string s,Action<long,T> a){}
 public void Register<T,U,V>(string s,Action<long,T,U,V> a){}
 public void InvokeRoutedRPC(long peer,string method,params object[] args){Requests++;LastMethod=method;}
}
