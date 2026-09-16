using System;
using UnityEngine;

namespace Quartermaster.Cosmetics;

// Use shader references from loaded native renderers, never Shader.Find.
// A chest shader gives the decorative bird the same lighting path as its surroundings.
internal static class GullMaterials
{
    internal static Material Create(Material source, Color tint, float metallic = 0f)
    {
        if (!source || !source.shader)
            throw new InvalidOperationException("Native gull material is not loaded yet.");
        var material = new Material(source.shader) { name = "Quartermaster gull (world lit)", color = tint };
        foreach (var property in new[] { "_EmissionColor", "_EmissiveColor", "_NoiseGlowColor" })
            if (material.HasProperty(property)) material.SetColor(property, Color.black);
        foreach (var property in new[] { "_NoiseGlowEnabled", "_Glossiness", "_MetalGloss", "_GlossMapScale",
            "_SpecularHighlights", "_GlossyReflections", "_MetallicAlphaGloss", "_TriplanarMap", "_ValueNoise", "_ValueNoiseVertex", "_AddRain", "_AddSnow" })
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

    internal static Material Feathers(Material source, Material worldMaterial)
    {
        var material=Create(worldMaterial,source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
        foreach(var property in new[]{"_MainTex","_BumpMap"})
            if(source.HasProperty(property) && material.HasProperty(property))
            {
                material.SetTexture(property,source.GetTexture(property));
                material.SetTextureScale(property,source.GetTextureScale(property));
                material.SetTextureOffset(property,source.GetTextureOffset(property));
            }
        return material;
    }
}
