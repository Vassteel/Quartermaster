using System;
using System.Reflection;

namespace Quartermaster;

internal static class ExternalSlotCompatibility
{
	private const string EaqsApiTypeName = "EquipmentAndQuickSlots.API, EquipmentAndQuickSlots";

	private static bool _lookupAttempted;

	private static bool _availabilityLogged;

	private static MethodInfo _eaqsIsSlotCell;

	private static MethodInfo _eaqsGetVisibleRows;

	internal static bool IsProtectedPlayerSlot(ItemDrop.ItemData item)
	{
		if (item != null)
		{
			return IsProtectedPlayerSlot(item.m_gridPos.x, item.m_gridPos.y);
		}
		return false;
	}

	internal static bool IsProtectedPlayerSlot(int x, int y)
	{
		EnsureEaqsApi();
		if (_eaqsIsSlotCell == null)
		{
			return false;
		}
		try
		{
			if (_eaqsGetVisibleRows != null && _eaqsGetVisibleRows.Invoke(null, null) is int num && y >= num)
			{
				return true;
			}
			object[] parameters = new object[3] { x, y, null };
			return _eaqsIsSlotCell.Invoke(null, parameters) is bool protectedSlot && protectedSlot;
		}
		catch (Exception ex)
		{
			Plugin.Log?.LogWarning("Equipment and Quick Slots compatibility check failed: " + ex.GetBaseException().Message);
			_eaqsIsSlotCell = null;
			return false;
		}
	}

	private static void EnsureEaqsApi()
	{
		if (!_lookupAttempted)
		{
			_lookupAttempted = true;
			Type type = Type.GetType(EaqsApiTypeName, throwOnError: false);
			_eaqsIsSlotCell = type?.GetMethod("IsSlotCell", BindingFlags.Static | BindingFlags.Public, null, new Type[3]
			{
				typeof(int),
				typeof(int),
				typeof(string).MakeByRefType()
			}, null);
			_eaqsGetVisibleRows = type?.GetMethod("GetVisibleRows", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
			if (_eaqsIsSlotCell != null && !_availabilityLogged)
			{
				_availabilityLogged = true;
				Plugin.Log?.LogInfo("Equipment and Quick Slots compatibility enabled; equipment, quick, and custom slots are protected.");
			}
		}
	}
}
