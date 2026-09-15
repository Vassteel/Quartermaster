using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Hearthkeeper;

internal static class ContainerRegistry
{
	private const string KeyReserve = "MaddCatter.Hearthkeeper.reserve";

	private const string KeyCustom = "MaddCatter.Hearthkeeper.custom";

	private const string KeyManual = "MaddCatter.Hearthkeeper.manual";

	private const string KeyAccept = "MaddCatter.Hearthkeeper.accept";

	private const string KeyFeed = "MaddCatter.Hearthkeeper.feed";

	private static readonly List<Container> Containers = new List<Container>();

	private static readonly Dictionary<Inventory, Container> InventoryOwners = new Dictionary<Inventory, Container>();

	private static readonly MethodInfo CheckAccessMethod = AccessTools.Method(typeof(Container), "CheckAccess");

	internal static void Register(Container container)
	{
		if (!(container == null) && !Containers.Contains(container))
		{
			Containers.Add(container);
			Inventory inventory = SafeInventory(container);
			if (inventory != null)
			{
				InventoryOwners[inventory] = container;
			}
		}
	}

	internal static void Unregister(Container container)
	{
		if (!(container == null))
		{
			Containers.Remove(container);
			Inventory inventory = SafeInventory(container);
			if (inventory != null)
			{
				InventoryOwners.Remove(inventory);
			}
		}
	}

	internal static Container OwnerOf(Inventory inventory)
	{
		if (inventory == null)
		{
			return null;
		}
		if (!InventoryOwners.TryGetValue(inventory, out var value) || !(value != null))
		{
			return null;
		}
		return value;
	}

	internal static List<Container> Nearby(Vector3 point, float range, bool requireAccept, bool allowInUse)
	{
		Cleanup();
		float num = range * range;
		List<Container> list = new List<Container>();
		for (int i = 0; i < Containers.Count; i++)
		{
			Container container = Containers[i];
			if (IsUsable(container, allowInUse) && !((container.transform.position - point).sqrMagnitude > num) && (!requireAccept || GetSettings(container).AcceptStorage))
			{
				list.Add(container);
			}
		}
		list.Sort(delegate(Container a, Container b)
		{
			int num2 = (a.transform.position - point).sqrMagnitude.CompareTo((b.transform.position - point).sqrMagnitude);
			return (num2 == 0) ? string.CompareOrdinal(PrefabName(a), PrefabName(b)) : num2;
		});
		return list;
	}

