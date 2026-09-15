namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a,Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public class Transform { public Vector3 position; }
    public class MonoBehaviour : Object
    {
        public Transform transform=new();
        public bool isActiveAndEnabled=true;
        public Dictionary<Type,Object> Components=new();
        public T GetComponent<T>() where T:Object => Components.TryGetValue(typeof(T),out var c)?(T)c:null;
    }
}
public class CraftingStation : UnityEngine.MonoBehaviour { public string m_name; }
public class Piece : UnityEngine.Object { }
public class ZNetView : UnityEngine.Object { public bool Valid=true; public bool IsValid()=>Valid; }
public class Player : UnityEngine.Object { public static Player m_localPlayer=new(); }
public static class PrivateArea
{
    public static Func<UnityEngine.Vector3,bool> Access=_=>true;
    public static bool CheckAccess(UnityEngine.Vector3 point,float radius,bool flash,bool wardCheck)=>Access(point);
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(){} public HarmonyPatch(Type type,string name){} }
    public class HarmonyPrefix : Attribute { }
    public class HarmonyPostfix : Attribute { }
}
