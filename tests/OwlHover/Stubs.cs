using UnityEngine;
namespace UnityEngine {
public class Object {public bool Destroyed; public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed;}
public class Component:Object {public GameObject gameObject=new();public Transform transform=>gameObject.transform;}
public class MonoBehaviour:Component {}
public class GameObject:Object {public bool activeInHierarchy=true;public Transform transform=new();}
public class Transform:Object {public Vector3 position,forward=new(0,0,1),lossyScale=new(1,1,1);public Transform Parent;
 public bool IsChildOf(Transform other)=>Parent==other;
 public Vector3 InverseTransformPoint(Vector3 x)=>new((x.x-position.x)/lossyScale.x,(x.y-position.y)/lossyScale.y,(x.z-position.z)/lossyScale.z);
 public Vector3 InverseTransformVector(Vector3 x)=>new(x.x/lossyScale.x,x.y/lossyScale.y,x.z/lossyScale.z);
 public Vector3 TransformPoint(Vector3 x)=>new(x.x*lossyScale.x+position.x,x.y*lossyScale.y+position.y,x.z*lossyScale.z+position.z);}
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public Vector3 normalized {get{var d=MathF.Sqrt(x*x+y*y+z*z);return new(x/d,y/d,z/d);}}
 public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
public struct Ray {public Vector3 origin,direction;public Ray(Vector3 o,Vector3 d){origin=o;direction=d;}public Vector3 GetPoint(float d)=>new(origin.x+direction.x*d,origin.y+direction.y*d,origin.z+direction.z*d);}
public struct Bounds {Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public bool IntersectRay(Ray ray,out float distance){
 float near=0,far=float.PositiveInfinity;float[] o={ray.origin.x,ray.origin.y,ray.origin.z},d={ray.direction.x,ray.direction.y,ray.direction.z},c={center.x,center.y,center.z},s={size.x,size.y,size.z};
 for(int i=0;i<3;i++){float min=c[i]-s[i]/2,max=c[i]+s[i]/2;if(Math.Abs(d[i])<.00001){if(o[i]<min||o[i]>max){distance=0;return false;}continue;}float a=(min-o[i])/d[i],b=(max-o[i])/d[i];near=Math.Max(near,Math.Min(a,b));far=Math.Min(far,Math.Max(a,b));}
 distance=near;return far>=near;}}
public class Collider:Component{}
public struct RaycastHit {public Collider collider;public float distance;}
public enum QueryTriggerInteraction {Ignore}
public static class LayerMask {public static int GetMask(params string[] names)=>1;}
public static class Physics {public static RaycastHit[] Obstacles=Array.Empty<RaycastHit>();public static int RaycastNonAlloc(Ray r,RaycastHit[] hits,float distance,int mask,QueryTriggerInteraction q){var matches=Obstacles.Where(h=>h.distance<=distance).Take(hits.Length).ToArray();matches.CopyTo(hits,0);return matches.Length;}}
}
public class Player:Component {public static Player m_localPlayer;public float m_maxInteractDistance=4;public Vector3 GetEyePoint()=>transform.position;}
public class GameCamera:Component {public static GameCamera instance=new();}
public class Hud {public Component m_crosshair=new();public Label m_hoverName=new();}
public class Label {public string text="";}
public static class InventoryGui {public static bool Visible;public static bool IsVisible()=>Visible;}
public static class Menu {public static bool IsVisible()=>false;}
public static class Console {public static bool IsVisible()=>false;public static void WriteLine(string s)=>System.Console.WriteLine(s);}
public class TextViewer:UnityEngine.Object {public static TextViewer instance=new();public bool Visible;public bool IsVisible()=>Visible;}
public static class ParticleMist {public static bool Blocked;public static bool IsMistBlocked(Vector3 a,Vector3 b)=>Blocked;}
namespace Quartermaster {public class Setting {public bool Value=true;} public static class Plugin {public static Setting Enabled=new();}public class DepositGull:Component {public string HoverStatus="Resting on the Deposit Chest";} }
