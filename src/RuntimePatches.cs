using System;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

[HarmonyPatch]
internal static class RuntimePatches
{
    [HarmonyPatch(typeof(Container), "Awake"), HarmonyPrefix]
    private static void PreserveExistingCapacity(Container __instance)
    {
        var v = ContainerRegistry.GetView(__instance);
        if (!v || !v.IsValid()) return;
        // Preserve old Hearthkeeper-sized chests before Inventory is constructed.
        var z = v.GetZDO();
        __instance.m_width = Math.Max(__instance.m_width, z.GetInt("MaddCatter.Hearthkeeper.width", 0));
        __instance.m_height = Math.Max(__instance.m_height, z.GetInt("MaddCatter.Hearthkeeper.height", 0));
    }
    [HarmonyPatch(typeof(Container), "Awake"), HarmonyPostfix]
    private static void ChestAwake(Container __instance) => ContainerRegistry.Register(__instance);
    [HarmonyPatch(typeof(Inventory), "Changed"), HarmonyPostfix]
    private static void InventoryChanged(Inventory __instance)
    {
        var c = ContainerRegistry.OwnerOf(__instance); if (c) ContainerRegistry.Learn(c);
    }
    [HarmonyPatch(typeof(Fireplace), "Awake"), HarmonyPostfix]
    private static void FireAwake(Fireplace __instance) => Automation.Register(__instance);
    [HarmonyPatch(typeof(Smelter), "Awake"), HarmonyPostfix]
    private static void SmelterAwake(Smelter __instance) => Automation.Register(__instance);
    [HarmonyPatch(typeof(Fermenter), "Awake"), HarmonyPostfix]
    private static void FermenterAwake(Fermenter __instance) => Automation.Register(__instance);
    [HarmonyPatch(typeof(CookingStation), "Awake"), HarmonyPostfix]
    private static void CookingAwake(CookingStation __instance) => Automation.Register(__instance);
    [HarmonyPatch(typeof(CookingStation), "UpdateCooking"), HarmonyPrefix]
    private static void CookingBefore(CookingStation __instance) => CookingReady(__instance);
    [HarmonyPatch(typeof(CookingStation), "UpdateCooking"), HarmonyPostfix]
    private static void CookingAfter(CookingStation __instance) => CookingReady(__instance);
    private static void CookingReady(CookingStation __instance)
    {
        try { Automation.HarvestCooking(__instance); }
        catch (Exception e) { Plugin.Log.LogWarning("Cooking automation check failed; native cooking continues: " + e.GetBaseException().Message); }
    }
    [HarmonyPatch(typeof(MonsterAI), "Awake"), HarmonyPostfix]
    private static void AnimalAwake(MonsterAI __instance) { if (!Automation.Animals.Contains(__instance)) Automation.Animals.Add(__instance); }
    [HarmonyPatch(typeof(ItemDrop), "Awake"), HarmonyPostfix]
    private static void OutputAwake(ItemDrop __instance) => ProductionOutput.Track(__instance);
    [HarmonyPatch(typeof(Smelter), "Spawn"), HarmonyPrefix]
    private static void SmelterOutput(Smelter __instance, string ore, out ProductionOutput.Scope __state)
        => __state = ProductionOutput.Begin(__instance, __instance.m_conversion.FirstOrDefault(c => !c.m_from || c.m_from.name == ore)?.m_to);
    [HarmonyPatch(typeof(Smelter), "Spawn"), HarmonyFinalizer]
    private static void SmelterOutputEnd(ProductionOutput.Scope __state) => ProductionOutput.End(__state);
    [HarmonyPatch(typeof(Fermenter), "DelayedTap"), HarmonyPrefix]
    private static void FermenterOutput(Fermenter __instance, out ProductionOutput.Scope __state)
    {
        int item = Traverse.Create(__instance).Field<int>("m_delayedTapItem").Value;
        __state = ProductionOutput.Begin(__instance, __instance.m_conversion.FirstOrDefault(c => c.m_from && c.m_from.name.GetStableHashCode() == item)?.m_to);
    }
    [HarmonyPatch(typeof(Fermenter), "DelayedTap"), HarmonyFinalizer]
    private static void FermenterOutputEnd(ProductionOutput.Scope __state) => ProductionOutput.End(__state);
    [HarmonyPatch(typeof(CookingStation), "SpawnItem"), HarmonyPrefix]
    private static void CookingOutput(CookingStation __instance, string name, out ProductionOutput.Scope __state)
    {
        var prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(name) : null;
        __state = ProductionOutput.Begin(__instance, prefab ? prefab.GetComponent<ItemDrop>() : null);
    }
    [HarmonyPatch(typeof(CookingStation), "SpawnItem"), HarmonyFinalizer]
    private static void CookingOutputEnd(ProductionOutput.Scope __state) => ProductionOutput.End(__state);
    [HarmonyPatch(typeof(ItemDrop), "OnCreateNew", new[] { typeof(ItemDrop), typeof(bool) }), HarmonyPostfix]
    private static void ProducedItem(ItemDrop item) => ProductionOutput.Created(item);
    [HarmonyPatch(typeof(ItemDrop), "AutoStackItems"), HarmonyPrefix]
    private static bool PreserveOutputDelay(ItemDrop __instance) => ProductionOutput.AllowAutoStack(__instance);
    [HarmonyPatch(typeof(Smelter), "UpdateSmelter"), HarmonyPrefix]
    private static void RecoverProcessorClock(Smelter __instance) => Automation.RecoverProcessorClock(__instance);
    [HarmonyPatch(typeof(InventoryGui), "Show"), HarmonyPostfix]
    private static void Show(Container container) { Plugin.OpenContainer = container; ChestUi.Attach(); }
    [HarmonyPatch(typeof(InventoryGui), "Hide"), HarmonyPostfix]
    private static void Hide() { Plugin.OpenContainer = null; ChestUi.Close(); }
    [HarmonyPatch(typeof(InventoryGui), "Update"), HarmonyPrefix]
    private static bool InventoryUpdate() => !ChestUi.IsOpen && !ChestUi.TryControllerShortcut();
    [HarmonyPatch(typeof(Player), "TakeInput"), HarmonyPostfix]
    private static void BlockPlayerInput(ref bool __result) { if (ChestUi.IsOpen) __result = false; }
    [HarmonyPatch(typeof(Container), "GetHoverText"), HarmonyPostfix]
    private static void ChestHover(Container __instance, ref string __result)
    {
        if (!Plugin.Enabled.Value || !ContainerRegistry.Accessible(__instance)) return;
        var s = ContainerRegistry.GetSettings(__instance);
        __result += s.Deposit ? "\n<color=#73ddff>Deposit Chest · " + s.Group + "</color>\n" + Automation.Status(__instance)
            : "\nChest Config available while open · " + s.Remembered.Count + " remembered item types";
    }
    [HarmonyPatch(typeof(Fireplace), "GetHoverText"), HarmonyPostfix]
    private static void FireHover(Fireplace __instance, ref string __result) => Append(__instance, ref __result);
    [HarmonyPatch(typeof(Fermenter), "GetHoverText"), HarmonyPostfix]
    private static void FermenterHover(Fermenter __instance, ref string __result) => Append(__instance, ref __result);
    [HarmonyPatch(typeof(Smelter), "OnHoverAddOre"), HarmonyPostfix]
    private static void OreHover(Smelter __instance, ref string __result) => Append(__instance, ref __result);
    [HarmonyPatch(typeof(Smelter), "OnHoverAddFuel"), HarmonyPostfix]
    private static void FuelHover(Smelter __instance, ref string __result) => Append(__instance, ref __result);
    [HarmonyPatch(typeof(CookingStation), "GetHoverText"), HarmonyPostfix]
    private static void CookingHover(CookingStation __instance, ref string __result) { if (__instance.m_requireFire) Append(__instance, ref __result); }
    private static void Append(Component c, ref string text)
    {
        if (!Plugin.Enabled.Value || !c.GetComponent<Piece>() || !PrivateArea.CheckAccess(c.transform.position, 0f, false, true)) return;
        text += "\n<color=#73ddff>" + Automation.Status(c) + "</color>\n[" + Plugin.MachineKey.Value.MainKey + "] Machine Config";
    }
}
