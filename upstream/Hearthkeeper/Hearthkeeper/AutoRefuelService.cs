using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthkeeper;

internal static class AutoRefuelService
{
	private const string DeviceDisabledKey = "MaddCatter.Hearthkeeper.autoRefuelDisabled";

	private static readonly List<Fireplace> Fireplaces = new List<Fireplace>();

	private static readonly List<Smelter> Smelters = new List<Smelter>();

	private static int _fireplaceCursor;

	private static int _smelterCursor;

	internal static void Register(Fireplace fireplace)
	{
		if (fireplace != null && !Fireplaces.Contains(fireplace))
		{
			Fireplaces.Add(fireplace);
		}
	}

	internal static void Register(Smelter smelter)
	{
		if (smelter != null && !Smelters.Contains(smelter))
		{
			Smelters.Add(smelter);
		}
	}

	internal static void Process(Player player)
	{
		if (!(player == null) && HearthkeeperPlugin.AutoRefuelEnabled.Value)
		{
			Cleanup();
			int value = HearthkeeperPlugin.AutoRefuelMaximumDevicesPerCycle.Value;
			if (HearthkeeperPlugin.AutoRefuelFires.Value)
			{
				ProcessRotating(Fireplaces, ref _fireplaceCursor, value, ProcessFireplace);
			}
			if (HearthkeeperPlugin.AutoRefuelProcessingFuel.Value || HearthkeeperPlugin.AutoRefuelProcessingInputs.Value)
			{
				ProcessRotating(Smelters, ref _smelterCursor, value, ProcessSmelter);
			}
		}
	}

