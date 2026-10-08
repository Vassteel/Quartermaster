using System;
using System.Collections.Generic;
namespace Quartermaster;

internal static class PlantBatchPlan
{
    // The planting mod supplies validity and order. This only limits resource usage.
    internal static int[] Select(bool[] valid, int affordable, bool requireWholeGrid)
    {
        if (valid == null || valid.Length == 0 || !valid[0] || affordable < 1) return Array.Empty<int>();
        var selected = new List<int>();
        for (int i=0;i<valid.Length;i++)
        {
            if (!valid[i]) { if (requireWholeGrid) return Array.Empty<int>(); continue; }
            if (selected.Count >= affordable)
            { if (requireWholeGrid) return Array.Empty<int>(); break; }
            selected.Add(i);
        }
        return selected.ToArray();
    }
}
