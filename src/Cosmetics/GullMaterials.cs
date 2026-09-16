using System;
using UnityEngine;

namespace Quartermaster.Cosmetics;

// Private, ordinary world-lit materials. Do not inherit creature glow keywords or
// replace a missing lit shader with an unlit sprite shader.
internal static class GullMaterials
{
    internal static Material Create(Color tint, float metallic = 0f)
    {
        var shader = Shader.Find("Custom/Piece");
        if (!shader || !shader.isSupported) shader = Shader.Find("Standard");
        if (!shader || !shader.isSupported)
            throw new InvalidOperationException("No supported world-lit shader for the Deposit gull.");
        var material = new Material(shader) { name = "Quartermaster gull (world lit)", color = tint };
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
        // Keep the vanilla skin and UV mapping, but none of its shader state.
        var material = Create(source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
        if (source.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", source.GetTexture("_MainTex"));
            material.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
            material.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
        }
        return material;
    }
}
