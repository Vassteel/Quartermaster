using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Quartermaster;

public sealed class GullToss : MonoBehaviour
{
    private static readonly List<GullToss> Active=new List<GullToss>();
    private Transform owner;
    private Vector3 velocity, spin;
    private float born, bouncedAt=-1;
    private Material[] materials;
    private Color[] colors;
    private int hits;
    private static readonly int FloorMask=LayerMask.GetMask("Default","static_solid","piece","terrain","vehicle");
    internal static void Spawn(Transform owner, ItemDrop.ItemData item, Vector3 position)
    {
        Active.RemoveAll(x=>!x);
        if(Active.Count>=24 || !owner || !item?.m_dropPrefab) return;
        // Borrow one mesh and its texture; never instantiate an item prefab or register a drop.
        MeshFilter source=null; MeshRenderer renderer=null;
        foreach(var candidate in item.m_dropPrefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var r=candidate.GetComponent<MeshRenderer>();
            if(candidate.sharedMesh && r && r.enabled && r.sharedMaterials.Length>0)
            { source=candidate; renderer=r; break; }
        }
        if(!source) return;
        var mesh=source.sharedMesh;
        float largest=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
        if(largest<.0001f) return;
        var root=new GameObject("Quartermaster temporary sorting prop");
        root.transform.position=position;
        var visual=new GameObject("Item mesh only"); visual.transform.SetParent(root.transform,false);
        float scale=.16f/largest;
        visual.transform.localScale=Vector3.one*scale; visual.transform.localPosition=-mesh.bounds.center*scale;
        visual.AddComponent<MeshFilter>().sharedMesh=mesh;
        var art=visual.AddComponent<MeshRenderer>(); art.shadowCastingMode=ShadowCastingMode.Off;
        var toss=root.AddComponent<GullToss>(); toss.owner=owner; toss.born=Time.time;
        var original=renderer.sharedMaterials;
        toss.materials=new Material[original.Length]; toss.colors=new Color[original.Length];
        for(int i=0;i<original.Length;i++)
        {
            var shader=Shader.Find("Standard"); if(!shader) shader=Shader.Find("Sprites/Default");
            var m=new Material(shader);
            var c=original[i] && original[i].HasProperty("_Color") ? original[i].color : Color.white;
            c.a=1; m.color=c;
            if(original[i] && original[i].HasProperty("_MainTex")) m.mainTexture=original[i].mainTexture;
            if(m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode",2); m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha); m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite",0); m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.renderQueue=(int)RenderQueue.Transparent;
            toss.materials[i]=m; toss.colors[i]=c;
        }
        art.sharedMaterials=toss.materials;
        toss.velocity=owner.TransformDirection(new Vector3(Random.Range(-1.2f,1.2f),0,Random.Range(.7f,1.6f)))+Vector3.up*Random.Range(1.3f,2.2f);
        toss.spin=Random.onUnitSphere*Random.Range(140f,330f);
        Active.Add(toss);
    }
    private void Update()
    {
        if(!owner || !owner.gameObject.activeInHierarchy || !Plugin.Instance || !Plugin.Enabled.Value || Time.time-born>=4)
        { Destroy(gameObject); return; }
        float dt=Mathf.Min(Time.deltaTime,.05f);
        if(hits<3)
        {
            velocity+=Vector3.down*6f*dt; var step=velocity*dt;
            if(step.sqrMagnitude>.000001f && Physics.SphereCast(transform.position,.035f,step.normalized,out var hit,step.magnitude,FloorMask,QueryTriggerInteraction.Ignore))
            {
                transform.position=hit.point+hit.normal*.04f;
                velocity=Vector3.Reflect(velocity,hit.normal)*.4f; spin*=.5f; hits++;
                // A wall/chest-side ricochet is allowed, but only a floor-facing surface starts the fade.
                if(hit.normal.y>.45f && bouncedAt<0) bouncedAt=Time.time;
            }
            else transform.position+=step;
        }
        transform.Rotate(spin*dt,Space.World);
        float alpha=GullTossLife.Alpha(Time.time-born,bouncedAt<0 ? -1 : Time.time-bouncedAt);
        for(int i=0;i<materials.Length;i++) { var c=colors[i]; c.a=alpha; materials[i].color=c; }
        if(alpha<=0) Destroy(gameObject);
    }
    internal static void ClearFor(Transform owner)
    {
        for(int i=Active.Count-1;i>=0;i--)
            if(!Active[i]) Active.RemoveAt(i);
            else if(Active[i].owner==owner) { Object.Destroy(Active[i].gameObject); Active.RemoveAt(i); }
    }
    private void OnDestroy()
    {
        Active.Remove(this);
        if(materials!=null) foreach(var material in materials) if(material) Destroy(material);
    }
}
