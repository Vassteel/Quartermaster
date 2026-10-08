using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

// Inputs are ordered nearest-first. Prefer fewer chests, then that distance order.
// Bound search work as well as leases; a very fragmented warehouse fails closed.
internal static class CraftSupplyPlan
{
    internal static int[] Select(int[] needed, int[][] supplies, int maxChests = 4)
    {
        if (needed.All(n => n <= 0)) return Array.Empty<int>();
        int nodes = 0;
        var suffix = new long[supplies.Length + 1, needed.Length];
        for (int i = supplies.Length - 1; i >= 0; i--)
            for (int j = 0; j < needed.Length; j++) suffix[i,j] = suffix[i+1,j] + supplies[i][j];
        var chosen = new List<int>();
        bool Search(int start, int slots, int[] left)
        {
            if (++nodes > 25000) return false;
            if (left.All(n => n <= 0)) return true;
            if (slots == 0 || start >= supplies.Length) return false;
            for (int j = 0; j < left.Length; j++) if (suffix[start,j] < left[j]) return false;
            for (int i = start; i < supplies.Length; i++)
            {
                if (!Enumerable.Range(0,left.Length).Any(j => left[j] > 0 && supplies[i][j] > 0)) continue;
                var rest = left.Select((n,j) => Math.Max(0,n-supplies[i][j])).ToArray();
                chosen.Add(i);
                if (Search(i+1,slots-1,rest)) return true;
                chosen.RemoveAt(chosen.Count-1);
                if (nodes > 25000) return false;
            }
            return false;
        }
        for (int count = 1; count <= maxChests; count++)
            if (Search(0,count,needed)) return chosen.ToArray();
        return null;
    }
}
