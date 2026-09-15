using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster;

// Stroke geometry keeps the requested inscription visible without a runic font dependency.
// One small mesh is shared by both faces; the material matches the existing gear glow.
internal static class RuneInscription
{
    internal const string Text = "ᛞᛖᛈᛟᛊᛁᛏ ᚲᚺᛖᛊᛏ";
    private const float Advance = .9f;
    internal const float Width = 12 * Advance + .65f;

    // Each array is a continuous stroke in a .65 by 1 glyph cell, bottom-left origin.
    private static readonly Dictionary<char, float[][]> Strokes = new Dictionary<char, float[][]>
    {
        ['ᛞ'] = new[] { new[] { 0f, 0f, 0f, 1f, .65f, 0f, .65f, 1f, 0f, 0f } },
        ['ᛖ'] = new[] { new[] { 0f, 0f, 0f, 1f, .325f, .78f, .65f, 1f, .65f, 0f } },
        ['ᛈ'] = new[] { new[] { .65f, 1f, .325f, .78f, 0f, 1f, 0f, 0f, .325f, .22f, .65f, 0f } },
        ['ᛟ'] = new[] { new[] { 0f, 0f, .65f, .65f, .325f, 1f, 0f, .65f, .65f, 0f } },
        ['ᛊ'] = new[] { new[] { .65f, 1f, 0f, .75f, .65f, .5f, 0f, .25f, .65f, 0f } },
        ['ᛁ'] = new[] { new[] { .325f, 0f, .325f, 1f } },
        ['ᛏ'] = new[] { new[] { .325f, 0f, .325f, 1f }, new[] { 0f, .84f, .325f, 1f, .65f, .84f } },
        ['ᚲ'] = new[] { new[] { .48f, .7f, .17f, .5f, .48f, .3f } },
        ['ᚺ'] = new[] { new[] { 0f, 0f, 0f, 1f }, new[] { .65f, 0f, .65f, 1f }, new[] { 0f, .72f, .65f, .35f } }
    };

    internal static Mesh CreateMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float x = -Width * .5f;
        foreach (char rune in Text)
        {
            if (rune != ' ')
            {
                foreach (var stroke in Strokes[rune])
                {
                    for (int j = 0; j < stroke.Length - 2; j += 2)
                    {
                        var a = new Vector3(x + stroke[j], stroke[j + 1], 0);
                        var b = new Vector3(x + stroke[j + 2], stroke[j + 3], 0);
                        var direction = (b - a).normalized;
                        var side = new Vector3(-direction.y, direction.x, 0) * .034f;
                        // Slightly overlapping ends avoid hairline gaps at angular joins.
                        a -= direction * .017f; b += direction * .017f;
                        int start = vertices.Count;
                        vertices.Add(a - side); vertices.Add(a + side);
                        vertices.Add(b + side); vertices.Add(b - side);
                        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                    }
                }
            }
            x += Advance;
        }
        var mesh = new Mesh { name = "Quartermaster_DepositRunes" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        // Sprites/Default multiplies its tint by the vertex colors and sampled texture.
        var colors = new Color[vertices.Count];
        var uv = new Vector2[vertices.Count];
        for (int i = 0; i < colors.Length; i++) { colors[i] = Color.white; uv[i] = new Vector2(.5f, .5f); }
        mesh.colors = colors; mesh.uv = uv;
        mesh.RecalculateBounds();
        return mesh;
    }
}
