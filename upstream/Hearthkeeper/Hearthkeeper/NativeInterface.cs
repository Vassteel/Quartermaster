using System;
using System.Reflection;
using UnityEngine;

namespace Hearthkeeper;

internal sealed class NativeInterface : IDisposable
{
	private readonly HearthkeeperPlugin _plugin;

	private readonly InventoryGui _gui;

	private readonly Component _buttonTemplate;

	private readonly GameObject _playerRoot;

	private readonly GameObject _chestDock;

	private readonly GameObject _rulesPanel;

	private readonly Component _storeButton;

	private readonly Component _sortInventoryButton;

	private readonly Component _storeAllButton;

	private readonly Component _consolidateButton;

	private readonly Component _rulesButton;

	private readonly Component _reserveButton;

	private readonly Component _customValueButton;

	private readonly Component _manualButton;

	private readonly Component _acceptButton;

	private readonly Component _feedButton;

	private bool _rulesOpen;

	private Container _lastContainer;

	private NativeAction _resultAction;

	private string _resultText;

	private float _resultUntil;

	internal NativeInterface(HearthkeeperPlugin plugin, InventoryGui gui)
	{
		_plugin = plugin ?? throw new ArgumentNullException("plugin");
		_gui = gui ?? throw new ArgumentNullException("gui");
		FieldInfo field = typeof(InventoryGui).GetField("m_stackAllButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_buttonTemplate = ((field == null) ? null : (field.GetValue(_gui) as Component));
		if (_buttonTemplate == null || _gui.m_container == null || _gui.m_player == null)
		{
			throw new InvalidOperationException("Valheim inventory button templates are not available.");
		}
		_playerRoot = CreateRoot("Hearthkeeper_PlayerActions", (_gui.m_playerGrid != null) ? (_gui.m_playerGrid.transform as RectTransform) : _gui.m_player, new Vector2(380f, 42f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(HearthkeeperPlugin.PlayerButtonsOffsetX.Value, HearthkeeperPlugin.PlayerButtonsOffsetY.Value));
		_storeButton = CreateButton(_playerRoot.transform, "StoreMatching", "Store Matching [" + HearthkeeperPlugin.StoreKey.Value.MainKey.ToString() + "]", new Vector2(0f, 0f), new Vector2(184f, 36f), _plugin.RunStoreMatching);
		_sortInventoryButton = CreateButton(_playerRoot.transform, "SortInventory", "Sort Inventory [" + HearthkeeperPlugin.SortInventoryKey.Value.MainKey.ToString() + "]", new Vector2(190f, 0f), new Vector2(184f, 36f), _plugin.RunSortInventory);
		_chestDock = CreateRoot("Hearthkeeper_ChestActions", _gui.m_container, new Vector2(190f, 122f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f + HearthkeeperPlugin.ChestButtonsOffsetX.Value, HearthkeeperPlugin.ChestButtonsOffsetY.Value));
		_storeAllButton = CreateButton(_chestDock.transform, "StoreAll", "Store All", Vector2.zero, new Vector2(184f, 36f), _plugin.RunStoreAll);
		_consolidateButton = CreateButton(_chestDock.transform, "Consolidate", "Consolidate Chests [" + HearthkeeperPlugin.SortWarehouseKey.Value.MainKey.ToString() + "]", new Vector2(0f, -40f), new Vector2(184f, 36f), _plugin.RunSortWarehouse);
		_rulesButton = CreateButton(_chestDock.transform, "Rules", "Chest Rules", new Vector2(0f, -80f), new Vector2(184f, 36f), ToggleRules);
		_rulesPanel = CreateRoot("Hearthkeeper_ChestRules", _gui.m_container, new Vector2(286f, 286f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f + HearthkeeperPlugin.ChestRulesOffsetX.Value, -128f + HearthkeeperPlugin.ChestRulesOffsetY.Value));
		AddNativeBackground(_rulesPanel, _gui.m_container);
		CreateButton(_rulesPanel.transform, "Heading", "CHEST RULES", new Vector2(8f, -8f), new Vector2(270f, 32f), null);
		_reserveButton = CreateButton(_rulesPanel.transform, "Reserve", "Reserve", new Vector2(8f, -44f), new Vector2(270f, 34f), CycleReserve);
		CreateButton(_rulesPanel.transform, "CustomMinus", "−", new Vector2(8f, -82f), new Vector2(48f, 34f), delegate
		{
			ChangeCustomReserve(-AmountStep());
		});
		_customValueButton = CreateButton(_rulesPanel.transform, "CustomValue", "Custom: 1", new Vector2(60f, -82f), new Vector2(166f, 34f), null);
		CreateButton(_rulesPanel.transform, "CustomPlus", "+", new Vector2(230f, -82f), new Vector2(48f, 34f), delegate
		{
			ChangeCustomReserve(AmountStep());
		});
		_manualButton = CreateButton(_rulesPanel.transform, "ManualLock", "Manual Reserve Lock", new Vector2(8f, -120f), new Vector2(270f, 34f), ToggleManualLock);
		_acceptButton = CreateButton(_rulesPanel.transform, "AcceptStorage", "Accept Automatic Storage", new Vector2(8f, -158f), new Vector2(270f, 34f), ToggleAcceptStorage);
		_feedButton = CreateButton(_rulesPanel.transform, "LivestockFeed", "Livestock Feed", new Vector2(8f, -196f), new Vector2(270f, 34f), ToggleLivestockFeed);
		CreateButton(_rulesPanel.transform, "SortChest", "Sort This Chest", new Vector2(8f, -238f), new Vector2(270f, 36f), SortThisChest);
		_rulesPanel.SetActive(value: false);
		UpdateVisibility();
	}

	internal void UpdateVisibility()
	{
		RectTransform rectTransform = ((_playerRoot == null) ? null : (_playerRoot.transform as RectTransform));
		RectTransform rectTransform2 = ((_chestDock == null) ? null : (_chestDock.transform as RectTransform));
		RectTransform rectTransform3 = ((_rulesPanel == null) ? null : (_rulesPanel.transform as RectTransform));
		if (rectTransform != null)
		{
			rectTransform.anchoredPosition = new Vector2(HearthkeeperPlugin.PlayerButtonsOffsetX.Value, HearthkeeperPlugin.PlayerButtonsOffsetY.Value);
		}
		if (rectTransform2 != null)
		{
			rectTransform2.anchoredPosition = new Vector2(12f + HearthkeeperPlugin.ChestButtonsOffsetX.Value, HearthkeeperPlugin.ChestButtonsOffsetY.Value);
		}
		if (rectTransform3 != null)
		{
			rectTransform3.anchoredPosition = new Vector2(12f + HearthkeeperPlugin.ChestRulesOffsetX.Value, -128f + HearthkeeperPlugin.ChestRulesOffsetY.Value);
		}
		bool flag = HearthkeeperPlugin.Enabled.Value && InventoryGui.IsVisible() && Player.m_localPlayer != null;
		if (_playerRoot != null)
		{
			_playerRoot.SetActive(flag);
		}
		bool flag2 = flag && HearthkeeperPlugin.OpenContainer != null && _gui.m_container.gameObject.activeInHierarchy;
		if (_chestDock != null)
		{
			_chestDock.SetActive(flag2);
		}
		if (_rulesPanel != null)
		{
			_rulesPanel.SetActive(flag2 && _rulesOpen);
		}
		if (!flag2)
		{
			_lastContainer = null;
		}
		if (flag2 && (_rulesOpen || _lastContainer != HearthkeeperPlugin.OpenContainer))
		{
			RefreshRules();
		}
		RefreshActionLabels();
	}

	internal void ShowActionResult(NativeAction action, string text)
	{
		_resultAction = action;
		_resultText = text;
		_resultUntil = Time.unscaledTime + 2.5f;
		RefreshActionLabels();
	}

	public void Dispose()
	{
		Destroy(_playerRoot);
		Destroy(_chestDock);
		Destroy(_rulesPanel);
	}

	private void ToggleRules()
	{
		_rulesOpen = !_rulesOpen;
		if (_rulesPanel != null)
		{
			_rulesPanel.SetActive(_rulesOpen && HearthkeeperPlugin.OpenContainer != null);
		}
		RefreshRules();
	}

	private void CycleReserve()
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (!(openContainer == null))
		{
			ContainerSettings settings = ContainerRegistry.GetSettings(openContainer);
			settings.Reserve = (ReserveMode)((int)(settings.Reserve + 1) % 4);
			_plugin.Save(openContainer, settings);
			RefreshRules();
		}
	}

	private void ChangeCustomReserve(int delta)
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (!(openContainer == null))
		{
			ContainerSettings settings = ContainerRegistry.GetSettings(openContainer);
			settings.CustomReserve = Mathf.Clamp(settings.CustomReserve + delta, 0, 100000);
			_plugin.Save(openContainer, settings);
			RefreshRules();
		}
	}

