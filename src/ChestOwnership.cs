using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

// Owner-routed execution without leases, fences or world data.
//
// One client per base (the owner of that base's first Deposit Chest) runs
// automation. When it must write an object another client owns, it asks that
// owner for ownership with a single routed RPC, exactly as the game itself does
// in ItemDrop.RequestOwn / Container.RPC_RequestOpen. The owner hands over only
// closed objects, after force-sending its latest ZDO so the new owner starts
// from current data. Unanswered requests (unowned objects, a dedicated server
// holding objects near the world origin, a disconnected owner) fall back to the
// game's own ClaimOwnership, and writes wait one cycle after such a claim.
internal static class ChestOwnership
{
    internal const string RequestRpc = "Quartermaster_RequestOwner_v1";
    internal const string TouchRpc = "Quartermaster_Touch_v1";
    internal const float ClaimSettleSeconds = 2.5f;
    internal const float GrantSettleSeconds = 0.5f;
    private const int RequestsPerCycle = 6;
    private const int UnansweredBeforeClaim = 3;

    private sealed class Pending
    {
        internal float NextAt, FirstAt;
        internal int Sent;
        internal long Owner;
    }
    private static readonly Dictionary<ZDOID, Pending> Requests = new Dictionary<ZDOID, Pending>();
    private static readonly Dictionary<ZDOID, float> Claimed = new Dictionary<ZDOID, float>();
    private static readonly Dictionary<ZDOID, string> Traced = new Dictionary<ZDOID, string>();
    private static int sentThisCycle;

    internal static void Clear() { Requests.Clear(); Claimed.Clear(); Traced.Clear(); sentThisCycle = 0; }
    internal static void BeginCycle() => sentThisCycle = 0;

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ZNetView, object> Registered = new System.Runtime.CompilerServices.ConditionalWeakTable<ZNetView, object>();
    // Registered from the Awake hooks of every object automation may need to own.
    internal static void Register(ZNetView view)
    {
        if (!view || view.GetZDO() == null || Registered.TryGetValue(view, out _)) return;
        Registered.Add(view, new object());
        try
        {
            view.Register<long>(RequestRpc, (sender, requester) => Grant(view, sender, requester));
            view.Register(TouchRpc, sender => Touched(view, sender));
        }
        catch (Exception e) { Plugin.Log.LogWarning("Ownership RPC registration failed for " + view.name + ": " + e.Message); }
    }

    private static long Self => ZDOMan.GetSessionID();
    private static long ServerId => ZNet.instance == null ? 0 : ZNet.instance.IsServer() ? Self : ZNet.instance.GetServerPeer()?.m_uid ?? 0;
    // Clients only have a direct connection to the server. Other owners are reached
    // by routed RPC; absence from GetPeers cannot establish that they disconnected.
    // Only the server can use its direct peer list to prove an owner is absent.
    private static bool ConnectedPeer(long uid) => ZNet.instance != null && uid != 0 && uid != ServerId &&
        (!ZNet.instance.IsServer() || ZNet.instance.GetPeers().Any(p => p != null && p.m_uid == uid));

    // A chest a player has open, or that another client is crafting from, is never handed over.
    internal static bool Busy(ZNetView view)
    {
        if (!view || !view.IsValid()) return true;
        var chest = view.GetComponent<Container>();
        if (!chest) return false;
        if (chest.IsInUse() || Plugin.OpenContainer == chest || view.GetZDO().GetInt(ZDOVars.s_inUse) != 0 || SharedChests.Viewed(chest)) return true;
        return ContainerRegistry.Yielding(chest);
    }

    private static void Grant(ZNetView view, long sender, long requester)
    {
        if (!view || !view.IsValid() || requester == 0 || requester == Self) return;
        // The requester id in the payload must be the routed sender itself; a grant to a
        // third party would let any client move ownership (and the force-sent snapshot)
        // somewhere it never asked for.
        if (requester != sender) { Trace("grant-refused", view, "requester " + requester + " does not match sender " + sender); return; }
        if (!view.IsOwner()) { Trace("grant-skip", view, "not owner; sender=" + sender); return; }
        if (Busy(view)) { Trace("grant-refused", view, "busy; requester=" + requester); return; }
        // Same order as Container.RPC_RequestOpen: latest data first, then the owner change.
        ZDOMan.instance.ForceSendZDO(requester, view.GetZDO().m_uid);
        view.GetZDO().SetOwner(requester);
        Plugin.Log.LogInfo("[ownership] granted " + view.name + " " + view.GetZDO().m_uid + " to peer " + requester);
    }

    private static void Touched(ZNetView view, long sender)
    {
        var chest = view ? view.GetComponent<Container>() : null;
        if (chest) ContainerRegistry.Yield(chest, 1.5f);
    }

    // Tell the owners of foreign supply chests that a local craft is about to read and
    // save them, so their automation yields and no handoff happens mid-withdrawal.
    internal static void Touch(IEnumerable<Container> sources)
    {
        if (!NativeStorageAccess.Networked) return;
        foreach (var chest in sources)
        {
            var view = ContainerRegistry.GetView(chest);
            if (!view || !view.IsValid() || view.IsOwner()) continue;
            long owner = view.GetZDO().GetOwner();
            if (ConnectedPeer(owner)) view.InvokeRPC(owner, TouchRpc);
        }
    }

