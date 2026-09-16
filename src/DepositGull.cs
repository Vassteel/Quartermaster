using System;
using System.Collections.Generic;
using Quartermaster.Cosmetics;
using UnityEngine;

namespace Quartermaster;

// Local decoration only: no ZNetView, inventory, drops, colliders or interaction interception.
public sealed class DepositGull : MonoBehaviour
{
    private readonly GullNoticeGate notices=new GullNoticeGate();
    private string pendingNotice="";
    private static float nextAnnouncement;
    internal static void Notice(Container chest,string message,bool empty)
    {
        var gull=chest ? chest.GetComponent<DepositGull>() : null;
        if(!gull)return;
        gull.pendingNotice=message;
        if(empty)gull.notices.Resolved();
    }
    private Container chest;
    private GameObject bird;
    private Vector3 perchLocal;
    private bool perchedOnOpenLid;
    private readonly List<Material> birdMaterials=new List<Material>();
    private GullMeshRig rig;
    private readonly DepositGullState state = new DepositGullState();
    private GullPose blended;
    private DepositGullMood mood;
    private float started, nextSense, nextLook, lookUntil, nextCreateAttempt;
    private bool creationWarning;
    private bool canWork, hasItems, visible;
    private Bounds bounds;
    private ItemDrop.ItemData sample;
    private readonly SlotThrows throws=new SlotThrows();
    internal static bool ReadyForNextSlot(Container chest)
    {
        var gull=chest ? chest.GetComponent<DepositGull>() : null;
        // Visible work gets its full three throws. Unloaded/missing/distant cosmetics
        // cannot stop the storage network from processing its next slot.
        return !gull || !gull.bird || !gull.visible || gull.throws.Remaining==0;
    }
    internal static DepositGull Attach(Container chest, Bounds bounds)
    {
        var gull=chest.GetComponent<DepositGull>();
        if(!gull) gull=chest.gameObject.AddComponent<DepositGull>();
        gull.chest=chest; gull.bounds=bounds;
        return gull;
    }
    internal static void Report(Container chest, int moved, bool blocked, ItemDrop.ItemData item)
    {
        var gull=chest ? chest.GetComponent<DepositGull>() : null;
        if(!gull) return;
        gull.state.Report(Time.time,moved,blocked);
        if(moved>0)
        {
            // Three throws for the processed slot, independent of its stack quantity.
            gull.sample=item?.Clone();
            gull.throws.Begin(Time.time); gull.started=Time.time;
        }
    }
    private void CreateBird()
    {
        var prefab=ZNetScene.instance ? ZNetScene.instance.GetPrefab("Seagal") : null;
        var source=prefab ? prefab.GetComponent<RandomFlyingBird>() : null;
        if(!source || !source.m_landedModel) return;
        bird=new GameObject("Quartermaster deposit gull");
        // Keep this local actor outside the chest hierarchy: chest-targeted glow mods enumerate
        // every child renderer, including accessories, and otherwise make the bird emissive.
        perchLocal=ChestPerch.OnVisibleLid(chest,bounds);
        perchedOnOpenLid=chest.m_open && chest.m_open.activeInHierarchy;
        FollowChest();
        // A slightly smaller gull fits the lid on all vanilla chest tiers.
        var model=CopyStaticVisual(source.m_landedModel.transform,bird.transform);
        if(!model.GetComponentInChildren<MeshRenderer>()) { Destroy(bird); bird=null; return; }
        ChestPerch.SetFeetOnPerch(model);
        rig=GullMeshRig.Create(model);
        started=Time.time; nextLook=Time.time+UnityEngine.Random.Range(2f,5f);
    }
    private GameObject CopyStaticVisual(Transform source, Transform parent)
    {
        // Construct renderer nodes from shared assets; none of the prefab's scripts can awaken.
        var node=new GameObject(source.name); node.transform.SetParent(parent,false);
        node.transform.localPosition=source.localPosition; node.transform.localRotation=source.localRotation;
        node.transform.localScale=source.localScale;
        var mesh=source.GetComponent<MeshFilter>(); var renderer=source.GetComponent<MeshRenderer>();
        if(mesh && mesh.sharedMesh && renderer && renderer.enabled)
        {
            node.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
            var originals=renderer.sharedMaterials; var materials=new Material[originals.Length];
            for(int i=0;i<originals.Length;i++)
            {
                if(!originals[i]) continue;
                var material=GullMaterials.Feathers(originals[i]);
                materials[i]=material; birdMaterials.Add(material);
            }
            var copy=node.AddComponent<MeshRenderer>(); copy.sharedMaterials=materials;
            copy.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; copy.receiveShadows=true;
        }
        foreach(Transform child in source) if(child.gameObject.activeSelf) CopyStaticVisual(child,node.transform);
        return node;
    }
    private void LateUpdate()
    {
        if(!chest || !Plugin.Instance || !Plugin.Enabled.Value)
        { Hide(); return; }
        if(Time.time>=nextSense)
        {
            nextSense=Time.time+.5f;
            visible=ContainerRegistry.GetSettings(chest).Deposit && Player.m_localPlayer &&
                (Player.m_localPlayer.transform.position-transform.position).sqrMagnitude<900;
            hasItems=chest.GetInventory()!=null && chest.GetInventory().NrOfItems()>0;
            canWork=visible && ContainerRegistry.IsUsable(chest,false);
            if(visible && !bird && Time.time>=nextCreateAttempt)
            {
                nextCreateAttempt=Time.time+3;
                try { CreateBird(); }
                catch(Exception error)
                {
                    rig?.Destroy();rig=null;
                    if(bird)Destroy(bird);bird=null;
                    foreach(var material in birdMaterials)if(material)Destroy(material);
                    birdMaterials.Clear();
                    if(!creationWarning){creationWarning=true;Plugin.Log.LogWarning("Deposit gull creation will retry: "+error.Message);}
                }
            }
        }
        if(!visible) { Hide(); return; }
        if(!bird) return;
        FollowChest();
        bird.SetActive(true);
        var next=state.Get(Time.time,hasItems,canWork);
        if(canWork && throws.Remaining>0) next=DepositGullMood.Sorting;
        if(next!=mood) { mood=next; started=Time.time; }
        if(mood==DepositGullMood.NeedsAttention && Time.time>=nextAnnouncement &&
            Player.m_localPlayer && (Player.m_localPlayer.transform.position-bird.transform.position).sqrMagnitude<144 &&
            notices.Take(Time.time,pendingNotice))
        { GullSpeech.Say(bird.transform,pendingNotice);nextAnnouncement=Time.time+8; }
        float t=Time.time-started;
        bool throwNow=false;
        var pose=GullPerformance.Sample(GullMood.Calm,t);
        if(mood==DepositGullMood.Sorting)
        {
            float cycle=t% .65f;
            float scoop=Mathf.Sin(Mathf.Clamp01(cycle/.34f)*Mathf.PI);
            pose.HeadPitch=60*scoop-22*Mathf.Sin(Mathf.Clamp01((cycle-.34f)/.31f)*Mathf.PI);
            pose.BodyPitch=25*scoop; pose.Crouch=.07*scoop;
            pose.HeadYaw=22*Mathf.Sin(t*5); pose.TailYaw=15*Mathf.Sin(t*12);
            throwNow=throws.Take(Time.time);
        }
        else if(mood==DepositGullMood.NeedsAttention)
        {
            // Two pointed pecks, then a sideways glare, with feet kept on the lid.
            float p=t%3.3f;
            float peck=Pulse(p,.25f,.45f)+Pulse(p,.85f,.4f);
            pose.HeadPitch=80*peck; pose.BodyPitch=32*peck; pose.Crouch=.065*peck;
            pose.HeadYaw=p>1.6f ? 35*Mathf.Sin((p-1.6f)*2) : 0;
            pose.HeadRoll=p>1.6f ? -14 : 0; pose.TailYaw=12*Mathf.Sin(t*20)*peck;
        }
        else
        {
            if(Time.time>=nextLook)
            { lookUntil=Time.time+UnityEngine.Random.Range(1.2f,2.2f); nextLook=lookUntil+UnityEngine.Random.Range(5f,10f); }
            var player=Player.m_localPlayer;
            if(Time.time<lookUntil && player && (player.transform.position-bird.transform.position).sqrMagnitude<64)
            {
                var delta=bird.transform.InverseTransformPoint(player.transform.position+Vector3.up*1.5f)-Vector3.up*1.15f;
                float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
                if(Mathf.Abs(yaw)<85)
                {
                    pose.HeadYaw=Mathf.Clamp(yaw,-65,65);
                    pose.HeadPitch=-Mathf.Clamp(Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg,-20,25);
                }
            }
        }
        blended=GullPerformance.Blend(blended,pose,1-Mathf.Exp(-Time.deltaTime*16));
        rig?.Apply(blended);
        if(throwNow) GullToss.Spawn(bird.transform,sample,rig!=null ? rig.BeakWorld : bird.transform.TransformPoint(new Vector3(0,.72f,.32f)));
        if(rig==null) bird.transform.rotation=transform.rotation*Quaternion.Euler((float)blended.BodyPitch,0,(float)blended.BodyRoll);
    }
    private static float Pulse(float t,float start,float length)
    { if(t<start || t>start+length) return 0; float s=Mathf.Sin((t-start)/length*Mathf.PI); return s*s; }
    private void FollowChest()
    {
        bool open=chest.m_open && chest.m_open.activeInHierarchy;
        if(open!=perchedOnOpenLid)
        { perchLocal=ChestPerch.OnVisibleLid(chest,bounds); perchedOnOpenLid=open; }
        bird.transform.position=transform.TransformPoint(perchLocal);
        bird.transform.rotation=transform.rotation;
        bird.transform.localScale=Vector3.Scale(transform.lossyScale,Vector3.one*.65f);
    }
    private void OnDisable() => Hide();
    private void Hide()
    {
        if(bird) bird.SetActive(false);
        throws.Clear(); state.Reset();
        GullToss.ClearFor(bird ? bird.transform : null);
    }
    private void OnDestroy()
    {
        GullToss.ClearFor(bird ? bird.transform : null);
        rig?.Destroy(); if(bird) Destroy(bird);
        foreach(var material in birdMaterials) if(material) Destroy(material);
    }
}
