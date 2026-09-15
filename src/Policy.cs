using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

[Serializable]
public sealed class ChestSettings
{
    public int Version = 1;
    public bool Deposit;
    public bool AcceptStorage = true;
    public bool CraftingSupply = true;
    public bool FuelSupply = true;
    public bool ProcessingSupply = true;
    public bool LivestockFeed;
    public bool FeedUntamed;
    public bool Preferred;
    public bool Overflow;
    public bool AutoSort;
    public bool Learn = true;
    public string Group = "Home";
    public List<string> Remembered = new List<string>();
    public List<string> Forgotten = new List<string>();

    public bool Observe(IEnumerable<string> types)
    {
        if (Deposit || Overflow || !Learn) return false;
        bool changed = false;
        foreach (string type in types)
            if (!string.IsNullOrEmpty(type) && !Forgotten.Contains(type) && !Remembered.Contains(type))
            { Remembered.Add(type); changed = true; }
        return changed;
    }
    public void Forget(string type)
    {
        Remembered.Remove(type);
        if (!Forgotten.Contains(type)) Forgotten.Add(type);
    }
    public void Restore(string type)
    {
        Forgotten.Remove(type);
        if (!Remembered.Contains(type)) Remembered.Add(type);
    }
    public bool Accepts(string type) => AcceptStorage && !Deposit &&
        (Overflow || (Remembered.Contains(type) && !Forgotten.Contains(type)));
}

[Serializable]
public sealed class ProductCap
{
    public string Item;
    public int Amount = 200;
}

[Serializable]
public sealed class MachineSettings
{
    public bool Paused;
    public bool AllowValuableWood;
    public string Group = "Home";
    public List<ProductCap> Caps = new List<ProductCap>();
    public int Cap(string item) => Caps.FirstOrDefault(c => c.Item == item)?.Amount ?? 200;
    public void SetCap(string item, int amount)
    {
        if (string.IsNullOrEmpty(item)) throw new ArgumentException("A production cap requires an output item", nameof(item));
        if (amount < 0 || amount > 100000) throw new ArgumentOutOfRangeException(nameof(amount));
        var cap = Caps.FirstOrDefault(c => c.Item == item);
        if (cap == null) Caps.Add(new ProductCap { Item = item, Amount = amount });
        else cap.Amount = amount;
    }
    public bool MigrateCapId(string oldId, string item)
    {
        if (string.IsNullOrEmpty(oldId) || oldId == item) return false;
        var old = Caps.FirstOrDefault(c => c.Item == oldId);
        if (old == null) return false;
        if (!Caps.Any(c => c.Item == item)) Caps.Add(new ProductCap { Item = item, Amount = old.Amount });
        Caps.RemoveAll(c => c.Item == oldId);
        return true;
    }
}

public static class Policy
{
    public const long OutputDelayTicks = TimeSpan.TicksPerSecond * 60;
    public static bool OutputReady(long now, long ready) => ready > 0 && now >= ready;
    public static bool CookingSlotReady(string item, int status) => !string.IsNullOrEmpty(item) && (status == 1 || status == 2);
    public static bool CookingSlotProduces(string slot, string raw, string cooked, string output) => !string.IsNullOrEmpty(slot) && output == cooked && (slot == raw || slot == cooked);
    public static float ValidProcessorAccumulator(float accumulated) => float.IsNaN(accumulated) || float.IsInfinity(accumulated) || accumulated < 0 ? 0f : accumulated;
    // Whole batches only: never knowingly enqueue a batch that exceeds the cap.
    public static int BatchesAllowed(int cap, long stored, long queued, int batchSize, int queueSpace)
    {
        if (cap <= 0 || batchSize <= 0 || queueSpace <= 0) return 0;
        long room = cap - Math.Min((long)cap, Math.Max(0L, stored));
        room -= Math.Min(room, Math.Max(0L, queued));
        return (int)Math.Min(queueSpace, room / batchSize);
    }
    public static string Group(string value) => (value ?? "Home").Trim().ToLowerInvariant();
    public static bool SameGroup(string a, string b) => Group(a) == Group(b);
    public static bool ProtectedWood(string prefab) => prefab == "FineWood" || prefab == "RoundLog" || prefab == "YggdrasilWood" || prefab == "Blackwood";
}
