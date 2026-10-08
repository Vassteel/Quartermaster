using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

// Original generated art. Native prefabs provide behavior and a shader, never clay artwork.
internal static class ClayModel
{
    private static readonly List<Object> assets=new List<Object>();
    private static Mesh[] meshes;
    private static Material material;
    private static Sprite icon;
    internal static Sprite Icon()
    {
        if(icon)return icon;
        var texture=Texture("icon");
        icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
        assets.Add(icon);return icon;
    }
    private static Texture2D Texture(string name)
    {
        using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Clay."+name))
        using(var bytes=new MemoryStream())
        {
            if(stream==null)throw new InvalidDataException("Missing clay "+name);
            stream.CopyTo(bytes);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,name!="icon"){name="Quartermaster clay "+name};
            assets.Add(texture);
            if(!ImageConversion.LoadImage(texture,bytes.ToArray()))throw new InvalidDataException("Invalid clay "+name);
            texture.filterMode=name=="icon"?FilterMode.Bilinear:FilterMode.Point;
            texture.wrapMode=TextureWrapMode.Clamp;return texture;
        }
    }
    internal static GameObject Replace(GameObject prefab,bool patch)
    {
        if(!material)
        {
            material=StorageMaterials.Opaque(prefab,"Quartermaster original raw clay",Texture("albedo"));
            assets.Add(material);
        }
        EnsureMeshes();
        // Both native templates have behavior on the root and art on child objects.
        // Remove old art, LODs and colliders so no template shape can reappear.
        foreach(var child in prefab.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        foreach(var collider in prefab.GetComponents<Collider>())Object.DestroyImmediate(collider);
        var visual=new GameObject(patch?"Quartermaster clay patch":"attach");
        visual.layer=prefab.layer;visual.transform.SetParent(prefab.transform,false);
        visual.AddComponent<MeshFilter>().sharedMesh=meshes[patch?1:0];
        visual.AddComponent<MeshRenderer>().sharedMaterial=material;
        var hit=visual.AddComponent<BoxCollider>();var bounds=meshes[patch?1:0].bounds;
        hit.center=bounds.center;hit.size=bounds.size;
        return visual;
    }
    private static void EnsureMeshes()
    {
        if(meshes!=null)return;
        using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Clay.model"))
        using(var reader=new BinaryReader(stream??throw new InvalidDataException("Missing clay mesh")))
        {
            if(new string(reader.ReadChars(4))!="QMY1"||reader.ReadInt32()!=2)throw new InvalidDataException("Invalid clay mesh format");
            meshes=new Mesh[2];
            for(int part=0;part<2;part++)
            {
                int count=reader.ReadInt32();if(count<3||count>10000)throw new InvalidDataException("Invalid clay vertex count");
                var vertices=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++)
                {vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());}
                int length=reader.ReadInt32();if(length<3||length>30000||length%3!=0)throw new InvalidDataException("Invalid clay index count");
                var indices=new int[length];for(int i=0;i<length;i++){indices[i]=reader.ReadInt32();if(indices[i]<0||indices[i]>=count)throw new InvalidDataException("Invalid clay index");}
                var mesh=new Mesh{name=part==0?"Quartermaster clay clod":"Quartermaster clay bank patch"};assets.Add(mesh);
                mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[part]=mesh;
            }
            if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing clay mesh data");
        }
    }
    internal static void Release()
    {foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();meshes=null;material=null;icon=null;}
}
