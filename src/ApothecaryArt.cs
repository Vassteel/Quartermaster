using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

// Shared authored meshes/materials; instances contain renderers only, never item scripts.
internal static class ApothecaryArt
{
    private sealed class Part { internal string Name; internal int Material; internal Mesh Mesh; }
    private static readonly Dictionary<string,List<Part>> models=new Dictionary<string,List<Part>>();
    private static readonly Dictionary<string,Vector3[]> slots=new Dictionary<string,Vector3[]>();
    private static readonly Dictionary<string,Vector3[]> anchors=new Dictionary<string,Vector3[]>();
    private static readonly List<Object> assets=new List<Object>();
    private static Material wood,glass,metal,soft,stone,food,bronze;
    private static bool initialized;
    private static readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
    private static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Invalid apothecary asset count");return n;}
    private static string ReadName(BinaryReader r)=>Encoding.UTF8.GetString(r.ReadBytes(Count(r,80)));
    private static Texture2D Texture(string key)
    {
        using(var s=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Apothecary."+key))
        using(var buffer=new MemoryStream())
        {
            if(s==null)throw new InvalidDataException("Missing apothecary "+key);
            s.CopyTo(buffer);var t=new Texture2D(2,2,TextureFormat.RGBA32,key.EndsWith("albedo",StringComparison.Ordinal));assets.Add(t);
            if(!ImageConversion.LoadImage(t,buffer.ToArray()))throw new InvalidDataException("Invalid apothecary texture");
            t.filterMode=key.EndsWith("albedo",StringComparison.Ordinal)?FilterMode.Point:FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;return t;
        }
    }
    internal static Sprite Icon(string key)
    {
        if(icons.TryGetValue(key,out var icon))return icon;
        var t=Texture(key);icon=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f));
        assets.Add(icon);icons[key]=icon;return icon;
    }
    internal static void Initialize(GameObject native)
    {
        if(initialized)return;
        try
        {
            wood=StorageMaterials.Opaque(native,"Quartermaster apothecary timber and pottery",Texture("albedo"));assets.Add(wood);
            soft=new Material(wood){name="Quartermaster hides cloth and bone",mainTexture=Texture("soft_albedo")};assets.Add(soft);
            stone=new Material(wood){name="Quartermaster rough masonry",mainTexture=Texture("stone_albedo")};assets.Add(stone);
            food=new Material(wood){name="Quartermaster pantry provisions",mainTexture=Texture("food_albedo")};assets.Add(food);
            metal=StorageMaterials.Opaque(native,"Quartermaster worn ingots",Texture("bulk_albedo"),.55f);assets.Add(metal);
            bronze=new Material(metal){name="Quartermaster forged bronze",color=new Color(.78f,.57f,.30f)};assets.Add(bronze);
            glass=StorageMaterials.Crystal(wood);assets.Add(glass);
            using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.Apothecary.model"))
            using(var r=new BinaryReader(stream??throw new InvalidDataException("Missing apothecary mesh")))
            {
                if(new string(r.ReadChars(4))!="QMA2")throw new InvalidDataException("Invalid apothecary format");
                int modelCount=Count(r,192);
                for(int m=0;m<modelCount;m++)
                {
                    string name=ReadName(r);int partCount=Count(r,16);var parts=new List<Part>();
                    for(int p=0;p<partCount;p++)
                    {
                        var part=new Part{Name=ReadName(r),Material=Count(r,7)};int n=Count(r,16000);
                        var v=new Vector3[n];var uv=new Vector2[n];
                        for(int i=0;i<n;i++){v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
                        int length=Count(r,48000);if(length%3!=0)throw new InvalidDataException("Invalid apothecary triangles");
                        var ix=new int[length];for(int i=0;i<length;i++){ix[i]=r.ReadInt32();if(ix[i]<0||ix[i]>=n)throw new InvalidDataException("Invalid apothecary index");}
                        var mesh=new Mesh{name="Quartermaster "+name+" "+part.Name,vertices=v,uv=uv,triangles=ix};mesh.RecalculateNormals();mesh.RecalculateBounds();assets.Add(mesh);part.Mesh=mesh;parts.Add(part);
                    }
                    models.Add(name,parts);int count=Count(r,12);var sockets=new Vector3[count];
                    for(int i=0;i<count;i++)sockets[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());slots.Add(name,sockets);
                    count=Count(r,16);var points=new Vector3[count];
                    for(int i=0;i<count;i++)points[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());anchors.Add(name,points);
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing apothecary asset data");
            }
            initialized=true;
        }
        catch { Release(); throw; }
    }
    internal static Vector3[] Slots(string model)=>model=="mead_cabinet"?MeadCabinetArt.Slots():model.StartsWith("modular_",StringComparison.Ordinal)?ModularShelfArt.Slots(model):slots[model];
    internal static GameObject Create(Transform parent,string model,string name=null)
    {
        var root=new GameObject(name??model);root.layer=parent.gameObject.layer;root.transform.SetParent(parent,false);
        foreach(var part in models[model].Where(p=>p.Material!=2))
        {
            var go=new GameObject(part.Name);go.layer=root.layer;go.transform.SetParent(root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=part.Mesh;go.AddComponent<MeshRenderer>().sharedMaterial=part.Material==1?glass:part.Material==3?metal:part.Material==4?soft:part.Material==5?stone:part.Material==6?food:part.Material==7?bronze:wood;
        }
        return root;
    }
    internal static void Item(GameObject prefab,string model)
    {
        foreach(var child in prefab.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        foreach(var c in prefab.GetComponents<Collider>())Object.DestroyImmediate(c);
        var visual=Create(prefab.transform,model,"attach");var hit=visual.AddComponent<BoxCollider>();hit.center=new Vector3(0,.185f,0);hit.size=new Vector3(.28f,.38f,.28f);
    }
    internal static void Cabinet(GameObject prefab,ApothecaryDefinition def)
    {
        if(def.Model.StartsWith("modular_",StringComparison.Ordinal)){ModularShelfArt.Build(prefab,def);return;}
        if(def.Model=="mead_cabinet"){MeadCabinetArt.Build(prefab);return;}
        // Keep native WNT/effect references intact, but remove their render/collision output.
        foreach(var r in prefab.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
        foreach(var c in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))Object.DestroyImmediate(t.gameObject);
        var visual=Create(prefab.transform,def.Model,"Quartermaster furniture");
        for(int i=0;def.Handling==FurnitureHandling.Jar&&i<slots[def.Model].Length;i++)
        {
            var jar=Create(visual.transform,def.Glass?"crystal_flask":"clay_jar","jar_"+i);
            jar.transform.localPosition=slots[def.Model][i];jar.transform.localRotation=Quaternion.Euler(0,(i%3-1)*3f,0);
            StyleJar(jar.transform,def.Glass,i);
        }
        var collision=models[def.Model].FirstOrDefault(p=>p.Material==2);
        if(collision!=null)
        {var hit=visual.AddComponent<MeshCollider>();hit.sharedMesh=collision.Mesh;hit.convex=true;}
        else
        {
            var hit=visual.AddComponent<BoxCollider>();
            // Use the same frame bounds as authored rear-edge snaps. The former
            // hand-sized cabinet box extended behind the back and missed the front.
            var bounds=models[def.Model][0].Mesh.bounds;
            foreach(var part in models[def.Model].Skip(1).Where(p=>p.Material!=2))bounds.Encapsulate(part.Mesh.bounds);
            hit.center=bounds.center;hit.size=bounds.size;
        }
        foreach(var point in anchors[def.Model])
        {var snap=new GameObject("snappoint");snap.tag="snappoint";snap.transform.SetParent(prefab.transform,false);snap.transform.localPosition=point;}
        var ctn=prefab.GetComponent<Container>();ctn.m_open=null;ctn.m_closed=null;
    }
    internal static void SetModel(GameObject target,string model)
    {
        // Bulk loads are authored as one part; switching thresholds allocates no meshes/materials.
        var part=models[model][0];target.GetComponent<MeshFilter>().sharedMesh=part.Mesh;target.GetComponent<MeshRenderer>().sharedMaterial=part.Material==3?metal:part.Material==4?soft:part.Material==5?stone:part.Material==6?food:part.Material==7?bronze:wood;
    }
    internal static void StyleJar(Transform jar,bool glassJar,int i)
    {
        float width=new[]{1f,.93f,1.04f,.97f}[i%4],height=new[]{1f,.94f,1.03f,.97f}[i%4];
        jar.localScale=new Vector3(width,height,width);
        if(!glassJar)
        {
            var tint=new[]{new Color(1.24f,1.16f,1.02f),new Color(.87f,.95f,.71f),new Color(1.02f,.82f,.65f),Color.white}[i%4];
            var properties=new MaterialPropertyBlock();properties.SetColor("_Color",tint);
            foreach(string part in new[]{"body","lid"})jar.Find(part).GetComponent<MeshRenderer>().SetPropertyBlock(properties);
        }
    }
    internal static void Release(){foreach(var a in assets)if(a)Object.Destroy(a);assets.Clear();models.Clear();slots.Clear();anchors.Clear();icons.Clear();wood=glass=metal=soft=stone=food=bronze=null;initialized=false;}
}