	internal static bool IsUsable(Container container, bool allowInUse)
	{
		if (container == null || !HearthkeeperPlugin.Enabled.Value)
		{
			return false;
		}
		if (!HearthkeeperPlugin.IsAllowedPrefab(PrefabName(container)))
		{
			return false;
		}
		if (!allowInUse && container.IsInUse())
		{
			return false;
		}
		if (SafeInventory(container) == null)
		{
			return false;
		}
		ZNetView view = GetView(container);
		if (view == null || !view.IsValid() || !view.IsOwner())
		{
			return false;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return false;
		}
		try
		{
			if (CheckAccessMethod != null && !(bool)CheckAccessMethod.Invoke(container, new object[1] { localPlayer.GetPlayerID() }))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, flash: false, wardCheck: true))
		{
			return false;
		}
		return true;
	}

	internal static Inventory SafeInventory(Container container)
	{
		try
		{
			return (container != null) ? container.GetInventory() : null;
		}
		catch
		{
			return null;
		}
	}

	internal static ZNetView GetView(Container container)
	{
		if (container == null)
		{
			return null;
		}
		if (container.m_rootObjectOverride != null)
		{
			return container.m_rootObjectOverride;
		}
		ZNetView component = container.GetComponent<ZNetView>();
		if (!(component != null))
		{
			return container.GetComponentInParent<ZNetView>();
		}
		return component;
	}

	internal static ContainerSettings GetSettings(Container container)
	{
		ContainerSettings containerSettings = new ContainerSettings
		{
			Reserve = HearthkeeperPlugin.DefaultReserve.Value,
			CustomReserve = HearthkeeperPlugin.DefaultCustomReserve.Value,
			ManualLock = HearthkeeperPlugin.DefaultManualLock.Value,
			AcceptStorage = HearthkeeperPlugin.DefaultAcceptStorage.Value,
			LivestockFeed = HearthkeeperPlugin.DefaultLivestockFeed.Value
		};
		ZNetView view = GetView(container);
		ZDO zDO = ((view != null && view.IsValid()) ? view.GetZDO() : null);
		if (zDO == null)
		{
			return containerSettings;
		}
		containerSettings.Reserve = (ReserveMode)zDO.GetInt("MaddCatter.Hearthkeeper.reserve", (int)containerSettings.Reserve);
		containerSettings.CustomReserve = zDO.GetInt("MaddCatter.Hearthkeeper.custom", containerSettings.CustomReserve);
		containerSettings.ManualLock = zDO.GetBool("MaddCatter.Hearthkeeper.manual", containerSettings.ManualLock);
		containerSettings.AcceptStorage = zDO.GetBool("MaddCatter.Hearthkeeper.accept", containerSettings.AcceptStorage);
		containerSettings.LivestockFeed = zDO.GetBool("MaddCatter.Hearthkeeper.feed", containerSettings.LivestockFeed);
		return containerSettings;
	}

	internal static bool SaveSettings(Container container, ContainerSettings settings)
	{
		if (container == null || settings == null)
		{
			return false;
		}
		ZNetView view = GetView(container);
		if (view == null || !view.IsValid())
		{
			return false;
		}
		if (!view.IsOwner())
		{
			view.ClaimOwnership();
		}
		if (!view.IsOwner())
		{
			return false;
		}
		ZDO zDO = view.GetZDO();
		if (zDO == null)
		{
			return false;
		}
		zDO.Set("MaddCatter.Hearthkeeper.reserve", (int)settings.Reserve);
		zDO.Set("MaddCatter.Hearthkeeper.custom", Math.Max(0, settings.CustomReserve));
		zDO.Set("MaddCatter.Hearthkeeper.manual", settings.ManualLock);
		zDO.Set("MaddCatter.Hearthkeeper.accept", settings.AcceptStorage);
		zDO.Set("MaddCatter.Hearthkeeper.feed", settings.LivestockFeed);
		return true;
	}

	internal static int ReserveFor(Container container, string sharedName)
	{
		Inventory inventory = SafeInventory(container);
		if (inventory == null)
		{
			return 0;
		}
		ItemDrop.ItemData itemData = null;
		List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
		for (int i = 0; i < allItems.Count; i++)
		{
			if (allItems[i].m_shared.m_name == sharedName)
			{
				itemData = allItems[i];
				break;
			}
		}
		if (itemData != null)
		{
			return GetSettings(container).AmountFor(itemData);
		}
		return 0;
	}

	internal static string PrefabName(Container container)
	{
		if (container == null || container.gameObject == null)
		{
			return string.Empty;
		}
		string text = container.gameObject.name ?? string.Empty;
		if (!text.EndsWith("(Clone)", StringComparison.Ordinal))
		{
			return text;
		}
		return text.Substring(0, text.Length - "(Clone)".Length);
	}

	private static void Cleanup()
	{
		for (int num = Containers.Count - 1; num >= 0; num--)
		{
			if (!(Containers[num] != null))
			{
				Containers.RemoveAt(num);
			}
		}
		List<Inventory> list = new List<Inventory>();
		foreach (KeyValuePair<Inventory, Container> inventoryOwner in InventoryOwners)
		{
			if (inventoryOwner.Key == null || inventoryOwner.Value == null)
			{
				list.Add(inventoryOwner.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			InventoryOwners.Remove(list[i]);
		}
	}
}
