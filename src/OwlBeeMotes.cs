using UnityEngine;
using UnityEngine.Rendering;

namespace Quartermaster;

// A small reusable local swarm. No AI, lights, audio, colliders or network objects.
internal sealed class OwlBeeMotes : MonoBehaviour
{
    private readonly Transform[] motes=new Transform[7];
    private Material amber,dark,wing;
    internal static OwlBeeMotes Create(Transform bird)
    {
        var go=new GameObject("Angry bee motes");go.transform.SetParent(bird,false);
        var swarm=go.AddComponent<OwlBeeMotes>();swarm.Build();return swarm;
    }
    private Material Material(Color color)
    {
        var material=new Material(Shader.Find("Sprites/Default"));material.color=color;return material;
    }
    private Transform Dot(Transform parent,Material material,Vector3 position,Vector3 size)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name="Bee mote";go.transform.SetParent(parent,false);
        go.transform.localPosition=position;go.transform.localScale=size;
        var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return go.transform;
    }
    private void Build()
    {
        amber=Material(new Color(.8f,.51f,.09f));dark=Material(new Color(.13f,.095f,.045f));
        wing=Material(new Color(.8f,.76f,.61f));
        for(int i=0;i<motes.Length;i++)
        {
            var bee=new GameObject("Angry bee").transform;bee.SetParent(transform,false);motes[i]=bee;
            Dot(bee,amber,Vector3.zero,new Vector3(.035f,.025f,.055f));
            Dot(bee,dark,new Vector3(0,0,.005f),new Vector3(.037f,.027f,.015f));
            Dot(bee,wing,new Vector3(0,.014f,-.005f),new Vector3(.08f,.008f,.022f));
        }
    }
    internal void Show(Vector3 owl,Vector3 hive,float time)
    {
        gameObject.SetActive(true);
        float envelope=Mathf.Clamp01(time/.25f)*Mathf.Clamp01((3.6f-time)/.6f);
        for(int i=0;i<motes.Length;i++)
        {
            float phase=i*2.39996f+time*(8+i*.4f);
            var center=Vector3.Lerp(hive,owl,Mathf.Clamp01(time*3));
            var offset=new Vector3(Mathf.Cos(phase)*(.19f+i*.013f),Mathf.Sin(phase*1.7f)*.14f,Mathf.Sin(phase)*.22f);
            motes[i].position=center+offset;
            motes[i].rotation=Quaternion.Euler(12*Mathf.Sin(phase*3),-phase*Mathf.Rad2Deg,25*Mathf.Sin(phase*7));
            motes[i].localScale=Vector3.one*envelope;
        }
    }
    internal void Hide()=>gameObject.SetActive(false);
    private void OnDestroy(){Destroy(amber);Destroy(dark);Destroy(wing);}
}
