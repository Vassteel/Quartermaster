using UnityEngine;

namespace Quartermaster;

internal static class ChestPerch
{
    internal static Vector3 OnVisibleLid(Container chest, Bounds fallback)
    {
        // Container includes inactive Open and Closed models. Use only the currently visible lid.
        bool open=chest.m_open && chest.m_open.activeInHierarchy;
        var lid=open ? chest.m_open : chest.m_closed;
        var root=lid ? lid.transform : chest.transform;
        bool found=false; Bounds bounds=default;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!Eligible(chest,filter,open)) continue;
            var mesh=filter.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var p=mesh.center+Vector3.Scale(mesh.extents,new Vector3((i&1)==0 ? -1 : 1,(i&2)==0 ? -1 : 1,(i&4)==0 ? -1 : 1));
                p=chest.transform.InverseTransformPoint(filter.transform.TransformPoint(p));
                if(!found) { bounds=new Bounds(p,Vector3.zero); found=true; } else bounds.Encapsulate(p);
            }
        }
        if(!found) bounds=fallback;
        var point=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
        float highest=float.NegativeInfinity;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!Eligible(chest,filter,open) || !filter.sharedMesh.isReadable) continue;
            var vertices=filter.sharedMesh.vertices; var triangles=filter.sharedMesh.triangles;
            for(int i=0;i<vertices.Length;i++) vertices[i]=chest.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
            for(int i=0;i+2<triangles.Length;i+=3)
                if(PerchGeometry.Height(vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]],point.x,point.z,out float y))
                    highest=Mathf.Max(highest,y);
        }
        if(!float.IsNegativeInfinity(highest)) point.y=highest;
        return point;
    }
    private static bool Eligible(Container chest, MeshFilter filter, bool open)
    {
        var renderer=filter.GetComponent<MeshRenderer>();
        return filter.sharedMesh && renderer && renderer.enabled &&
            (open || !chest.m_open || !filter.transform.IsChildOf(chest.m_open.transform));
    }
    internal static void SetFeetOnPerch(GameObject model)
    {
        float lowest=float.PositiveInfinity;
        var perch=model.transform.parent;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh) continue;
            var mesh=filter.sharedMesh;
            // Feet on the readable vanilla gull are more precise than a rotated renderer AABB.
            if(mesh.isReadable)
                foreach(var p in mesh.vertices) lowest=Mathf.Min(lowest,perch.InverseTransformPoint(filter.transform.TransformPoint(p)).y);
            else
            {
                var bounds=mesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0 ? -1 : 1,(i&2)==0 ? -1 : 1,(i&4)==0 ? -1 : 1));
                    lowest=Mathf.Min(lowest,perch.InverseTransformPoint(filter.transform.TransformPoint(p)).y);
                }
            }
        }
        if(!float.IsPositiveInfinity(lowest)) model.transform.localPosition-=Vector3.up*lowest;
    }
}
