using System;
using System.Collections.Generic;

namespace Hearthkeeper;

internal static class ContainerSizeService
{
	private struct Size
	{
		internal readonly int Width;

		internal readonly int Height;

		internal Size(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}

	private const string WidthKey = "MaddCatter.Hearthkeeper.width";

	private const string HeightKey = "MaddCatter.Hearthkeeper.height";

	internal static void ApplyBeforeAwake(Container container)
	{
		if (container == null || !HearthkeeperPlugin.ChestSizingEnabled.Value)
		{
			return;
		}
		string text = ContainerRegistry.PrefabName(container);
		Size value;
		bool flag = ParseRules(HearthkeeperPlugin.ChestSizeRules.Value).TryGetValue(text, out value);
		if (flag || IsUnlistedChestPiece(container, text))
		{
			if (!flag)
			{
				value = new Size(HearthkeeperPlugin.MinimumChestColumns.Value, HearthkeeperPlugin.MinimumChestRows.Value);
			}
			int val = Math.Max(container.m_width, Math.Max(HearthkeeperPlugin.MinimumChestColumns.Value, value.Width));
			int val2 = Math.Max(container.m_height, Math.Max(HearthkeeperPlugin.MinimumChestRows.Value, value.Height));
			val = Math.Min(HearthkeeperPlugin.MaximumChestColumns.Value, val);
			val2 = Math.Min(HearthkeeperPlugin.MaximumChestRows.Value, val2);
			ZNetView zNetView = container.GetComponent<ZNetView>();
			if (zNetView == null)
			{
				zNetView = container.GetComponentInParent<ZNetView>();
			}
			ZDO zDO = ((zNetView != null && zNetView.IsValid()) ? zNetView.GetZDO() : null);
			if (zDO != null)
			{
				val = Math.Max(val, zDO.GetInt("MaddCatter.Hearthkeeper.width"));
				val2 = Math.Max(val2, zDO.GetInt("MaddCatter.Hearthkeeper.height"));
			}
			container.m_width = val;
			container.m_height = val2;
		}
	}

	internal static void RememberAppliedSize(Container container)
	{
		if (container == null || !HearthkeeperPlugin.ChestSizingEnabled.Value)
		{
			return;
		}
		ZNetView view = ContainerRegistry.GetView(container);
		if (view == null || !view.IsValid() || !view.IsOwner())
		{
			return;
		}
		ZDO zDO = view.GetZDO();
		if (zDO != null)
		{
			int num = zDO.GetInt("MaddCatter.Hearthkeeper.width");
			int num2 = zDO.GetInt("MaddCatter.Hearthkeeper.height");
			if (container.m_width > num)
			{
				zDO.Set("MaddCatter.Hearthkeeper.width", container.m_width);
			}
			if (container.m_height > num2)
			{
				zDO.Set("MaddCatter.Hearthkeeper.height", container.m_height);
			}
		}
	}

	private static bool IsUnlistedChestPiece(Container container, string prefab)
	{
		if (!HearthkeeperPlugin.ResizeUnlistedChestPrefabs.Value)
		{
			return false;
		}
		Piece piece = container.GetComponent<Piece>();
		if (piece == null)
		{
			piece = container.GetComponentInParent<Piece>();
		}
		if (piece != null)
		{
			return prefab.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static Dictionary<string, Size> ParseRules(string raw)
	{
		Dictionary<string, Size> dictionary = new Dictionary<string, Size>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(raw))
		{
			return dictionary;
		}
		string[] array = raw.Split(new char[3] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { '=' }, 2);
			if (array2.Length == 2)
			{
				string[] array3 = array2[1].Trim().ToLowerInvariant().Split(new char[1] { 'x' });
				if (array3.Length == 2 && int.TryParse(array3[0], out var result) && int.TryParse(array3[1], out var result2) && result > 0 && result2 > 0)
				{
					dictionary[array2[0].Trim()] = new Size(result, result2);
				}
			}
		}
		return dictionary;
	}
}
