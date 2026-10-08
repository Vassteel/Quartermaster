using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Quartermaster.Cosmetics;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Quartermaster;
internal static class MeadCabinetArt
{
    private static readonly List<Object> assets=new List<Object>();
    private static Mesh[] meshes;
    private static Material[] materials;
    internal static Vector3[] Slots()
    {
        // Centres sit half an item above each shelf so NativeHangingItem's
        // restOnBase fitting keeps bottles standing on the planks.
        var result=new Vector3[12];float[] floors={.063f,.370f,.661f};
        for(int row=0;row<3;row++)for(int col=0;col<4;col++)
        {float x=row==2?new[]{-.33f,-.13f,.23f,.37f}[col]:-.33f+col*.22f;result[row*4+col]=new Vector3(x,floors[row]+.10f,.01f);}
        return result;
    }
    private static void Ensure(GameObject prefab)
    {
        if(meshes!=null)return;
        var made=new List<Mesh>();
        try
        {
            materials=new Material[3];string[] ids={"Wood","RoundLog","FineWood"};
            for(int i=0;i<3;i++)
            {
                var source=PrefabManager.Instance.GetPrefab(ids[i]);
                var texture=source?source.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.mainTexture):null;
                if(!texture)throw new InvalidOperationException("Missing native "+ids[i]+" texture");
                var material=StorageMaterials.Opaque(prefab,"Mead cabinet "+ids[i],texture.mainTexture);material.color=new Color(.8f,.8f,.8f);materials[i]=material;assets.Add(material);
            }
            OwlModelData data;using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.MeadCabinet.model"))data=OwlModelData.Read(stream,true);
            foreach(var p in data.Levels[0])
            {
                int count=p.Vertices.Length/8;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){int j=i*8;var a=p.Vertices;v[i]=new Vector3(a[j],a[j+1],a[j+2]);n[i]=new Vector3(a[j+3],a[j+4],a[j+5]);uv[i]=new Vector2(a[j+6],a[j+7]);}
                var mesh=new Mesh{name="Mead cabinet surface "+p.Material,vertices=v,normals=n,uv=uv,triangles=p.Triangles};mesh.RecalculateBounds();made.Add(mesh);assets.Add(mesh);
            }
            meshes=made.ToArray();
        }
        catch{Release();throw;}
    }
    internal static void Build(GameObject prefab)
    {
        Ensure(prefab);
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
        ModularCabinetPlacement.Configure(prefab);
        var chest=prefab.GetComponent<Container>();chest.m_open=null;chest.m_closed=null;
    }
    internal static void Release(){foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();meshes=null;materials=null;}
}
