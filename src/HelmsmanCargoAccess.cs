using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Optional integration boundary. Ship cargo is deliberately never registered with base automation.
public static partial class HelmsmanCargoAccess
{
    private static readonly System.Reflection.MethodInfo CheckAccess=AccessTools.Method(typeof(Container),"CheckAccess");
    private static readonly System.Reflection.MethodInfo Controlling=AccessTools.Method(typeof(Ship),"HaveControllingPlayer");
    public static bool Available => Plugin.Instance && Plugin.Instance.isActiveAndEnabled && Plugin.Enabled.Value &&
        Chainloader.PluginInfos.TryGetValue("local.valheim.helmsman",out var info) && info.Instance && info.Instance.isActiveAndEnabled;

    internal static string ValidateBoat(Ship ship, out Container cargo)
    {
        cargo=null;
        if(!Available) return "Helmsman and Quartermaster must both be enabled.";
        var player=Player.m_localPlayer;
        if(!ship || !player || player.IsDead() || !ship.IsPlayerInBoat(player)) return "Board the boat to request cargo handling.";
        if(!ship.IsOwner()) return "Waiting for ownership of the boat.";
        if(ship.GetSpeedSetting()!=Ship.Speed.Stop || Mathf.Abs(ship.GetSpeed())>.5f || (bool)Controlling.Invoke(ship,null))
            return "Stop the boat and release the helm first.";
        var containers=ship.GetComponentsInChildren<Container>().Where(c=>c.GetComponentInParent<Ship>()==ship).ToArray();
        if(containers.Length!=1) return "This boat needs one supported cargo hold.";
        cargo=containers[0]; var view=ContainerRegistry.GetView(cargo);
        if(!view || !view.IsValid() || !view.IsOwner() || cargo.GetInventory()==null) return "The cargo hold is not ready or locally owned.";
        if(cargo.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse)!=0) return "Close the cargo hold first.";
        if(!(bool)CheckAccess.Invoke(cargo,new object[]{player.GetPlayerID()}) ||
            !PrivateArea.CheckAccess(ship.transform.position,0f,false,true)) return "You do not have access to this boat's cargo.";
        return "";
    }
    internal static bool HubInRange(Container hub, Ship ship)
    {
        return hub && ship && ContainerRegistry.Accessible(hub) && ContainerRegistry.GetSettings(hub).Deposit &&
            (hub.transform.position-ship.transform.position).sqrMagnitude<=Plugin.Range.Value*Plugin.Range.Value;
    }
    internal static Container FindHub(Ship ship) => ContainerRegistry.All.Where(h=>HubInRange(h,ship))
        .OrderBy(h=>(h.transform.position-ship.transform.position).sqrMagnitude)
        .ThenBy(h=>ContainerRegistry.GetView(h).GetZDO().m_uid.ToString(),System.StringComparer.Ordinal).FirstOrDefault();

    public static string Check(Ship ship)
    {
        var reason=ValidateBoat(ship,out _);
        if(reason.Length>0) return reason;
        return FindHub(ship) ? "" : "No accessible Quartermaster Deposit Chest is in range.";
    }
    public static bool InRange(Ship ship) => Available && ship && FindHub(ship);
    public static void Throw(Transform gull, ItemDrop.ItemData item, Vector3 beak)
    {
        if(Available && gull) GullToss.Spawn(gull,item,beak);
    }
    public static void ClearThrows(Transform gull) => GullToss.ClearFor(gull);
}
