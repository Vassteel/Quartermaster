using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Quartermaster;

// Only gameplay settings cross the wire. Client controls and appearance stay local.
// Applying server values never writes them into the client's configuration file.
public sealed class ServerSettings : MonoBehaviour
{
    private ConfigFile config;
    private ConfigEntryBase[] entries;
    private readonly Dictionary<ConfigEntryBase,string> local=new();
    private ZRoutedRpc rpc;
    private float next;
    internal string StackStatus { get; private set; }="";
    internal bool CanEditStacks=>!ZNet.instance||ZNet.instance.LocalPlayerIsAdminOrHost();
    internal bool CustomStacks=>Convert.ToBoolean(config["Inventory","EnableStackSizes"].BoxedValue);
    internal int StackMaximum=>Convert.ToInt32(config["Inventory","MaximumStackSize"].BoxedValue);
    internal void Initialize(ConfigFile file)
    {
        config=file;
        entries=config.Select(p=>p.Value).Where(e=>e.Definition.Section=="Inventory"||
            e.Definition.Section=="General"&&e.Definition.Key!="ClearCheatItemTagsOnLoad"&&e.Definition.Key!="HideCheatItemMessages").ToArray();
    }
    private void Update()
    {
        if(!ZNet.instance||ZRoutedRpc.instance==null){Restore();rpc=null;return;}
        if(rpc!=ZRoutedRpc.instance)
        {
            Restore();rpc=ZRoutedRpc.instance;
            rpc.Register("QuartermasterSettingsRequest",Request);
            rpc.Register<ZPackage>("QuartermasterSettingsReply",Reply);
            rpc.Register<ZPackage>("QuartermasterStackChange",ChangeStacks);
            rpc.Register<int>("QuartermasterStackResult",StackResult);
            next=0;
        }
        if(!ZNet.instance.IsServer()&&Time.unscaledTime>=next)
        {
            next=Time.unscaledTime+5;
            var server=ZNet.instance.GetServerPeer();if(server!=null&&server.IsReady())rpc.InvokeRoutedRPC(server.m_uid,"QuartermasterSettingsRequest");
        }
    }
    internal string SetStacks(bool custom,int maximum)
    {
        if(maximum<2||maximum>100000)return "Enter a maximum from 2 to 100000";
        if(!CanEditStacks)return "Only the server host or an admin can change stack sizes";
        if(!ZNet.instance||ZNet.instance.IsServer())
        {
            try {SaveStacks(custom,maximum);return StackStatus="Stack sizes saved and applied";}
            catch(Exception error){Plugin.Log.LogWarning("Could not save stack settings: "+error.Message);return StackStatus="Could not save stack sizes; previous settings retained";}
        }
        var server=ZNet.instance.GetServerPeer();
        if(rpc==null||server==null||!server.IsReady())return "Server settings are not connected yet";
        var package=new ZPackage();package.Write(custom);package.Write(maximum);
        rpc.InvokeRoutedRPC(server.m_uid,"QuartermasterStackChange",package);
        return StackStatus="Requesting stack-size change from the server…";
    }
    private void SaveStacks(bool custom,int maximum)
    {
        bool save=config.SaveOnConfigSet;config.SaveOnConfigSet=false;
        object oldCustom=config["Inventory","EnableStackSizes"].BoxedValue,oldMaximum=config["Inventory","MaximumStackSize"].BoxedValue;
        try
        {
            config["Inventory","EnableStackSizes"].BoxedValue=custom;
            config["Inventory","MaximumStackSize"].BoxedValue=maximum;
            config.Save();ApplyStacks();
        }
        catch
        {
            config["Inventory","EnableStackSizes"].BoxedValue=oldCustom;
            config["Inventory","MaximumStackSize"].BoxedValue=oldMaximum;
            ApplyStacks();throw;
        }
        finally{config.SaveOnConfigSet=save;}
    }
    private void ChangeStacks(long sender,ZPackage data)
    {
        if(!ZNet.instance||!ZNet.instance.IsServer())return;
        var peer=ZNet.instance.GetPeer(sender);if(peer==null)return;
        // Match vanilla's remote-admin check against the authenticated socket,
        // never an ID or privilege flag supplied in the request payload.
        if(peer.m_socket==null||!ZNet.instance.IsAdmin(peer.m_socket.GetHostName()))
        {rpc.InvokeRoutedRPC(sender,"QuartermasterStackResult",0);return;}
        try
        {
            bool custom=data.ReadBool();int maximum=data.ReadInt();
            if(maximum<2||maximum>100000){rpc.InvokeRoutedRPC(sender,"QuartermasterStackResult",-1);return;}
            SaveStacks(custom,maximum);Request(sender);
            rpc.InvokeRoutedRPC(sender,"QuartermasterStackResult",1);
        }
        catch(Exception error){Plugin.Log.LogWarning("Stack-size request failed: "+error.Message);rpc.InvokeRoutedRPC(sender,"QuartermasterStackResult",-1);}
    }
    private void StackResult(long sender,int result)
    {
        if(!ZNet.instance||ZNet.instance.IsServer()||ZNet.instance.GetServerPeer()?.m_uid!=sender)return;
        StackStatus=result==1?"Stack sizes saved and applied":result==0?"Server rejected the change: admin access required":"Stack-size change failed";
    }
    private void Request(long sender)
    {
        if(!ZNet.instance.IsServer()||ZNet.instance.GetPeer(sender)==null)return;
        var data=new ZPackage();data.Write(entries.Length);
        foreach(var entry in entries){data.Write(entry.Definition.Section);data.Write(entry.Definition.Key);data.Write(entry.GetSerializedValue());}
        rpc.InvokeRoutedRPC(sender,"QuartermasterSettingsReply",data);
    }
    private void Reply(long sender,ZPackage data)
    {
        if(ZNet.instance.IsServer()||ZNet.instance.GetServerPeer()?.m_uid!=sender)return;
        var incoming=new Dictionary<ConfigEntryBase,string>();
        try
        {
            int count=data.ReadInt();if(count<0||count>64)return;
            for(int i=0;i<count;i++)
            {
                string section=data.ReadString(),key=data.ReadString(),value=data.ReadString();
                if(value.Length>128)return;
                var entry=entries.FirstOrDefault(e=>e.Definition.Section==section&&e.Definition.Key==key);
                if(entry==null||incoming.ContainsKey(entry))return;incoming.Add(entry,value);
            }
            if(incoming.Count!=entries.Length)return;
            bool save=config.SaveOnConfigSet;config.SaveOnConfigSet=false;
            try
            {
                bool changed=false;
                foreach(var pair in incoming)
                {
                    if(!local.ContainsKey(pair.Key))local.Add(pair.Key,pair.Key.GetSerializedValue());
                    if(pair.Key.GetSerializedValue()!=pair.Value){pair.Key.SetSerializedValue(pair.Value);changed=true;}
                }
                if(changed)ApplyStacks();
            }
            finally{config.SaveOnConfigSet=save;}
        }
        catch(Exception error){Plugin.Log.LogWarning("Could not synchronize Quartermaster settings: "+error.Message);}
    }
    private void ApplyStacks()
    {
        ItemStacks.Restore();
        ItemStacks.Configure(Plugin.Enabled.Value&&Convert.ToBoolean(config["Inventory","EnableStackSizes"].BoxedValue),
            Convert.ToInt32(config["Inventory","MaximumStackSize"].BoxedValue));
        if(ObjectDB.instance)ItemStacks.ApplyDatabase(ObjectDB.instance);
    }
    private void Restore()
    {
        if(local.Count==0||config==null)return;
        bool save=config.SaveOnConfigSet;config.SaveOnConfigSet=false;
        try{foreach(var pair in local)pair.Key.SetSerializedValue(pair.Value);local.Clear();ApplyStacks();}
        finally{config.SaveOnConfigSet=save;}
    }
    internal void Shutdown(){Restore();enabled=false;}
    private void OnDestroy()=>Restore();
}
