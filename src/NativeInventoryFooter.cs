using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster;

// Extend only the artwork. Resizing the inventory root would also stretch its
// slot grid, move header controls and change the native scroll viewport.
internal sealed class NativeInventoryFooter
{
    private sealed class Edge
    {
        internal RectTransform Rect;
        internal float Original, Applied;
    }
    private readonly List<Edge> edges = new List<Edge>();

    internal NativeInventoryFooter(RectTransform panel)
    {
        if (!panel) return;
        foreach (string path in new[] { "Bkg", "Darken", "selected_frame/selected (1)" })
        {
            var rect = panel.Find(path) as RectTransform;
            if (rect) edges.Add(new Edge { Rect = rect, Original = rect.offsetMin.y, Applied = rect.offsetMin.y });
        }
    }

    internal void Extend(float height)
    {
        foreach (var edge in edges)
        {
            if (!edge.Rect) continue;
            var min = edge.Rect.offsetMin;
            // Respect another layout writer instead of restoring a stale snapshot.
            if (!Mathf.Approximately(min.y, edge.Applied)) edge.Original = min.y;
            edge.Applied = edge.Original - height;
            min.y = edge.Applied;
            edge.Rect.offsetMin = min;
        }
    }
}
