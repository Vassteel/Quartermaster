using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[BepInPlugin("local.valheim.quartermaster", "Quartermaster", "0.1.10")]
[BepInIncompatibility("local.valheim.hearthward")]
[BepInIncompatibility("MaddCatter.Hearthkeeper")]
[BepInIncompatibility("TastyChickenLegs.AutomaticFermenters")]
public sealed class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance;
    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> Enabled, CraftFromContainers, ExtendStationCoverage, ClearCheatItemTagsOnLoad;
    internal static ConfigEntry<float> Range, CraftRange;
    internal static ConfigEntry<int> Budget;
    internal static Container OpenContainer;
    internal static ConfigEntry<KeyboardShortcut> MachineKey;
    private Harmony harmony;
    private float next;
    private bool inWorld;
    private void Awake()
    {
        Instance = this; Log = Logger;
        Enabled = Config.Bind("General", "Enabled", true, "Enable base automation.");
        Range = Config.Bind("General", "BaseRange", 100f, new ConfigDescription("Radius in metres around each Deposit Chest; loaded objects only.", new AcceptableValueRange<float>(10, 200)));
        CraftRange = Config.Bind("General", "CraftRange", 100f, new ConfigDescription("Accessible container radius for crafting, building and upgrades.", new AcceptableValueRange<float>(5, 200)));
        CraftFromContainers = Config.Bind("General", "CraftFromContainers", true, "Use materials from chests with Crafting Supply enabled.");
        ExtendStationCoverage = Config.Bind("General", "ExtendStationCoverage", true, "A required station inside a Deposit Chest's BaseRange supports building, structure repair and dismantling throughout that same area. Crafting and item upgrades still require station interaction.");
        ClearCheatItemTagsOnLoad = Config.Bind("General", "ClearCheatItemTagsOnLoad", true, "Clear cheat item tags once from your inventory after world entry and from accessible, locally owned storage chests after their saved contents load. Distant chests are scanned when loaded. Changes persist on normal saves; disabling this does not restore removed tags.");
        Budget = Config.Bind("General", "ObjectsPerCycle", 24, new ConfigDescription("Rotating machine and deposit work budget every two seconds.", new AcceptableValueRange<int>(1, 100)));
        MachineKey = Config.Bind("Controls", "MachineConfig", new KeyboardShortcut(KeyCode.F9), "Open config for the machine or fire under the crosshair. Controller: use Machine Config from inventory.");
        harmony = new Harmony("local.valheim.quartermaster");
        try { harmony.PatchAll(typeof(Plugin).Assembly); }
        catch (Exception e) { harmony.UnpatchSelf(); Log.LogError("Quartermaster disabled: game hooks did not match. " + e); enabled = false; }
    }
    private void Update()
    {
        if (!Player.m_localPlayer)
        {
            if (inWorld) { ContainerRegistry.Clear(); Automation.Clear(); ItemTagCleanup.Clear(); ChestUi.Close(); inWorld = false; }
            return;
        }
        inWorld = true;
        ChestUi.Tick();
        if (!Enabled.Value) return;
        if (!InventoryGui.IsVisible() && (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible() && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.IsTeleporting() && MachineKey.Value.IsDown()) ChestUi.OpenHoveredMachine();
        if (Time.time < next) return;
        next = Time.time + 2f;
        try { ItemTagCleanup.Tick(); } catch (Exception e) { Log.LogError("Item tag scan failed; retrying next cycle: " + e); }
        try { Automation.Tick(); } catch (Exception e) { Log.LogError("Automation cycle failed; retrying next cycle: " + e); }
    }
    private void OnDestroy() { harmony?.UnpatchSelf(); ChestUi.Dispose(); ContainerRegistry.Clear(); Automation.Clear(); ItemTagCleanup.Clear(); }
}
