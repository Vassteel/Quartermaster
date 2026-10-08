using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

// Render-only copies of the stored prefab. Never instantiate ItemDrop, Fish, physics or ZNetView.
internal sealed class NativeHangingItem : IDisposable
{
    private sealed class Part
    {
        internal Transform Transform;
        internal Mesh Mesh;
        internal Material[] Materials;
        internal MaterialPropertyBlock Properties;
        internal bool Baked;
    }
    private sealed class Source
    {
        internal GameObject Prefab;
        internal Part[] Parts;
        internal Bounds Bounds;
        internal int Users;
    }
    private static readonly Dictionary<GameObject,Source> Sources=new Dictionary<GameObject,Source>();
    private static readonly HashSet<GameObject> Warned=new HashSet<GameObject>();
    private readonly Transform parent;
    private Source source;
    private GameObject visual;
    private MeshRenderer[] renderers;
    private int variant=-1;
    private readonly bool rack,horizontal,preserveUp,restOnBase;
    private readonly Vector3 fitSize=new Vector3(.32f,.42f,.24f);
    internal NativeHangingItem(Transform parent){this.parent=parent;}
    internal NativeHangingItem(Transform parent,Vector3 fitSize,bool horizontal,bool preserveUp=false,bool restOnBase=false)
    {this.parent=parent;this.fitSize=fitSize;this.horizontal=horizontal;this.preserveUp=preserveUp;this.restOnBase=restOnBase;rack=true;}
    internal bool Set(ItemDrop.ItemData item)
    {
        var prefab=item?.m_dropPrefab;
        if(!prefab||item.m_stack<=0){Dispose();return false;}
        if(source!=null&&source.Prefab==prefab)
        {ApplyVariant(item.m_variant);return true;}
        Dispose();
        if(!Sources.TryGetValue(prefab,out var entry))
        {
            entry=Read(prefab);
            if(entry==null)
            {
                if(Warned.Add(prefab))Plugin.Log.LogWarning("No bounded render-only hanging model for "+prefab.name+"; inventory is unchanged.");
                return false;
            }
            Sources.Add(prefab,entry);
        }
        source=entry;source.Users++;
        try
        {
        visual=new GameObject("Stored item model: "+prefab.name);visual.layer=parent.gameObject.layer;visual.transform.SetParent(parent,false);
        var copies=new Dictionary<Transform,Transform>();renderers=new MeshRenderer[source.Parts.Length];
        for(int i=0;i<source.Parts.Length;i++)
        {
            var part=source.Parts[i];var target=CopyPath(part.Transform,copies);
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=part.Mesh;
            var renderer=target.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=part.Materials;renderer.SetPropertyBlock(part.Properties);renderers[i]=renderer;
        }
        // Keep native proportions; center rack items or suspend hanging items from their top.
        Vector3 size=source.Bounds.size;
        Vector3 axis=size.y>=size.x&&size.y>=size.z?Vector3.up:size.z>=size.x?Vector3.forward:Vector3.right;
        var rotation=preserveUp?Quaternion.identity:Quaternion.FromToRotation(axis,rack&&horizontal?Vector3.right:Vector3.up);
        var bounds=RotateBounds(source.Bounds,rotation);
        if(rack&&!preserveUp&&(horizontal?bounds.size.y<bounds.size.z:bounds.size.x<bounds.size.z))
        {
            rotation=Quaternion.AngleAxis(90,horizontal?Vector3.right:Vector3.up)*rotation;
            bounds=RotateBounds(source.Bounds,rotation);
        }
        float scale=Mathf.Min(fitSize.y/Mathf.Max(.0001f,bounds.size.y),Mathf.Min(fitSize.x/Mathf.Max(.0001f,bounds.size.x),fitSize.z/Mathf.Max(.0001f,bounds.size.z)));
        visual.transform.localRotation=rotation;visual.transform.localScale=Vector3.one*scale;
        visual.transform.localPosition=new Vector3(-bounds.center.x*scale,restOnBase?-fitSize.y*.5f-bounds.min.y*scale:rack?-bounds.center.y*scale:.505f-bounds.max.y*scale,-bounds.center.z*scale);
        ApplyVariant(item.m_variant);return true;
        }
        catch(Exception)
        {
            Dispose();
            if(Warned.Add(prefab))Plugin.Log.LogWarning("Could not construct hanging model for "+prefab.name+"; inventory is unchanged.");
            return false;
        }
    }
    private Transform CopyPath(Transform original,Dictionary<Transform,Transform> copies)
    {
        if(original==source.Prefab.transform)return visual.transform;
        if(copies.TryGetValue(original,out var existing))return existing;
        var parentCopy=CopyPath(original.parent,copies);
        var child=new GameObject(original.name);child.layer=parent.gameObject.layer;child.transform.SetParent(parentCopy,false);
        child.transform.localPosition=original.localPosition;child.transform.localRotation=original.localRotation;child.transform.localScale=original.localScale;
        copies.Add(original,child.transform);return child.transform;
    }
    private void ApplyVariant(int value)
    {
        value=Math.Max(0,value);if(variant==value||renderers==null)return;variant=value;
        for(int i=0;i<renderers.Length;i++)
        {
            var properties=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(properties);
            // Native ItemStyle.Setup writes this same property through MaterialMan.
            properties.SetInt("_Style",variant);renderers[i].SetPropertyBlock(properties);
        }
    }
    private static Source Read(GameObject prefab)
    {
        var root=prefab.transform.Find("attach")??prefab.transform;
        var ignored=new HashSet<Renderer>();
        foreach(var group in root.GetComponentsInChildren<LODGroup>(true))
        {
            var levels=group.GetLODs();var first=new HashSet<Renderer>(levels.Length>0?levels[0].renderers:Array.Empty<Renderer>());
            foreach(var level in levels.Skip(1))foreach(var renderer in level.renderers)if(renderer&&!first.Contains(renderer))ignored.Add(renderer);
        }
        var parts=new List<Part>();int vertices=0;var bounds=new Bounds();bool hasBounds=false;
        try
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer.enabled||ignored.Contains(renderer)||!ActiveBranch(renderer.transform,root))continue;
                Mesh mesh=null;bool baked=false;
                if(renderer is MeshRenderer)
                {var filter=renderer.GetComponent<MeshFilter>();if(filter)mesh=filter.sharedMesh;}
                else if(renderer is SkinnedMeshRenderer skin&&skin.sharedMesh)
                {
                    if(skin.sharedMesh.vertexCount>30000)throw new InvalidOperationException("Oversized skinned item");
                    mesh=new Mesh{name="Quartermaster static hanging pose"};
                    try {skin.BakeMesh(mesh);baked=true;}catch{Object.Destroy(mesh);throw;}
                }
                if(!mesh)continue;
                var part=new Part{Transform=renderer.transform,Mesh=mesh,Materials=renderer.sharedMaterials,Properties=new MaterialPropertyBlock(),Baked=baked};
                parts.Add(part);renderer.GetPropertyBlock(part.Properties);vertices+=mesh.vertexCount;
                if(parts.Count>8||vertices>30000||part.Materials.Length==0||part.Materials.Length>8)throw new InvalidOperationException("Item display budget exceeded");
                var matrix=prefab.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                foreach(var corner in Corners(mesh.bounds))
                {
                    var point=matrix.MultiplyPoint3x4(corner);
                    if(!Finite(point))throw new InvalidOperationException("Invalid item bounds");
                    if(!hasBounds){bounds=new Bounds(point,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(point);
                }
            }
            if(!hasBounds||Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))<.0001f)throw new InvalidOperationException("Empty item visual");
            return new Source{Prefab=prefab,Parts=parts.ToArray(),Bounds=bounds};
        }
        catch(Exception)
        {foreach(var part in parts)if(part.Baked)Object.Destroy(part.Mesh);return null;}
    }
    private static bool ActiveBranch(Transform node,Transform root)
    {for(var t=node;t&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
    private static bool Finite(Vector3 p)=>!(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z));
    internal static IEnumerable<Vector3> Corners(Bounds b)
    {for(int x=0;x<2;x++)for(int y=0;y<2;y++)for(int z=0;z<2;z++)yield return new Vector3(x==0?b.min.x:b.max.x,y==0?b.min.y:b.max.y,z==0?b.min.z:b.max.z);}
    internal static Bounds RotateBounds(Bounds original,Quaternion rotation)
    {var result=new Bounds(rotation*original.center,Vector3.zero);foreach(var point in Corners(original))result.Encapsulate(rotation*point);return result;}
    public void Dispose()
    {
        if(visual){visual.SetActive(false);Object.Destroy(visual);}visual=null;renderers=null;variant=-1;
        if(source!=null&&--source.Users==0)
        {Sources.Remove(source.Prefab);foreach(var part in source.Parts)if(part.Baked)Object.Destroy(part.Mesh);}
        source=null;
    }
    internal static void ClearWarnings()=>Warned.Clear();
}
