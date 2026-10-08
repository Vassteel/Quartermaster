using UnityEngine;

namespace Quartermaster;

internal static class OwlNavigation
{
    internal static readonly int Mask=LayerMask.GetMask("Default","static_solid","piece","terrain","vehicle");
    private static readonly Collider[] overlaps=new Collider[24];
    private static int frame=-1,budget;
    internal static bool SearchTurn()
    {
        if(frame!=Time.frameCount){frame=Time.frameCount;budget=3;}
        if(budget<=0)return false;budget--;return true;
    }
    internal static OwlPoint Point(Vector3 v)=>new OwlPoint(v.x,v.y,v.z);
    internal static Vector3 Vector(OwlPoint p)=>new Vector3(p.X,p.Y,p.Z);
    internal static bool Clear(OwlPoint a,OwlPoint b)=>Clear(Vector(a),Vector(b));
    internal static bool Clear(Vector3 a,Vector3 b,float radius=.28f)
    {
        var low=Vector3.up*(radius+.035f);var high=Vector3.up*Mathf.Max(radius+.035f,.72f-radius);
        if(Physics.OverlapCapsuleNonAlloc(a+low,a+high,radius,overlaps,Mask,QueryTriggerInteraction.Ignore)>0||
           Physics.OverlapCapsuleNonAlloc(b+low,b+high,radius,overlaps,Mask,QueryTriggerInteraction.Ignore)>0)return false;
        var delta=b-a;
        return delta.sqrMagnitude<.000001f||!Physics.CapsuleCast(a+low,a+high,radius,delta.normalized,delta.magnitude,Mask,QueryTriggerInteraction.Ignore);
    }
    internal static OwlPoint? Floor(OwlPoint point)
    {
        var p=Vector(point);
        if(!Physics.Raycast(p+Vector3.up*.4f,Vector3.down,out var hit,.8f,Mask,QueryTriggerInteraction.Ignore)||hit.normal.y<.75f)return null;
        return Point(hit.point+Vector3.up*.004f);
    }
    internal static bool WalkClear(OwlPoint a,OwlPoint b)
    {
        if(!Clear(a,b))return false;
        // Check support along the entire segment, including gaps and stair edges.
        int steps=Mathf.Max(1,Mathf.CeilToInt(OwlPoint.Distance(a,b)/.18f));
        for(int i=0;i<=steps;i++)
        {
            float t=(float)i/steps;
            var p=new OwlPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t);
            var surface=Floor(p);
            if(!surface.HasValue||Mathf.Abs(surface.Value.Y-p.Y)>.14f)return false;
        }
        return true;
    }
    internal static bool GroundNear(Vector3 point,Vector3 toward,Component avoid,out Vector3 feet,bool beside=false)
    {
        var surface=Floor(Point(point));
        if(!beside&&surface.HasValue&&Mathf.Abs(surface.Value.Y-point.y)<.14f&&Clear(Vector(surface.Value),Vector(surface.Value)))
        {feet=Vector(surface.Value);return true;}
        return StandNear(point,toward,avoid,out feet,out var onGround)&&onGround;
    }
    internal static bool StandNear(Vector3 aim,Vector3 from,Component avoid,out Vector3 feet,out bool grounded)
    {
        var forward=from-aim;forward.y=0;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward.Normalize();
        // Approach the actual interaction socket, not the object's origin or furnace roof.
        foreach(float radius in new[]{1.15f,1.8f,2.5f})
        for(int i=0;i<8;i++)
        {
            float angle=i==0?0:((i+1)/2)*45*(i%2==0?-1:1);
            var p=aim+Quaternion.Euler(0,angle,0)*forward*radius;
            if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,Mask,QueryTriggerInteraction.Ignore)||hit.normal.y<.7f)continue;
            if(avoid&&(hit.collider.transform==avoid.transform||hit.collider.transform.IsChildOf(avoid.transform)))continue;
            p=hit.point+Vector3.up*.004f;
            if(!Clear(p,p))continue;
            // The beak needs a clear throw toward the input; allow the final station surface.
            var origin=p+Vector3.up*.57f;var delta=aim-origin;
            if(Physics.Linecast(origin,aim,out var obstruction,Mask,QueryTriggerInteraction.Ignore)&&
                (!avoid||!(obstruction.transform==avoid.transform||obstruction.transform.IsChildOf(avoid.transform))))continue;
            feet=p;grounded=true;return true;
        }
        // A crowded work area can be serviced from a clear hovering approach.
        for(int i=0;i<8;i++)
        {
            var p=aim+Quaternion.Euler(0,i*45,0)*forward*1.6f+Vector3.up*.5f;
            if(Clear(p,p)){feet=p;grounded=false;return true;}
        }
        feet=default;grounded=false;return false;
    }
    internal static bool Hop(Vector3 start,Vector3 end,out Vector3 raised)
    {
        // Three swept segments prevent a hop from cutting through a beam or roof.
        foreach(float height in new[]{.65f,1.25f,2f})
        {
            var a=start+Vector3.up*height;var b=end+Vector3.up*height;
            if(Clear(start,a)&&Clear(a,b)&&Clear(b,end)){raised=a;return true;}
        }
        raised=default;return false;
    }
}
