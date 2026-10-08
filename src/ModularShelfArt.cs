using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Quartermaster.Cosmetics;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Quartermaster;
internal static class ModularShelfArt
{
    private static readonly List<Object> assets=new List<Object>();
    private static Mesh[] meshes;
    private static readonly Dictionary<int,Mesh[]> models=new Dictionary<int,Mesh[]>();
    private static Material[] materials;
    private static readonly Dictionary<bool,Material[]> vesselMaterials=new Dictionary<bool,Material[]>();
    internal static Vector3[] Slots(string model)=>model=="modular_clay"?ModularShelfSlots.Slots0:ModularShelfSlots.Slots1;
    private static void Ensure(GameObject prefab,int variant)
    {
        if(models.TryGetValue(variant,out meshes))return;
        var made=new List<Mesh>();
        try
        {
            if(materials==null){materials=new Material[3];string[] ids={"Wood","RoundLog","FineWood"};
            for(int i=0;i<3;i++)
            {
                var source=PrefabManager.Instance.GetPrefab(ids[i]);
                var texture=source?source.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.mainTexture):null;
                if(!texture)throw new InvalidOperationException("Missing native "+ids[i]+" texture");
                var material=StorageMaterials.Opaque(prefab,"Mead cabinet "+ids[i],texture.mainTexture);material.color=new Color(.8f,.8f,.8f);materials[i]=material;assets.Add(material);
            }
            }
            OwlModelData data;using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.ModularShelf."+variant))data=OwlModelData.Read(stream,true);
            foreach(var p in data.Levels[0])
            {
                int count=p.Vertices.Length/8;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){int j=i*8;var a=p.Vertices;v[i]=new Vector3(a[j],a[j+1],a[j+2]);n[i]=new Vector3(a[j+3],a[j+4],a[j+5]);uv[i]=new Vector2(a[j+6],a[j+7]);}
                var mesh=new Mesh{name="Mead cabinet surface "+p.Material,vertices=v,normals=n,uv=uv,triangles=p.Triangles};mesh.RecalculateBounds();made.Add(mesh);assets.Add(mesh);
            }
            meshes=made.ToArray();models[variant]=meshes;
        }
        catch{Release();throw;}
    }
    internal static void Build(GameObject prefab,ApothecaryDefinition def)
    {
        Ensure(prefab,def.Glass?1:0);
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;renderer.forceRenderingOff=true;}
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(group);
        foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))Object.DestroyImmediate(t.gameObject);
        var art=new GameObject("Quartermaster furniture");art.layer=prefab.layer;art.transform.SetParent(prefab.transform,false);
        for(int i=0;i<meshes.Length;i++)
        {var go=new GameObject("Native timber surface");go.layer=prefab.layer;go.transform.SetParent(art.transform,false);go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterial=materials[i];}
        var hit=art.AddComponent<BoxCollider>();hit.center=new Vector3(0,.5f,0);hit.size=new Vector3(.994f,1,.30f);
        // Rear/top snap planes clear walls and share the authored grid, scaled together by ModularCabinetPlacement.
        foreach(float y in new[]{0f,1f})foreach(float x in new[]{-.5f,0f,.5f})
        {var point=new GameObject("snappoint");point.tag="snappoint";point.transform.SetParent(prefab.transform,false);point.transform.localPosition=new Vector3(x,y,-.235f);}
        var sockets=Slots(def.Model);
        for(int i=0;i<sockets.Length;i++)
        {
            var jar=CreateVessel(prefab,art.transform,def.Glass,i);
            jar.transform.localPosition=sockets[i];ApothecaryArt.StyleJar(jar.transform,def.Glass,i);
        }
        ModularCabinetPlacement.Configure(prefab);
        var chest=prefab.GetComponent<Container>();chest.m_open=null;chest.m_closed=null;
    }
    private static GameObject CreateVessel(GameObject prefab,Transform parent,bool glass,int index)
    {
        Ensure(prefab,glass?3:2);
        if(!vesselMaterials.TryGetValue(glass,out var surfaces))
        {
            var clay=StorageMaterials.Opaque(prefab,"Cabinet clay",null);clay.color=new Color(.59f,.48f,.30f);assets.Add(clay);
            var body=glass?StorageMaterials.Crystal(clay):clay;if(glass)assets.Add(body);
            var label=StorageMaterials.Opaque(prefab,"Jar parchment",null);label.color=new Color(.76f,.70f,.53f);assets.Add(label);
            surfaces=new[]{body,materials[0],label};vesselMaterials[glass]=surfaces;
        }
        var jar=new GameObject("jar_"+index);jar.layer=prefab.layer;jar.transform.SetParent(parent,false);
        for(int i=0;i<meshes.Length;i++)
        {
            var part=new GameObject(i==0?"body":i==1?"lid":"label");part.layer=prefab.layer;part.transform.SetParent(jar.transform,false);
            part.AddComponent<MeshFilter>().sharedMesh=meshes[i];part.AddComponent<MeshRenderer>().sharedMaterial=surfaces[i];
        }
        return jar;
    }
    internal static void Release(){foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();models.Clear();vesselMaterials.Clear();meshes=null;materials=null;}
}