	internal static bool ToggleHoveredDevice(Player player)
	{
		if (player == null)
		{
			return false;
		}
		GameObject hoverObject = player.GetHoverObject();
		if (hoverObject == null)
		{
			return false;
		}
		Component componentInParent = hoverObject.GetComponentInParent<Fireplace>();
		if (componentInParent == null)
		{
			componentInParent = hoverObject.GetComponentInParent<Smelter>();
		}
		if (componentInParent == null)
		{
			return false;
		}
		ZNetView view = GetView(componentInParent);
		if (view == null || !view.IsValid() || view.GetZDO() == null)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(componentInParent.transform.position))
		{
			return true;
		}
		if (!view.IsOwner())
		{
			view.ClaimOwnership();
		}
		if (!view.IsOwner())
		{
			player.Message(MessageHud.MessageType.Center, "Hearthkeeper could not change this device");
			return true;
		}
		bool flag = view.GetZDO().GetBool("MaddCatter.Hearthkeeper.autoRefuelDisabled");
		view.GetZDO().Set("MaddCatter.Hearthkeeper.autoRefuelDisabled", !flag);
		player.Message(MessageHud.MessageType.Center, (!flag) ? "Hearthkeeper auto-refuel: DISABLED for this device" : "Hearthkeeper auto-refuel: ENABLED for this device");
		return true;
	}

	private static void ProcessFireplace(Fireplace fireplace)
	{
		if (fireplace == null || fireplace.m_infiniteFuel || !fireplace.m_canRefill || fireplace.m_fuelItem == null || fireplace.m_maxFuel <= 0f)
		{
			return;
		}
		ZNetView view = GetView(fireplace);
		if (!CanAutomate(fireplace, view))
		{
			return;
		}
		float num = view.GetZDO().GetFloat(ZDOVars.s_fuel);
		float num2 = PercentOf(fireplace.m_maxFuel, HearthkeeperPlugin.AutoRefuelFireThresholdPercent.Value);
		if (num > num2)
		{
			return;
		}
		float a = PercentOf(fireplace.m_maxFuel, HearthkeeperPlugin.AutoRefuelFireTargetPercent.Value);
		a = Mathf.Clamp(Mathf.Max(a, num2), 0f, fireplace.m_maxFuel);
		int num3 = Math.Min(HearthkeeperPlugin.AutoRefuelMaximumItemsPerDevice.Value, Mathf.CeilToInt(a - num));
		if (num3 > 0)
		{
			string name = fireplace.m_fuelItem.m_itemData.m_shared.m_name;
			int num4 = TakeItems(fireplace.transform.position, name, num3, null);
			if (num4 > 0)
			{
				fireplace.AddFuel(num4);
			}
		}
	}

	private static void ProcessSmelter(Smelter smelter)
	{
		if (smelter == null)
		{
			return;
		}
		ZNetView view = GetView(smelter);
		if (CanAutomate(smelter, view))
		{
			int num = HearthkeeperPlugin.AutoRefuelMaximumItemsPerDevice.Value;
			if (HearthkeeperPlugin.AutoRefuelProcessingFuel.Value && num > 0)
			{
				num -= RefillProcessingFuel(smelter, view, num);
			}
			if (HearthkeeperPlugin.AutoRefuelProcessingInputs.Value && num > 0)
			{
				RefillProcessingInputs(smelter, view, num);
			}
		}
	}

	private static int RefillProcessingFuel(Smelter smelter, ZNetView view, int budget)
	{
		if (smelter.m_fuelItem == null || smelter.m_maxFuel <= 0)
		{
			return 0;
		}
		float num = view.GetZDO().GetFloat(ZDOVars.s_fuel);
		float num2 = PercentOf(smelter.m_maxFuel, HearthkeeperPlugin.AutoRefuelProcessingFuelThresholdPercent.Value);
		if (num > num2)
		{
			return 0;
		}
		float a = PercentOf(smelter.m_maxFuel, HearthkeeperPlugin.AutoRefuelProcessingFuelTargetPercent.Value);
		a = Mathf.Clamp(Mathf.Max(a, num2), 0f, smelter.m_maxFuel);
		int num3 = Math.Min(budget, Mathf.CeilToInt(a - num));
		if (num3 <= 0)
		{
			return 0;
		}
		string name = smelter.m_fuelItem.m_itemData.m_shared.m_name;
		int num4 = TakeItems(smelter.transform.position, name, num3, null);
		for (int i = 0; i < num4; i++)
		{
			view.InvokeRPC("RPC_AddFuel");
		}
		return num4;
	}

	private static int RefillProcessingInputs(Smelter smelter, ZNetView view, int budget)
	{
		if (smelter.m_conversion == null || smelter.m_maxOre <= 0)
		{
			return 0;
		}
		int num = view.GetZDO().GetInt(ZDOVars.s_queued);
		float num2 = PercentOf(smelter.m_maxOre, HearthkeeperPlugin.AutoRefuelProcessingInputThresholdPercent.Value);
		if ((float)num > num2)
		{
			return 0;
		}
		int num3 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(PercentOf(smelter.m_maxOre, HearthkeeperPlugin.AutoRefuelProcessingInputTargetPercent.Value), num2)), 0, smelter.m_maxOre);
		int num4 = Math.Min(budget, num3 - num);
		if (num4 <= 0)
		{
			return 0;
		}
		for (int i = 0; i < smelter.m_conversion.Count; i++)
		{
			Smelter.ItemConversion itemConversion = smelter.m_conversion[i];
			if (itemConversion == null || itemConversion.m_from == null || itemConversion.m_from.m_itemData == null)
			{
				continue;
			}
			string name = itemConversion.m_from.m_itemData.m_shared.m_name;
			List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
			int num5 = TakeItems(smelter.transform.position, name, num4, list);
			if (num5 > 0)
			{
				string name2 = itemConversion.m_from.gameObject.name;
				for (int j = 0; j < list.Count; j++)
				{
					view.InvokeRPC("RPC_AddOre", name2, list[j].m_cheated);
				}
				return num5;
			}
		}
		return 0;
	}

	private static int TakeItems(Vector3 point, string sharedName, int requested, List<ItemDrop.ItemData> taken)
	{
		if (requested <= 0 || string.IsNullOrEmpty(sharedName))
		{
			return 0;
		}
		List<Container> list = ContainerRegistry.Nearby(point, HearthkeeperPlugin.AutoRefuelRange.Value, requireAccept: false, allowInUse: false);
		int num = requested;
		for (int i = 0; i < list.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			Container container = list[i];
			Inventory inventory = ContainerRegistry.SafeInventory(container);
			if (inventory == null)
			{
				continue;
			}
			List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>(inventory.GetAllItems());
			for (int j = 0; j < list2.Count; j++)
			{
				if (num <= 0)
				{
					break;
				}
				ItemDrop.ItemData itemData = list2[j];
				if (itemData == null || itemData.m_shared == null || itemData.m_shared.m_name != sharedName)
				{
					continue;
				}
				int val = InventoryTransfers.AvailableInContainer(container, sharedName, itemData.m_quality);
				int num2 = Math.Min(num, Math.Min(itemData.m_stack, val));
				if (num2 <= 0)
				{
					continue;
				}
				ItemDrop.ItemData itemData2 = itemData.Clone();
				itemData2.m_stack = 1;
				if (!inventory.RemoveItem(itemData, num2))
				{
					continue;
				}
				if (taken != null)
				{
					for (int k = 0; k < num2; k++)
					{
						taken.Add(itemData2.Clone());
					}
				}
				num -= num2;
			}
		}
		return requested - num;
	}

	private static bool CanAutomate(Component device, ZNetView view)
	{
		if (device == null || view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO() == null)
		{
			return false;
		}
		if (device.GetComponentInParent<Piece>() == null)
		{
			return false;
		}
		if (view.GetZDO().GetBool("MaddCatter.Hearthkeeper.autoRefuelDisabled"))
		{
			return false;
		}
		return PrivateArea.CheckAccess(device.transform.position, 0f, flash: false, wardCheck: true);
	}

	private static ZNetView GetView(Component component)
	{
		if (component == null)
		{
			return null;
		}
		ZNetView component2 = component.GetComponent<ZNetView>();
		if (!(component2 != null))
		{
			return component.GetComponentInParent<ZNetView>();
		}
		return component2;
	}

	private static float PercentOf(float maximum, float percent)
	{
		return maximum * Mathf.Clamp(percent, 0f, 100f) / 100f;
	}

	private static void ProcessRotating<T>(List<T> devices, ref int cursor, int budget, Action<T> process) where T : UnityEngine.Object
	{
		if (devices.Count != 0 && budget > 0)
		{
			cursor = Mathf.Clamp(cursor, 0, devices.Count - 1);
			int num = Math.Min(budget, devices.Count);
			for (int i = 0; i < num; i++)
			{
				process(devices[(cursor + i) % devices.Count]);
			}
			cursor = (cursor + num) % devices.Count;
		}
	}

	private static void Cleanup()
	{
		for (int num = Fireplaces.Count - 1; num >= 0; num--)
		{
			if (Fireplaces[num] == null)
			{
				Fireplaces.RemoveAt(num);
			}
		}
		for (int num2 = Smelters.Count - 1; num2 >= 0; num2--)
		{
			if (Smelters[num2] == null)
			{
				Smelters.RemoveAt(num2);
			}
		}
		if (Fireplaces.Count == 0)
		{
			_fireplaceCursor = 0;
		}
		if (Smelters.Count == 0)
		{
			_smelterCursor = 0;
		}
	}
}
