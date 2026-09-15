using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Hearthkeeper;

[BepInPlugin("MaddCatter.Hearthkeeper", "Hearthkeeper", "0.1.7")]
[BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class HearthkeeperPlugin : BaseUnityPlugin
{
	public const string PluginGuid = "MaddCatter.Hearthkeeper";

	public const string PluginName = "Hearthkeeper";

	public const string PluginVersion = "0.1.7";

	internal static HearthkeeperPlugin Instance;

	internal static ManualLogSource Log;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<bool> GroundStorage;

	internal static ConfigEntry<float> GroundPickupRange;

	internal static ConfigEntry<float> GroundInterval;

	internal static ConfigEntry<int> GroundItemsPerCycle;

	internal static ConfigEntry<float> InventoryStoreRange;

	internal static ConfigEntry<float> CraftRange;

	internal static ConfigEntry<bool> CraftFromContainers;

	internal static ConfigEntry<float> WarehouseSortRange;

	internal static ConfigEntry<bool> AutomaticSortEnabled;

	internal static ConfigEntry<float> AutomaticSortIntervalMinutes;

	internal static ConfigEntry<bool> ProtectHotbar;

	internal static ConfigEntry<bool> LivestockFeeding;

	internal static ConfigEntry<float> FeedRange;

	internal static ConfigEntry<float> FeedInterval;

	internal static ConfigEntry<ReserveMode> DefaultReserve;

	internal static ConfigEntry<int> DefaultCustomReserve;

	internal static ConfigEntry<bool> DefaultManualLock;

	internal static ConfigEntry<bool> DefaultAcceptStorage;

	internal static ConfigEntry<bool> DefaultLivestockFeed;

	internal static ConfigEntry<string> AllowedPrefabs;

	internal static ConfigEntry<string> BlockedPrefabs;

	internal static ConfigEntry<KeyboardShortcut> StoreKey;

	internal static ConfigEntry<KeyboardShortcut> SortWarehouseKey;

	internal static ConfigEntry<KeyboardShortcut> SortInventoryKey;

	internal static ConfigEntry<bool> StackSizesEnabled;

	internal static ConfigEntry<float> StackMultiplier;

	internal static ConfigEntry<int> MaximumStackSize;

	internal static ConfigEntry<string> StackOverrides;

	internal static ConfigEntry<bool> ChestSizingEnabled;

	internal static ConfigEntry<int> MinimumChestColumns;

	internal static ConfigEntry<int> MinimumChestRows;

	internal static ConfigEntry<int> MaximumChestColumns;

	internal static ConfigEntry<int> MaximumChestRows;

	internal static ConfigEntry<bool> ResizeUnlistedChestPrefabs;

	internal static ConfigEntry<string> ChestSizeRules;

	internal static ConfigEntry<InterfaceMode> InterfaceStyle;

	internal static ConfigEntry<float> PlayerButtonsOffsetX;

	internal static ConfigEntry<float> PlayerButtonsOffsetY;

	internal static ConfigEntry<float> ChestButtonsOffsetX;

	internal static ConfigEntry<float> ChestButtonsOffsetY;

	internal static ConfigEntry<float> ChestRulesOffsetX;

	internal static ConfigEntry<float> ChestRulesOffsetY;

	internal static ConfigEntry<bool> ReceivingGlowEnabled;

	internal static ConfigEntry<float> ReceivingGlowDuration;

	internal static ConfigEntry<float> ReceivingGlowIntensity;

	internal static ConfigEntry<float> ReceivingGlowRadius;

	internal static ConfigEntry<string> ReceivingGlowColor;

	internal static ConfigEntry<bool> AutoRefuelEnabled;

	internal static ConfigEntry<bool> AutoRefuelFires;

	internal static ConfigEntry<bool> AutoRefuelProcessingFuel;

	internal static ConfigEntry<bool> AutoRefuelProcessingInputs;

	internal static ConfigEntry<float> AutoRefuelRange;

	internal static ConfigEntry<float> AutoRefuelIntervalSeconds;

	internal static ConfigEntry<int> AutoRefuelMaximumDevicesPerCycle;

	internal static ConfigEntry<int> AutoRefuelMaximumItemsPerDevice;

	internal static ConfigEntry<float> AutoRefuelFireThresholdPercent;

	internal static ConfigEntry<float> AutoRefuelFireTargetPercent;

	internal static ConfigEntry<float> AutoRefuelProcessingFuelThresholdPercent;

	internal static ConfigEntry<float> AutoRefuelProcessingFuelTargetPercent;

	internal static ConfigEntry<float> AutoRefuelProcessingInputThresholdPercent;

	internal static ConfigEntry<float> AutoRefuelProcessingInputTargetPercent;

	internal static ConfigEntry<KeyboardShortcut> AutoRefuelDeviceToggleKey;

	internal static Container OpenContainer;

	private ConfigEntry<bool> _panelsDraggable;

	private ConfigEntry<float> _warehousePanelX;

	private ConfigEntry<float> _warehousePanelY;

	private ConfigEntry<float> _chestPanelX;

	private ConfigEntry<float> _chestPanelY;

	private ConfigEntry<bool> _resetPanelPositions;

	private Harmony _harmony;

	private NativeInterface _nativeInterface;

	private bool _nativeInterfaceFailed;

	private float _nextGroundRun;

	private float _nextFeedRun;

	private float _nextAutomaticSortRun;

	private float _nextAutoRefuelRun;

	private bool _automaticSortScheduled;

	private string _status = string.Empty;

	private float _statusUntil;

	private string _customReserveText = "1";

	private Container _lastPanelContainer;

	private GUIStyle _heading;

	private GUIStyle _small;

	private Rect _warehouseRect;

	private Rect _chestRect;

	private bool _panelPositionsInitialized;

	private bool _warehouseMoved;

	private bool _chestMoved;

	private const float WarehouseWidth = 220f;

	private const float WarehouseHeight = 176f;

	private const float ChestWidth = 252f;

	private const float ChestHeight = 318f;

	private const float DefaultWarehouseX = 0.01f;

	private const float DefaultWarehouseY = 0.98f;

	private const float DefaultChestX = 0.99f;

	private const float DefaultChestY = 0.5f;

	private void Awake()
	{
		Instance = this;
		Log = base.Logger;
		Game.isModded = true;
		BindConfiguration();
		_harmony = new Harmony("MaddCatter.Hearthkeeper");
		_harmony.PatchAll(typeof(HearthkeeperPatches));
		StackSizeService.Apply();
		StackSizesEnabled.SettingChanged += delegate
		{
			StackSizeService.Apply();
		};
		StackMultiplier.SettingChanged += delegate
		{
			StackSizeService.Apply();
		};
		MaximumStackSize.SettingChanged += delegate
		{
			StackSizeService.Apply();
		};
		StackOverrides.SettingChanged += delegate
		{
			StackSizeService.Apply();
		};
		Enabled.SettingChanged += delegate
		{
			_automaticSortScheduled = false;
		};
		AutomaticSortEnabled.SettingChanged += delegate
		{
			_automaticSortScheduled = false;
		};
		AutomaticSortIntervalMinutes.SettingChanged += delegate
		{
			_automaticSortScheduled = false;
		};
		_resetPanelPositions.SettingChanged += delegate
		{
			if (_resetPanelPositions.Value)
			{
				ResetPanelPositions();
			}
		};
		InterfaceStyle.SettingChanged += delegate
		{
			_nativeInterfaceFailed = false;
			if (InterfaceStyle.Value != InterfaceMode.NativeAttached && _nativeInterface != null)
			{
				_nativeInterface.Dispose();
				_nativeInterface = null;
			}
		};
		if (_resetPanelPositions.Value)
		{
			ResetPanelPositions();
		}
		base.Logger.LogInfo("Hearthkeeper 0.1.7 loaded for Valheim 1.0.12.");
	}

	private void OnDestroy()
	{
		if (_nativeInterface != null)
		{
			_nativeInterface.Dispose();
		}
		ChestGlowService.Clear();
		if (_harmony != null)
		{
			_harmony.UnpatchSelf();
		}
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		ChestGlowService.Update();
		if (!Enabled.Value || Player.m_localPlayer == null)
		{
			return;
		}
		if (InterfaceStyle.Value == InterfaceMode.NativeAttached)
		{
			EnsureNativeInterface();
			if (_nativeInterface != null)
			{
				_nativeInterface.UpdateVisibility();
			}
		}
		else if (_nativeInterface != null)
		{
			_nativeInterface.Dispose();
			_nativeInterface = null;
		}
		if (Time.time >= _nextGroundRun)
		{
			_nextGroundRun = Time.time + GroundInterval.Value;
			try
			{
				WarehouseService.ProcessGroundItems();
			}
			catch (Exception arg)
			{
				base.Logger.LogError($"Ground storage cycle failed: {arg}");
			}
		}
		if (Time.time >= _nextFeedRun)
		{
			_nextFeedRun = Time.time + FeedInterval.Value;
			try
			{
				WarehouseService.ProcessFeeding();
			}
			catch (Exception arg2)
			{
				base.Logger.LogError($"Livestock feed cycle failed: {arg2}");
			}
		}
		Player localPlayer = Player.m_localPlayer;
		if (Time.time >= _nextAutoRefuelRun)
		{
			_nextAutoRefuelRun = Time.time + AutoRefuelIntervalSeconds.Value;
			try
			{
				AutoRefuelService.Process(localPlayer);
			}
			catch (Exception arg3)
			{
				base.Logger.LogError($"Automatic refueling cycle failed: {arg3}");
			}
		}
		ProcessAutomaticSort(localPlayer);
		if (!HotkeysBlocked(localPlayer))
		{
			bool flag = InventoryGui.IsVisible();
			if (AutoRefuelDeviceToggleKey.Value.IsDown())
			{
				AutoRefuelService.ToggleHoveredDevice(localPlayer);
			}
			else if (StoreKey.Value.IsDown())
			{
				RunStoreMatching();
			}
			else if (flag && SortWarehouseKey.Value.IsDown())
			{
				RunSortWarehouse();
			}
			else if (flag && SortInventoryKey.Value.IsDown())
			{
				RunSortInventory();
			}
		}
	}

	private void OnGUI()
	{
		if (Enabled.Value && InventoryGui.IsVisible() && !(Player.m_localPlayer == null) && (InterfaceStyle.Value != InterfaceMode.NativeAttached || _nativeInterfaceFailed))
		{
			EnsureStyles();
			EnsurePanelPositions();
			DrawWarehousePanel();
			if (OpenContainer != null)
			{
				DrawContainerPanel(OpenContainer);
			}
		}
	}

	private void DrawWarehousePanel()
	{
		Rect warehouseRect = _warehouseRect;
		_warehouseRect = GUI.Window(487201, ClampToScreen(_warehouseRect), DrawWarehouseWindow, string.Empty);
		SavePanelPosition(warehouseRect, _warehouseRect, _warehousePanelX, _warehousePanelY, ref _warehouseMoved);
	}

	private void DrawContainerPanel(Container container)
	{
		if (_lastPanelContainer != container)
		{
			_lastPanelContainer = container;
			_customReserveText = ContainerRegistry.GetSettings(container).CustomReserve.ToString();
		}
		Rect chestRect = _chestRect;
		_chestRect = GUI.Window(487202, ClampToScreen(_chestRect), DrawChestWindow, string.Empty);
		SavePanelPosition(chestRect, _chestRect, _chestPanelX, _chestPanelY, ref _chestMoved);
	}

	private void DrawWarehouseWindow(int windowId)
	{
		GUI.Label(new Rect(12f, 8f, 196f, 25f), "HEARTHKEEPER", _heading);
		if (GUI.Button(new Rect(12f, 38f, 196f, 28f), $"Store Matching  [{StoreKey.Value.MainKey}]"))
		{
			RunStoreMatching();
		}
		if (GUI.Button(new Rect(12f, 70f, 196f, 28f), $"Consolidate Chests  [{SortWarehouseKey.Value.MainKey}]"))
		{
			RunSortWarehouse();
		}
		if (GUI.Button(new Rect(12f, 102f, 196f, 28f), $"Sort Inventory  [{SortInventoryKey.Value.MainKey}]"))
		{
			RunSortInventory();
		}
		string text = ((Time.unscaledTime < _statusUntil) ? _status : "Ready — drag this title bar to move");
		GUI.Label(new Rect(12f, 136f, 196f, 34f), text, _small);
		if (_panelsDraggable.Value)
		{
			GUI.DragWindow(new Rect(0f, 0f, 220f, 34f));
		}
	}

	private void DrawChestWindow(int windowId)
	{
		Container openContainer = OpenContainer;
		if (!(openContainer == null))
		{
			ContainerSettings settings = ContainerRegistry.GetSettings(openContainer);
			GUI.Label(new Rect(12f, 8f, 228f, 24f), "CHEST RULES", _heading);
			GUI.Label(new Rect(12f, 36f, 228f, 22f), ContainerRegistry.PrefabName(openContainer), _small);
			if (GUI.Button(new Rect(12f, 64f, 228f, 28f), "Reserve: " + ReserveLabel(settings.Reserve)))
			{
				settings.Reserve = (ReserveMode)((int)(settings.Reserve + 1) % 4);
				Save(openContainer, settings);
			}
			GUI.Label(new Rect(12f, 98f, 100f, 24f), "Custom amount", _small);
			_customReserveText = GUI.TextField(new Rect(118f, 96f, 122f, 24f), _customReserveText, 6);
			if (int.TryParse(_customReserveText, out var result) && result >= 0 && result != settings.CustomReserve)
			{
				settings.CustomReserve = result;
				Save(openContainer, settings);
			}
			if (GUI.Button(new Rect(12f, 128f, 228f, 28f), "Manual reserve lock: " + OnOff(settings.ManualLock)))
			{
				settings.ManualLock = !settings.ManualLock;
				Save(openContainer, settings);
			}
			if (GUI.Button(new Rect(12f, 160f, 228f, 28f), "Accept automatic storage: " + OnOff(settings.AcceptStorage)))
			{
				settings.AcceptStorage = !settings.AcceptStorage;
				Save(openContainer, settings);
			}
			if (GUI.Button(new Rect(12f, 192f, 228f, 28f), "Livestock feed: " + OnOff(settings.LivestockFeed)))
			{
				settings.LivestockFeed = !settings.LivestockFeed;
				Save(openContainer, settings);
			}
			if (GUI.Button(new Rect(12f, 232f, 228f, 28f), "Store All"))
			{
				RunStoreAll();
			}
			if (GUI.Button(new Rect(12f, 272f, 228f, 28f), "Sort This Chest"))
			{
				WarehouseService.SortInventory(ContainerRegistry.SafeInventory(openContainer), preserveFirstRow: false);
				SetStatus("Chest sorted");
			}
			if (_panelsDraggable.Value)
			{
				GUI.DragWindow(new Rect(0f, 0f, 252f, 34f));
			}
		}
	}

	private void EnsurePanelPositions()
	{
		if (!_panelPositionsInitialized)
		{
			_warehouseRect = RectFromNormalized(_warehousePanelX.Value, _warehousePanelY.Value, 220f, 176f);
			_chestRect = RectFromNormalized(_chestPanelX.Value, _chestPanelY.Value, 252f, 318f);
			_panelPositionsInitialized = true;
		}
	}

	private static Rect RectFromNormalized(float x, float y, float width, float height)
	{
		float num = Math.Max(0f, (float)Screen.width - width);
		float num2 = Math.Max(0f, (float)Screen.height - height);
		return new Rect(Mathf.Clamp01(x) * num, Mathf.Clamp01(y) * num2, width, height);
	}

	private static Rect ClampToScreen(Rect rect)
	{
		rect.x = Mathf.Clamp(rect.x, 0f, Math.Max(0f, (float)Screen.width - rect.width));
		rect.y = Mathf.Clamp(rect.y, 0f, Math.Max(0f, (float)Screen.height - rect.height));
		return rect;
	}

	private static void SavePanelPosition(Rect before, Rect after, ConfigEntry<float> xEntry, ConfigEntry<float> yEntry, ref bool moved)
	{
		if ((before.position - after.position).sqrMagnitude >= 0.01f)
		{
			moved = true;
		}
		if (Event.current.type == EventType.MouseUp && moved)
		{
			float num = Math.Max(1f, (float)Screen.width - after.width);
			float num2 = Math.Max(1f, (float)Screen.height - after.height);
			xEntry.Value = Mathf.Clamp01(after.x / num);
			yEntry.Value = Mathf.Clamp01(after.y / num2);
			moved = false;
		}
	}

	private void ResetPanelPositions()
	{
		_warehousePanelX.Value = 0.01f;
		_warehousePanelY.Value = 0.98f;
		_chestPanelX.Value = 0.99f;
		_chestPanelY.Value = 0.5f;
		_resetPanelPositions.Value = false;
		_panelPositionsInitialized = false;
	}

	internal void RunStoreMatching()
	{
		try
		{
			int num = WarehouseService.StoreMatchingFromPlayer(Player.m_localPlayer);
			SetStatus($"Stored {num} item(s)");
			if (_nativeInterface != null)
			{
				_nativeInterface.ShowActionResult(NativeAction.StoreMatching, $"Stored {num}");
			}
		}
		catch (Exception ex)
		{
			ReportError("Store Matching failed", ex);
		}
	}

	internal void RunStoreAll()
	{
		try
		{
			int num = WarehouseService.StoreAllInOpenChest(Player.m_localPlayer, OpenContainer);
			SetStatus($"Stored {num} item(s)");
			if (_nativeInterface != null)
			{
				_nativeInterface.ShowActionResult(NativeAction.StoreAll, $"Stored {num}");
			}
		}
		catch (Exception ex)
		{
			ReportError("Store All failed", ex);
		}
	}

	internal void RunSortWarehouse()
	{
		try
		{
			int num = WarehouseService.SortWarehouse(Player.m_localPlayer);
			SetStatus($"Moved {num} item(s)");
			if (_nativeInterface != null)
			{
				_nativeInterface.ShowActionResult(NativeAction.Consolidate, $"Moved {num}");
			}
		}
		catch (Exception ex)
		{
			ReportError("Consolidate Chests failed", ex);
		}
	}

	internal void RunSortInventory()
	{
		try
		{
			WarehouseService.SortInventory(Player.m_localPlayer.GetInventory(), ProtectHotbar.Value, preserveEquipped: true);
			SetStatus("Inventory sorted");
			if (_nativeInterface != null)
			{
				_nativeInterface.ShowActionResult(NativeAction.SortInventory, "Inventory Sorted");
			}
		}
		catch (Exception ex)
		{
			ReportError("Inventory sort failed", ex);
		}
	}

	private void ProcessAutomaticSort(Player player)
	{
		if (!AutomaticSortEnabled.Value)
		{
			_automaticSortScheduled = false;
		}
		else if (!_automaticSortScheduled)
		{
			ScheduleNextAutomaticSort();
		}
		else
		{
			if (Time.time < _nextAutomaticSortRun)
			{
				return;
			}
			if (!InventoryGui.IsVisible() && !HotkeysBlocked(player))
			{
				ScheduleNextAutomaticSort();
				try
				{
					int arrangedChests;
					int num = WarehouseService.AutomaticSortWarehouse(player, out arrangedChests);
					if (num > 0 || arrangedChests > 0)
					{
						base.Logger.LogInfo($"Automatic sort moved {num} item(s) and arranged {arrangedChests} chest(s).");
					}
					return;
				}
				catch (Exception ex)
				{
					ReportError("Automatic sorting failed", ex);
					return;
				}
			}
			_nextAutomaticSortRun = Time.time + 5f;
		}
	}

	private void ScheduleNextAutomaticSort()
	{
		_nextAutomaticSortRun = Time.time + Mathf.Max(0.25f, AutomaticSortIntervalMinutes.Value) * 60f;
		_automaticSortScheduled = true;
	}

	internal void Save(Container container, ContainerSettings settings)
	{
		if (!ContainerRegistry.SaveSettings(container, settings))
		{
			SetStatus("Could not save chest rules");
		}
	}

	private void ReportError(string message, Exception ex)
	{
		base.Logger.LogError(message + ": " + ex);
		SetStatus(message + "; see log");
	}

	private void SetStatus(string status)
	{
		_status = status;
		_statusUntil = Time.unscaledTime + 4f;
	}

	private void EnsureStyles()
	{
		if (_heading == null)
		{
			_heading = new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				fontSize = 15
			};
			_small = new GUIStyle(GUI.skin.label)
			{
				wordWrap = true,
				fontSize = 12
			};
		}
	}

	private static string ReserveLabel(ReserveMode mode)
	{
		return mode switch
		{
			ReserveMode.OneItem => "1 item", 
			ReserveMode.OneStack => "1 stack", 
			ReserveMode.Custom => "custom", 
			_ => "off", 
		};
	}

	private static string OnOff(bool value)
	{
		if (!value)
		{
			return "OFF";
		}
		return "ON";
	}

	internal static string ReserveLabelForUi(ReserveMode mode)
	{
		return ReserveLabel(mode);
	}

	private static bool HotkeysBlocked(Player player)
	{
		if (!(player == null) && !player.IsDead() && !player.InCutscene() && !player.IsTeleporting() && !TextInput.IsVisible() && !Console.IsVisible() && !Menu.IsVisible())
		{
			if (Chat.instance != null)
			{
				return Chat.instance.HasFocus();
			}
			return false;
		}
		return true;
	}

	private void EnsureNativeInterface()
	{
		if (InterfaceStyle.Value != InterfaceMode.NativeAttached || _nativeInterface != null || _nativeInterfaceFailed)
		{
			return;
		}
		InventoryGui instance = InventoryGui.instance;
		if (instance == null)
		{
			return;
		}
		try
		{
			_nativeInterface = new NativeInterface(this, instance);
			base.Logger.LogInfo("Attached Hearthkeeper controls to the Valheim inventory interface.");
		}
		catch (Exception ex)
		{
			_nativeInterfaceFailed = true;
			base.Logger.LogError("Could not attach the native Hearthkeeper interface; using the floating fallback. " + ex);
		}
	}

	internal static void AttachInterface(InventoryGui gui)
	{
		if (!(Instance == null) && !(gui == null))
		{
			if (Instance._nativeInterface != null)
			{
				Instance._nativeInterface.Dispose();
			}
			Instance._nativeInterface = null;
			Instance._nativeInterfaceFailed = false;
			Instance.EnsureNativeInterface();
		}
	}

	internal static bool IsAllowedPrefab(string prefab)
	{
		HashSet<string> hashSet = ParseNames(AllowedPrefabs.Value);
		if (ParseNames(BlockedPrefabs.Value).Contains(prefab))
		{
			return false;
		}
		if (hashSet.Count != 0)
		{
			return hashSet.Contains(prefab);
		}
		return true;
	}

	private static HashSet<string> ParseNames(string raw)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(raw))
		{
			return hashSet;
		}
		string[] array = raw.Split(new char[3] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			hashSet.Add(array[i].Trim());
		}
		return hashSet;
	}

	private void BindConfiguration()
	{
		Enabled = base.Config.Bind("01 General", "Enabled", defaultValue: true, "Master switch for Hearthkeeper.");
		GroundStorage = base.Config.Bind("02 Ground Storage", "Enabled", defaultValue: true, "Move loaded ground drops into eligible matching chests.");
		GroundPickupRange = base.Config.Bind("02 Ground Storage", "Range", 20f, Range(1f, 100f, "Maximum distance from a ground item to a matching chest."));
		GroundInterval = base.Config.Bind("02 Ground Storage", "IntervalSeconds", 2f, Range(0.5f, 30f, "Seconds between ground storage passes."));
		GroundItemsPerCycle = base.Config.Bind("02 Ground Storage", "MaximumItemsPerCycle", 50, new ConfigDescription("Performance budget for each pass.", new AcceptableValueRange<int>(1, 500)));
		InventoryStoreRange = base.Config.Bind("03 Inventory Storage", "Range", 20f, Range(1f, 100f, "Store Matching search radius around the player."));
		ProtectHotbar = base.Config.Bind("03 Inventory Storage", "ProtectHotbar", defaultValue: true, "Do not auto-store or rearrange the first inventory row.");
		CraftFromContainers = base.Config.Bind("04 Crafting and Building", "Enabled", defaultValue: true, "Count and consume eligible nearby chest materials for recipes and pieces.");
		CraftRange = base.Config.Bind("04 Crafting and Building", "Range", 20f, Range(1f, 100f, "Crafting and building container radius."));
		WarehouseSortRange = base.Config.Bind("05 Sorting", "Range", 20f, Range(1f, 100f, "Warehouse redistribution radius."));
		AutomaticSortEnabled = base.Config.Bind("05 Sorting", "AutomaticCycleEnabled", defaultValue: false, "Periodically consolidate matching items between nearby chests and arrange each chest. The cycle waits while inventory or chest interfaces are open.");
		AutomaticSortIntervalMinutes = base.Config.Bind("05 Sorting", "AutomaticCycleMinutes", 5f, Range(0.25f, 120f, "Minutes between automatic warehouse sorting passes."));
		LivestockFeeding = base.Config.Bind("06 Livestock Feed", "Enabled", defaultValue: true, "Supply hungry vanilla-style tameables from feed-enabled chests.");
		FeedRange = base.Config.Bind("06 Livestock Feed", "Range", 20f, Range(1f, 100f, "Maximum chest-to-creature feeding distance."));
		FeedInterval = base.Config.Bind("06 Livestock Feed", "IntervalSeconds", 5f, Range(1f, 60f, "Seconds between feeding passes."));
		DefaultReserve = base.Config.Bind("07 New Chest Defaults", "ReserveMode", ReserveMode.OneItem, "Off, OneItem, OneStack, or Custom.");
		DefaultCustomReserve = base.Config.Bind("07 New Chest Defaults", "CustomReserve", 1, new ConfigDescription("Quantity used by Custom reserve mode.", new AcceptableValueRange<int>(0, 100000)));
		DefaultManualLock = base.Config.Bind("07 New Chest Defaults", "ManualReserveLock", defaultValue: false, "Prevent manual removal below the chest reserve.");
		DefaultAcceptStorage = base.Config.Bind("07 New Chest Defaults", "AcceptAutomaticStorage", defaultValue: true, "Allow automated deposits into new/unconfigured chests.");
		DefaultLivestockFeed = base.Config.Bind("07 New Chest Defaults", "LivestockFeed", defaultValue: false, "Allow new/unconfigured chests to supply livestock.");
		ChestSizingEnabled = base.Config.Bind("08 Chest Sizes", "Enabled", defaultValue: true, "Resize listed player storage chests when they load. A world reload is required after changes.");
		MinimumChestColumns = base.Config.Bind("08 Chest Sizes", "MinimumColumns", 7, new ConfigDescription("Smallest permitted width for a resized chest.", new AcceptableValueRange<int>(1, 20)));
		MinimumChestRows = base.Config.Bind("08 Chest Sizes", "MinimumRows", 4, new ConfigDescription("Smallest permitted height. Set to 5 for a 7x5 smallest chest.", new AcceptableValueRange<int>(1, 20)));
		MaximumChestColumns = base.Config.Bind("08 Chest Sizes", "MaximumColumns", 12, new ConfigDescription("Maximum width accepted from a size rule.", new AcceptableValueRange<int>(1, 20)));
		MaximumChestRows = base.Config.Bind("08 Chest Sizes", "MaximumRows", 10, new ConfigDescription("Maximum height accepted from a size rule.", new AcceptableValueRange<int>(1, 20)));
		ResizeUnlistedChestPrefabs = base.Config.Bind("08 Chest Sizes", "ResizeUnlistedChestPrefabs", defaultValue: true, "Apply the minimum to unlisted build pieces whose prefab name contains 'chest'. Other container types remain untouched.");
		ChestSizeRules = base.Config.Bind("08 Chest Sizes", "PrefabSizes", "piece_chest_wood=7x4,piece_chest_private=7x5,piece_chest=9x6,piece_chest_blackmetal=10x8,piece_chest_ashwood=10x8", "Comma-separated prefab=size rules. Sizes only grow automatically; they never silently shrink a previously enlarged chest.");
		AllowedPrefabs = base.Config.Bind("09 Compatibility", "AllowedContainerPrefabs", string.Empty, "Optional comma-separated allow list. Empty permits all standard Container prefabs.");
		BlockedPrefabs = base.Config.Bind("09 Compatibility", "BlockedContainerPrefabs", string.Empty, "Comma-separated prefab names Hearthkeeper must ignore.");
		StoreKey = base.Config.Bind("10 Hotkeys", "StoreMatching", new KeyboardShortcut(KeyCode.F6), "Store matching player items during normal gameplay or while inventory is open.");
		SortWarehouseKey = base.Config.Bind("10 Hotkeys", "SortWarehouse", new KeyboardShortcut(KeyCode.F7), "Consolidate matching items while inventory is open.");
		SortInventoryKey = base.Config.Bind("10 Hotkeys", "SortInventory", new KeyboardShortcut(KeyCode.F8), "Sort player inventory while inventory is open.");
		AutoRefuelDeviceToggleKey = base.Config.Bind("10 Hotkeys", "ToggleHoveredDeviceAutoRefuel", new KeyboardShortcut(KeyCode.F9), "Toggle automatic refueling for the fire, torch, or processing device under the crosshair.");
		InterfaceStyle = base.Config.Bind("11 Interface", "Style", InterfaceMode.NativeAttached, "NativeAttached uses Valheim-styled controls on the inventory and chest frames. FloatingLegacy keeps the movable gray test panels.");
		PlayerButtonsOffsetX = base.Config.Bind("11 Interface", "PlayerButtonsOffsetX", 0f, Range(-500f, 500f, "Horizontal offset for the attached player inventory buttons."));
		PlayerButtonsOffsetY = base.Config.Bind("11 Interface", "PlayerButtonsOffsetY", -8f, Range(-500f, 500f, "Vertical offset for the attached player inventory buttons."));
		ChestButtonsOffsetX = base.Config.Bind("11 Interface", "ChestButtonsOffsetX", 0f, Range(-500f, 500f, "Horizontal offset for Consolidate Chests and Chest Rules."));
		ChestButtonsOffsetY = base.Config.Bind("11 Interface", "ChestButtonsOffsetY", -8f, Range(-500f, 500f, "Vertical offset for Consolidate Chests and Chest Rules."));
		ChestRulesOffsetX = base.Config.Bind("11 Interface", "ChestRulesPanelOffsetX", 0f, Range(-800f, 800f, "Horizontal offset for the attached Chest Rules panel."));
		ChestRulesOffsetY = base.Config.Bind("11 Interface", "ChestRulesPanelOffsetY", 0f, Range(-800f, 800f, "Vertical offset for the attached Chest Rules panel."));
		_panelsDraggable = base.Config.Bind("11 Interface", "PanelsDraggable", defaultValue: true, "Drag either panel by its title area. Positions are saved as screen-relative values.");
		_warehousePanelX = base.Config.Bind("11 Interface", "WarehousePanelX", 0.01f, Range(0f, 1f, "Horizontal Warehouse panel position: 0 is left, 1 is right."));
		_warehousePanelY = base.Config.Bind("11 Interface", "WarehousePanelY", 0.98f, Range(0f, 1f, "Vertical Warehouse panel position: 0 is top, 1 is bottom."));
		_chestPanelX = base.Config.Bind("11 Interface", "ChestRulesPanelX", 0.99f, Range(0f, 1f, "Horizontal Chest Rules position: 0 is left, 1 is right."));
		_chestPanelY = base.Config.Bind("11 Interface", "ChestRulesPanelY", 0.5f, Range(0f, 1f, "Vertical Chest Rules position: 0 is top, 1 is bottom."));
		_resetPanelPositions = base.Config.Bind("11 Interface", "ResetPanelPositions", defaultValue: false, "Set true to restore both default panel positions; Hearthkeeper changes it back to false.");
		ReceivingGlowEnabled = base.Config.Bind("12 Chest Activity Glow", "Enabled", defaultValue: true, "Briefly pulse a chest when Hearthkeeper automatically deposits items into it.");
		ReceivingGlowDuration = base.Config.Bind("12 Chest Activity Glow", "DurationSeconds", 3f, Range(0.25f, 10f, "How long a receiving chest remains highlighted. Repeated deposits refresh the timer."));
		ReceivingGlowIntensity = base.Config.Bind("12 Chest Activity Glow", "Intensity", 2.2f, Range(0.1f, 8f, "Brightness of the pulsing chest light and emissive highlight."));
		ReceivingGlowRadius = base.Config.Bind("12 Chest Activity Glow", "LightRadius", 3.5f, Range(0.5f, 12f, "World-space radius of the temporary light around a receiving chest."));
		ReceivingGlowColor = base.Config.Bind("12 Chest Activity Glow", "Color", "#FFD27A", "HTML color for the receiving-chest glow, such as #FFD27A. Pulsing brightness keeps the signal readable without relying only on hue.");
		StackSizesEnabled = base.Config.Bind("12 Stack Sizes - Experimental", "Enabled", defaultValue: false, "Optional global stack resizing. Leave disabled while testing core storage features.");
		StackMultiplier = base.Config.Bind("12 Stack Sizes - Experimental", "Multiplier", 1f, Range(0.1f, 100f, "Multiplier applied to each item's native stack size."));
		MaximumStackSize = base.Config.Bind("12 Stack Sizes - Experimental", "Maximum", 10000, new ConfigDescription("Hard safety cap.", new AcceptableValueRange<int>(1, 100000)));
		StackOverrides = base.Config.Bind("12 Stack Sizes - Experimental", "PerItemOverrides", string.Empty, "Comma-separated prefab/shared-name values, for example Wood=200,Stone=150.");
		AutoRefuelEnabled = base.Config.Bind("13 Automatic Refueling", "Enabled", defaultValue: false, "Master switch for automatic refueling and processing input loading. Disabled by default.");
		AutoRefuelFires = base.Config.Bind("13 Automatic Refueling", "FiresAndTorches", defaultValue: true, "Refill player-built fires, hearths, torches, and other standard Fireplace devices.");
		AutoRefuelProcessingFuel = base.Config.Bind("13 Automatic Refueling", "ProcessingFuel", defaultValue: true, "Refill fuel in standard Smelter devices, including smelters, blast furnaces, and eitr refineries.");
		AutoRefuelProcessingInputs = base.Config.Bind("13 Automatic Refueling", "ProcessingInputs", defaultValue: false, "Load processable materials into standard Smelter devices, including kilns, furnaces, windmills, and spinning wheels.");
		AutoRefuelRange = base.Config.Bind("13 Automatic Refueling", "ChestRange", 20f, Range(1f, 100f, "Maximum distance from a device to eligible supply chests."));
		AutoRefuelIntervalSeconds = base.Config.Bind("13 Automatic Refueling", "IntervalSeconds", 10f, Range(1f, 120f, "Seconds between automatic refueling passes."));
		AutoRefuelMaximumDevicesPerCycle = base.Config.Bind("13 Automatic Refueling", "MaximumDevicesPerCycle", 50, new ConfigDescription("Performance limit applied separately to fires and processing devices on each pass.", new AcceptableValueRange<int>(1, 500)));
		AutoRefuelMaximumItemsPerDevice = base.Config.Bind("13 Automatic Refueling", "MaximumItemsPerDevicePerCycle", 10, new ConfigDescription("Maximum combined fuel and input items transferred into one device per pass.", new AcceptableValueRange<int>(1, 100)));
		AutoRefuelFireThresholdPercent = base.Config.Bind("13 Automatic Refueling", "FireRefillBelowPercent", 25f, Range(0f, 100f, "Begin refilling a fire or torch when fuel is at or below this percentage."));
		AutoRefuelFireTargetPercent = base.Config.Bind("13 Automatic Refueling", "FireRefillToPercent", 100f, Range(0f, 100f, "Target fuel percentage for fires and torches."));
		AutoRefuelProcessingFuelThresholdPercent = base.Config.Bind("13 Automatic Refueling", "ProcessingFuelRefillBelowPercent", 25f, Range(0f, 100f, "Begin refilling processing fuel at or below this percentage."));
		AutoRefuelProcessingFuelTargetPercent = base.Config.Bind("13 Automatic Refueling", "ProcessingFuelRefillToPercent", 100f, Range(0f, 100f, "Target processing-fuel percentage."));
		AutoRefuelProcessingInputThresholdPercent = base.Config.Bind("13 Automatic Refueling", "ProcessingInputRefillBelowPercent", 25f, Range(0f, 100f, "Begin loading processing materials when the input queue is at or below this percentage."));
		AutoRefuelProcessingInputTargetPercent = base.Config.Bind("13 Automatic Refueling", "ProcessingInputRefillToPercent", 100f, Range(0f, 100f, "Target processing-input queue percentage."));
	}

	private static ConfigDescription Range(float min, float max, string description)
	{
		return new ConfigDescription(description, new AcceptableValueRange<float>(min, max));
	}
}
