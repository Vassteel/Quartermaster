using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Quartermaster;

// Borrow shader references from loaded game prefabs. Unity's built-in Standard
// shader is stripped from some Valheim players and cannot be a registration dependency.
internal static class StorageMaterials
{
    private static Material PieceSurface(GameObject prefab) => !prefab ? null :
        prefab.GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials)
            .FirstOrDefault(material => material && material.shader && material.shader.name == "Custom/Piece");

    internal static Material Opaque(GameObject preferred, string name, Texture texture, float metallic = 0)
    {
        var source = PieceSurface(preferred);
        if (!source) source = PieceSurface(PrefabManager.Instance.GetPrefab("piece_chest_wood"));
        if (!source) source = PieceSurface(PrefabManager.Instance.GetPrefab("sign"));
        if (!source) throw new InvalidOperationException("Native building material unavailable for " + name);
        var result = new Material(source.shader) { name = name, color = Color.white, mainTexture = texture, enableInstancing = true };
        foreach (var key in new[] { "_Glossiness", "_Metallic", "_MetalGloss", "_NoiseGlowEnabled", "_TriplanarMap", "_ValueNoise", "_ValueNoiseVertex" })
            if (result.HasProperty(key)) result.SetFloat(key, 0);
        if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", metallic);
        if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", metallic > 0 ? .24f : 0);
        foreach (var key in new[] { "_EmissionColor", "_EmissiveColor", "_NoiseGlowColor" })
            if (result.HasProperty(key)) result.SetColor(key, Color.black);
        result.DisableKeyword("_EMISSION"); result.DisableKeyword("NOISEGLOW");
        return result;
    }

    internal static Material Crystal(Material opaqueFallback)
    {
        var wall = PrefabManager.Instance.GetPrefab("crystal_wall_1x1");
        var source = !wall ? null : wall.GetComponentsInChildren<MeshRenderer>(true)
            .SelectMany(renderer => renderer.sharedMaterials)
            .Where(material => material && material.shader)
            .OrderByDescending(material => material.renderQueue >= 2500)
            .ThenByDescending(material => material.name.IndexOf("crystal", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
        // Preserve Valheim's shader-specific transparency settings, textures and
        // keywords instead of applying Standard-only blend properties to another shader.
        if (source) return new Material(source) { name = "Quartermaster native crystal glass" };
        Plugin.Log.LogWarning("Native crystal-wall material unavailable; flask glass will use an opaque fallback. Furniture remains available.");
        return new Material(opaqueFallback) { name = "Quartermaster opaque crystal fallback", color = new Color(.58f, .68f, .60f, 1) };
    }
}
