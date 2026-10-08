using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

// Local visual query only: never reserve storage or transfer network ownership.
internal static class ChestSearch
{
    private static readonly HashSet<Container> Matches = new HashSet<Container>();
    private static readonly HashSet<Container> Opened = new HashSet<Container>();
    private static OwlLedger source;
    private static string query = "";
    private static float next;
    internal static int Start(OwlLedger book, string text)
    {
        Clear(); query = (text ?? "").Trim();
        if (query.Length == 0 || !book) return 0;
        source = book; Tick(); return Matches.Count;
    }
    internal static bool Highlighted(Container chest) => Matches.Contains(chest);
    internal static void Clear() { Matches.Clear(); Opened.Clear(); source = null; query = ""; next = 0; }
    internal static void OnOpened(Container chest)
    { if (chest && source) { Opened.Add(chest); Matches.Remove(chest); } }
    internal static void Tick()
    {
        if (!source || !Plugin.Enabled.Value || !Player.m_localPlayer || Player.m_localPlayer.IsDead())
        { Clear(); return; }
        if (Time.time < next) return;
        next = Time.time + 1f; Matches.Clear();
        if (!OwlLedger.Allowed(source)) { Clear(); return; }
        var network = source.Network;
        if (network == null) return;
        foreach (var chest in network.Chests)
        {
            if (Opened.Contains(chest) || !ContainerRegistry.CanObserve(chest)) continue;
            if (StorageObservation.Read(chest).GetAllItems().Any(item => item.m_stack > 0 && item.m_shared != null &&
                Localization.instance.Localize(item.m_shared.m_name).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                Matches.Add(chest);
        }
    }
}
