using UnityEngine;
namespace UnityEngine {
 public class Object {public bool Destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.Destroyed;}
 public class Component:Object {public Transform transform=new();public ZNetView View=new();}
 public class MonoBehaviour:Component {}
 public class Transform:Object {public Vector3 position;public Quaternion localRotation;public Transform Find(string name)=>new();public Vector3 TransformPoint(Vector3 p)=>position+p;}
 public struct Vector3 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float sqrMagnitude=>x*x+y*y+z*z;
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 }
 public struct Quaternion {public static Quaternion Euler(float a,float b,float c)=>new();}
 public static class Time {public static float time;}
 public static class Mathf {public const float PI=MathF.PI;public static float Sin(float v)=>MathF.Sin(v);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
 public static class JsonUtility {
  static System.Text.Json.JsonSerializerOptions options=new(){IncludeFields=true};
  public static string ToJson<T>(T o)=>System.Text.Json.JsonSerializer.Serialize(o,options);
  public static T FromJson<T>(string s)=>System.Text.Json.JsonSerializer.Deserialize<T>(s,options);
 }
}
public interface Hoverable {string GetHoverText();string GetHoverName();float GetHoverOffset();}
public interface Interactable {bool Interact(Humanoid u,bool h,bool a);bool UseItem(Humanoid u,ItemDrop.ItemData i);}
public class Humanoid:Component {}
public class Player:Humanoid {public static Player m_localPlayer=new();}
public class ItemDrop {public class ItemData {}}
public class Container:Component {public Quartermaster.ChestSettings Settings=new();}
public class ZDO {public bool IgnoreWrites;public string m_uid="1";public Dictionary<string,string> Data=new();public string GetString(string k,string fallback)=>Data.TryGetValue(k,out var v)?v:fallback;public void Set(string k,string v){if(!IgnoreWrites)Data[k]=v;}}
public class ZNetView:UnityEngine.Object {public bool Valid=true,Owner=true,CanClaim=true;public ZDO Data=new();public bool IsValid()=>Valid;public bool IsOwner()=>Owner;public ZDO GetZDO()=>Data;public void ClaimOwnership(){Owner=CanClaim;}}
public class Localization {public static Localization instance=new();public string Localize(string s)=>s;}
public static class PrivateArea {public static bool Access=true;public static bool CheckAccess(Vector3 p,float a,bool b,bool c)=>Access;}
namespace Quartermaster {
 public class ProductCap {public string Item;public int Amount;}
 public class ChestSettings {public string Group="Home";public bool Deposit=true;}
 internal class TestLog {internal void LogInfo(string s){}internal void LogWarning(string s){}}
 internal static class Plugin {internal static TestLog Log=new();internal static Setting<bool> Enabled=new(){Value=true};internal static Setting<float> Range=new(){Value=100};internal class Setting<T>{public T Value;}}
 internal static class Policy {internal static bool SameGroup(string a,string b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase);}
 internal static class ContainerRegistry {internal static List<Container> All=new();internal static bool Accessible(Container c)=>c;internal static ChestSettings GetSettings(Container c)=>c.Settings;}
 internal sealed class BaseNetwork {internal string Group="Home";internal bool Contains(Vector3 point)=>point.sqrMagnitude<=10000;}
 internal static class Automation {internal static ZNetView View(Component c)=>c.View;internal static BaseNetwork Network(Container c)=>new(){Group=c.Settings.Group};}
 internal static class ChestUi {internal static int Opens;internal static void OpenLedger(OwlLedger l)=>Opens++;}
}
