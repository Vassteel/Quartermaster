using UnityEngine;
namespace UnityEngine
{
    public class Object { public bool Destroyed;private static int next;private int id=++next;public int GetInstanceID()=>id;public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed; }
    public class Component:Object { public Transform transform=new(); }
    public class Transform:Object { public Vector3 position,localScale=Vector3.one;public Quaternion rotation;public Vector3 forward=>rotation*Vector3.forward;public Vector3 TransformPoint(Vector3 p)=>position+rotation*new Vector3(p.x*localScale.x,p.y*localScale.y,p.z*localScale.z); }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new();public static Vector3 one=>new(1,1,1);public static Vector3 up=>new(0,1,0);public static Vector3 forward=>new(0,0,1);
        public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;
        public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float amount)=>Distance(a,b)<=amount?b:a+(b-a)*(amount/Distance(a,b));
        public static float SignedAngle(Vector3 a,Vector3 b,Vector3 axis)=>(float)((Math.Atan2(b.x,b.z)-Math.Atan2(a.x,a.z))*180/Math.PI);
    }
    public struct Quaternion
    {
        public float yaw;public static Quaternion identity=>new();
        public static Quaternion LookRotation(Vector3 v)=>new(){yaw=(float)Math.Atan2(v.x,v.z)};
        public static Quaternion RotateTowards(Quaternion a,Quaternion b,float step)=>b;
        public static Vector3 operator *(Quaternion a,Vector3 b)=>new((float)(b.x*Math.Cos(a.yaw)+b.z*Math.Sin(a.yaw)),b.y,(float)(b.z*Math.Cos(a.yaw)-b.x*Math.Sin(a.yaw)));
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI;public static float Sin(float x)=>(float)Math.Sin(x);
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Sqrt(float x)=>(float)Math.Sqrt(x);public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float MoveTowards(float a,float b,float step)=>a+Math.Clamp(b-a,-step,step);
    }
    public static class Time { public static int frameCount;public static float time,deltaTime=1f/30; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics { public static bool BlockSight;public static bool Linecast(Vector3 a,Vector3 b,int mask,QueryTriggerInteraction q)=>BlockSight; }
}
public class Container:Component {}
public class Beehive:Component {}
public class ItemDrop:Component { public int Count=10;public class ItemData{} }
namespace Quartermaster.Cosmetics
{
    internal sealed class QuartermasterOwl
    {
        internal Vector3 BeakWorld=>Vector3.zero;internal readonly List<OwlAction> Phases=new();
        internal void Perform(OwlPose pose,float dt){}internal void Rest(bool sleeping,float time,float dt){}
    }
}
namespace Quartermaster
{
    internal static class Plugin { internal static Setting OwlCollectDroppedItems=new();internal class Setting { public bool Value=true; } }
    internal static class OwlNavigation
    {
        internal const int Mask=1;internal static Func<Vector3,Vector3,bool> Permit=(_,_)=>true;internal static int Hops;
        internal static OwlPoint Point(Vector3 v)=>new(v.x,v.y,v.z);internal static Vector3 Vector(OwlPoint v)=>new(v.X,v.Y,v.Z);
        internal static bool Clear(OwlPoint a,OwlPoint b)=>Permit(Vector(a),Vector(b));
        internal static bool Clear(Vector3 a,Vector3 b,float radius=.28f)=>Permit(a,b);
        internal static bool GroundEnabled=true,FrontStand;
        internal static OwlPoint? Floor(OwlPoint p)=>GroundEnabled&&Math.Abs(p.Y)<.4f?new OwlPoint(p.X,0,p.Z):null;
        internal static bool WalkClear(OwlPoint a,OwlPoint b)=>Floor(a).HasValue&&Floor(b).HasValue&&Clear(a,b);
        internal static bool GroundNear(Vector3 p,Vector3 toward,Component avoid,out Vector3 feet,bool beside=false)
        {feet=new Vector3(p.x,0,p.z);return GroundEnabled;}
        internal static bool SearchTurn()=>true;
        internal static bool StandNear(Vector3 aim,Vector3 from,Component avoid,out Vector3 feet,out bool grounded)
        {feet=aim+new Vector3(0,0,FrontStand?1.15f:-1.15f);if(FrontStand)feet.y=0;grounded=true;return true;}
        internal static bool Hop(Vector3 start,Vector3 end,out Vector3 raised){Hops++;raised=start+Vector3.up;return false;}
    }
    internal enum FurnitureHandling {Jar,Lumber,Ingot,Bin,Hide,Textile,Feather,Bone,Masonry,Pantry,Hanging,Grain,Ammunition,Weapon,Shield,Wardrobe,Trophy,Gem,Treasure,Mead}
    internal sealed class OwlWork
    {
        internal static readonly List<OwlWork> Jobs=new();internal bool Active=true;internal Component Machine=new();
        internal ApothecaryDisplay Furniture;internal bool Retrieving;
        internal ItemDrop Input=new(),Fuel=new();internal Vector3 InputPoint;
        internal Vector3 Aim(bool fuel)=>InputPoint;internal ItemDrop Prop(bool fuel)=>fuel?Fuel:Input;
        internal static OwlWork Read(Container home,Component machine)=>Jobs.FirstOrDefault(j=>j.Active&&j.Machine==machine);
        internal static OwlWork Next(Container home,IDictionary<int,float> visited,ISet<int> tour,Vector3 from)=>Jobs.Where(j=>j.Active&&!tour.Contains(j.Machine.GetInstanceID())&&(!visited.TryGetValue(j.Machine.GetInstanceID(),out var t)||Time.time-t>=12))
            .OrderBy(j=>visited.TryGetValue(j.Machine.GetInstanceID(),out var t)?t:float.NegativeInfinity).FirstOrDefault();
    }
    internal sealed class ApothecaryDisplay:Component
    {
        internal int Animations,Completions;
        internal FurnitureHandling Handling;
        internal float ContactTime=>FurnitureMotion.Contact(Handling);
        internal float Duration=>FurnitureMotion.Duration(Handling);
        internal float Lean(float time)=>FurnitureMotion.Lean(Handling,time);
        internal Vector3 Approach(OwlWork work)=>work.InputPoint+Vector3.forward*.70f;
        internal void Animate(OwlWork work,float time)=>Animations++;
        internal void Complete(OwlWork work){Completions++;work.Active=false;}
    }
    internal static class OwlCleanup
    {
        internal static bool Request;
        internal static bool Requested(Container home)=>Request;
        internal static bool CanStart(Container home)=>Request&&Access;
        internal static void Complete(Container home)=>Request=false;
        internal static ItemDrop Drop;internal static int Collected,Capacity=100;internal static bool Access=true;
        internal static bool Eligible(Container home,ItemDrop drop)=>drop&&drop.Count>0&&Access&&Capacity>0&&(Plugin.OwlCollectDroppedItems.Value||Request);
        internal static ItemDrop Find(Container home,ISet<int> skipped)=>Eligible(home,Drop)&&!skipped.Contains(Drop.GetInstanceID())?Drop:null;
        internal static ItemDrop.ItemData TakeOne(Container home,ItemDrop drop)
        {if(!Eligible(home,drop))return null;drop.Count--;Collected++;Capacity--;return new();}
    }
    internal static class Automation { internal static ItemDrop.ItemData NewOutput(ItemDrop item,bool cheated)=>new(); }
    internal static class GullToss
    {
        internal static int Tosses,Carried,Returned,Placed;internal static List<Vector3> Targets=new();
        internal static void SpawnToward(Transform owner,ItemDrop.ItemData item,Vector3 from,Vector3 to){Tosses++;Targets.Add(to);}
        internal static void Place(Transform owner,ItemDrop.ItemData item,Vector3 from,Vector3 to){Placed++;Targets.Add(to);}
        internal static void Carry(Transform owner,ItemDrop.ItemData item,Vector3 from)=>Carried++;
        internal static void Spawn(Transform owner,ItemDrop.ItemData item,Vector3 from)=>Returned++;
        internal static void ClearFor(Transform owner){}
    }
    internal sealed class OwlLedger:Component
    {
        internal static OwlLedger Current;internal int Pages;internal bool Access=true;
        internal Vector3 Perch=>transform.position;internal Vector3 Book=>transform.position+Vector3.forward;
        internal void TurnPage()=>Pages++;
        internal static bool Allowed(OwlLedger book)=>book&&book.Access;
        internal static OwlLedger VisitFor(Container home)=>Allowed(Current)?Current:null;
    }
    internal sealed class OwlBeeMotes:UnityEngine.Object
    {
        internal static OwlBeeMotes Last;internal int Frames;internal bool Visible;
        internal static OwlBeeMotes Create(Transform root)=>Last=new();
        internal void Show(Vector3 owl,Vector3 hive,float time){Frames++;Visible=true;}
        internal void Hide()=>Visible=false;
    }
    internal static class OwlVoice { internal static void Call(Transform root,bool attention){} }
}
