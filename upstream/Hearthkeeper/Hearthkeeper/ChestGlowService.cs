using System.Collections.Generic;
using UnityEngine;

namespace Hearthkeeper;

internal static class ChestGlowService
{
	private sealed class GlowState
	{
		internal Container Container;

		internal GameObject VisualRoot;

		internal GameObject LightObject;

		internal Light Light;

		internal float EndsAt;

		internal bool EmissionApplied;
	}

	private static readonly Dictionary<Container, GlowState> Active = new Dictionary<Container, GlowState>();

	private static readonly List<Container> Finished = new List<Container>();

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	internal static void Pulse(Container container)
	{
		if (!HearthkeeperPlugin.ReceivingGlowEnabled.Value || container == null)
		{
			return;
		}
		if (!Active.TryGetValue(container, out var value) || value == null)
		{
			value = Create(container);
			if (value == null)
			{
				return;
			}
			Active[container] = value;
		}
		value.EndsAt = Time.unscaledTime + HearthkeeperPlugin.ReceivingGlowDuration.Value;
	}

	internal static void Update()
	{
		if (!HearthkeeperPlugin.ReceivingGlowEnabled.Value)
		{
			Clear();
		}
		else
		{
			if (Active.Count == 0)
			{
				return;
			}
			Finished.Clear();
			Color color = ParseColor(HearthkeeperPlugin.ReceivingGlowColor.Value);
			float b = Mathf.Max(0.25f, HearthkeeperPlugin.ReceivingGlowDuration.Value);
			foreach (KeyValuePair<Container, GlowState> item in Active)
			{
				GlowState value = item.Value;
				if (item.Key == null || value == null || value.Container == null || value.VisualRoot == null || Time.unscaledTime >= value.EndsAt)
				{
					Finish(value);
					Finished.Add(item.Key);
					continue;
				}
				float num = Mathf.Clamp01((value.EndsAt - Time.unscaledTime) / Mathf.Min(0.45f, b));
				float num2 = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 11f);
				float num3 = (0.35f + num2 * 0.65f) * num;
				float value2 = HearthkeeperPlugin.ReceivingGlowIntensity.Value;
				if (value.Light != null)
				{
					value.Light.color = color;
					value.Light.range = HearthkeeperPlugin.ReceivingGlowRadius.Value;
					value.Light.intensity = value2 * num3;
				}
				if (MaterialMan.instance != null)
				{
					MaterialMan.instance.SetValue(value.VisualRoot, EmissionColor, color * (value2 * 0.24f * num3), !value.EmissionApplied);
					value.EmissionApplied = true;
				}
			}
			for (int i = 0; i < Finished.Count; i++)
			{
				Active.Remove(Finished[i]);
			}
		}
	}

	internal static void Clear()
	{
		foreach (GlowState value in Active.Values)
		{
			Finish(value);
		}
		Active.Clear();
		Finished.Clear();
	}

	private static GlowState Create(Container container)
	{
		GameObject gameObject = container.gameObject;
		ZNetView view = ContainerRegistry.GetView(container);
		if (view != null)
		{
			gameObject = view.gameObject;
		}
		GameObject gameObject2 = new GameObject("Hearthkeeper_ReceivingGlow");
		gameObject2.transform.SetParent(container.transform, worldPositionStays: false);
		gameObject2.transform.localPosition = Vector3.up * 0.75f;
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren.Length != 0)
		{
			Bounds bounds = componentsInChildren[0].bounds;
			for (int i = 1; i < componentsInChildren.Length; i++)
			{
				bounds.Encapsulate(componentsInChildren[i].bounds);
			}
			gameObject2.transform.position = bounds.center + Vector3.up * 0.2f;
		}
		Light light = gameObject2.AddComponent<Light>();
		light.type = LightType.Point;
		light.shadows = LightShadows.None;
		light.renderMode = LightRenderMode.Auto;
		light.intensity = 0f;
		return new GlowState
		{
			Container = container,
			VisualRoot = gameObject,
			LightObject = gameObject2,
			Light = light,
			EndsAt = Time.unscaledTime + HearthkeeperPlugin.ReceivingGlowDuration.Value
		};
	}

	private static void Finish(GlowState state)
	{
		if (state != null)
		{
			if (state.EmissionApplied && state.VisualRoot != null && MaterialMan.instance != null)
			{
				MaterialMan.instance.ResetValue(state.VisualRoot, EmissionColor);
			}
			if (state.LightObject != null)
			{
				Object.Destroy(state.LightObject);
			}
		}
	}

	private static Color ParseColor(string value)
	{
		if (!string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value.Trim(), out var color))
		{
			color.a = 1f;
			return color;
		}
		return new Color(1f, 0.82f, 0.48f, 1f);
	}
}
