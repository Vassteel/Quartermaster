using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster.Cosmetics;

// Local visual rig only. Shares immutable meshes/materials across all deposit owls;
// each instance owns just its articulated transforms and renderers.
internal sealed class QuartermasterOwl : IDisposable
{
    internal readonly GameObject Root;
    private readonly Transform[] pivots=new Transform[8];
    private OwlAssets assets;
    private bool disposed;
    private float rest;
    private OwlPose legPose;
    private Vector3 sleepOffset;
    internal Vector3 BeakWorld => pivots[2].TransformPoint(new Vector3(0,.095f,.247f));
    internal QuartermasterOwl(Transform parent,Material worldMaterial)
    {
        Root=new GameObject("Quartermaster detailed burrowing owl");Root.transform.SetParent(parent,false);
        try
        {
            assets=OwlAssets.Acquire(worldMaterial.shader);
            Vector3[] positions={new Vector3(-.099f,.34f,-.015f),new Vector3(0,.29f,0),new Vector3(0,.47f,.035f),
                new Vector3(-.22f,.34f,-.02f),new Vector3(.22f,.34f,-.02f),
                new Vector3(-.112f,.15f,.204f),new Vector3(.112f,.15f,.204f),new Vector3(.099f,.34f,-.015f)};
            string[] names={"Left foot","Body","Head","Left wing","Right wing","Left eye","Right eye","Right foot"};
            for(int i=0;i<pivots.Length;i++)
            {
                var go=new GameObject(names[i]);pivots[i]=go.transform;
                pivots[i].SetParent(i<2||i==7?Root.transform:pivots[i<5?1:2],false);
                pivots[i].localPosition=positions[i];
            }
            var lods=new LOD[2];
            for(int level=0;level<2;level++)
            {
                var parts=assets.Data.Levels[level];var renderers=new Renderer[parts.Length];
                for(int i=0;i<parts.Length;i++)
                {
                    var part=parts[i];var go=new GameObject("Owl LOD "+level+" surface "+i);
                    go.layer=parent.gameObject.layer;go.transform.SetParent(pivots[part.Pivot],false);
                    go.AddComponent<MeshFilter>().sharedMesh=assets.Meshes[level][i];
                    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=assets.Materials[part.Material];
                    renderer.receiveShadows=true;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                    renderers[i]=renderer;
                }
                lods[level]=new LOD(level==0?.065f:.009f,renderers);
            }
            var group=Root.AddComponent<LODGroup>();group.SetLODs(lods);
            // Fixed animated bounds include the forward peck and opened wings.
            group.localReferencePoint=new Vector3(0,.56f,0);group.size=1.8f;
        }
        catch {Dispose();throw;}
    }
    internal void Pose(float pitch,float yaw,float roll,float bodyPitch,float crouch,float wing=0)
    {
        sleepOffset=Vector3.zero;legPose=new OwlPose();
        pivots[1].localPosition=new Vector3(0,.29f-Mathf.Clamp(crouch,0,.13f),0);
        pivots[1].localRotation=Quaternion.Euler(bodyPitch,0,0);
        pivots[2].localRotation=Quaternion.Euler(pitch,yaw,roll);
        pivots[3].localRotation=Quaternion.Euler(-wing,0,wing*.4f);
        pivots[4].localRotation=Quaternion.Euler(wing*.3f,0,-wing*.4f);
        pivots[0].localPosition=new Vector3(-.099f,.34f,-.015f);
        pivots[7].localPosition=new Vector3(.099f,.34f,-.015f);
        pivots[2].localPosition=new Vector3(0,.47f,.035f);
        pivots[0].localRotation=pivots[7].localRotation=Quaternion.identity;
    }
    internal void Perform(OwlPose pose,float delta)
    {
        pivots[1].localPosition-=sleepOffset;sleepOffset=Vector3.zero;legPose=pose;
        float blend=1-Mathf.Exp(-Mathf.Max(0,delta)*18);
        pivots[1].localPosition=Vector3.Lerp(pivots[1].localPosition,new Vector3(pose.BodySway,.29f-pose.Crouch+pose.BodyLift,0),blend);
        Rotate(1,Quaternion.Euler(pose.BodyPitch,0,pose.BodyRoll),blend);
        Rotate(2,Quaternion.Euler(pose.HeadPitch,pose.HeadYaw,pose.HeadRoll),blend);
        Rotate(3,Quaternion.Euler(-pose.LeftWing*.12f,0,-pose.LeftWing),blend);
        Rotate(4,Quaternion.Euler(-pose.RightWing*.12f,0,pose.RightWing),blend);
        pivots[2].localPosition=Vector3.Lerp(pivots[2].localPosition,new Vector3(0,.47f,.035f+pose.HeadForward),blend);
        float feetBlend=Mathf.Max(blend,pose.FootPlant);
        pivots[0].localPosition=Vector3.Lerp(pivots[0].localPosition,new Vector3(-.099f,.34f+pose.LeftFootLift,-.015f+pose.LeftFootForward),feetBlend);
        pivots[7].localPosition=Vector3.Lerp(pivots[7].localPosition,new Vector3(.099f,.34f+pose.RightFootLift,-.015f+pose.RightFootForward),feetBlend);
        float shuffle=pose.FootPlant>0?0:pose.Step*25;
        Rotate(0,Quaternion.Euler(65*pose.FeetTuck+shuffle+pose.LeftFootPitch,0,0),feetBlend);
        Rotate(7,Quaternion.Euler(65*pose.FeetTuck-shuffle+pose.RightFootPitch,0,0),feetBlend);
    }
    private void Rotate(int pivot,Quaternion rotation,float blend)
    {pivots[pivot].localRotation=Quaternion.Slerp(pivots[pivot].localRotation,rotation,blend);}
    internal void Rest(bool sleeping,float time,float delta)
    {
        rest=Mathf.MoveTowards(rest,sleeping?1:0,delta*(sleeping?.7f:5));
        float phase=time%5.3f,blink=phase>5.12f?Mathf.Sin((phase-5.12f)/.18f*Mathf.PI):0;
        float close=Mathf.Clamp01(Mathf.Max(rest,blink));
        for(int i=5;i<7;i++)pivots[i].localScale=new Vector3(1,1-.97f*close,1);
        if(rest<=0){AttachLegs();return;}
        pivots[2].localRotation=Quaternion.Slerp(pivots[2].localRotation,Quaternion.Euler(34,85,-12),rest);
        sleepOffset=Vector3.up*(-.055f+.004f*Mathf.Sin(time*1.5f))*rest;
        pivots[1].localPosition+=sleepOffset;
        AttachLegs();
    }
    // Keep each upper leg attached to its moving hip. Moving a whole rigid leg
    // forward independently of the torso previously pulled it out of the feathers.
    private void AttachLegs()
    {
        Leg(0,-1,legPose.LeftFootForward,legPose.LeftFootLift);
        Leg(7,1,legPose.RightFootForward,legPose.RightFootLift);
    }
    private void Leg(int index,float side,float forward,float lift)
    {
        var hip=Root.transform.InverseTransformPoint(pivots[1].TransformPoint(new Vector3(side*.099f,.05f,-.015f)));
        pivots[index].localPosition=hip;
        if(legPose.FeetTuck>.1f)
        {
            pivots[index].localRotation=pivots[1].localRotation*Quaternion.Euler(65*legPose.FeetTuck,0,0);
            pivots[index].localScale=Vector3.one;return;
        }
        var target=new Vector3(side*.099f,.018f+lift,.012f+forward);
        var direction=target-hip;
        float scale=Mathf.Clamp(direction.magnitude/.32313f,.5f,1.6f);
        var restDirection=new Vector3(0,-.322f*scale,.027f);
        pivots[index].localRotation=Quaternion.FromToRotation(restDirection,direction);
        pivots[index].localScale=new Vector3(1,scale,1);
    }
    internal void Probes(Transform anchor)
    {foreach(var renderer in Root.GetComponentsInChildren<Renderer>(true))renderer.probeAnchor=anchor;}
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        if(Root){Root.SetActive(false);Object.Destroy(Root);}
        assets?.Release();assets=null;
    }

    private sealed class OwlAssets
    {
        private static readonly Dictionary<Shader,OwlAssets> Cache=new Dictionary<Shader,OwlAssets>();
        private readonly List<Object> owned=new List<Object>();
        private readonly Shader shader;
        private int users;
        internal OwlModelData Data;
        internal Mesh[][] Meshes;
        internal Material[] Materials;
        private OwlAssets(Shader shader){this.shader=shader;}
        internal static OwlAssets Acquire(Shader shader)
        {
            if(!shader)throw new InvalidOperationException("Owl needs a world lighting shader.");
            if(!Cache.TryGetValue(shader,out var value))
            {
                value=new OwlAssets(shader);
                try {value.Load();Cache.Add(shader,value);}
                catch {value.Destroy();throw;}
            }
            value.users++;return value;
        }
        private void Load()
        {
            var assembly=typeof(QuartermasterOwl).Assembly;
            using(var stream=assembly.GetManifestResourceStream("Quartermaster.Owl.model")??throw new InvalidDataException("Missing owl mesh."))
                Data=OwlModelData.Read(stream);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,true){name="Quartermaster owl material atlas",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=2};
            owned.Add(texture);
            using(var stream=assembly.GetManifestResourceStream("Quartermaster.Owl.albedo")??throw new InvalidDataException("Missing owl texture."))
            using(var bytes=new MemoryStream())
            {stream.CopyTo(bytes);if(!texture.LoadImage(bytes.ToArray(),true))throw new InvalidDataException("Invalid owl texture.");}
            var colors=new[]{Color.white,new Color(.018f,.014f,.012f),new Color(.33f,.25f,.125f),new Color(.66f,.36f,.04f)};
            Materials=new Material[4];
            for(int i=0;i<4;i++)
            {
                var material=new Material(shader){name="Quartermaster owl material "+i,color=colors[i]};owned.Add(material);Materials[i]=material;
                if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",i==0?texture:Texture2D.whiteTexture);
                foreach(var key in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(material.HasProperty(key))material.SetColor(key,Color.black);
                foreach(var key in new[]{"_NoiseGlowEnabled","_TriplanarMap","_ValueNoise","_ValueNoiseVertex","_AddRain","_AddSnow"})if(material.HasProperty(key))material.SetFloat(key,0);
                if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",i==0?.12f:i==2?.4f:.55f);
                if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",i==2?.65f:0);
                if(material.HasProperty("_MetalGloss"))material.SetFloat("_MetalGloss",i==2?.4f:0);
                if(material.HasProperty("_MoveableObject"))material.SetFloat("_MoveableObject",1);
                material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");
                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            Meshes=new Mesh[2][];
            for(int level=0;level<2;level++)
            {
                var parts=Data.Levels[level];Meshes[level]=new Mesh[parts.Length];
                for(int p=0;p<parts.Length;p++)
                {
                    var part=parts[p];int n=part.Vertices.Length/8;
                    var vertices=new Vector3[n];var normals=new Vector3[n];var uv=new Vector2[n];
                    for(int i=0;i<n;i++)
                    {
                        int j=i*8;var v=part.Vertices;
                        vertices[i]=new Vector3(v[j],v[j+1],v[j+2]);normals[i]=new Vector3(v[j+3],v[j+4],v[j+5]);uv[i]=new Vector2(v[j+6],v[j+7]);
                    }
                    var mesh=new Mesh{name="Quartermaster owl LOD "+level+" part "+p};owned.Add(mesh);Meshes[level][p]=mesh;
                    mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=part.Triangles;mesh.RecalculateBounds();mesh.UploadMeshData(true);
                }
            }
        }
        internal void Release()
        {if(--users==0){Cache.Remove(shader);Destroy();}}
        private void Destroy(){foreach(var asset in owned)if(asset)Object.Destroy(asset);owned.Clear();}
    }
}
