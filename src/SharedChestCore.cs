using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quartermaster;

// Multi-user chests: pure protocol logic, no game types (tested by tests/SharedChests).
//
// Only the chest's network owner ever changes the authoritative contents. A player
// viewing a chest they do not own edits a local copy; each UI action becomes one
// transaction of exact slot changes (before -> after). The owner applies it only if
// every "before" still matches, atomically, and records a receipt in the chest's own
// world data. Items never exist twice: what the viewer gives the chest leaves their
// inventory at once and comes back if the owner refuses; what they take from the chest
// is held back and only enters their inventory once the owner has applied the change.

internal sealed class SlotValue : IEquatable<SlotValue>
{
    internal readonly string Key;   // canonical item serialization with stack 1, slot 0, unequipped
    internal readonly int Stack;
    internal SlotValue(string key, int stack) { Key = key ?? ""; Stack = stack; }
    public bool Equals(SlotValue other) => other != null && other.Stack == Stack && string.Equals(other.Key, Key, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SlotValue);
    public override int GetHashCode() => Key.GetHashCode() * 31 + Stack;
    internal static bool Same(SlotValue a, SlotValue b) => a == null ? b == null : a.Equals(b);
    public override string ToString() => Stack + "x" + (Key.Length > 12 ? Key.Substring(0, 12) : Key);
}

internal readonly struct SlotPos : IEquatable<SlotPos>
{
    internal readonly int X, Y;
    internal SlotPos(int x, int y) { X = x; Y = y; }
    public bool Equals(SlotPos o) => o.X == X && o.Y == Y;
    public override bool Equals(object obj) => obj is SlotPos o && Equals(o);
    public override int GetHashCode() => X * 4099 + Y;
    public override string ToString() => X + "," + Y;
}

internal sealed class SlotChange
{
    internal int X, Y;
    internal SlotValue Before, After;   // null = empty slot
}

internal enum SharedTxResult : byte { Applied = 1, Conflict = 2, NotOwner = 3, Denied = 4, Expired = 5, Invalid = 6 }

internal interface ISharedChestSlots
{
    int Width { get; }
    int Height { get; }
    SlotValue Get(int x, int y);
    bool Accepts(SlotValue value);
    void Apply(IReadOnlyList<SlotChange> changes);
}

internal static class SharedChestOwner
{
    internal const int MaxChanges = 256, MaxStack = 65535;

    // Validation and precondition check, then atomic apply. The caller is the chest's
    // current owner; nothing else may change the chest between check and apply.
    internal static SharedTxResult Decide(ISharedChestSlots chest, IReadOnlyList<SlotChange> changes, long now, long deadline)
    {
        if (changes == null || changes.Count == 0 || changes.Count > MaxChanges) return SharedTxResult.Invalid;
        var seen = new HashSet<SlotPos>();
        foreach (var c in changes)
        {
            if (c == null || c.X < 0 || c.Y < 0 || c.X >= chest.Width || c.Y >= chest.Height) return SharedTxResult.Invalid;
            if (!seen.Add(new SlotPos(c.X, c.Y))) return SharedTxResult.Invalid;
            if (!ValidValue(c.Before) || !ValidValue(c.After)) return SharedTxResult.Invalid;
        }
        if (now > deadline) return SharedTxResult.Expired;
        foreach (var c in changes)
            if (c.After != null && !SlotValue.Same(c.Before, c.After) && !chest.Accepts(c.After)) return SharedTxResult.Denied;
        foreach (var c in changes)
            if (!SlotValue.Same(chest.Get(c.X, c.Y), c.Before)) return SharedTxResult.Conflict;
        return SharedTxResult.Applied;
    }

    internal static SharedTxResult Execute(ISharedChestSlots chest, IReadOnlyList<SlotChange> changes, long now, long deadline)
    {
        var result = Decide(chest, changes, now, deadline);
        if (result == SharedTxResult.Applied) chest.Apply(changes);
        return result;
    }

