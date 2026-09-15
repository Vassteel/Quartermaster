using System;
using UnityEngine;

namespace Quartermaster;

// Observe native production spawns; never replace or suppress the game's Spawn method.
internal static class ProductionOutput
{
    internal const string GroupKey = "Hearthward.outputGroup";
    internal const string ReadyKey = "Quartermaster.outputCollectAfter";
    [ThreadStatic] private static Scope current;
    internal sealed class Scope
    {
        internal Scope Previous;
        internal string Group, Item;
    }
    internal static Scope Begin(Component machine, ItemDrop output)
    {
        var scope = new Scope { Previous = current };
        current = scope;
        try
        {
            if (!Plugin.Enabled.Value || !output || !Automation.Owned(machine) || Automation.Settings(machine).Paused) return scope;
            if (machine is CookingStation rack && !rack.m_requireFire) return scope;
            var network = Automation.Network(machine);
            if (network == null) return scope;
            scope.Group = network.Group;
            scope.Item = InventoryTransfers.PrefabId(output);
        }
        catch (Exception e) { Plugin.Log.LogWarning("Could not mark production for later collection: " + e.Message); }
        return scope;
    }
    internal static void End(Scope scope)
    {
        if (scope != null) current = scope.Previous;
    }
    internal static void Created(ItemDrop item)
    {
        if (current?.Group == null || !item || InventoryTransfers.ItemId(item.m_itemData) != current.Item) return;
        try
        {
            var view = Automation.View(item);
            if (!view || !view.IsValid() || !view.IsOwner()) return;
            var z = view.GetZDO();
            z.Set(GroupKey, current.Group);
            z.Set(ReadyKey, ZNet.instance.GetTime().Ticks + Policy.OutputDelayTicks);
            Track(item);
        }
        catch (Exception e) { Plugin.Log.LogWarning("Output remains available for pickup; could not schedule collection: " + e.Message); }
    }
    internal static void Track(ItemDrop item)
    {
        var view = Automation.View(item);
        if (view && view.IsValid() && view.GetZDO().GetString(GroupKey, "").Length > 0 && !Automation.Outputs.Contains(item)) Automation.Outputs.Add(item);
    }
    internal static bool Ready(ItemDrop item)
    {
        var z = Automation.View(item).GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        long ready = z.GetLong(ReadyKey, 0L);
        // Old tagged output and a backwards clock correction receive a fresh grace period.
        if (ready <= 0 || ready - now > Policy.OutputDelayTicks)
        { z.Set(ReadyKey, now + Policy.OutputDelayTicks); return false; }
        return Policy.OutputReady(now, ready);
    }
    internal static bool AllowAutoStack(ItemDrop item)
    {
        if (!Plugin.Enabled.Value || !item) return true;
        // Vanilla world auto-stacking discards source ZDO tags/timestamps. Keep nearby
        // output piles separate so fresh output cannot inherit an older collection time,
        // and player drops cannot be swept up by merging with a tagged product.
        foreach (var output in Automation.Outputs)
            if (output && (output.transform.position - item.transform.position).sqrMagnitude <= 16f) return false;
        return true;
    }
}
