using N=System.Numerics;
namespace UnityEngine {
 public class Object {public string name;public bool destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;public static void Destroy(Object o){if(o==null)return;o.destroyed=true;if(o is GameObject g)foreach(var c in g.transform.children.ToArray())Destroy(c.gameObject);}}
 public class GameObject:Object {
  public int layer;public bool activeSelf=true;public Transform transform;public List<Component> components=new();public static int Created;
  public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};components.Add(transform);Created++;}
  public void SetActive(bool v)=>activeSelf=v;
  public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>components.OfType<T>().Concat(transform.children.Where(c=>c.gameObject).SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(inactive))).ToArray();
 }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public new string name=>gameObject.name;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>gameObject.GetComponentsInChildren<T>(inactive);}
 public class Transform:Component {
  public Transform parent;public List<Transform> children=new();public Vector3 localPosition;public Vector3 localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;
  public void SetParent(Transform p,bool world){parent?.children.Remove(this);parent=p;p?.children.Add(this);}
  public Transform Find(string name)=>children.FirstOrDefault(c=>c.name==name);
  public Matrix4x4 localToWorldMatrix=>new(N.Matrix4x4.CreateScale(localScale.N)*N.Matrix4x4.CreateFromQuaternion(localRotation.N)*N.Matrix4x4.CreateTranslation(localPosition.N)*(parent?.localToWorldMatrix.N??N.Matrix4x4.Identity));
  public Matrix4x4 worldToLocalMatrix{get{N.Matrix4x4.Invert(localToWorldMatrix.N,out var m);return new(m);}}
 }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public N.Vector3 N=>new(x,y,z);public static Vector3 From(N.Vector3 p)=>new(p.X,p.Y,p.Z);public static Vector3 zero=>new();public static Vector3 one=>new(1,1,1);public static Vector3 up=>new(0,1,0);public static Vector3 forward=>new(0,0,1);public static Vector3 right=>new(1,0,0);public static Vector3 operator*(Vector3 v,float n)=>From(v.N*n);}
 public struct Quaternion {public N.Quaternion N;public static Quaternion AngleAxis(float a,Vector3 axis)=>new(){N=System.Numerics.Quaternion.CreateFromAxisAngle(axis.N,a*MathF.PI/180)};public static Quaternion operator*(Quaternion a,Quaternion b)=>new(){N=a.N*b.N};public static Quaternion identity=>new(){N=System.Numerics.Quaternion.Identity};public static Quaternion FromToRotation(Vector3 a,Vector3 b){var an=System.Numerics.Vector3.Normalize(a.N);var bn=System.Numerics.Vector3.Normalize(b.N);var dot=System.Numerics.Vector3.Dot(an,bn);if(dot>.99999f)return identity;var cross=System.Numerics.Vector3.Cross(an,bn);return new(){N=System.Numerics.Quaternion.Normalize(new(cross,1+dot))};}public static Vector3 operator*(Quaternion q,Vector3 p)=>Vector3.From(System.Numerics.Vector3.Transform(p.N,q.N));}
 public struct Matrix4x4 {public N.Matrix4x4 N;public Matrix4x4(N.Matrix4x4 m){N=m;}public static Matrix4x4 operator*(Matrix4x4 a,Matrix4x4 b)=>new(b.N*a.N);public Vector3 MultiplyPoint3x4(Vector3 v)=>Vector3.From(System.Numerics.Vector3.Transform(v.N,N));}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>Vector3.From(center.N-size.N/2);public Vector3 max=>Vector3.From(center.N+size.N/2);public void Encapsulate(Vector3 p){var low=N.Vector3.Min(min.N,p.N);var high=N.Vector3.Max(max.N,p.N);center=Vector3.From((low+high)/2);size=Vector3.From(high-low);}}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
 public class Material:Object {}
 public class Mesh:Object {public int vertexCount=24;public Bounds bounds=new(Vector3.zero,Vector3.one);}
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MaterialPropertyBlock {public Dictionary<string,int> Values=new();public void SetInt(string key,int value)=>Values[key]=value;public void Copy(MaterialPropertyBlock b){Values=new(b.Values);}}
 public class Renderer:Component {public bool enabled=true;public Material[] sharedMaterials={new Material()};public MaterialPropertyBlock Props=new();public void GetPropertyBlock(MaterialPropertyBlock b)=>b.Copy(Props);public void SetPropertyBlock(MaterialPropertyBlock b)=>Props.Copy(b);}
 public class MeshRenderer:Renderer {}
 public class SkinnedMeshRenderer:Renderer {public Mesh sharedMesh;public static int Bakes;public static Mesh LastBake;public void BakeMesh(Mesh m){Bakes++;LastBake=m;m.vertexCount=sharedMesh.vertexCount;m.bounds=sharedMesh.bounds;}}
 public class LODGroup:Component {public LOD[] levels;public LOD[] GetLODs()=>levels;}
 public struct LOD {public Renderer[] renderers;}
}
public class ItemDrop:UnityEngine.Component {public class ItemData {public UnityEngine.GameObject m_dropPrefab;public int m_stack=1,m_variant;}}
public class Fish:UnityEngine.Component {}
public class ZNetView:UnityEngine.Component {}
namespace Quartermaster {internal static class Plugin {internal static Logger Log=new();internal class Logger {internal List<string> Warnings=new();internal void LogWarning(string s)=>Warnings.Add(s);}}}
