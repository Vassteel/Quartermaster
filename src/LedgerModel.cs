using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

internal static class LedgerModel
{
    private static readonly List<Object> assets=new List<Object>();
    internal static Sprite Icon()
    {
        using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Ledger.icon"))
        using(var buffer=new MemoryStream())
        {
            stream.CopyTo(buffer);var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);assets.Add(texture);
            if(!ImageConversion.LoadImage(texture,buffer.ToArray()))throw new InvalidDataException("Invalid ledger icon");
            var icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));assets.Add(icon);return icon;
        }
    }
    internal static void Build(GameObject prefab)
    {
        var source=prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)
            .FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Custom/Piece");
        if(!source)throw new InvalidOperationException("Lectern needs the native wood building shader");
        foreach(var r in prefab.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
        foreach(var c in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        var colors=new[]{new Color(.28f,.16f,.075f),new Color(.12f,.075f,.038f),new Color(.84f,.76f,.55f),new Color(.025f,.021f,.014f),new Color(.43f,.29f,.10f),new Color(.24f,.035f,.023f)};
        var materials=colors.Select((color,i)=>{
            var m=new Material(source.shader){name="Quartermaster ledger material "+i,color=color};assets.Add(m);
            if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",Texture2D.whiteTexture);
            foreach(var key in new[]{"_Glossiness","_Metallic","_MetalGloss","_NoiseGlowEnabled","_TriplanarMap","_ValueNoise","_ValueNoiseVertex"})if(m.HasProperty(key))m.SetFloat(key,0);
            foreach(var key in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(m.HasProperty(key))m.SetColor(key,Color.black);
            m.DisableKeyword("_EMISSION");m.DisableKeyword("NOISEGLOW");return m;
        }).ToArray();
        using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Ledger.model"))
        using(var reader=new BinaryReader(stream??throw new InvalidDataException("Missing ledger model")))
        {
            if(new string(reader.ReadChars(4))!="QML1"||reader.ReadInt32()!=2)throw new InvalidDataException("Invalid ledger model");
            for(int part=0;part<2;part++)
            {
                int count=reader.ReadInt32();if(count<3||count>20000)throw new InvalidDataException("Invalid ledger vertex count");
                var vertices=new Vector3[count];for(int i=0;i<count;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                if(reader.ReadInt32()!=materials.Length)throw new InvalidDataException("Invalid ledger palette");
                var mesh=new Mesh{name=part==0?"Quartermaster lectern and ledger":"Quartermaster turning ledger page"};assets.Add(mesh);mesh.vertices=vertices;mesh.subMeshCount=materials.Length;
                for(int group=0;group<materials.Length;group++)
                {
                    int n=reader.ReadInt32();if(n<0||n>60000||n%3!=0)throw new InvalidDataException("Invalid ledger indices");
                    var triangles=new int[n];for(int j=0;j<n;j++){triangles[j]=reader.ReadInt32();if(triangles[j]<0||triangles[j]>=count)throw new InvalidDataException("Invalid ledger triangle");}
                    mesh.SetTriangles(triangles,group);
                }
                mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(part==0?"Quartermaster ledger lectern":"Quartermaster ledger page");go.layer=prefab.layer;go.transform.SetParent(prefab.transform,false);
                if(part==1){go.transform.localPosition=new Vector3(0,1.251487f,-.036938f);go.transform.localRotation=Quaternion.Euler(-20,0,0);}
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;
            }
        }
        Box(prefab.transform,new Vector3(0,.12f,0),new Vector3(.85f,.24f,.66f));
        Box(prefab.transform,new Vector3(0,.66f,.045f),new Vector3(.2f,.96f,.22f));
        var desk=new GameObject("Ledger desk collision");desk.layer=prefab.layer;desk.transform.SetParent(prefab.transform,false);
        desk.transform.localPosition=new Vector3(0,1.15f,0);desk.transform.localRotation=Quaternion.Euler(-20,0,0);
        Box(desk.transform,new Vector3(0,.07f,0),new Vector3(1,.23f,.72f));
        Box(prefab.transform,new Vector3(-.64f,.66f,.05f),new Vector3(.10f,1.2f,.10f));
        Box(prefab.transform,new Vector3(-.64f,1.25f,.05f),new Vector3(.36f,.06f,.34f));
    }
    private static void Box(Transform parent,Vector3 center,Vector3 size)
    {var collider=parent.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;}
    internal static void Release(){foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();}
}