    // True when the object is owned locally and safe to write now. False schedules a
    // request/claim (bounded per cycle, exponential backoff) and the caller skips.
    internal static bool Request(ZNetView view, string reason, bool onlyUnserved = false)
    {
        if (!view || !view.IsValid()) return false;
        var zdo = view.GetZDO();
        if (view.IsOwner())
        {
            // Ownership arrived in answer to our request. The grantor force-sent its data
            // first, but receipt order at this peer is not verified, so wait a short
            // moment before the first write (unilateral claims wait the full settle).
            if (Requests.Remove(zdo.m_uid) && !Claimed.ContainsKey(zdo.m_uid))
                Claimed[zdo.m_uid] = Time.unscaledTime + GrantSettleSeconds;
            return Settled(view);
        }
        if (!NativeStorageAccess.Networked) return false;
        if (Busy(view)) { Trace("wait", view, reason + ": in use"); return false; }
        float now = Time.unscaledTime;
        long owner = zdo.GetOwner();
        if (!Requests.TryGetValue(zdo.m_uid, out var pending) || pending.Owner != owner)
            Requests[zdo.m_uid] = pending = new Pending { Owner = owner, FirstAt = now, NextAt = now };
        if (now < pending.NextAt) return false;
        bool served = ConnectedPeer(owner);
        if (!served)
        {
            // Nobody can answer: unowned, held by the dedicated server, or owner gone.
            Claim(view, reason + ": owner " + owner + " cannot answer");
            pending.NextAt = now + 2f; return false;
        }
        if (onlyUnserved) return false;
        if (pending.Sent >= UnansweredBeforeClaim && Plugin.ClaimUnansweredOwnership.Value)
        {
            Claim(view, reason + ": " + pending.Sent + " unanswered requests to peer " + owner);
            pending.Sent = 0; pending.NextAt = now + 30f; return false;
        }
        if (sentThisCycle >= RequestsPerCycle) return false;
        sentThisCycle++; pending.Sent++;
        pending.NextAt = now + Mathf.Min(30f, 2f * Mathf.Pow(2f, pending.Sent - 1));
        view.InvokeRPC(owner, RequestRpc, Self);
        Trace("request", view, reason + "; attempt " + pending.Sent + " to peer " + owner);
        return false;
    }

    private static void Claim(ZNetView view, string reason)
    {
        view.ClaimOwnership();
        Claimed[view.GetZDO().m_uid] = Time.unscaledTime + ClaimSettleSeconds;
        Plugin.Log.LogInfo("[ownership] claimed " + view.name + " " + view.GetZDO().m_uid + " (" + reason + ")");
    }

    // Claimed maps a ZDO to the time its ownership becomes safe to write: after a
    // unilateral claim the server may still deliver a newer snapshot, so writes wait
    // one cycle; a granted handoff was force-sent current data first and waits only
    // a short moment (receipt order at this peer is not verified).
    internal static bool Settled(ZNetView view)
    {
        if (!view || !view.IsValid() || !view.IsOwner()) return false;
        if (!Claimed.TryGetValue(view.GetZDO().m_uid, out var readyAt)) return true;
        if (Time.unscaledTime < readyAt) return false;
        Claimed.Remove(view.GetZDO().m_uid); return true;
    }

    internal static string Describe(ZNetView view)
    {
        if (!view || !view.IsValid()) return "invalid";
        long owner = view.GetZDO().GetOwner();
        return owner == 0 ? "unowned" : owner == Self ? "local" : owner == ServerId ? "server" : ConnectedPeer(owner) ? "another player" : "disconnected peer";
    }

    // Throttled state-transition trace; one line per object per distinct state.
    internal static void Trace(string op, ZNetView view, string reason)
    {
        if (Plugin.OwnershipTrace == null || !Plugin.OwnershipTrace.Value || !view || !view.IsValid()) return;
        var zdo = view.GetZDO();
        string state = op + "|" + zdo.GetOwner() + "|" + zdo.DataRevision + "|" + reason;
        if (Traced.TryGetValue(zdo.m_uid, out var last) && last == state) return;
        Traced[zdo.m_uid] = state;
        var chest = view.GetComponent<Container>();
        Plugin.Log.LogInfo("[ownership] " + op + " utc=" + DateTime.UtcNow.ToString("HH:mm:ss.fff")
            + " role=" + (ZNet.instance != null && ZNet.instance.IsServer() ? "server" : "client") + " me=" + Self
            + " player=" + (Player.m_localPlayer ? "yes" : "no") + " obj=" + view.name + " zdo=" + zdo.m_uid
            + " owner=" + zdo.GetOwner() + "(" + Describe(view) + ") rev=" + zdo.DataRevision + "/" + zdo.OwnerRevision
            + (chest ? " inUse=" + (chest.IsInUse() ? 1 : 0) + "/" + zdo.GetInt(ZDOVars.s_inUse) : "")
            + " reason=" + reason);
    }
}
