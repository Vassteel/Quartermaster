using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace Quartermaster;
internal sealed class MailAddress
{
    internal ZDOID Id,Receiver;
    internal Vector3 Position,ReceiverPosition;
    internal string Name;
    internal bool Mailbox;
}
internal sealed class MailProgress
{
    internal ZDOID Parcel;
    internal MailPhase Phase;
    internal string Status;
    internal int Count;
}
internal sealed class PostalDirectory:MonoBehaviour
{
    private static PostalDirectory instance;
    private ZRoutedRpc rpc;
    private ZDOMan world;
    private ZDO[] scan;
    private int cursor;
    private float nextScan,nextQuery;
    private readonly List<ZDO> deposits=new List<ZDO>(),mailboxes=new List<ZDO>(),parcels=new List<ZDO>();
    private readonly Dictionary<ZDOID,MailAddress> addresses=new Dictionary<ZDOID,MailAddress>();
    private readonly Dictionary<ZDOID,MailProgress> progress=new Dictionary<ZDOID,MailProgress>();
    private readonly HashSet<ZDOID> cancel=new HashSet<ZDOID>(),drop=new HashSet<ZDOID>();
    private static readonly FieldInfo Objects=AccessTools.Field(typeof(ZDOMan),"m_objectsByID");
    internal static IEnumerable<MailAddress> Addresses=>instance?instance.addresses.Values:Enumerable.Empty<MailAddress>();
    internal static MailProgress Progress(Container c)
    {
        var v=ContainerRegistry.GetView(c);if(!v||!v.IsValid())return null;
        var id=v.GetZDO().GetZDOID(PostalParcel.Link);if(id==ZDOID.None)return null;
        if(instance&&instance.progress.TryGetValue(v.GetZDO().m_uid,out var p)&&p.Parcel==id)return p;
        var z=ZDOMan.instance.GetZDO(id);
        return z!=null?ReadProgress(z):new MailProgress{Parcel=id,Phase=MailPhase.Waiting,Status="Checking queued mail"};
    }
    private static MailProgress ReadProgress(ZDO z)=>new MailProgress{Parcel=z.m_uid,Phase=PostalParcel.Phase(z),Status=z.GetString(PostalParcel.StatusKey,"Waiting for destination to load"),Count=z.GetInt(PostalParcel.CountKey,0)};
    internal static bool Pending(Container c){var p=Progress(c);return p!=null&&!MailRoute.Terminal(p.Phase);}
    private void Awake()=>instance=this;
    private void Update()
    {
        if(!ZNet.instance||ZRoutedRpc.instance==null||ZDOMan.instance==null){Reset();return;}
        if(world!=ZDOMan.instance){Reset();world=ZDOMan.instance;}
        if(rpc!=ZRoutedRpc.instance)
        {
            rpc=ZRoutedRpc.instance;rpc.Register<ZPackage>("QM_MailDirectory",Request);rpc.Register<ZPackage>("QM_MailAddresses",Reply);
            rpc.Register<ZDOID>("QM_CancelMail",CancelRequest);rpc.Register<ZDOID>("QM_ReturnMail",ReturnRequest);rpc.Register<ZDOID>("QM_DropReturnedMail",DropRequest);
        }
        if(ZNet.instance.IsServer())Scan();
        if(Time.unscaledTime>=nextQuery)
        {
            nextQuery=Time.unscaledTime+5;
            var p=new ZPackage();var boxes=PostalMailbox.All.Where(m=>m&&ContainerRegistry.GetView(m.Chest)&&ContainerRegistry.GetView(m.Chest).IsValid()).Take(64).ToArray();
            p.Write(boxes.Length);foreach(var m in boxes)p.Write(ContainerRegistry.GetView(m.Chest).GetZDO().m_uid);
            if(ZNet.instance.IsServer()){p.SetPos(0);Request(ZDOMan.GetSessionID(),p);}
            else{var server=ZNet.instance.GetServerPeer();if(server!=null&&server.IsReady())rpc.InvokeRoutedRPC(server.m_uid,"QM_MailDirectory",p);}
        }
        foreach(var id in cancel.Concat(drop).Distinct().ToArray())
        {
            var z=world.GetZDO(id);if(z==null||MailRoute.Terminal(PostalParcel.Phase(z))){cancel.Remove(id);drop.Remove(id);continue;}
            if(z.GetOwner()!=ZDOMan.GetSessionID()){cancel.Remove(id);drop.Remove(id);continue;}
            var go=ZNetScene.instance?ZNetScene.instance.FindInstance(id):null;var view=go?go.GetComponent<ZNetView>():null;
            if(drop.Contains(id))z.Set("QM.mail.dropReturn",true);else PostalParcel.Return(z);cancel.Remove(id);drop.Remove(id);
        }
    }
    private void Reset(){rpc=null;world=null;scan=null;addresses.Clear();progress.Clear();cancel.Clear();drop.Clear();deposits.Clear();mailboxes.Clear();parcels.Clear();nextScan=nextQuery=0;}
    private void Scan()
    {
        if(scan==null)
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+10;
            scan=((Dictionary<ZDOID,ZDO>)Objects.GetValue(world)).Values.ToArray();cursor=0;deposits.Clear();mailboxes.Clear();parcels.Clear();
        }
        // Bound JSON work per frame; most world objects have no chest settings.
        for(int n=0;n<1000&&cursor<scan.Length;n++,cursor++)
        {
            var z=scan[cursor];if(world.GetZDO(z.m_uid)!=z)continue;
            if(z.GetPrefab()==PostalParcel.Prefab.GetStableHashCode()){parcels.Add(z);continue;}
            if(z.GetPrefab()==PostalMailbox.PrefabName.GetStableHashCode()){mailboxes.Add(z);continue;}
            var settings=z.GetString(ContainerRegistry.SettingsKey,"");if(settings.Length==0)continue;
            try{if(JsonUtility.FromJson<ChestSettings>(settings)?.Deposit==true)deposits.Add(z);}catch{}
        }
        if(cursor<scan.Length)return;scan=null;addresses.Clear();
        foreach(var z in deposits)
        {
            var s=JsonUtility.FromJson<ChestSettings>(z.GetString(ContainerRegistry.SettingsKey,""));
            var pos=z.GetPosition();var mailbox=mailboxes.Where(m=>Belongs(m,z)).OrderBy(m=>(m.GetPosition()-pos).sqrMagnitude).ThenBy(m=>m.m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
            var receiver=z; // Receiving mail enters the deposit sorting queue.
            addresses[z.m_uid]=new MailAddress{Id=z.m_uid,Receiver=receiver.m_uid,Position=pos,ReceiverPosition=receiver.GetPosition(),Mailbox=mailbox!=null,Name=(string.IsNullOrWhiteSpace(s.BaseName)?"Unnamed base":s.BaseName)+" ("+Mathf.RoundToInt(pos.x)+", "+Mathf.RoundToInt(pos.z)+")"};
        }
        foreach(var z in parcels)
        {
            if(MailRoute.Terminal(PostalParcel.Phase(z)))
            {
                var source=world.GetZDO(z.GetZDOID(PostalParcel.SourceKey));
                if(source==null||source.GetZDOID(PostalParcel.Link)!=z.m_uid){z.SetOwner(ZDOMan.GetSessionID());world.DestroyZDO(z);}
            }
            else if(PostalParcel.Phase(z)!=MailPhase.Preparing&&world.GetZDO(z.GetZDOID(PostalParcel.Phase(z)==MailPhase.Returning?PostalParcel.ReceiverKey:PostalParcel.HomeKey))==null)
            {
                // Retain cargo and route it back when the destination was removed.
                SendReturn(z,PostalParcel.Phase(z)==MailPhase.Returning);
            }
        }
    }
    private bool Belongs(ZDO mailbox,ZDO home)
    {
        if((mailbox.GetPosition()-home.GetPosition()).sqrMagnitude>Plugin.Range.Value*Plugin.Range.Value)return false;
        string selected=mailbox.GetString(PostalMailbox.HomeKey,"");if(selected.Length>0)return selected==home.m_uid.ToString();
        return deposits.OrderBy(d=>(d.GetPosition()-mailbox.GetPosition()).sqrMagnitude).FirstOrDefault()==home;
    }
    private void Request(long sender,ZPackage p)
    {
        if(!ZNet.instance.IsServer())return;
        int count=p.ReadInt();if(count<0||count>64)return;
        var reply=new ZPackage();var entries=addresses.Values.Take(2048).ToArray();reply.Write(entries.Length);
        foreach(var a in entries){reply.Write(a.Id);reply.Write(a.Receiver);reply.Write(a.Position);reply.Write(a.ReceiverPosition);reply.Write(a.Name);reply.Write(a.Mailbox);}
        reply.Write(count);
        for(int i=0;i<count;i++)
        {
            var source=p.ReadZDOID();var z=world.GetZDO(source);var parcel=z==null?null:world.GetZDO(z.GetZDOID(PostalParcel.Link));
            reply.Write(source);reply.Write(parcel!=null);
            if(parcel!=null){var state=ReadProgress(parcel);reply.Write(state.Parcel);reply.Write((int)state.Phase);reply.Write(state.Status);reply.Write(state.Count);}
        }
        if(sender==ZDOMan.GetSessionID()){reply.SetPos(0);ReadReply(reply);}else rpc.InvokeRoutedRPC(sender,"QM_MailAddresses",reply);
    }
    private void Reply(long sender,ZPackage p){if(!ZNet.instance.IsServer()&&ZNet.instance.GetServerPeer()?.m_uid==sender)ReadReply(p);}
    private void ReadReply(ZPackage p)
    {
        int count=p.ReadInt();if(count<0||count>2048)return;
        // On a host, preserve the authoritative catalogue instead of clearing it.
        if(!ZNet.instance.IsServer())addresses.Clear();
        for(int i=0;i<count;i++){var a=new MailAddress{Id=p.ReadZDOID(),Receiver=p.ReadZDOID(),Position=p.ReadVector3(),ReceiverPosition=p.ReadVector3(),Name=p.ReadString(),Mailbox=p.ReadBool()};addresses[a.Id]=a;}
        count=p.ReadInt();if(count<0||count>64)return;
        for(int i=0;i<count;i++){var source=p.ReadZDOID();if(!p.ReadBool()){progress.Remove(source);continue;}progress[source]=new MailProgress{Parcel=p.ReadZDOID(),Phase=(MailPhase)p.ReadInt(),Status=p.ReadString(),Count=p.ReadInt()};}
    }
    internal static bool Cancel(Container source)
    {
        if(!instance||!ContainerRegistry.IsUsable(source,true)||!Pending(source))return false;
        var id=ContainerRegistry.GetView(source).GetZDO().m_uid;
        if(ZNet.instance.IsServer())instance.CancelRequest(ZDOMan.GetSessionID(),id);
        else{var server=ZNet.instance.GetServerPeer();if(server==null)return false;instance.rpc.InvokeRoutedRPC(server.m_uid,"QM_CancelMail",id);}
        return true;
    }
    private void CancelRequest(long sender,ZDOID id)
    {
        if(!ZNet.instance.IsServer())return;var source=world.GetZDO(id);
        if(source==null||source.GetPrefab()!=PostalMailbox.PrefabName.GetStableHashCode()||source.GetOwner()!=sender)return;
        var parcel=world.GetZDO(source.GetZDOID(PostalParcel.Link));if(parcel==null||parcel.GetZDOID(PostalParcel.SourceKey)!=id)return;SendReturn(parcel);
    }
    private void SendReturn(ZDO parcel,bool spill=false)
    {
        long owner=parcel.GetOwner();
        if(owner==0||owner!=ZDOMan.GetSessionID()&&ZNet.instance.GetPeer(owner)==null){parcel.SetOwner(ZDOMan.GetSessionID());owner=ZDOMan.GetSessionID();}
        if(owner==ZDOMan.GetSessionID()){if(spill)drop.Add(parcel.m_uid);else cancel.Add(parcel.m_uid);}else rpc.InvokeRoutedRPC(owner,spill?"QM_DropReturnedMail":"QM_ReturnMail",parcel.m_uid);
    }
    private void DropRequest(long sender,ZDOID id)
    {if(ZNet.instance.GetServerPeer()?.m_uid==sender)drop.Add(id);}
    private void ReturnRequest(long sender,ZDOID id)
    {if(ZNet.instance.GetServerPeer()?.m_uid==sender)cancel.Add(id);}
}
