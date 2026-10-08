using System;
using System.Collections.Generic;
using System.IO;
using Quartermaster.Cosmetics;
using UnityEngine;

namespace Quartermaster;

internal static class EagleModel
{
    private static readonly List<UnityEngine.Object> Assets = new List<UnityEngine.Object>();
    internal static GameObject Build(Transform parent, GameObject native)
    {
        var assembly = typeof(Plugin).Assembly;
        OwlModelData data;
        using(var stream=assembly.GetManifestResourceStream("Quartermaster.Eagle.model"))
            data=OwlModelData.Read(stream ?? throw new InvalidDataException("Missing eagle mesh"));
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,true) {name="Quartermaster eagle atlas",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
        Assets.Add(texture);
        using(var stream=assembly.GetManifestResourceStream("Quartermaster.Eagle.albedo"))
        using(var buffer=new MemoryStream())
        {if(stream==null)throw new InvalidDataException("Missing eagle texture");stream.CopyTo(buffer);if(!texture.LoadImage(buffer.ToArray(),true))throw new InvalidDataException("Invalid eagle texture");}
        var materials=new Material[4];
        var colors=new[]{Color.white,new Color(.018f,.014f,.012f),new Color(.50f,.25f,.045f),new Color(.24f,.16f,.065f)};
        for(int i=0;i<4;i++)
        {
            materials[i]=StorageMaterials.Opaque(native,"Quartermaster eagle surface "+i,i==0?texture:Texture2D.whiteTexture,i==3?.4f:0);
            materials[i].color=colors[i];Assets.Add(materials[i]);
        }
        var root=new GameObject("QuartermasterPostalEagle");root.transform.SetParent(parent,false);
        Vector3[] positions={new Vector3(-.11f,.16f,.02f),new Vector3(0,.44f,0),new Vector3(0,.92f,.07f),new Vector3(-.23f,.72f,0),new Vector3(.23f,.72f,0),new Vector3(-.10f,1.02f,.21f),new Vector3(.10f,1.02f,.21f),new Vector3(.11f,.16f,.02f)};
        var pivots=new Transform[8];
        for(int i=0;i<8;i++)
        {
            pivots[i]=new GameObject("EagleJoint"+i).transform;
            pivots[i].SetParent(root.transform,false);pivots[i].localPosition=positions[i];
        }
        var lods=new LOD[2];
        for(int level=0;level<2;level++)
        {
            var renderers=new List<Renderer>();
            foreach(var part in data.Levels[level])
            {
                int count=part.Vertices.Length/8;var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++)
                {int j=i*8;var v=part.Vertices;vertices[i]=new Vector3(v[j],v[j+1],v[j+2]);normals[i]=new Vector3(v[j+3],v[j+4],v[j+5]);uv[i]=new Vector2(v[j+6],v[j+7]);}
                var mesh=new Mesh{name="Quartermaster eagle LOD "+level};Assets.Add(mesh);
                mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=part.Triangles;mesh.RecalculateBounds();mesh.UploadMeshData(true);
                var go=new GameObject("Eagle surface");go.layer=parent.gameObject.layer;go.transform.SetParent(pivots[part.Pivot],false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[part.Material];renderers.Add(renderer);
            }
            lods[level]=new LOD(level==0?.06f:.008f,renderers.ToArray());
        }
        var lod=root.AddComponent<LODGroup>();lod.SetLODs(lods);lod.localReferencePoint=new Vector3(0,.65f,0);lod.size=1.6f;
        root.AddComponent<PostalEagle>();return root;
    }
    internal static void Release(){foreach(var asset in Assets)if(asset)UnityEngine.Object.Destroy(asset);Assets.Clear();}
}

// Cosmetic local motion; the bird never holds inventory or controls transfers.
public sealed class PostalEagle : MonoBehaviour
{
    private Transform head,leftWing,rightWing,body;
    private float phase;
    internal bool Flight;
    private void Awake()
    {
        head=transform.Find("EagleJoint2");body=transform.Find("EagleJoint1");
        leftWing=transform.Find("EagleJoint3");rightWing=transform.Find("EagleJoint4");
        phase=transform.position.x*.31f+transform.position.z*.17f;
    }
    private void Update()
    {
        if(!head||!body||!leftWing||!rightWing)return;
        var player=Player.m_localPlayer;
        if(!player||(player.transform.position-transform.position).sqrMagnitude>2500f)return;
        float t=Time.time+phase;
        head.localRotation=Quaternion.Euler(3f*Mathf.Sin(t*.7f),22f*Mathf.Sin(t*.24f),2f*Mathf.Sin(t*.39f));
        body.localScale=new Vector3(1,1+.006f*Mathf.Sin(t*1.6f),1);
        float stretch=Flight?55f+30f*Mathf.Sin(t*11f):Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*.15f)),16)*12f;
        leftWing.localRotation=Quaternion.Euler(0,0,-stretch);rightWing.localRotation=Quaternion.Euler(0,0,stretch);
    }
}
