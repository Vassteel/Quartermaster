using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

[BepInPlugin("local.valheim.quartermaster", "Quartermaster", "0.1.64")]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(PlantEasilyCompatibility.Guid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInIncompatibility("local.valheim.hearthward")]
[BepInDependency("TastyChickenLegs.AutomaticFermenters", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ExternalStorageCompatibility.BarrelsGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ExternalStorageCompatibility.PilesGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance;
    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> Enabled, CraftFromContainers, ExtendStationCoverage, ClearCheatItemTagsOnLoad, HideCheatItemMessages;
    internal static ConfigEntry<bool> ClaimUnansweredOwnership, OwnershipTrace, SharedChests;
    internal static ConfigEntry<float> Range, CraftRange;
    internal static ConfigEntry<int> Budget;
    internal static ConfigEntry<bool> OwlCollectDroppedItems;
    internal static ConfigEntry<float> OwlVolume;
    internal static ConfigEntry<bool> ShowSortInventory;
    internal static ConfigEntry<int> SortInventoryOffsetX, SortInventoryOffsetY;
    internal static ConfigEntry<string> StorageCategoryOverrides;
    internal static Container OpenContainer;
    internal static ConfigEntry<KeyboardShortcut> MachineKey;
    private Harmony harmony;
    private float next;
    private bool playerReady;
    private void Awake()
    {
        Instance = this; Log = Logger;
        FermenterCompatibility.Initialize();
        Enabled = Config.Bind("General", "Enabled", true, "Enable base automation.");
        Range = Config.Bind("General", "BaseRange", 100f, new ConfigDescription("Radius in metres around each Deposit Chest; loaded objects only.", new AcceptableValueRange<float>(10, 200)));
        CraftRange = Config.Bind("General", "CraftRange", 100f, new ConfigDescription("Accessible container radius for crafting, building and upgrades.", new AcceptableValueRange<float>(5, 200)));
        CraftFromContainers = Config.Bind("General", "CraftFromContainers", true, "Use materials from chests with Crafting Supply enabled.");
        ExtendStationCoverage = Config.Bind("General", "ExtendStationCoverage", true, "A required station inside a Deposit Chest's BaseRange supports building, structure repair and dismantling throughout that same area. Crafting and item upgrades still require station interaction.");
        ClearCheatItemTagsOnLoad = Config.Bind("General", "ClearCheatItemTagsOnLoad", true, "Clear cheat item tags once from your inventory after world entry and from accessible, locally owned storage chests after their saved contents load. Distant chests are scanned when loaded. Changes persist on normal saves; disabling this does not restore removed tags.");
        HideCheatItemMessages = Config.Bind("General", "HideCheatItemMessages", true, "Hide item cheat notices in tooltips and inventory pickup/removal messages. Independent of the item-tag cleanup scan.");
        ClaimUnansweredOwnership = Config.Bind("Multiplayer", "ClaimUnansweredOwnership", true, "When another connected player owns a closed chest or station this base's automation needs and does not answer three ownership requests (about 14 seconds; usually a client without Quartermaster), take ownership with the game's own claim. Unowned, server-held and disconnected-owner objects are always claimed. Never applies to open chests.");
        SharedChests = Config.Bind("Multiplayer", "SharedChests", true, "Several players can use the same chest, drawer or Apothecary storage at once. Only works with players running the same Quartermaster version; anyone else gets the game's normal one-player-at-a-time chests. Local.");
        OwnershipTrace = Config.Bind("Diagnostics", "OwnershipTrace", false, "Log throttled ownership state transitions (requests, grants, claims, waits) and a 30-second per-base summary to the BepInEx log. Local only; no world data.");
        Budget = Config.Bind("General", "ObjectsPerCycle", 24, new ConfigDescription("Rotating machine and deposit work budget every two seconds.", new AcceptableValueRange<int>(1, 100)));
        OwlCollectDroppedItems=Config.Bind("General","OwlCollectDroppedItems",true,"After 20 seconds idle, the owl collects up to three individual dropped items per trip within 25 metres of its Deposit Chest. Respects ward access, ignored pickup types, chest room and the production-output grace period.");
        StorageCategoryOverrides=Config.Bind("General","StorageCategoryOverrides","","Storage prefab exceptions: ItemPrefab=ingredients;OtherPrefab=reagents;ExcludedPrefab=none. Categories: ingredients, reagents, lumber, ingots, ores, coal, hides, textiles, feathers, bones, masonry, food, produce, meat, fish, grain, arrows, bolts, weapons, longweapons, bows, crossbows, shields, armor. Server-synchronized. Per-chest learning and ignored items remain available.");
        OwlVolume=Config.Bind("Cosmetics","OwlVolume",.35f,new ConfigDescription("Local owl coos and clucks. Set 0 to mute.",new AcceptableValueRange<float>(0,1)));
        ShowSortInventory=Config.Bind("Interface","ShowSortInventory",true,"Show the Sort Inventory button. Local display preference.");
        SortInventoryOffsetX=Config.Bind("Interface","SortInventoryOffsetX",0,new ConfigDescription("Horizontal offset of Sort Inventory in UI units. Positive moves right. Local display preference.",new AcceptableValueRange<int>(-1000,1000)));
        SortInventoryOffsetY=Config.Bind("Interface","SortInventoryOffsetY",0,new ConfigDescription("Vertical offset of Sort Inventory in UI units. Positive moves down. Local display preference.",new AcceptableValueRange<int>(-1000,1000)));
        MachineKey = Config.Bind("Controls", "MachineConfig", new KeyboardShortcut(KeyCode.F9), "Open config for the machine or fire under the crosshair. Controller: use Machine Config from inventory.");
        var stackSizes = Config.Bind("Inventory", "EnableStackSizes", true, "Use MaximumStackSize for stackable items. Set false to keep normal item stack limits. Equipment and existing item quantities are unchanged. Servers running Quartermaster control this setting. Use the ledger book to apply changes in game; direct config-file edits require a restart.");
        var stackLimit = Config.Bind("Inventory", "MaximumStackSize", 1000, new ConfigDescription("Maximum items per stack, not a multiplier. Does not change item weight or delete existing items when lowered. Use the ledger book to apply changes in game; direct config-file edits require a restart.", new AcceptableValueRange<int>(2, 100000)));
        ItemStacks.Configure(Enabled.Value && stackSizes.Value, stackLimit.Value);
        gameObject.AddComponent<ServerSettings>().Initialize(Config);
        harmony = new Harmony("local.valheim.quartermaster");
        ExternalStorageCompatibility.Initialize();
        InventoryTransfers.Admission=(inventory,item)=>MeadCabinet.Allows(inventory,item)&&ExternalStorageCompatibility.Allows(inventory,item);
        try { harmony.PatchAll(typeof(Plugin).Assembly); Quartermaster.SharedChests.Install(harmony); PlantEasilyCompatibility.Initialize(harmony); BuildPieces.Initialize(); PostalMailbox.Initialize(); ClayResource.Initialize(); Apothecary.Initialize(); DrawerFurniture.Initialize(); gameObject.AddComponent<DrawerNetwork>(); gameObject.AddComponent<PostalDirectory>(); }
        catch (Exception e)
        {
            enabled = false;
            Log.LogError("Quartermaster disabled: initialization failed. " + e);
            PatchCleanup.Run(() => harmony.UnpatchSelf(), ItemStacks.Restore, message => Log.LogError(message));
        }
    }
    private void OnApplicationFocus(bool focused)
    { Log.LogInfo("Window focus="+focused+", runInBackground="+Application.runInBackground); }
    private void Update()
    {
        try { Quartermaster.SharedChests.Tick(); } catch (Exception e) { Log.LogError("Shared chest update failed: " + e); }
        if (!Player.m_localPlayer || Player.m_localPlayer.IsDead())
        {
            // Respawn replaces the player while loaded chests and machines survive.
            // Their Awake hooks do not run again, so retain the world registries.
            if (playerReady) { ChestSearch.Clear(); ChestUi.Close(); OpenContainer = null; PickupFilter.Clear(); playerReady = false; }
            return;
        }
        playerReady = true;
        ChestSearch.Tick();
        ChestUi.Tick();
        if (!Enabled.Value) return;
        if (!InventoryGui.IsVisible() && (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible() && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.IsTeleporting() && MachineKey.Value.IsDown()) ChestUi.OpenHoveredMachine();
        if (Time.time < next) return;
        next = Time.time + 2f;
        try { ItemTagCleanup.Tick(); } catch (Exception e) { Log.LogError("Item tag scan failed; retrying next cycle: " + e); }
        try { ChestStackOverflow.Tick(); } catch (Exception e) { Log.LogError("Chest overflow check failed: " + e); }
        try { Automation.Tick(); } catch (Exception e) { Log.LogError("Automation cycle failed; retrying next cycle: " + e); }
    }
    internal static void WorldUnloaded()
    {
        Quartermaster.SharedChests.Clear(); ChestSearch.Clear(); ContainerRegistry.Clear(); Automation.Clear(); ItemTagCleanup.Clear(); NativeHangingItem.ClearWarnings();
        ChestUi.Close(); OpenContainer = null; PickupFilter.Clear();
        if (Instance) { Instance.playerReady = false; Instance.next = 0; }
    }
    private void OnDestroy() { BuildPieces.Shutdown(); PostalMailbox.Shutdown(); EagleModel.Release(); MailboxModel.Release(); ClayResource.Shutdown(); Apothecary.Shutdown(); DrawerFurniture.Shutdown(); ApothecaryArt.Release(); MeadCabinetArt.Release(); DrawerCabinetArt.Release(); ModularShelfArt.Release(); MeadCabinet.Clear(); InventoryTransfers.Admission=null; FurnitureAudio.Release(); FurnitureAssignment.Clear(); ClayModel.Release(); QuartermasterChestModel.Release(); OwlVoice.Release(); LedgerModel.Release(); var settings=GetComponent<ServerSettings>();if(settings)settings.Shutdown(); PatchCleanup.Run(() => harmony?.UnpatchSelf(), ItemStacks.Restore, message => Log.LogError(message)); ChestUi.Dispose(); WorldUnloaded(); }
}
