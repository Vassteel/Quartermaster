using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Quartermaster;

[HarmonyPatch]
internal static class ItemTagCleanup
{
    private static readonly HashSet<Container> Pending = new HashSet<Container>();
    private static ConditionalWeakTable<Inventory, object> scanned = new ConditionalWeakTable<Inventory, object>();
    private static Player readyPlayer;

    [HarmonyPatch(typeof(Player), "OnSpawned"), HarmonyPostfix]
    internal static void PlayerLoaded(Player __instance)
    {
        if (__instance == Player.m_localPlayer) readyPlayer = __instance;
    }

    [HarmonyPatch(typeof(Container), "Load"), HarmonyPostfix]
    internal static void ContainerLoaded(Container __instance, bool __result)
    {
        if (!__result || !__instance) return;
        var inventory = ContainerRegistry.SafeInventory(__instance);
        // Container.Awake creates an empty inventory before its saved items load.
        if (inventory != null && ContainerRegistry.OwnerOf(inventory) == __instance
            && !scanned.TryGetValue(inventory, out _)) Pending.Add(__instance);
    }

    internal static void Tick()
    {
        if (!Plugin.Enabled.Value || !Plugin.ClearCheatItemTagsOnLoad.Value || !Player.m_localPlayer) return;
        if (readyPlayer == Player.m_localPlayer) ScanOnce(readyPlayer.GetInventory());
        foreach (var chest in Pending.ToArray())
        {
            if (!chest) { Pending.Remove(chest); continue; }
            if (!ContainerRegistry.IsUsable(chest, false)) continue;
            ScanOnce(ContainerRegistry.SafeInventory(chest));
            Pending.Remove(chest);
        }
    }

    internal static int ScanOnce(Inventory inventory)
    {
        if (inventory == null || scanned.TryGetValue(inventory, out _)) return 0;
        scanned.Add(inventory, new object());
        int changed = 0;
        foreach (var item in inventory.GetAllItems())
        {
            if (item == null || !item.m_cheated) continue;
            item.m_cheated = false;
            changed++;
        }
        if (changed > 0) InventoryTransfers.Notify(inventory);
        return changed;
    }

    internal static void Clear()
    {
        Pending.Clear();
        scanned = new ConditionalWeakTable<Inventory, object>();
        readyPlayer = null;
    }
}