    // Full owner handling: a repeated request is answered from its receipt and never re-applied.
    // receipts is the chest's stored receipt text; the updated text is returned for saving with the contents.
    // refused remembers requests this client answered "not owner" (it could not record a receipt then),
    // so a late duplicate can never apply after the viewer has already taken its items back.
    internal static SharedTxResult Handle(ISharedChestSlots chest, ref string receipts, long sender, long id,
        IReadOnlyList<SlotChange> changes, long now, long deadline, SharedRefusals refused = null)
    {
        if (SharedReceipts.TryFind(receipts, sender, id, out var earlier)) return earlier;
        if (refused != null && refused.Contains(sender, id)) return SharedTxResult.NotOwner;
        var result = Execute(chest, changes, now, deadline);
        receipts = SharedReceipts.Add(receipts, sender, id, result);
        return result;
    }

    // A client that is not (or no longer) the owner answers from its copy of the receipts:
    // a request it already applied while it was the owner must not be reported as refused.
    internal static SharedTxResult NotOwner(string receipts, SharedRefusals refused, long sender, long id)
    {
        if (SharedReceipts.TryFind(receipts, sender, id, out var earlier)) return earlier;
        refused?.Add(sender, id);
        return SharedTxResult.NotOwner;
    }

    private static bool ValidValue(SlotValue v) => v == null || (v.Key.Length > 0 && v.Stack >= 1 && v.Stack <= MaxStack);

    // Overlay a still-pending transaction onto a freshly reloaded local copy: only when
    // every "before" still matches (otherwise it was applied already, or will be refused).
    internal static bool Overlay(ISharedChestSlots view, IReadOnlyList<SlotChange> changes)
    {
        foreach (var c in changes) if (!SlotValue.Same(view.Get(c.X, c.Y), c.Before)) return false;
        view.Apply(changes);
        return true;
    }
}

internal sealed class SharedRefusals
{
    private readonly HashSet<(long, long)> set = new HashSet<(long, long)>();
    private readonly Queue<(long, long)> order = new Queue<(long, long)>();
    internal const int Keep = 1024;
    internal void Add(long sender, long id)
    {
        if (!set.Add((sender, id))) return;
        order.Enqueue((sender, id));
        while (order.Count > Keep) set.Remove(order.Dequeue());
    }
    internal bool Contains(long sender, long id) => set.Contains((sender, id));
    internal void Clear() { set.Clear(); order.Clear(); }
}

// Receipts live in the chest's own world data, written in the same update as the
// contents they describe: "sender:id:result;..." newest last, bounded.
internal static class SharedReceipts
{
    internal const int Keep = 128;
    internal static string Add(string stored, long sender, long id, SharedTxResult result)
    {
        var entries = Parse(stored).Where(e => !(e.Sender == sender && e.Id == id)).ToList();
        entries.Add((sender, id, result));
        if (entries.Count > Keep) entries.RemoveRange(0, entries.Count - Keep);
        var sb = new StringBuilder();
        foreach (var e in entries) sb.Append(e.Sender).Append(':').Append(e.Id).Append(':').Append((int)e.Result).Append(';');
        return sb.ToString();
    }
    internal static bool TryFind(string stored, long sender, long id, out SharedTxResult result)
    {
        foreach (var e in Parse(stored))
            if (e.Sender == sender && e.Id == id) { result = e.Result; return true; }
        result = 0; return false;
    }
    private static IEnumerable<(long Sender, long Id, SharedTxResult Result)> Parse(string stored)
    {
        if (string.IsNullOrEmpty(stored)) yield break;
        foreach (var part in stored.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var f = part.Split(':');
            if (f.Length != 3 || !long.TryParse(f[0], out long s) || !long.TryParse(f[1], out long i) || !int.TryParse(f[2], out int r)) continue;
            yield return (s, i, (SharedTxResult)r);
        }
    }
}

