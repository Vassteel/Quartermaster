using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Quartermaster.Cosmetics;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

internal static class MailboxModel
{
    private static readonly List<Object> Assets=new List<Object>();
    private static Material[] Surfaces;
    internal static readonly Vector3 EaglePerch=new Vector3(.02f,1.97f,-.35f);
    internal static void Build(GameObject prefab)
    {
        EnsureMaterials(prefab);
        foreach(var r in prefab.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(group);
        OwlModelData data;using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Mailbox.model"))data=OwlModelData.Read(stream, tiledUv:true);
        foreach(var part in data.Levels[0])
        {
            int count=part.Vertices.Length/8;var v=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                int j=i*8;var a=part.Vertices;v[i]=new Vector3(a[j],a[j+1],a[j+2]);normals[i]=new Vector3(a[j+3],a[j+4],a[j+5]);
                uv[i]=new Vector2(a[j+6],a[j+7]);
            }
            var mesh=new Mesh{name="Quartermaster rough timber mailbox"};Assets.Add(mesh);mesh.vertices=v;mesh.normals=normals;mesh.uv=uv;mesh.triangles=part.Triangles;mesh.RecalculateBounds();mesh.UploadMeshData(true);
            var go=new GameObject("Mailbox surface");go.layer=prefab.layer;go.transform.SetParent(prefab.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Surfaces[part.Material];
        }
        // Separate support/body bounds leave open space beneath the box.
        var post=prefab.AddComponent<BoxCollider>();post.center=new Vector3(.01f,.99f,-.35f);post.size=new Vector3(.28f,1.98f,.25f);
        var body=prefab.AddComponent<BoxCollider>();body.center=new Vector3(0,1.50f,.12f);body.size=new Vector3(.98f,.90f,1.10f);
        var brace=new GameObject("Mailbox brace collider");brace.layer=prefab.layer;brace.transform.SetParent(prefab.transform,false);
        var lower=new Vector3(.075f,.56f,-.31f);var upper=new Vector3(.085f,1.025f,.42f);
        brace.transform.localPosition=(lower+upper)*.5f;brace.transform.localRotation=Quaternion.FromToRotation(Vector3.up,upper-lower);
        var support=brace.AddComponent<BoxCollider>();support.size=new Vector3(.10f,(upper-lower).magnitude,.11f);
        var bird=EagleModel.Build(prefab.transform,prefab);bird.transform.localPosition=EaglePerch;bird.transform.localScale=Vector3.one*.55f;bird.SetActive(false);
    }
    private static void EnsureMaterials(GameObject prefab)
    {
        if(Surfaces!=null&&Surfaces.All(m=>m))return;
        var pole=PrefabManager.Instance.GetPrefab("wood_pole");
        var bench=PrefabManager.Instance.GetPrefab("piece_bench01");
        var wood=pole?pole.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.mainTexture&&m.name=="woodpole"):null;
        var boards=bench?bench.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.mainTexture&&m.name=="Bench_Mat"):null;
        if(!wood||!boards)throw new InvalidOperationException("Native mailbox bench/timber materials unavailable");
        Surfaces=new Material[4];float[] tones={.79f,.86f,.92f};
        for(int i=0;i<3;i++)
        {
            var material=StorageMaterials.Opaque(prefab,"Mailbox plank tone "+i,boards.mainTexture);
            material.color=new Color(tones[i],tones[i]*.985f,tones[i]*.965f);Surfaces[i]=material;Assets.Add(material);
        }
        Surfaces[3]=StorageMaterials.Opaque(prefab,"Mailbox structural timber",wood.mainTexture);
        Surfaces[3].color=new Color(.96f,.96f,.96f);Assets.Add(Surfaces[3]);
    }
    internal static void Release(){foreach(var asset in Assets)if(asset)Object.Destroy(asset);Assets.Clear();Surfaces=null;}
}
