using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace Quartermaster;

// Native owner checks only. No leases, queues, ownership handoffs or chest-open patches.
internal static class NativeStorageAccess
{
    internal static bool Networked=>ZNet.instance && (!ZNet.instance.IsServer() || ZNet.instance.GetPeers().Count>0);
    internal static bool CanWrite(ZNetView view)=>view&&view.IsValid()&&view.IsOwner();
    internal static bool Start(string name,IEnumerable<ZNetView> objects,Func<bool> valid,Action work,Action<string> result=null,Action afterRelease=null)
    {
        if(!Plugin.Enabled.Value||!Player.m_localPlayer||Player.m_localPlayer.IsDead()||Player.m_localPlayer.IsTeleporting())return false;
        var views=objects.Distinct().ToArray();
        if(views.Length==0||views.Any(v=>!CanWrite(v)||!Access(v,Player.m_localPlayer.GetPlayerID()))||!valid())
        {result?.Invoke("Storage is not locally available; open the chest or carry the materials");return false;}
        try {work();result?.Invoke("Saved");afterRelease?.Invoke();return true;}
        catch(Exception error){Plugin.Log.LogError(name+" failed: "+error);result?.Invoke("Operation failed; check the log");return false;}
    }
    private static readonly MethodInfo AccessContainer=AccessTools.Method(typeof(Container),"CheckAccess");
    private static readonly FieldInfo Areas=AccessTools.Field(typeof(PrivateArea),"m_allAreas");
    private static readonly MethodInfo AreaEnabled=AccessTools.Method(typeof(PrivateArea),"IsEnabled"),AreaInside=AccessTools.Method(typeof(PrivateArea),"IsInside"),AreaPermitted=AccessTools.Method(typeof(PrivateArea),"IsPermitted");
    internal static bool AllowedForPlayer(ZNetView view,long player)=>Access(view,player);
    private static bool Access(ZNetView view,long player)
    {
        var c=view.GetComponent<Container>();
        if(c)
        {
            if(!ContainerRegistry.All.Contains(c)||!(bool)AccessContainer.Invoke(c,new object[]{player}))return false;
            if(!c.m_checkGuardStone)return true;
        }
        else if(!Automation.Devices.Any(m=>m&&Automation.View(m)==view))return false;
        foreach(var area in (List<PrivateArea>)Areas.GetValue(null))
            if(area&&(bool)AreaEnabled.Invoke(area,null)&&(bool)AreaInside.Invoke(area,new object[]{view.transform.position,0f})&&
                area.GetComponent<Piece>().GetCreator()!=player&&!(bool)AreaPermitted.Invoke(area,new object[]{player}))return false;
        return true;
    }
}
