using System.Collections.Generic;
using UnityEngine;

namespace VikingGull;

// Original procedural accessory. Each actor owns its meshes/materials; vanilla assets stay untouched.
public sealed class GullHelmet : MonoBehaviour
{
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    internal static readonly Vector3 Crown = new Vector3(0, 1.216f, -.345f);
    internal static GullHelmet Create(Transform parent, Vector3 position)
    {
        var root = new GameObject("Viking gull helmet");
        root.transform.SetParent(parent, false); root.transform.localPosition = position;
        var helmet = root.AddComponent<GullHelmet>();
        var iron = helmet.Material(new Color(.27f, .30f, .32f), .65f);
        var bronze = helmet.Material(new Color(.57f, .35f, .13f), .55f);
        var horn = helmet.Material(new Color(.82f, .75f, .54f), .05f);
        helmet.Surface("Iron cap", iron, 12, 4, (u,v) => {
            float a = u * Mathf.PI * 2, e = v * Mathf.PI * .5f;
            return new Vector3(Mathf.Cos(a) * .079f * Mathf.Cos(e), .094f * Mathf.Sin(e), Mathf.Sin(a) * .103f * Mathf.Cos(e));
        });
        helmet.Surface("Bronze brow band", bronze, 12, 1, (u,v) => {
            float a = u * Mathf.PI * 2;
            return new Vector3(Mathf.Cos(a)*.082f, -.006f + v*.024f, Mathf.Sin(a)*.106f);
        });
        for (int side = -1; side <= 1; side += 2)
        {
            int s = side;
            helmet.Surface("Curved horn", horn, 7, 6, (u,v) => {
                float a = u*Mathf.PI*2, r = .031f*(1-v);
                // Short outward sweep, then upturned tips like the gull in the package artwork.
                return new Vector3(s*(.067f+.143f*v)+r*Mathf.Cos(a)*.5f,
                    .046f+.032f*v+.135f*v*v+r*Mathf.Sin(a), .008f+.042f*v+r*Mathf.Cos(a));
            });
        }
        return helmet;
    }
    private Material Material(Color color, float metallic)
    {
        var shader = Shader.Find("Standard");
        if (!shader) shader = Shader.Find("Custom/Creature");
        if (!shader) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .28f);
        materials.Add(material); return material;
    }
    private void Surface(string label, Material material, int sides, int rings, System.Func<float,float,Vector3> point)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        // Separate triangle vertices give the cap/horns the game's faceted silhouette.
        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        { int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2); }
        for (int j = 0; j < rings; j++) for (int i = 0; i < sides; i++)
        {
            var a=point((float)i/sides,(float)j/rings); var b=point((float)(i+1)/sides,(float)j/rings);
            var c=point((float)i/sides,(float)(j+1)/rings); var d=point((float)(i+1)/sides,(float)(j+1)/rings);
            Triangle(a,c,b); Triangle(b,c,d);
            // Double-sided geometry also closes off the underside of the narrow brow band.
            Triangle(b,c,a); Triangle(d,c,b);
        }
        var mesh = new Mesh { name = label }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
        var part=new GameObject(label); part.transform.SetParent(transform,false);
        part.AddComponent<MeshFilter>().sharedMesh=mesh; part.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    internal void FollowStanding(Quaternion head, Quaternion body, Vector3 neck, Vector3 hips, float crouch)
    {
        var p=neck+head*(Crown-neck);
        transform.localPosition=hips+body*(p-hips)-Vector3.up*crouch;
        transform.localRotation=body*head;
    }
    internal static void FitFlying(GameObject model, Transform actor)
    {
        var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
        if(!skin) return;
        // The vanilla flight rig has body/wing bones, but no separate head bone.
        // Measure the posed head in actor metres, then follow its body bone (not the wing rootBone).
        Transform body=skin.transform; bool foundBody=false;
        foreach(var bone in skin.bones) if(bone && bone.name=="Bone") { body=bone; foundBody=true; break; }
        if(!foundBody) return;
        var baked=new Mesh();
        try
        {
            skin.BakeMesh(baked);
            var points=baked.vertices; float front=float.NegativeInfinity, back=float.PositiveInfinity;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=actor.InverseTransformPoint(skin.transform.TransformPoint(points[i]));
                front=Mathf.Max(front,points[i].z); back=Mathf.Min(back,points[i].z);
            }
            var top=new Vector3(0,float.NegativeInfinity,0);
            foreach(var p in points)
                if(Mathf.Abs(p.x)<.09f && p.z>Mathf.Lerp(back,front,.68f) && p.y>top.y) top=p;
            if(float.IsInfinity(top.y)) return;
            var helmet=Create(actor,new Vector3(0,top.y-.02f*model.transform.localScale.y,top.z));
            helmet.transform.localScale=model.transform.localScale;
            helmet.transform.localRotation=Quaternion.Euler(0,180,0);
            helmet.transform.SetParent(body,true);
        }
        finally { Object.Destroy(baked); }
    }
    private void OnDestroy()
    {
        foreach(var mesh in meshes) if(mesh) Destroy(mesh);
        foreach(var material in materials) if(material) Destroy(material);
    }
}
