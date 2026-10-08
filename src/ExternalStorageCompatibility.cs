using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;

namespace Quartermaster;

internal static class ExternalStorageCompatibility
{
    internal const string BarrelsGuid="gravebear.odinsfoodbarrels";
    internal const string PilesGuid="com.maxsch.valheim.DynamicStoragePiles";
    private static readonly List<ExternalStorageRules> rules=new List<ExternalStorageRules>();
    private static bool warned;
    internal static void Initialize()
    {
        rules.Clear();warned=false;
        Bind(BarrelsGuid,true);Bind(PilesGuid,false);
    }
    private static void Bind(string guid,bool barrel)
    {
        if(!Chainloader.PluginInfos.TryGetValue(guid,out var info))return;
        try
        {
            Assembly assembly=info.Instance.GetType().Assembly;
            rules.Add(new ExternalStorageRules(barrel?assembly:null,barrel?null:assembly));
            Plugin.Log.LogInfo("Storage compatibility enabled: "+info.Metadata.Name+" "+info.Metadata.Version);
        }
        catch(Exception e){Plugin.Log.LogWarning("Storage compatibility unavailable for "+guid+": "+e.Message);}
    }
    private static void Warn(Exception e)
    {if(!warned){warned=true;Plugin.Log.LogWarning("External storage rules failed; transfer skipped: "+e.Message);}}
    internal static bool Recognizes(Container c)
    {
        if(!c)return false;
        try{return rules.Any(r=>r.Recognizes(ContainerRegistry.PrefabName(c),c.m_name));}
        catch(Exception e){Warn(e);return false;}
    }
    internal static IEnumerable<string> Assigned(Container c)
    {
        if(!c)return Array.Empty<string>();
        try{return rules.SelectMany(r=>r.Assigned(ContainerRegistry.PrefabName(c),c.m_name)).Distinct().ToArray();}
        catch(Exception e){Warn(e);return Array.Empty<string>();}
    }
    internal static bool Allows(Inventory inventory,ItemDrop.ItemData item)
    {
        if(inventory==null||item==null)return false;
        try{return rules.All(r=>r.Allows(inventory.GetName(),InventoryTransfers.ItemId(item)));}
        catch(Exception e){Warn(e);return false;}
    }
}
