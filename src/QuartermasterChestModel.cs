using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

// Native black-metal storage, ZDO, access, damage and lid-state objects remain intact.
// Only their renderers are replaced. The owl never marks the inventory as in use.
public sealed class QuartermasterChestModel : MonoBehaviour
{
    public Transform Lid;
    public Vector3 Perch=new Vector3(1.24f,1.531f,0);
    public Vector3 Inside=new Vector3(0,.43f,-.10f);
    internal bool SortingOpen;
    private float openness;
    private Container chest;
    private static readonly List<Object> assets=new List<Object>();
    private void Awake() { chest=GetComponent<Container>(); }
    private void LateUpdate()
    {
        if(!Lid || !chest)return;
        bool playerOpen=chest.m_open && chest.m_open.activeSelf;
        openness=Mathf.MoveTowards(openness,playerOpen||SortingOpen?1:0,Time.deltaTime*5);
        Lid.localRotation=Quaternion.Euler(105*openness,0,0);
    }
    internal static void Build(GameObject prefab)
    {
        var source=prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)
            .FirstOrDefault(m=>m && m.shader && m.shader.name=="Custom/Piece");
        if(!source)throw new InvalidOperationException("Black metal chest has no native building material.");
        var colors=new[]{new Color(.29f,.20f,.12f),new Color(.085f,.11f,.105f),new Color(.52f,.39f,.19f),new Color(.16f,.27f,.25f),new Color(.12f,.075f,.04f)};
        var palette=colors.Select((color,i)=> {
            var m=new Material(source.shader){name="Quartermaster coffer "+i,color=color};
            if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",Texture2D.whiteTexture);
            foreach(var key in new[]{"_Glossiness","_Metallic","_MetalGloss","_NoiseGlowEnabled"})if(m.HasProperty(key))m.SetFloat(key,0);
            foreach(var key in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(m.HasProperty(key))m.SetColor(key,Color.black);
            m.DisableKeyword("_EMISSION");m.DisableKeyword("NOISEGLOW");assets.Add(m);return m;
        }).ToArray();
        var model=prefab.AddComponent<QuartermasterChestModel>();
        using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Quartermaster.DepositChest.model") ?? throw new InvalidOperationException("Missing coffer model."))
        using(var reader=new BinaryReader(stream))
        {
            if(new string(reader.ReadChars(4))!="QMC1" || reader.ReadInt32()!=2)throw new InvalidDataException("Invalid coffer model.");
            for(int part=0;part<2;part++)
            {
                int count=reader.ReadInt32();if(count<1||count>100000)throw new InvalidDataException("Invalid vertex count.");
                var vertices=new Vector3[count];for(int i=0;i<count;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                int groups=reader.ReadInt32();if(groups!=palette.Length)throw new InvalidDataException("Invalid material count.");
                var mesh=new Mesh{name=part==0?"Quartermaster hollow coffer and perch":"Quartermaster hinged lid"};assets.Add(mesh);
                mesh.vertices=vertices;mesh.subMeshCount=groups;
                for(int i=0;i<groups;i++)
                {
                    int n=reader.ReadInt32();if(n<0||n>300000||n%3!=0)throw new InvalidDataException("Invalid triangle count.");
                    var indices=new int[n];for(int j=0;j<n;j++){indices[j]=reader.ReadInt32();if(indices[j]<0||indices[j]>=count)throw new InvalidDataException("Invalid vertex index.");}
                    mesh.SetTriangles(indices,i);
                }
                mesh.uv=vertices.Select(v=>new Vector2(v.x+v.z,v.y)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(part==0?"Quartermaster coffer":"Quartermaster hinged lid");go.layer=prefab.layer;go.transform.SetParent(prefab.transform,false);
                if(part==1){go.transform.localPosition=new Vector3(0,.85f,.64f);model.Lid=go.transform;}
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=palette;
            }
            if(reader.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing coffer model data.");
        }
        foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            if(!renderer.gameObject.name.StartsWith("Quartermaster",StringComparison.Ordinal))
            {renderer.enabled=false;renderer.forceRenderingOff=true;}
        // Keep native functional colliders; give the added perch matching geometry.
        var perch=new GameObject("Quartermaster perch solid");perch.layer=prefab.layer;perch.transform.SetParent(prefab.transform,false);
        var rail=perch.AddComponent<BoxCollider>();rail.center=new Vector3(1.24f,1.46f,0);rail.size=new Vector3(.14f,.14f,.72f);
        // The placement-only box covers the custom silhouette without filling the cavity.
        prefab.AddComponent<DepositChestPlacement>();
    }
    internal static void Release(){foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();}
}

public sealed class DepositChestPlacement:MonoBehaviour {}

[HarmonyLib.HarmonyPatch(typeof(Player),"SetupPlacementGhost")]
internal static class DepositChestPreview
{
    private static void Postfix(GameObject ___m_placementGhost)
    {
        var ghost=___m_placementGhost;
        if(!ghost || !ghost.GetComponent<DepositChestPlacement>())return;
        var go=new GameObject("Quartermaster preview bounds");go.layer=ghost.layer;go.transform.SetParent(ghost.transform,false);
        var box=go.AddComponent<BoxCollider>();box.center=new Vector3(.1f,.765f,0);box.size=new Vector3(2.42f,1.53f,1.63f);
    }
}
