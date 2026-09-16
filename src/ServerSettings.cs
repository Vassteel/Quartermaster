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
            next=0;
        }
        if(!ZNet.instance.IsServer()&&Time.unscaledTime>=next)
        {
            next=Time.unscaledTime+5;
            var server=ZNet.instance.GetServerPeer();if(server!=null&&server.IsReady())rpc.InvokeRoutedRPC(server.m_uid,"QuartermasterSettingsRequest");
        }
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