// What one UI action did, computed from before/after snapshots of the chest and the
// viewer's own inventory.
internal sealed class SharedPlan
{
    internal readonly List<SlotChange> Changes = new List<SlotChange>();
    internal readonly List<EscrowLine> TakeIn = new List<EscrowLine>();   // net gains: withheld until the owner applies
    internal readonly List<EscrowLine> GiveOut = new List<EscrowLine>();  // net losses: returned if the owner refuses
    internal bool Conserved;
    internal bool ChestChanged => Changes.Count > 0;

    internal static SharedPlan Build(IDictionary<SlotPos, SlotValue> chestBefore, IDictionary<SlotPos, SlotValue> chestAfter,
        IDictionary<SlotPos, SlotValue> playerBefore, IDictionary<SlotPos, SlotValue> playerAfter)
    {
        var plan = new SharedPlan();
        foreach (var pos in chestBefore.Keys.Union(chestAfter.Keys).OrderBy(p => p.Y).ThenBy(p => p.X))
        {
            chestBefore.TryGetValue(pos, out var b); chestAfter.TryGetValue(pos, out var a);
            if (!SlotValue.Same(a, b)) plan.Changes.Add(new SlotChange { X = pos.X, Y = pos.Y, Before = b, After = a });
        }
        var delta = new Dictionary<string, long>(StringComparer.Ordinal);
        void Add(string key, long n) { if (n == 0) return; delta.TryGetValue(key, out long v); delta[key] = v + n; }
        foreach (var c in plan.Changes) { if (c.After != null) Add(c.After.Key, c.After.Stack); if (c.Before != null) Add(c.Before.Key, -c.Before.Stack); }
        // Per-slot player gains and losses by key, so withheld items come from the slots that received them.
        var gains = new Dictionary<string, List<(SlotPos Pos, int Count)>>(StringComparer.Ordinal);
        var losses = new Dictionary<string, List<(SlotPos Pos, int Count)>>(StringComparer.Ordinal);
        var playerNet = new Dictionary<string, long>(StringComparer.Ordinal);
        void Note(Dictionary<string, List<(SlotPos, int)>> map, string key, SlotPos pos, int n)
        { if (n <= 0) return; if (!map.TryGetValue(key, out var l)) map[key] = l = new List<(SlotPos, int)>(); l.Add((pos, n)); }
        foreach (var pos in playerBefore.Keys.Union(playerAfter.Keys).OrderBy(p => p.Y).ThenBy(p => p.X))
        {
            playerBefore.TryGetValue(pos, out var b); playerAfter.TryGetValue(pos, out var a);
            if (SlotValue.Same(a, b)) continue;
            if (a != null && b != null && a.Key == b.Key)
            {
                int d = a.Stack - b.Stack;
                if (d > 0) Note(gains, a.Key, pos, d); else Note(losses, a.Key, pos, -d);
                Add(a.Key, d); playerNet[a.Key] = (playerNet.TryGetValue(a.Key, out long n0) ? n0 : 0) + d;
                continue;
            }
            if (b != null) { Note(losses, b.Key, pos, b.Stack); Add(b.Key, -b.Stack); playerNet[b.Key] = (playerNet.TryGetValue(b.Key, out long n1) ? n1 : 0) - b.Stack; }
            if (a != null) { Note(gains, a.Key, pos, a.Stack); Add(a.Key, a.Stack); playerNet[a.Key] = (playerNet.TryGetValue(a.Key, out long n2) ? n2 : 0) + a.Stack; }
        }
        plan.Conserved = delta.Values.All(v => v == 0);
        foreach (var kv in playerNet.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            long left = Math.Abs(kv.Value);
            if (left == 0) continue;
            var sources = kv.Value > 0 ? (gains.TryGetValue(kv.Key, out var g) ? g : null) : (losses.TryGetValue(kv.Key, out var l) ? l : null);
            var target = kv.Value > 0 ? plan.TakeIn : plan.GiveOut;
            if (sources != null)
                foreach (var (pos, n) in sources)
                {
                    if (left == 0) break;
                    int take = (int)Math.Min(left, n);
                    target.Add(new EscrowLine { Key = kv.Key, Count = take, X = pos.X, Y = pos.Y });
                    left -= take;
                }
            if (left > 0) target.Add(new EscrowLine { Key = kv.Key, Count = (int)left, X = -1, Y = -1 });
        }
        return plan;
    }
}

