using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthkeeper;

internal static class StackSizeService
{
	private static readonly Dictionary<ItemDrop.ItemData.SharedData, int> Originals = new Dictionary<ItemDrop.ItemData.SharedData, int>();

	internal static void Apply()
	{
		ObjectDB instance = ObjectDB.instance;
		if (instance == null || instance.m_items == null)
		{
			return;
		}
		Dictionary<string, int> dictionary = ParseOverrides(HearthkeeperPlugin.StackOverrides.Value);
		for (int i = 0; i < instance.m_items.Count; i++)
		{
			GameObject gameObject = instance.m_items[i];
			ItemDrop itemDrop = ((gameObject != null) ? gameObject.GetComponent<ItemDrop>() : null);
			if (!(itemDrop == null) && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared != null)
			{
				ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
				if (!Originals.TryGetValue(shared, out var value))
				{
					value = Math.Max(1, shared.m_maxStackSize);
					Originals[shared] = value;
				}
				int value2 = ((!HearthkeeperPlugin.StackSizesEnabled.Value) ? value : ((!dictionary.TryGetValue(gameObject.name, out value2) && !dictionary.TryGetValue(shared.m_name, out value2)) ? ((int)Math.Round((float)value * HearthkeeperPlugin.StackMultiplier.Value)) : Math.Max(1, value2)));
				shared.m_maxStackSize = Math.Min(HearthkeeperPlugin.MaximumStackSize.Value, Math.Max(1, value2));
			}
		}
	}

	private static Dictionary<string, int> ParseOverrides(string raw)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(raw))
		{
			return dictionary;
		}
		string[] array = raw.Split(new char[3] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { '=' }, 2);
			if (array2.Length == 2 && int.TryParse(array2[1].Trim(), out var result) && result > 0)
			{
				dictionary[array2[0].Trim()] = result;
			}
		}
		return dictionary;
	}
}
