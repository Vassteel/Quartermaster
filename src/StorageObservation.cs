using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster;

// Read-only copies never participate in transfers or invoke Container save callbacks.
internal static class StorageObservation
{
    private sealed class Entry
    {
        internal ZDO Data;
        internal uint Revision;
        internal Inventory Inventory;
    }
    private static readonly Dictionary<Container, Entry> Copies = new Dictionary<Container, Entry>();
    internal static void Forget(Container chest) => Copies.Remove(chest);
    internal static void Clear() => Copies.Clear();
    internal static Inventory Read(Container chest)
    {
        var live = ContainerRegistry.SafeInventory(chest);
        var view = ContainerRegistry.GetView(chest);
        if (live == null || !view || !view.IsValid()) return live;
        if (view.IsOwner())
        {
            Copies.Remove(chest);
            ContainerRegistry.Refresh(chest);
            return live;
        }
        var data = view.GetZDO();
        int width = live.GetWidth(), height = live.GetHeight();
        if (Copies.TryGetValue(chest, out var entry) && ReferenceEquals(entry.Data, data)
            && entry.Revision == data.DataRevision && entry.Inventory.GetWidth() == width && entry.Inventory.GetHeight() == height)
            return entry.Inventory;
        // Deserialize into a new object before publishing; a bad packet cannot leave a partial cached inventory.
        var copy = new Inventory(chest.name, null, width, height);
        var bytes = data.GetByteArray(ZDOVars.s_items);
        if (bytes != null && bytes.Length > 0) copy.Load(new ZPackage(bytes));
        Copies[chest] = new Entry { Data = data, Revision = data.DataRevision, Inventory = copy };
        return copy;
    }
}