internal enum EscrowDestination : byte { Inventory = 0, Drop = 1, Consume = 2 }

internal sealed class EscrowLine
{
    internal string Key;
    internal int Count;
    internal int X = -1, Y = -1;          // preferred slot when given back
    internal EscrowDestination Destination;
}

internal interface ISharedPlayerSink
{
    // Must never lose items: add to inventory (preferred slot first), else drop at the player's feet.
    void Give(EscrowLine line);
}

internal sealed class SharedPendingTx
{
    internal long Id;
    internal string Chest;               // chest identity (ZDOID text in the game)
    internal long Owner;                 // session the request was sent to
    internal long Deadline;              // owner refuses after this (game time ticks)
    internal List<SlotChange> Changes;
    internal List<EscrowLine> TakeIn = new List<EscrowLine>(), GiveOut = new List<EscrowLine>();
    internal bool Applied;
}

// The viewer's side: every pending transaction and its withheld items. Resolution is by the
// owner's reply, or by finding the receipt in the chest's world data, or (only after the
// owner's deadline has passed with the chest data visible and no receipt) as refused.
internal sealed class SharedLedger
{
    private readonly Dictionary<long, SharedPendingTx> pending = new Dictionary<long, SharedPendingTx>();
    private readonly List<long> order = new List<long>();
    private long next;
    internal int Count => pending.Count;
    internal IEnumerable<SharedPendingTx> Pending => order.Select(id => pending[id]);
    internal IEnumerable<SharedPendingTx> PendingFor(string chest) => Pending.Where(p => p.Chest == chest);

    internal SharedLedger(long seed) { next = seed; }

    internal SharedPendingTx Open(string chest, long owner, long deadline, List<SlotChange> changes, IEnumerable<EscrowLine> takeIn, IEnumerable<EscrowLine> giveOut)
    {
        var tx = new SharedPendingTx { Id = ++next, Chest = chest, Owner = owner, Deadline = deadline, Changes = changes };
        if (takeIn != null) tx.TakeIn.AddRange(takeIn);
        if (giveOut != null) tx.GiveOut.AddRange(giveOut);
        pending[tx.Id] = tx; order.Add(tx.Id);
        return tx;
    }

    // Returns the resolved transaction, or null if it was unknown/already resolved.
    internal SharedPendingTx Resolve(long id, bool applied, ISharedPlayerSink sink)
    {
        if (!pending.TryGetValue(id, out var tx)) return null;
        pending.Remove(id); order.Remove(id);
        tx.Applied = applied;
        foreach (var line in applied ? tx.TakeIn : tx.GiveOut) if (line.Count > 0) sink.Give(line);
        return tx;
    }

    // receipts(chest) returns the chest's stored receipts, or null when its data is not visible here.
    internal List<SharedPendingTx> Poll(long self, Func<string, string> receipts, long now, long grace, ISharedPlayerSink sink)
    {
        var resolved = new List<SharedPendingTx>();
        foreach (var tx in Pending.ToList())
        {
            string stored = receipts(tx.Chest);
            if (stored == null) continue;
            if (SharedReceipts.TryFind(stored, self, tx.Id, out var r)) { var done = Resolve(tx.Id, r == SharedTxResult.Applied, sink); if (done != null) resolved.Add(done); }
            else if (now > tx.Deadline + grace) { var done = Resolve(tx.Id, false, sink); if (done != null) resolved.Add(done); }
        }
        return resolved;
    }

    // Leaving the world with unresolved transactions: what could not be settled.
    internal List<SharedPendingTx> Abandon() { var all = Pending.ToList(); pending.Clear(); order.Clear(); return all; }
}
