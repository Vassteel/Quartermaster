using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Quartermaster.Cosmetics;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Quartermaster;
internal static class DrawerCabinetArt
{
    private static readonly List<Object> assets=new List<Object>();
    private static readonly Dictionary<int,Mesh[]> models=new Dictionary<int,Mesh[]>();
    private static Mesh[] meshes;
    private static Material[] materials;
    internal static Vector3 Position(int index)=>ModularCabinetPlacement.DrawerPosition(index);
    private static void Ensure(GameObject prefab,int variant)
    {
        if(models.TryGetValue(variant,out meshes))return;
        var made=new List<Mesh>();
        try
        {
            if(materials==null){materials=new Material[5];string[] ids={"Wood","RoundLog","FineWood"};
            for(int i=0;i<3;i++)
            {
                var source=PrefabManager.Instance.GetPrefab(ids[i]);
                var texture=source?source.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.mainTexture):null;
                if(!texture)throw new InvalidOperationException("Missing native "+ids[i]+" texture");
                var material=StorageMaterials.Opaque(prefab,"Apothecary drawers "+ids[i],texture.mainTexture);material.color=new Color(.8f,.8f,.8f);materials[i]=material;assets.Add(material);
            }
            materials[3]=StorageMaterials.Opaque(prefab,"Drawer iron",null);materials[3].color=new Color(.12f,.11f,.10f);assets.Add(materials[3]);
            materials[4]=StorageMaterials.Opaque(prefab,"Drawer labels",null);materials[4].color=new Color(.61f,.52f,.35f);assets.Add(materials[4]);}
            OwlModelData data;using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.DrawerCabinet."+variant))data=OwlModelData.Read(stream,true,5);
            foreach(var p in data.Levels[0])
            {
                int count=p.Vertices.Length/8;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){int j=i*8;var a=p.Vertices;v[i]=new Vector3(a[j],a[j+1],a[j+2]);n[i]=new Vector3(a[j+3],a[j+4],a[j+5]);uv[i]=new Vector2(a[j+6],a[j+7]);}
                var mesh=new Mesh{name="Apothecary drawers surface "+p.Material,vertices=v,normals=n,uv=uv,triangles=p.Triangles};mesh.RecalculateBounds();made.Add(mesh);assets.Add(mesh);
            }
            meshes=made.ToArray();models[variant]=meshes;
        }
        catch{Release();throw;}
    }
    internal static void Build(GameObject prefab,int variant)
    {
        Ensure(prefab,variant);
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;renderer.forceRenderingOff=true;}
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(group);
        foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))Object.DestroyImmediate(t.gameObject);
        var art=new GameObject("Quartermaster furniture");art.layer=prefab.layer;art.transform.SetParent(prefab.transform,false);
        for(int i=0;i<meshes.Length;i++)
        {var go=new GameObject("Native timber surface");go.layer=prefab.layer;go.transform.SetParent(art.transform,false);go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterial=materials[i];}
        // Only the carcass collides here. Independent drawer colliders cover the fronts.
        foreach(float x in new[]{-.472f,.472f}){var side=art.AddComponent<BoxCollider>();side.center=new Vector3(x,.5f,0);side.size=new Vector3(.05f,1,.50f);}
        foreach(float y in new[]{.03f,.975f}){var cap=art.AddComponent<BoxCollider>();cap.center=new Vector3(0,y,0);cap.size=new Vector3(.994f,.05f,.50f);}
        var back=art.AddComponent<BoxCollider>();back.center=new Vector3(0,.5f,-.23f);back.size=new Vector3(.90f,.90f,.04f);
        // Rear/top snap planes clear walls and share the authored grid, scaled together by ModularCabinetPlacement.
        foreach(float y in new[]{0f,1f})foreach(float x in new[]{-.5f,0f,.5f})
        {var point=new GameObject("snappoint");point.tag="snappoint";point.transform.SetParent(prefab.transform,false);point.transform.localPosition=new Vector3(x,y,-.26f);}
        ModularCabinetPlacement.Configure(prefab,true);
    }
    internal static void Release(){foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();models.Clear();meshes=null;materials=null;}
}
