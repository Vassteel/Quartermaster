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
    private Material worldMaterial;
    private Transform lightAnchor;
    private Vector3 perchLocal;
    private bool perchedOnOpenLid;
    private QuartermasterChestModel chestModel;
    private readonly ChestSortVisit visit=new ChestSortVisit();
    private VikingBirds.PerchedBird rig;
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
        return !gull || !gull.bird || !gull.visible || (gull.throws.Remaining==0 && !gull.visit.Busy);
    }
    internal static DepositGull Attach(Container chest, Bounds bounds)
    {
        var gull=chest.GetComponent<DepositGull>();
        if(!gull) gull=chest.gameObject.AddComponent<DepositGull>();
        gull.chest=chest; gull.bounds=bounds; gull.chestModel=chest.GetComponent<QuartermasterChestModel>();
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
            if(gull.chestModel)gull.visit.Begin(Time.time);
            gull.throws.Begin(Time.time+(gull.chestModel ? ChestSortVisit.ArrivalDelay : 0)); gull.started=Time.time;
        }
    }
    private void CreateBird()
    {
        // Read the actual chest renderer: its bundled shader may not be in Shader.Find.
        worldMaterial=null;lightAnchor=chest.transform;
        foreach(var renderer in chest.GetComponentsInChildren<MeshRenderer>(true))
            foreach(var material in renderer.sharedMaterials)
                if(material && material.shader && material.shader.name=="Custom/Piece")
                { worldMaterial=material;lightAnchor=renderer.probeAnchor ? renderer.probeAnchor : renderer.transform;break; }
        if(!worldMaterial)throw new InvalidOperationException("Chest's world-lit material is not available yet.");
        bird=new GameObject("Quartermaster deposit owl perch");
        // A separate root prevents chest glow effects from touching the owl's materials.
        perchLocal=ChestPerch.OnVisibleLid(chest,bounds);
        perchedOnOpenLid=chest.m_open && chest.m_open.activeInHierarchy;
        FollowChest();
        rig=new VikingBirds.PerchedBird(bird.transform,worldMaterial,true);
        rig.Probes(lightAnchor);
        Plugin.Log.LogInfo("Deposit burrowing owl uses matte world lighting and a separate chest perch.");
        started=Time.time; nextLook=Time.time+UnityEngine.Random.Range(2f,5f);
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
                    rig?.Dispose();rig=null;
                    if(bird)Destroy(bird);bird=null;
                    if(!creationWarning){creationWarning=true;Plugin.Log.LogWarning("Deposit owl creation will retry: "+error.Message);}
                }
            }
        }
        if(!visible) { Hide(); return; }
        if(!bird) return;
        visit.Tick(Time.time,throws.Remaining);
        if(chestModel)chestModel.SortingOpen=visit.Busy;
        FollowChest();
        bird.SetActive(true);
        var next=state.Get(Time.time,hasItems,canWork);
        if((canWork || (chestModel && visit.Busy)) && throws.Remaining>0) next=DepositGullMood.Sorting;
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
            throwNow=(!chestModel || visit.CanThrow) && throws.Take(Time.time);
        }
        else if(mood==DepositGullMood.NeedsAttention)
        {
            // Two pointed pecks, then a sideways glare, with feet kept on the lid.
            float p=t%3.3f;
            float peck=Pulse(p,.25f,.45f)+Pulse(p,.85f,.4f);
            pose.HeadPitch=80*peck; pose.BodyPitch=32*peck; pose.Crouch=.065*peck;
            pose.HeadYaw=p>1.6f ? 35*Mathf.Sin((p-1.6f)*2) : 0;
            pose.HeadRoll=p>1.6f ? -42 : 0; pose.TailYaw=12*Mathf.Sin(t*20)*peck;
        }
        else
        {
            // Burrowing owls punctuate still stares with exaggerated bobs and tilts.
            float idle=t%6;
            pose.HeadRoll=idle>4.8f ? 46*Mathf.Sin((idle-4.8f)/1.2f*Mathf.PI) : 0;
            pose.HeadPitch+=22*Pulse(idle,.3f,.3f)+18*Pulse(idle,.9f,.3f);
            pose.Crouch=.04*(Pulse(idle,.3f,.3f)+Pulse(idle,.9f,.3f));
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
        bool hopping=chestModel && (visit.Phase==ChestVisitPhase.Entering || visit.Phase==ChestVisitPhase.Returning);
        rig?.Pose((float)blended.HeadPitch,(float)blended.HeadYaw,(float)blended.HeadRoll,
            (float)blended.BodyPitch,(float)blended.Crouch,hopping ? 45*Mathf.Sin(visit.Progress(Time.time)*Mathf.PI) : mood==DepositGullMood.Sorting ? 18*Mathf.Sin(t*9) : 0);
        bool nearby=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-bird.transform.position).sqrMagnitude<9;
        rig?.Rest(!hasItems&&mood!=DepositGullMood.Sorting&&EnvMan.instance&&!EnvMan.IsDaylight()&&!nearby,Time.time,Time.deltaTime);
        if(throwNow) GullToss.Spawn(bird.transform,sample,rig!=null ? rig.BeakWorld : bird.transform.TransformPoint(new Vector3(0,.72f,.32f)));
        if(rig==null) bird.transform.rotation=transform.rotation*Quaternion.Euler((float)blended.BodyPitch,0,(float)blended.BodyRoll);
    }
    private static float Pulse(float t,float start,float length)
    { if(t<start || t>start+length) return 0; float s=Mathf.Sin((t-start)/length*Mathf.PI); return s*s; }
    private void FollowChest()
    {
        if(chestModel)
        {
            Vector3 local=chestModel.Perch;
            if(visit.Phase==ChestVisitPhase.Throwing)local=chestModel.Inside;
            else if(visit.Phase==ChestVisitPhase.Entering || visit.Phase==ChestVisitPhase.Returning)
            {
                float p=visit.Progress(Time.time);bool inward=visit.Phase==ChestVisitPhase.Entering;
                local=Vector3.Lerp(inward?chestModel.Perch:chestModel.Inside,inward?chestModel.Inside:chestModel.Perch,p)
                    +Vector3.up*(Mathf.Sin(p*Mathf.PI)*.85f);
            }
            bird.transform.position=transform.TransformPoint(local);
            bird.transform.rotation=transform.rotation*Quaternion.Euler(0,180,0);
            bird.transform.localScale=Vector3.Scale(transform.lossyScale,Vector3.one*.65f);
            return;
        }
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
        throws.Clear(); state.Reset(); visit.Reset();
        if(chestModel)chestModel.SortingOpen=false;
        GullToss.ClearFor(bird ? bird.transform : null);
    }
    private void OnDestroy()
    {
        GullToss.ClearFor(bird ? bird.transform : null);
        rig?.Dispose(); if(bird) Destroy(bird);
    }
}
