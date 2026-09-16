#nullable enable annotations
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace VikingBirds;

// Original faceted geometry. All renderers use private, non-emissive world materials.
// The feet stay at y=0; body, neck, wings and tools have separate animation pivots.
internal sealed class PerchedBird : IDisposable
{
    internal readonly GameObject Root;
    private readonly Transform body,head,leftWing,rightWing;
    private readonly List<Object> assets=new();
    private readonly Dictionary<string,Material> palette=new();
    private readonly Material source;
    private Texture2D? surface;
    private readonly bool owl;
    private readonly bool pelican;
    private readonly float bodyHeight;
    private readonly List<Transform> eyes=new();
    private float rest;
    private readonly GameObject hammer,chisel,needle,sailcloth;
    private GameObject? fishingPole;
    private readonly Vector3 headRest=new(0,.47f,.035f);
    internal Vector3 BeakWorld=>head.TransformPoint(new Vector3(0,.10f,owl?.27f:pelican?.73f:.38f));
    internal PerchedBird(Transform parent,Material worldMaterial,bool burrowingOwl,bool fishingPelican=false)
    {
        source=worldMaterial;owl=burrowingOwl;pelican=fishingPelican;
        bodyHeight=owl?.29f:pelican?.26f:.18f;
        if(pelican)headRest=new Vector3(0,.68f,.035f);
        Root=new GameObject(owl?"Quartermaster burrowing owl":pelican?"Pelican fisherman":"Puffin shipwright");Root.transform.SetParent(parent,false);
        try
        {
        body=Node("Body pivot",Root.transform,new Vector3(0,bodyHeight,0));
        head=Node("Neck pivot",body,headRest);
        leftWing=Node("Left wing",body,new Vector3(-.22f,.34f,-.02f));
        rightWing=Node("Right wing",body,new Vector3(.22f,.34f,-.02f));
        var brown=new Color(.28f,.20f,.12f);var cream=new Color(.68f,.63f,.48f);
        var dark=new Color(.085f,.09f,.095f);var orange=new Color(.64f,.25f,.07f);
        var feather=owl?brown:pelican?cream:dark;var feet=owl?new Color(.52f,.43f,.29f):orange;
        for(int side=-1;side<=1;side+=2)
        {
            if(pelican&&side<0)
            {Rod("Wooden peg leg",Root.transform,new(-.105f,.01f,0),new(-.105f,.32f,0),.033f,brown);continue;}
            float leg=owl?.18f:pelican?.16f:.115f;
            Ellipsoid("Leg",Root.transform,new(side*.105f,leg,0),new(.027f,leg,.026f),feet,6,3);
            for(int toe=-1;toe<=1;toe++)
                Rod("Toe",Root.transform,new(side*.105f,.02f,0),new(side*.105f+toe*.047f,.015f,.145f-Math.Abs(toe)*.03f),.014f,feet);
            Rod("Back toe",Root.transform,new(side*.105f,.02f,0),new(side*.13f,.012f,-.07f),.012f,dark);
        }
        Ellipsoid("Body",body,new(0,.29f,-.035f),new(.245f,.34f,.195f),feather,12,7);
        Ellipsoid("Breast",body,new(0,.30f,.09f),new(.19f,.30f,.12f),cream,10,6);
        if(pelican)Ellipsoid("Long white neck",body,new(0,.56f,.015f),new(.075f,.25f,.08f),cream,9,6);
        Ellipsoid("Head",head,new(0,.105f,0),new(.24f,.215f,.19f),feather,12,7);
        for(int side=-1;side<=1;side+=2)
        {
            Transform wing=side<0?leftWing:rightWing;
            Ellipsoid("Folded wing",wing,new(side*.012f,-.12f,-.045f),new(.066f,.22f,.105f),feather,8,5);
            for(int f=0;f<4;f++)
                Ellipsoid("Flight feather",wing,new(side*.005f,-.20f+f*.017f,-.105f+f*.035f),new(.035f,.135f,.029f),feather*.82f,6,3);
            Ellipsoid("Face",head,new(side*.113f,.12f,.11f),new(.118f,.142f,.086f),cream,8,5);
            var eye=Node("Blink",head,new(side*(owl?.125f:.16f),.15f,owl?.19f:.18f));eyes.Add(eye);
            Ellipsoid("Eye surround",eye,Vector3.zero,new(owl?.072f:.043f,owl?.077f:.049f,.028f),dark,10,3);
            Ellipsoid("Eye",eye,new(0,0,.024f),new(owl?.053f:.028f,owl?.055f:.032f,.014f),owl?new Color(.63f,.55f,.10f):new Color(.24f,.12f,.045f),10,3);
            Ellipsoid("Pupil",eye,new(0,0,.037f),new(owl?.024f:.021f,owl?.032f:.028f,.009f),dark,8,3);
            if(owl)
            {
                var brow=Ellipsoid("Cream eyebrow",head,new(side*.115f,.232f,.16f),new(.13f,.025f,.032f),cream,6,3);
                brow.localRotation=Quaternion.Euler(0,0,-side*13);
                for(int f=0;f<7;f++)
                    Ellipsoid("Mottled wing spot",wing,new(side*.068f,-.02f-f*.034f,-.085f+(f%2)*.07f),new(.007f,.012f,.021f),cream,5,2);
            }
        }
        Wedge("Beak",head,new(0,.065f,.18f),owl?new(.09f,.11f,.12f):pelican?new(.13f,.10f,.55f):new(.13f,.22f,.24f),owl?new Color(.4f,.36f,.23f):orange);
        if(pelican)Ellipsoid("Bill pouch",head,new(0,-.015f,.37f),new(.07f,.095f,.23f),new Color(.65f,.43f,.21f),9,5);
        if(!owl&&!pelican)Wedge("Beak stripe",head,new(0,.065f,.20f),new(.132f,.18f,.075f),new Color(.41f,.36f,.22f));
        if(owl)
        {
            var cloth=new Color(.23f,.27f,.16f);
            for(int side=-1;side<=1;side+=2)
                Ellipsoid("Cloth waistcoat",body,new(side*.093f,.21f,.225f),new(.093f,.196f,.046f),cloth,8,5);
            for(int i=0;i<3;i++)Ellipsoid("Wooden button",body,new(.015f,.31f-i*.083f,.27f),new(.012f,.012f,.008f),brown,6,2);
            Ellipsoid("Cloth patch",body,new(-.13f,.12f,.26f),new(.038f,.041f,.013f),cloth*1.22f,4,3);
            var leather=new Color(.22f,.13f,.065f);
            var cap=Node("Crooked leather cap",head,new(0,.28f,0));cap.localRotation=Quaternion.Euler(0,0,-9);
            Ellipsoid("Cap crown",cap,Vector3.zero,new(.248f,.09f,.20f),leather,12,4);
            Ellipsoid("Short brim",cap,new(0,-.031f,.16f),new(.25f,.025f,.14f),leather,10,3);
            Ring("Keyring",body,new(.19f,.15f,.17f),.045f,.006f,new Color(.21f,.21f,.18f));
            for(int k=0;k<3;k++)
            {
                var a=new Vector3(.16f+k*.025f,.12f,.18f);
                Rod("Key shaft",body,a,a+new Vector3(.01f,-.085f,0),.007f,dark);
                Rod("Key bit",body,a+new Vector3(.01f,-.075f,0),a+new Vector3(.035f,-.075f,0),.01f,dark);
            }
        }
        else if(pelican)
        {
            var oilcloth=new Color(.32f,.30f,.16f);
            Ellipsoid("Fisherman's hat",head,new(0,.27f,0),new(.235f,.105f,.19f),oilcloth,12,4);
            Ellipsoid("Wide turned brim",head,new(0,.20f,0),new(.32f,.033f,.26f),oilcloth*.82f,12,3);
            Rod("Hat cord",head,new(-.19f,.18f,.1f),new(-.1f,-.075f,.14f),.009f,brown);
        }
        else
        {
            var leather=new Color(.31f,.20f,.10f);var wool=new Color(.12f,.17f,.22f);
            Ellipsoid("Leather apron",body,new(0,.18f,.235f),new(.21f,.265f,.04f),leather,8,5);
            Ellipsoid("Apron pocket",body,new(.075f,.15f,.277f),new(.065f,.059f,.018f),leather*.8f,6,3);
            Rod("Carpenter pencil",body,new(.083f,.17f,.295f),new(.10f,.30f,.296f),.012f,new Color(.63f,.43f,.15f));
            Ellipsoid("Knit cap",head,new(-.02f,.29f,-.02f),new(.244f,.14f,.20f),wool,12,6);
            Ellipsoid("Folded wool brim",head,new(0,.25f,0),new(.252f,.047f,.20f),wool*1.2f,12,3);
            Ellipsoid("Fuzzy pompom",head,new(-.045f,.435f,-.045f),new(.079f,.072f,.077f),cream,9,5);
            for(int side=-1;side<=1;side+=2)
                Rod("Apron strap",body,new(side*.16f,.38f,.16f),new(side*.10f,.52f,.08f),.018f,leather);
        }
        hammer=Tool("Wooden hammer",head);chisel=Tool("Chisel",head);needle=Tool("Sail needle",head);
        Rod("Handle",hammer.transform,new(0,0,0),new(.23f,0,0),.018f,new Color(.36f,.23f,.10f));
        Ellipsoid("Hammer head",hammer.transform,new(.24f,0,0),new(.043f,.09f,.042f),brown,4,3);
        Rod("Chisel handle",chisel.transform,Vector3.zero,new(0,-.085f,.02f),.024f,brown);
        Rod("Chisel blade",chisel.transform,new(0,-.07f,.02f),new(0,-.20f,.045f),.011f,new Color(.27f,.28f,.27f));
        Rod("Needle",needle.transform,Vector3.zero,new(.16f,0,.06f),.005f,new Color(.35f,.36f,.33f));
        sailcloth=new GameObject("Sailcloth sample");sailcloth.transform.SetParent(leftWing,false);
        Ellipsoid("Linen",sailcloth.transform,new(-.025f,-.05f,.20f),new(.11f,.025f,.15f),cream,6,3);
        if(pelican)
        {
            fishingPole=new GameObject("Fishing pole");fishingPole.transform.SetParent(leftWing,false);
            fishingPole.transform.localPosition=new Vector3(.08f,-.09f,.15f);
            Rod("Ash rod",fishingPole.transform,Vector3.zero,new Vector3(0,.60f,.85f),.012f,brown);
            Rod("Flexible rod tip",fishingPole.transform,new Vector3(0,.60f,.85f),new Vector3(0,.55f,1.1f),.006f,brown);
            Rod("Fishing line",fishingPole.transform,new Vector3(0,.55f,1.1f),new Vector3(0,-.65f,1.35f),.0025f,cream);
            Ellipsoid("Cork float",fishingPole.transform,new Vector3(0,-.63f,1.35f),new Vector3(.018f,.036f,.018f),orange,6,3);
        }
        SetTool(0);
        MergeStaticParts();
        }
        catch{Dispose();throw;}
    }
    private void MergeStaticParts()
    {
        var pivots=new HashSet<Transform>{Root.transform,body,head,leftWing,rightWing,hammer.transform,chisel.transform,needle.transform,sailcloth.transform};
        foreach(var eye in eyes)pivots.Add(eye);
        if(fishingPole)pivots.Add(fishingPole.transform);
        var groups=Root.GetComponentsInChildren<MeshRenderer>(true).GroupBy(r=>
        {
            var pivot=r.transform.parent;while(pivot&&!pivots.Contains(pivot))pivot=pivot.parent;
            return (Pivot:pivot,Material:r.sharedMaterial);
        }).ToArray();
        foreach(var group in groups)
        {
            var parts=group.ToArray();if(parts.Length<2||!group.Key.Pivot)continue;
            var parent=group.Key.Pivot;
            var merged=new Mesh{name="Bird combined surface"};
            merged.CombineMeshes(parts.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,
                transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);
            assets.Add(merged);
            var node=Node("Combined bird surface",parent,Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=merged;
            var target=node.gameObject.AddComponent<MeshRenderer>();target.sharedMaterial=group.Key.Material;
            target.receiveShadows=true;target.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            foreach(var renderer in parts)
            {
                var filter=renderer.GetComponent<MeshFilter>();var old=filter.sharedMesh;
                Object.DestroyImmediate(renderer);Object.DestroyImmediate(filter);assets.Remove(old);Object.DestroyImmediate(old);
            }
        }
    }
    private static Transform Node(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    private GameObject Tool(string name,Transform parent)
    {var t=Node(name,parent,new Vector3(0,.07f,.30f));return t.gameObject;}
    internal void Fishing(bool active){if(fishingPole)fishingPole.SetActive(active);}
    internal void SetTool(int tool)
    {hammer.SetActive(tool==1);chisel.SetActive(tool==2);needle.SetActive(tool==3);sailcloth.SetActive(tool==3);}
    internal void Pose(float pitch,float yaw,float roll,float bodyPitch,float crouch,float wing=0)
    {
        body.localPosition=new Vector3(0,bodyHeight-Mathf.Clamp(crouch,0,.13f),0);
        body.localRotation=Quaternion.Euler(bodyPitch,0,0);
        head.localPosition=headRest;head.localRotation=Quaternion.Euler(pitch,yaw,roll);
        leftWing.localRotation=Quaternion.Euler(-wing,0,wing*.4f);
        rightWing.localRotation=Quaternion.Euler(wing*.3f,0,-wing*.4f);
    }
    internal void CloseEyes(float amount)
    {foreach(var eye in eyes)eye.localScale=new Vector3(1,1-.97f*Mathf.Clamp(amount,0,1),1);}
    internal void Rest(bool sleeping,float time,float delta)
    {
        rest=Mathf.MoveTowards(rest,sleeping?1:0,delta*(sleeping?.7f:5));
        float blink=time%5.3f>5.12f?Mathf.Sin((time%5.3f-5.12f)/.18f*Mathf.PI):0;
        CloseEyes(Mathf.Max(rest,blink));
        if(rest<=0)return;
        head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(34,owl?85:115,-12),rest);
        body.localPosition+=Vector3.up*(-.055f+.004f*Mathf.Sin(time*1.5f))*rest;
    }
    internal void Probes(Transform anchor)
    {foreach(var renderer in Root.GetComponentsInChildren<Renderer>(true))renderer.probeAnchor=anchor;}
    private Texture2D Surface()
    {
        if(surface)return surface;
        using var resource=typeof(PerchedBird).Assembly.GetManifestResourceStream("VikingBirds.surface.png");
        if(resource==null)return Texture2D.whiteTexture;
        using var stream=new MemoryStream();resource.CopyTo(stream);
        var loaded=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!loaded.LoadImage(stream.ToArray())){Object.Destroy(loaded);return Texture2D.whiteTexture;}
        var pixels=loaded.GetPixels32();var small=new Color32[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)small[y*64+x]=pixels[y*loaded.height/64*loaded.width+x*loaded.width/64];
        surface=new Texture2D(64,64,TextureFormat.RGBA32,true){name="Bird coarse surface",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
        surface.SetPixels32(small);surface.Apply();Object.Destroy(loaded);assets.Add(surface);return surface;
    }
    private Material Paint(Color color)
    {
        string key=ColorUtility.ToHtmlStringRGB(color);
        if(palette.TryGetValue(key,out var found))return found;
        var m=new Material(source.shader){name="Bird matte "+key,color=color};
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",Surface());
        foreach(var p in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(m.HasProperty(p))m.SetColor(p,Color.black);
        foreach(var p in new[]{"_NoiseGlowEnabled","_Glossiness","_Metallic","_MetalGloss","_TriplanarMap","_ValueNoise","_ValueNoiseVertex","_AddRain","_AddSnow"})if(m.HasProperty(p))m.SetFloat(p,0);
        if(m.HasProperty("_MoveableObject"))m.SetFloat("_MoveableObject",1);
        m.DisableKeyword("_EMISSION");m.DisableKeyword("NOISEGLOW");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        assets.Add(m);palette[key]=m;return m;
    }
    private Transform MeshPart(string name,Transform parent,Vector3 position,List<Vector3> vertices,List<int> triangles,Color color)
    {
        // Duplicate triangle vertices for flat normals, making the silhouette visibly faceted.
        var flat=new Vector3[triangles.Count];var indices=new int[flat.Length];var uv=new Vector2[flat.Length];
        for(int i=0;i<flat.Length;i++){flat[i]=vertices[triangles[i]];indices[i]=i;uv[i]=new Vector2(flat[i].x*3+flat[i].z*1.7f+.5f,flat[i].y*3+flat[i].z*.3f+.5f);}
        // The parametric rings traverse clockwise viewed from inside. Reverse each
        // triangle so Unity lights and culls the exterior, not the inside of the bird.
        for(int i=0;i<indices.Length;i+=3){indices[i+1]=i+2;indices[i+2]=i+1;}
        var mesh=new Mesh{name=name,vertices=flat,triangles=indices,uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();assets.Add(mesh);
        var t=Node(name,parent,position);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=Paint(color);
        renderer.receiveShadows=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;return t;
    }
    private Transform Ellipsoid(string name,Transform parent,Vector3 position,Vector3 size,Color color,int sides,int rings)
    {
        var v=new List<Vector3>();var faces=new List<int>();
        for(int j=0;j<=rings;j++)for(int i=0;i<sides;i++)
        {float a=i*2*Mathf.PI/sides,b=j*Mathf.PI/rings;v.Add(Vector3.Scale(size,new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))));}
        for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
        {int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+sides,d=b+sides;faces.AddRange(new[]{a,c,b,b,c,d});}
        return MeshPart(name,parent,position,v,faces,color);
    }
    private void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Color color)
    {var t=Ellipsoid(name,parent,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),color,6,3);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    private void Wedge(string name,Transform parent,Vector3 position,Vector3 size,Color color)
    {
        var v=new List<Vector3>{new(-size.x*.5f,size.y*.5f,0),new(size.x*.5f,size.y*.5f,0),new(-size.x*.5f,-size.y*.5f,0),new(size.x*.5f,-size.y*.5f,0),new(0,-size.y*.3f,size.z)};
        MeshPart(name,parent,position,v,new List<int>{0,1,4,1,3,4,3,2,4,2,0,4,0,2,1,1,2,3},color);
    }
    private void Ring(string name,Transform parent,Vector3 center,float radius,float wire,Color color)
    {for(int i=0;i<10;i++){float a=i*2*Mathf.PI/10,b=(i+1)*2*Mathf.PI/10;Rod(name,parent,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,center+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius,wire,color);}}
    public void Dispose(){if(Root)Object.Destroy(Root);foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();palette.Clear();}
}
