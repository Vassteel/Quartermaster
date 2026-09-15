
using UnityEngine;

namespace Quartermaster.Cosmetics;

/// <summary>Small runtime deformation rig for the vanilla readable 344-vertex sitting gull.
/// Rest vertices and materials come from the installed game; only the private clone is modified.</summary>
internal sealed class GullMeshRig
{
    private readonly Mesh mesh;
    private readonly Vector3[] rest, normals, vertices, animatedNormals;
    private readonly float[] head,body,tail;
    private readonly VikingGull.GullHelmet helmet;
    private readonly Transform meshTransform;
    internal Vector3 BeakWorld { get; private set; }
    private readonly Vector3 neck=new Vector3(0,.79f,-.24f), hips=new Vector3(0,.28f,.04f), tailRoot=new Vector3(0,.32f,.26f);
    internal static GullMeshRig Create(GameObject model)
    {
        var filter=model.GetComponentInChildren<MeshFilter>();
        if(!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable)
        {Plugin.Log.LogWarning("Gull sitting mesh is unavailable or unreadable; using body gestures and flight hops.");return null;}
        return new GullMeshRig(filter);
    }
    private GullMeshRig(MeshFilter filter)
    {
        meshTransform=filter.transform;
        mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Helmsman private gull rig";mesh.MarkDynamic();filter.sharedMesh=mesh;
        rest=mesh.vertices;normals=mesh.normals;vertices=new Vector3[rest.Length];animatedNormals=new Vector3[rest.Length];
        head=new float[rest.Length];body=new float[rest.Length];tail=new float[rest.Length];
        helmet=VikingGull.GullHelmet.Create(filter.transform,VikingGull.GullHelmet.Crown);
        for(int i=0;i<rest.Length;i++)
        {
            var p=rest[i];
            // Measured vanilla model coordinates: head at negative Z, tail at positive Z.
            // A smooth neck blend and fixed feet avoid disconnected pieces or sliding legs.
            head[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.69f,.98f,p.y));
            body[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.44f,p.y));
            tail[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.72f,p.z));
        }
    }
    internal void Apply(GullPose pose)
    {
        var h=Quaternion.Euler(-(float)pose.HeadPitch,(float)pose.HeadYaw,-(float)pose.HeadRoll);
        var b=Quaternion.Euler(-(float)pose.BodyPitch,0,-(float)pose.BodyRoll);
        var tr=Quaternion.Euler(0,(float)pose.TailYaw,0);
        helmet.FollowStanding(h,b,neck,hips,(float)pose.Crouch);
        var beak=neck+h*(new Vector3(0,1.1484f,-.6125f)-neck);
        BeakWorld=meshTransform.TransformPoint(hips+b*(beak-hips)-Vector3.up*(float)pose.Crouch);
        for(int i=0;i<rest.Length;i++)
        {
            var p=rest[i];var n=normals.Length==rest.Length ? normals[i] : Vector3.up;
            p=Vector3.Lerp(p,neck+h*(p-neck),head[i]);n=Vector3.Lerp(n,h*n,head[i]);
            var tp=tailRoot+tr*(p-tailRoot);tp.x*=1+(float)pose.TailFan;
            p=Vector3.Lerp(p,tp,tail[i]);n=Vector3.Lerp(n,tr*n,tail[i]);
            p=Vector3.Lerp(p,hips+b*(p-hips)-Vector3.up*(float)pose.Crouch,body[i]);
            n=Vector3.Lerp(n,b*n,body[i]);vertices[i]=p;animatedNormals[i]=n.normalized;
        }
        mesh.vertices=vertices;mesh.normals=animatedNormals;mesh.RecalculateBounds();
    }
    internal void Destroy()=>Object.Destroy(mesh);
}
