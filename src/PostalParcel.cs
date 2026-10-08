using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Quartermaster;

// An invisible native container travels to the destination's sector. Its inventory
// and route are world data, so neither a sender nor the origin zone must stay loaded.
[HarmonyPatch]
internal sealed class PostalParcel:MonoBehaviour
{
    internal const string Prefab="Quartermaster_mail_parcel_chest",Link="Quartermaster.outgoingMail",PhaseKey="QM.mail.phase",SourceKey="QM.mail.source",HomeKey="QM.mail.destination",ReceiverKey="QM.mail.receiver",PlayerKey="QM.mail.sender",OriginKey="QM.mail.origin",TargetKey="QM.mail.target",CountKey="QM.mail.count",StatusKey="QM.mail.status";
    internal static bool IsParcel(Container c)=>c&&ContainerRegistry.PrefabName(c)==Prefab;
    internal static MailPhase Phase(ZDO z)=>(MailPhase)z.GetInt(PhaseKey,0);
    private Container chest;
    private ZNetView view;
    private bool busy;
    private float next,created;
    private static readonly MethodInfo DropItems=AccessTools.Method(typeof(Container),"DropAllItems",Type.EmptyTypes);
    internal static void Register()
    {
        var p=PrefabManager.Instance.CreateClonedPrefab(Prefab,"piece_chest_wood");
        foreach(var r in p.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
        foreach(var c in p.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        Object.DestroyImmediate(p.GetComponent<WearNTear>());
        var piece=p.GetComponent<Piece>();piece.m_canBeRemoved=false;piece.m_resources=Array.Empty<Piece.Requirement>();
        var box=p.GetComponent<Container>();box.m_width=4;box.m_height=2;box.m_name="Mail in transit";box.m_defaultItems=new DropTable();box.m_open=null;box.m_closed=null;box.m_checkGuardStone=false;
        p.AddComponent<PostalParcel>();PrefabManager.Instance.AddPrefab(new CustomPrefab(p,false));
    }
    internal static PostalParcel Create(PostalMailbox source,MailAddress address)
    {
        var go=Object.Instantiate(PrefabManager.Instance.GetPrefab(Prefab),source.transform.position,Quaternion.identity);
        var parcel=go.GetComponent<PostalParcel>();var z=go.GetComponent<ZNetView>().GetZDO();
        z.Set(SourceKey,ContainerRegistry.GetView(source.Chest).GetZDO().m_uid);z.Set(HomeKey,address.Id);z.Set(ReceiverKey,address.Receiver);
        z.Set(OriginKey,source.transform.position);z.Set(TargetKey,address.Position);z.Set(PlayerKey,Player.m_localPlayer.GetPlayerID());
        z.Set("QM.mail.originName",source.BaseName);z.Set("QM.mail.playerName",Player.m_localPlayer.GetPlayerName());
        z.Set(StatusKey,"Reserving mail");z.Set(PhaseKey,(int)MailPhase.Preparing);
        ContainerRegistry.GetView(source.Chest).GetZDO().Set(Link,z.m_uid);
        return parcel;
    }
    internal Container Chest=>chest;
    internal ZNetView View=>view;
    private void Awake(){chest=GetComponent<Container>();view=GetComponent<ZNetView>();created=Time.time;}
    internal void Activate()
    {
        var z=view.GetZDO();int count=chest.GetInventory().GetAllItems().Sum(i=>i.m_stack);z.Set(CountKey,count);
        z.Set(PhaseKey,(int)(count>0?MailPhase.Waiting:MailPhase.Cancelled));
        z.Set(StatusKey,count>0?"Waiting for destination to load":"No items sent");
        if(count>0){var pos=z.GetVec3(TargetKey,transform.position);z.SetPosition(pos);transform.position=pos;}
    }
    internal static void Return(ZDO z)
    {
        var phase=MailRoute.Cancel(Phase(z));if(MailRoute.Terminal(phase))return;
        z.Set(PhaseKey,(int)phase);z.Set(ReceiverKey,z.GetZDOID(SourceKey));
        z.SetPosition(z.GetVec3(OriginKey,z.GetPosition()));z.Set(StatusKey,"Returning remaining mail");
        ZDOMan.instance.ForceSendZDO(z.m_uid);
    }
    private void Update()
    {
        if(!view||!view.IsValid())return;
        var z=view.GetZDO();transform.position=z.GetPosition();
        if(Time.time<next||busy||!view.IsOwner()||!Plugin.Enabled.Value)return;next=Time.time+3;
        var phase=Phase(z);if(MailRoute.Terminal(phase))return;
        if(phase==MailPhase.Preparing)
        {
            // Recover a disconnect after the native transfer but before activation.
            if(Time.time-created>40)Activate();return;
        }
        if(z.GetBool("QM.mail.dropReturn",false)&&phase==MailPhase.Returning)
        {DropItems.Invoke(chest,null);z.Set(CountKey,0);z.Set(PhaseKey,(int)MailPhase.Returned);z.Set(StatusKey,"Origin removed; mail dropped at its former location");return;}
        if(!Player.m_localPlayer||Player.m_localPlayer.IsDead())return;
        // Incoming cargo enters the deposit queue directly, even when addressed to a mailbox.
        // Returns retain their original mailbox destination.
        var go=ZNetScene.instance.FindInstance(z.GetZDOID(phase==MailPhase.Returning?ReceiverKey:HomeKey));var target=go?go.GetComponent<Container>():null;
        if(!target){z.Set(StatusKey,phase==MailPhase.Returning?"Waiting for origin to load":"Waiting for destination to load");return;}
        var targetView=ContainerRegistry.GetView(target);
        bool Valid()=>view&&view.IsValid()&&Phase(z)==phase&&target&&ContainerRegistry.IsUsable(chest,false)&&ContainerRegistry.IsUsable(target,false)&&
            NativeStorageAccess.AllowedForPlayer(targetView,z.GetLong(PlayerKey,0))&&
            (phase==MailPhase.Returning?PostalMailbox.IsMailbox(target):ValidDestination(target,z));
        if(target.IsInUse()){z.Set(StatusKey,"Waiting for destination to be free");return;}
        if(!targetView.IsOwner())ContainerRegistry.Refresh(target);
        if(!chest.GetInventory().GetAllItems().Any(item=>InventoryTransfers.CapacityFor(target.GetInventory(),item,false)>0))
        {z.Set(StatusKey,"Waiting for space; "+z.GetInt(CountKey,0)+" items remain");return;}
        busy=true;
        void Work()
        {
            int delivered=0;
            foreach(var item in chest.GetInventory().GetAllItems().ToArray())
                delivered+=InventoryTransfers.Move(chest.GetInventory(),target.GetInventory(),item,item.m_stack,false);
            if(delivered>0 && phase!=MailPhase.Returning)DeliveryHistory.Record(target,z,delivered);
            if(delivered>0)ChestVisual.Pulse(target);
            int remaining=chest.GetInventory().GetAllItems().Sum(i=>i.m_stack);z.Set(CountKey,remaining);
            z.Set(PhaseKey,(int)MailRoute.Finish(phase,remaining));
            z.Set(StatusKey,remaining>0?"Waiting for space; "+remaining+" items remain":phase==MailPhase.Returning?"Mail returned":"Delivered");
            if(remaining==0&&!PostalMailbox.IsMailbox(target))ChestVisual.Pulse(target);
        }
        if(!NativeStorageAccess.Start("queued mail",new[]{view,targetView},Valid,Work,message=>{busy=false;if(message!="Saved"&&view&&view.IsValid()&&view.IsOwner())z.Set(StatusKey,"Waiting for access or ownership");}))busy=false;
    }
    private static bool ValidDestination(Container target,ZDO z)
    {
        if(PostalMailbox.IsMailbox(target))
        {var m=target.GetComponent<PostalMailbox>();return m&&m.Home&&ContainerRegistry.GetView(m.Home).GetZDO().m_uid==z.GetZDOID(HomeKey);}
        return ContainerRegistry.GetView(target).GetZDO().m_uid==z.GetZDOID(HomeKey)&&ContainerRegistry.GetSettings(target).Deposit;
    }
    // Parcels have no colliders or recipe; also reject native open RPCs so cargo
    // cannot be taken independently of the queued delivery/cancellation.
    [HarmonyPatch(typeof(Container),"RPC_RequestOpen"),HarmonyPrefix]
    private static bool NoOpen(Container __instance)=>!IsParcel(__instance);
}