	private void ToggleManualLock()
	{
		ChangeSettings(delegate(ContainerSettings settings)
		{
			settings.ManualLock = !settings.ManualLock;
		});
	}

	private void ToggleAcceptStorage()
	{
		ChangeSettings(delegate(ContainerSettings settings)
		{
			settings.AcceptStorage = !settings.AcceptStorage;
		});
	}

	private void ToggleLivestockFeed()
	{
		ChangeSettings(delegate(ContainerSettings settings)
		{
			settings.LivestockFeed = !settings.LivestockFeed;
		});
	}

	private void SortThisChest()
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (!(openContainer == null))
		{
			WarehouseService.SortInventory(ContainerRegistry.SafeInventory(openContainer), preserveFirstRow: false);
			SetText(_rulesButton.gameObject, "Chest Sorted");
			_resultAction = NativeAction.Consolidate;
			_resultText = null;
			_resultUntil = Time.unscaledTime + 1.5f;
		}
	}

	private void ChangeSettings(Action<ContainerSettings> change)
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (!(openContainer == null))
		{
			ContainerSettings settings = ContainerRegistry.GetSettings(openContainer);
			change(settings);
			_plugin.Save(openContainer, settings);
			RefreshRules();
		}
	}

	private void RefreshRules()
	{
		Container openContainer = HearthkeeperPlugin.OpenContainer;
		if (!(openContainer == null))
		{
			_lastContainer = openContainer;
			ContainerSettings settings = ContainerRegistry.GetSettings(openContainer);
			SetText(_reserveButton.gameObject, "Reserve: " + HearthkeeperPlugin.ReserveLabelForUi(settings.Reserve));
			SetText(_customValueButton.gameObject, "Custom: " + settings.CustomReserve);
			SetText(_manualButton.gameObject, "Manual Reserve Lock: " + OnOff(settings.ManualLock));
			SetText(_acceptButton.gameObject, "Accept Automatic Storage: " + OnOff(settings.AcceptStorage));
			SetText(_feedButton.gameObject, "Livestock Feed: " + OnOff(settings.LivestockFeed));
		}
	}

	private void RefreshActionLabels()
	{
		bool flag = Time.unscaledTime < _resultUntil && !string.IsNullOrEmpty(_resultText);
		SetText(_storeButton.gameObject, (flag && _resultAction == NativeAction.StoreMatching) ? _resultText : ("Store Matching [" + HearthkeeperPlugin.StoreKey.Value.MainKey.ToString() + "]"));
		SetText(_sortInventoryButton.gameObject, (flag && _resultAction == NativeAction.SortInventory) ? _resultText : ("Sort Inventory [" + HearthkeeperPlugin.SortInventoryKey.Value.MainKey.ToString() + "]"));
		SetText(_storeAllButton.gameObject, (flag && _resultAction == NativeAction.StoreAll) ? _resultText : "Store All");
		SetText(_consolidateButton.gameObject, (flag && _resultAction == NativeAction.Consolidate) ? _resultText : ("Consolidate Chests [" + HearthkeeperPlugin.SortWarehouseKey.Value.MainKey.ToString() + "]"));
		if (Time.unscaledTime >= _resultUntil)
		{
			SetText(_rulesButton.gameObject, "Chest Rules");
		}
	}

	private Component CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size, Action clicked, bool interactable = true)
	{
		Component component = UnityEngine.Object.Instantiate(_buttonTemplate, parent, worldPositionStays: false);
		component.gameObject.name = "Hearthkeeper_" + name;
		ConfigureClick(component, clicked, interactable);
		RectTransform obj = component.transform as RectTransform;
		obj.anchorMin = new Vector2(0f, 1f);
		obj.anchorMax = new Vector2(0f, 1f);
		obj.pivot = new Vector2(0f, 1f);
		obj.anchoredPosition = position;
		obj.sizeDelta = size;
		SetText(component.gameObject, text);
		return component;
	}

	private static GameObject CreateRoot(string name, RectTransform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		RectTransform obj = gameObject.transform as RectTransform;
		obj.SetParent(parent, worldPositionStays: false);
		obj.anchorMin = anchor;
		obj.anchorMax = anchor;
		obj.pivot = pivot;
		obj.anchoredPosition = position;
		obj.sizeDelta = size;
		obj.SetAsLastSibling();
		return gameObject;
	}

	private static void AddNativeBackground(GameObject panel, RectTransform sourceRoot)
	{
		Component component = FindLargestImage(sourceRoot);
		Type type = ((component != null) ? component.GetType() : Type.GetType("UnityEngine.UI.Image, UnityEngine.UI"));
		if (!(type == null))
		{
			Component component2 = panel.AddComponent(type);
			if (component != null)
			{
				CopyProperty(component, component2, "sprite");
				CopyProperty(component, component2, "material");
				CopyProperty(component, component2, "color");
				CopyProperty(component, component2, "type");
				CopyProperty(component, component2, "preserveAspect");
			}
			SetProperty(component2, "raycastTarget", true);
		}
	}

	private static void SetText(GameObject root, string value)
	{
		Component[] componentsInChildren = root.GetComponentsInChildren<Component>(includeInactive: true);
		foreach (Component component in componentsInChildren)
		{
			if (component == null)
			{
				continue;
			}
			Type type = component.GetType();
			if (!(type.Namespace != "TMPro") || !(type.FullName != "UnityEngine.UI.Text"))
			{
				PropertyInfo property = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
				if (property != null && property.CanWrite && property.PropertyType == typeof(string))
				{
					property.SetValue(component, value, null);
				}
				PropertyInfo property2 = type.GetProperty("enableAutoSizing", BindingFlags.Instance | BindingFlags.Public);
				if (property2 != null && property2.CanWrite && property2.PropertyType == typeof(bool))
				{
					property2.SetValue(component, true, null);
				}
				PropertyInfo property3 = type.GetProperty("fontSizeMin", BindingFlags.Instance | BindingFlags.Public);
				if (property3 != null && property3.CanWrite && property3.PropertyType == typeof(float))
				{
					property3.SetValue(component, 10f, null);
				}
				SetProperty(component, "resizeTextForBestFit", true);
				SetProperty(component, "resizeTextMinSize", 10);
			}
		}
	}

	private static void ConfigureClick(Component button, Action clicked, bool interactable)
	{
		SetProperty(button, "interactable", interactable);
		PropertyInfo property = button.GetType().GetProperty("onClick", BindingFlags.Instance | BindingFlags.Public);
		object obj = ((property == null) ? null : property.GetValue(button, null));
		if (obj == null)
		{
			return;
		}
		MethodInfo method = obj.GetType().GetMethod("RemoveAllListeners", BindingFlags.Instance | BindingFlags.Public);
		if (method != null)
		{
			method.Invoke(obj, null);
		}
		if (clicked == null)
		{
			return;
		}
		MethodInfo methodInfo = null;
		MethodInfo[] methods = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < methods.Length; i++)
		{
			if (methods[i].Name == "AddListener" && methods[i].GetParameters().Length == 1)
			{
				methodInfo = methods[i];
				break;
			}
		}
		if (methodInfo == null)
		{
			throw new MissingMethodException(obj.GetType().FullName, "AddListener");
		}
		Delegate obj2 = Delegate.CreateDelegate(methodInfo.GetParameters()[0].ParameterType, clicked.Target, clicked.Method);
		methodInfo.Invoke(obj, new object[1] { obj2 });
	}

	private static Component FindLargestImage(RectTransform root)
	{
		Component[] componentsInChildren = root.GetComponentsInChildren<Component>(includeInactive: true);
		Component result = null;
		float num = -1f;
		foreach (Component component in componentsInChildren)
		{
			if (component == null || component.GetType().FullName != "UnityEngine.UI.Image")
			{
				continue;
			}
			PropertyInfo property = component.GetType().GetProperty("sprite", BindingFlags.Instance | BindingFlags.Public);
			if (!(property == null) && property.GetValue(component, null) != null)
			{
				RectTransform rectTransform = component.transform as RectTransform;
				float num2 = ((rectTransform == null) ? 0f : Mathf.Abs(rectTransform.rect.width * rectTransform.rect.height));
				if (!(num2 <= num))
				{
					num = num2;
					result = component;
				}
			}
		}
		return result;
	}

	private static void CopyProperty(object source, object destination, string name)
	{
		PropertyInfo property = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
		PropertyInfo property2 = destination.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
		if (!(property == null) && !(property2 == null) && property2.CanWrite)
		{
			property2.SetValue(destination, property.GetValue(source, null), null);
		}
	}

	private static void SetProperty(object target, string name, object value)
	{
		if (target != null)
		{
			PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
			{
				property.SetValue(target, value, null);
			}
		}
	}

	private static int AmountStep()
	{
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			return 100;
		}
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			return 10;
		}
		return 1;
	}

	private static string OnOff(bool value)
	{
		if (!value)
		{
			return "OFF";
		}
		return "ON";
	}

	private static void Destroy(UnityEngine.Object value)
	{
		if (value != null)
		{
			UnityEngine.Object.Destroy(value);
		}
	}
}
