using System;
using UnityEngine;

namespace Quartermaster.Cosmetics;

// Clone a material referenced by a native prefab: bundled shaders need not be
// discoverable through Shader.Find. Preserve native lighting variants, remove glow.
internal static class GullMaterials
{
    internal static Material Create(Material source, Color tint, float metallic = 0f)
    {
        if (!source || !source.shader)
            throw new InvalidOperationException("Native gull material is not loaded yet.");
        var material = new Material(source) { name = "Quartermaster gull (world lit)", color = tint };
        foreach (var property in new[] { "_EmissionColor", "_EmissiveColor", "_NoiseGlowColor" })
            if (material.HasProperty(property)) material.SetColor(property, Color.black);
        foreach (var property in new[] { "_NoiseGlowEnabled", "_Glossiness", "_MetalGloss", "_GlossMapScale",
            "_SpecularHighlights", "_GlossyReflections", "_ValueNoise", "_ValueNoiseVertex", "_AddRain", "_AddSnow" })
            if (material.HasProperty(property)) material.SetFloat(property, 0f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_MoveableObject")) material.SetFloat("_MoveableObject", 1f);
        material.DisableKeyword("_EMISSION");
        material.DisableKeyword("NOISEGLOW");
        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        return material;
    }

    internal static Material Feathers(Material source)
    {
        return Create(source, source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
    }
}
