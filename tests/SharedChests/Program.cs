using System;
using System.Collections.Generic;
using System.Linq;
using Quartermaster;

// Multi-user chest protocol: unit checks on the production core, then a randomized
// network simulation (latency, dropped replies, duplicated requests, ownership moving
// mid-flight, stale replicas, direct owner edits) asserting exact item conservation.
static class Program
{
    static int checks; static readonly Dictionary<string, long> Stats = new(); static void Count(string k) => Stats[k] = Stats.GetValueOrDefault(k) + 1;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception("FAILED: " + label); }

    sealed class Grid : ISharedChestSlots
    {
        public readonly Dictionary<SlotPos, SlotValue> Slots = new();
        public int W, H; public Func<SlotValue, bool> Policy = _ => true;
        public Grid(int w, int h) { W = w; H = h; }
        public int Width => W; public int Height => H;
        public SlotValue Get(int x, int y) => Slots.TryGetValue(new SlotPos(x, y), out var v) ? v : null;
        public bool Accepts(SlotValue v) => Policy(v);
        public void Apply(IReadOnlyList<SlotChange> changes)
        { foreach (var c in changes) { var p = new SlotPos(c.X, c.Y); if (c.After == null) Slots.Remove(p); else Slots[p] = c.After; } }
        public Grid Copy() { var g = new Grid(W, H) { Policy = Policy }; foreach (var kv in Slots) g.Slots[kv.Key] = kv.Value; return g; }
        public void Set(int x, int y, string key, int n) { var p = new SlotPos(x, y); if (n <= 0) Slots.Remove(p); else Slots[p] = new SlotValue(key, n); }
        public int Count(string key) => Slots.Values.Where(v => v.Key == key).Sum(v => v.Stack);
    }

    sealed class Sink : ISharedPlayerSink
    {
        public Grid Inv; public Dictionary<string, int> Dropped = new(); public Dictionary<string, int> Consumed = new();
        public void Give(EscrowLine line)
        {
            if (line.Destination == EscrowDestination.Drop) { Dropped[line.Key] = Dropped.GetValueOrDefault(line.Key) + line.Count; return; }
            if (line.Destination == EscrowDestination.Consume) { Consumed[line.Key] = Consumed.GetValueOrDefault(line.Key) + line.Count; return; }
            int left = line.Count;
            if (line.X >= 0)
            {
                var v = Inv.Get(line.X, line.Y);
                if (v == null) { Inv.Set(line.X, line.Y, line.Key, left); return; }
                if (v.Key == line.Key) { Inv.Set(line.X, line.Y, line.Key, v.Stack + left); return; }
            }
            foreach (var kv in Inv.Slots.ToList()) if (kv.Value.Key == line.Key) { Inv.Slots[kv.Key] = new SlotValue(line.Key, kv.Value.Stack + left); return; }
            for (int y = 0; y < Inv.H; y++) for (int x = 0; x < Inv.W; x++) if (Inv.Get(x, y) == null) { Inv.Set(x, y, line.Key, left); return; }
            Dropped[line.Key] = Dropped.GetValueOrDefault(line.Key) + left;
        }
    }

    static Dictionary<SlotPos, SlotValue> Snap(Grid g) => new(g.Slots);

    static void Main()
    {
        if (Environment.GetEnvironmentVariable("QM_SKIP_UNITS") != "1") Units();
        int seeds = int.TryParse(Environment.GetEnvironmentVariable("QM_SEEDS"), out int n) ? n : 400;
        long steps = 0;
        for (int seed = 1; seed <= seeds; seed++) steps += Simulate(seed, players: 2 + seed % 3);
        Console.WriteLine(string.Join(", ", Stats.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
        Console.WriteLine($"PASS: {checks} shared-chest checks ({seeds} randomized sessions, {steps} simulated actions).");
    }

    static void Units()
    {
        var A = "A"; var B = "B";
        // Owner: precondition match applies atomically; mismatch changes nothing.
        var chest = new Grid(4, 2); chest.Set(0, 0, A, 10); chest.Set(1, 0, B, 3);
        var take = new List<SlotChange> { new() { X = 0, Y = 0, Before = new SlotValue(A, 10), After = new SlotValue(A, 4) } };
        Check(SharedChestOwner.Execute(chest, take, 0, 10) == SharedTxResult.Applied && chest.Get(0, 0).Stack == 4, "matching before applies");
        Check(SharedChestOwner.Execute(chest, take, 0, 10) == SharedTxResult.Conflict && chest.Get(0, 0).Stack == 4, "stale before is refused and changes nothing");
        var two = new List<SlotChange> {
            new() { X = 1, Y = 0, Before = new SlotValue(B, 3), After = null },
            new() { X = 0, Y = 0, Before = new SlotValue(A, 99), After = null } };
        Check(SharedChestOwner.Execute(chest, two, 0, 10) == SharedTxResult.Conflict && chest.Get(1, 0)?.Stack == 3, "one stale slot refuses the whole transaction");
        Check(SharedChestOwner.Execute(chest, take, 11, 10) == SharedTxResult.Expired, "past deadline is refused");
        Check(SharedChestOwner.Decide(chest, new List<SlotChange> { new() { X = 4, Y = 0, After = new SlotValue(A, 1) } }, 0, 10) == SharedTxResult.Invalid, "out of bounds is invalid");
        Check(SharedChestOwner.Decide(chest, new List<SlotChange> { new() { X = 2, Y = 0, After = new SlotValue(A, 0) } }, 0, 10) == SharedTxResult.Invalid, "zero stack is invalid");
        Check(SharedChestOwner.Decide(chest, new List<SlotChange> { new() { X = 2, Y = 0, After = new SlotValue(A, 1) }, new() { X = 2, Y = 0, After = new SlotValue(A, 2) } }, 0, 10) == SharedTxResult.Invalid, "duplicate slot is invalid");
        Check(SharedChestOwner.Decide(chest, new List<SlotChange>(), 0, 10) == SharedTxResult.Invalid, "empty transaction is invalid");
        chest.Policy = v => v.Key != "Junk";
        Check(SharedChestOwner.Decide(chest, new List<SlotChange> { new() { X = 3, Y = 1, After = new SlotValue("Junk", 1) } }, 0, 10) == SharedTxResult.Denied, "owner storage policy refuses items it does not allow");
        chest.Policy = _ => true;

        // Duplicate delivery is answered from the receipt, never re-applied.
        var dup = new Grid(2, 1); dup.Set(0, 0, A, 5); string receipts = "";
        var add = new List<SlotChange> { new() { X = 0, Y = 0, Before = new SlotValue(A, 5), After = new SlotValue(A, 8) } };
        Check(SharedChestOwner.Handle(dup, ref receipts, 7, 1, add, 0, 10) == SharedTxResult.Applied, "first delivery applies");
        dup.Set(0, 0, A, 5); // even if the slot happens to match again, a repeat must not apply twice
        Check(SharedChestOwner.Handle(dup, ref receipts, 7, 1, add, 0, 10) == SharedTxResult.Applied && dup.Get(0, 0).Stack == 5, "repeat answered from receipt without re-applying");
        Check(SharedReceipts.TryFind(receipts, 7, 1, out var r1) && r1 == SharedTxResult.Applied && !SharedReceipts.TryFind(receipts, 8, 1, out _), "receipts are per sender");
        string many = ""; int total = SharedReceipts.Keep + 20; for (int i = 0; i < total; i++) many = SharedReceipts.Add(many, 1, i, SharedTxResult.Conflict);
        Check(!SharedReceipts.TryFind(many, 1, 0, out _) && SharedReceipts.TryFind(many, 1, total - 1, out _), "receipts are bounded, newest kept");

        // Plans: take, put, swap, internal player moves, non-conserving actions.
        var cb = new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(A, 10) };
        var ca = new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(A, 4) };
        var pb = new Dictionary<SlotPos, SlotValue>(); var pa = new Dictionary<SlotPos, SlotValue> { [new SlotPos(3, 1)] = new SlotValue(A, 6) };
        var plan = SharedPlan.Build(cb, ca, pb, pa);
        Check(plan.Conserved && plan.Changes.Count == 1 && plan.TakeIn.Count == 1 && plan.TakeIn[0].Count == 6 && plan.TakeIn[0].X == 3 && plan.GiveOut.Count == 0, "take withholds exactly the gained items at their slot");
        plan = SharedPlan.Build(ca, cb, pa, pb);
        Check(plan.Conserved && plan.GiveOut.Count == 1 && plan.GiveOut[0].Count == 6 && plan.TakeIn.Count == 0, "put records what to return on refusal");
        var swapCb = new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(A, 1) };
        var swapCa = new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(B, 1) };
        var swapPb = new Dictionary<SlotPos, SlotValue> { [new SlotPos(2, 0)] = new SlotValue(B, 1) };
        var swapPa = new Dictionary<SlotPos, SlotValue> { [new SlotPos(2, 0)] = new SlotValue(A, 1) };
        plan = SharedPlan.Build(swapCb, swapCa, swapPb, swapPa);
        Check(plan.Conserved && plan.TakeIn.Single().Key == A && plan.GiveOut.Single().Key == B, "swap withholds the taken item and can return the given one");
        var moveB = new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(B, 2), [new SlotPos(1, 0)] = new SlotValue(A, 5) };
        var moveA = new Dictionary<SlotPos, SlotValue> { [new SlotPos(1, 1)] = new SlotValue(B, 2), [new SlotPos(1, 0)] = new SlotValue(A, 2) };
        plan = SharedPlan.Build(cb, new Dictionary<SlotPos, SlotValue> { [new SlotPos(0, 0)] = new SlotValue(A, 13) }, moveB, moveA);
        Check(plan.Conserved && plan.TakeIn.Count == 0 && plan.GiveOut.Single().Count == 3, "internal player moves are not escrowed, only the net loss");
        plan = SharedPlan.Build(cb, ca, pb, pb);
        Check(!plan.Conserved, "items leaving the chest to nowhere (world drop) is detected");
        plan = SharedPlan.Build(cb, cb, pb, pa);
        Check(!plan.ChestChanged, "player-only actions produce no transaction");

        // Ledger: refusal returns given items, apply delivers withheld items, unknown ids ignored.
        var inv = new Grid(4, 2); var sink = new Sink { Inv = inv };
        var ledger = new SharedLedger(0);
        var t1 = ledger.Open("c", 2, 10, add, new[] { new EscrowLine { Key = A, Count = 6, X = 3, Y = 1 } }, null);
        var t2 = ledger.Open("c", 2, 10, add, null, new[] { new EscrowLine { Key = B, Count = 2, X = 0, Y = 0 } });
        ledger.Resolve(t1.Id, true, sink); ledger.Resolve(t2.Id, false, sink);
        Check(inv.Get(3, 1)?.Stack == 6 && inv.Get(0, 0)?.Key == B && ledger.Count == 0, "apply delivers withheld items; refusal returns given items");
        Check(ledger.Resolve(t1.Id, false, sink) == null && inv.Count(A) == 6, "a second resolution is ignored");
        var t3 = ledger.Open("c", 2, 10, add, new[] { new EscrowLine { Key = A, Count = 1 } }, null);
        string rc = SharedReceipts.Add("", 5, t3.Id, SharedTxResult.Applied);
        Check(ledger.Poll(5, _ => null, 1000, 10, sink).Count == 0 && ledger.Count == 1, "no decision while chest data is not visible");
        Check(ledger.Poll(5, _ => rc, 0, 10, sink).Single().Applied && inv.Count(A) == 7, "receipt in chest data resolves a lost reply");
        var t4 = ledger.Open("c", 2, 10, add, null, new[] { new EscrowLine { Key = B, Count = 1 } });
        Check(ledger.Poll(5, _ => "", 15, 10, sink).Count == 0, "no refusal before deadline plus grace");
        Check(!ledger.Poll(5, _ => "", 21, 10, sink).Single().Applied && inv.Count(B) == 3, "after deadline and grace with no receipt the given items come back");
        var full = new Grid(1, 1); full.Set(0, 0, B, 1); var fs = new Sink { Inv = full };
        fs.Give(new EscrowLine { Key = A, Count = 4, X = 0, Y = 0 });
        Check(fs.Dropped[A] == 4, "test sink drops at feet when inventory is full (never lost)");
    }

    // ---------------- randomized network simulation ----------------
    sealed class Msg { public long At; public int Kind; public int From, To; public SharedPendingTx Tx; public long TxId; public SharedTxResult Result; public Grid Snap; public string Receipts; public long Rev; }
    sealed class Peer
    {
        public int Id; public Grid View; public Grid LastSnap; public long ViewRev = -1; public Grid Inv; public Sink Sink; public SharedLedger Ledger; public string KnownReceipts = ""; public SharedRefusals Refused = new();
    }

    static long Simulate(int seed, int players)
    {
        var rng = new Random(seed);
        string[] keys = { "Wood", "Stone", "Iron", "Mead", "Sword|q2", "Sword|q3" };
        var chest = new Grid(6, 3); string receipts = ""; long rev = 0; int owner = 0;
        var peers = new List<Peer>();
        for (int i = 0; i < players; i++)
        {
            var p = new Peer { Id = i, Inv = new Grid(6, 4) }; p.Sink = new Sink { Inv = p.Inv }; p.Ledger = new SharedLedger(i * 1_000_000L); peers.Add(p);
            for (int k = 0; k < 6; k++) p.Inv.Set(rng.Next(6), rng.Next(4), keys[rng.Next(keys.Length)], 1 + rng.Next(30));
        }
        for (int k = 0; k < 10; k++) chest.Set(rng.Next(6), rng.Next(3), keys[rng.Next(keys.Length)], 1 + rng.Next(40));
        Dictionary<string, long> Totals()
        {
            var t = new Dictionary<string, long>();
            void Add(string k, long n) => t[k] = t.GetValueOrDefault(k) + n;
            foreach (var v in chest.Slots.Values) Add(v.Key, v.Stack);
            foreach (var p in peers) { foreach (var v in p.Inv.Slots.Values) Add(v.Key, v.Stack); foreach (var d in p.Sink.Dropped) Add(d.Key, d.Value); foreach (var d in p.Sink.Consumed) Add(d.Key, d.Value); }
            return t;
        }
        var initial = Totals();
        var net = new List<Msg>(); long now = 0; var truth = new Dictionary<(int, long), SharedTxResult>();
        const long deadlineIn = 60, grace = 40;
        double ownerCrash = rng.NextDouble() * 0.05, dropReply = rng.NextDouble() * 0.3, dupRequest = rng.NextDouble() * 0.2, ownerMove = rng.NextDouble() * 0.03;
        int maxLatency = 1 + rng.Next(25);
        long Lat() => 1 + rng.Next(maxLatency);
        void Broadcast() { rev++; foreach (var p in peers) if (p.Id != owner) net.Add(new Msg { At = now + Lat(), Kind = 2, To = p.Id, Snap = chest.Copy(), Receipts = receipts, Rev = rev }); }
        void Refresh(Peer p)
        {
            // Viewer reload: authoritative snapshot, then still-pending transactions overlaid in order.
            foreach (var tx in p.Ledger.Pending) SharedChestOwner.Overlay(p.View, tx.Changes);
        }
        foreach (var p in peers) { p.View = chest.Copy(); p.LastSnap = chest.Copy(); p.ViewRev = 0; }
        void Settled(Peer p, SharedPendingTx done)
        {
            if (done != null && truth.TryGetValue((p.Id, done.Id), out var real) && (real == SharedTxResult.Applied) != done.Applied)
                throw new Exception($"seed {seed}: tx {done.Id} of peer {p.Id} settled applied={done.Applied} but owner result was {real} (now {now}, deadline {done.Deadline})");
            // A refused transaction: reload the local copy from the last replica (the game forces a Load).
            if (done == null || done.Applied || p.Id == owner) return;
            p.View = p.LastSnap.Copy(); Refresh(p);
        }

        int actions = 150 + rng.Next(250);
        long performed = 0;
        for (int step = 0; step < actions || net.Count > 0 || peers.Any(p => p.Ledger.Count > 0); step++)
        {
            now++;
            if (now > 200000) throw new Exception("simulation did not settle (seed " + seed + ")");
            // Deliver due messages in time order (TCP per pair is ordered: equal At keeps insertion order).
            foreach (var m in net.Where(m => m.At <= now).OrderBy(m => m.At).ToList())
            {
                net.Remove(m);
                if (m.Kind == 1) // request to owner
                {
                    SharedTxResult result;
                    if (m.To != owner) result = SharedChestOwner.NotOwner(peers[m.To].KnownReceipts, peers[m.To].Refused, m.From, m.Tx.Id);
                    else
                    {
                        string before = receipts; var chestBefore = chest.Copy();
                        result = SharedChestOwner.Handle(chest, ref receipts, m.From, m.Tx.Id, m.Tx.Changes, now, m.Tx.Deadline, peers[owner].Refused);
                        if (!truth.ContainsKey((m.From, m.Tx.Id))) truth[(m.From, m.Tx.Id)] = result;
                        peers[owner].KnownReceipts = receipts; // the owner's own copy of the chest data is current
                        if (result == SharedTxResult.Applied && rng.NextDouble() < ownerCrash)
                        {
                            // Owner applied and answered, then crashed before its update reached the server:
                            // the world keeps the old contents and ownership moves on.
                            Count("owner-crash-after-reply");
                            chest.Slots.Clear(); foreach (var kv in chestBefore.Slots) chest.Slots[kv.Key] = kv.Value;
                            receipts = before; truth.Remove((m.From, m.Tx.Id));
                            net.Add(new Msg { At = now + Lat(), Kind = 3, To = m.From, TxId = m.Tx.Id, Result = result });
                            owner = rng.Next(players); peers[owner].View = chest.Copy(); peers[owner].KnownReceipts = receipts;
                            Broadcast();
                            continue;
                        }
                        if (receipts != before) Broadcast();
                    }
                    Count(result.ToString());
                    if (rng.NextDouble() >= dropReply) net.Add(new Msg { At = now + Lat(), Kind = 3, To = m.From, TxId = m.Tx.Id, Result = result });
                }
                else if (m.Kind == 2) // replica snapshot
                {
                    var p = peers[m.To];
                    if (m.Rev <= p.ViewRev) continue;
                    p.ViewRev = m.Rev; p.KnownReceipts = m.Receipts; p.LastSnap = m.Snap;
                    if (p.Id != owner) { p.View = m.Snap.Copy(); Refresh(p); }
                }
                else if (m.Kind == 3 && m.Result != SharedTxResult.Applied) Settled(peers[m.To], peers[m.To].Ledger.Resolve(m.TxId, false, peers[m.To].Sink)); // "Applied" replies are hints; receipts settle
            }
            // Viewers settle lost replies from receipts in their replica.
            foreach (var p in peers) foreach (var done in p.Ledger.Poll(p.Id, _ => p.KnownReceipts, now, grace, p.Sink)) { Count(done.Applied ? "settled-applied-by-receipt" : "settled-refused-by-receipt-or-timeout"); Settled(p, done); }
            // Ownership moves (server reassignment / grant): the new owner works on the authoritative data.
            if (rng.NextDouble() < ownerMove)
            {
                owner = rng.Next(players); Count("owner-moved");
                peers[owner].View = chest.Copy(); // new owner reloads authoritative data before any write
                Broadcast();
            }
            if (step >= actions) continue;
            var actor = peers[rng.Next(players)];
            bool isOwner = actor.Id == owner;
            var cv = isOwner ? chest.Copy() : actor.View.Copy();
            var pv = actor.Inv.Copy();
            var cb = Snap(cv); var pb = Snap(pv);
            int kind = rng.Next(10);
            if (kind == 9 && !isOwner && cv.Slots.Count > 0 && rng.Next(2) == 0)
            {
                // Right-click consume / drop to world from a chest the viewer does not own: built directly.
                var slot = cv.Slots.Keys.ElementAt(rng.Next(cv.Slots.Count)); var v = cv.Slots[slot];
                int n = rng.Next(2) == 0 ? 1 : v.Stack;
                var ch = new List<SlotChange> { new() { X = slot.X, Y = slot.Y, Before = v, After = v.Stack - n > 0 ? new SlotValue(v.Key, v.Stack - n) : null } };
                var line = new EscrowLine { Key = v.Key, Count = n, Destination = n == 1 ? EscrowDestination.Consume : EscrowDestination.Drop };
                var tx = actor.Ledger.Open("chest", owner, now + deadlineIn, ch, new[] { line }, null);
                SharedChestOwner.Overlay(actor.View, ch);
                Send(tx, actor.Id);
                performed++; continue;
            }
            Mutate(rng, kind, cv, pv);
            var plan = SharedPlan.Build(cb, Snap(cv), pb, Snap(pv));
            Check(plan.Conserved, "generated UI actions conserve items");
            if (!plan.ChestChanged) { actor.Inv.Slots.Clear(); foreach (var kv in pv.Slots) actor.Inv.Slots[kv.Key] = kv.Value; continue; }
            performed++;
            if (isOwner)
            {
                // Owner edits directly, as vanilla does for the owner.
                chest.Slots.Clear(); foreach (var kv in cv.Slots) chest.Slots[kv.Key] = kv.Value;
                actor.Inv.Slots.Clear(); foreach (var kv in pv.Slots) actor.Inv.Slots[kv.Key] = kv.Value;
                Broadcast();
                continue;
            }
            // Viewer: local copy shows the result; withheld gains leave the inventory now.
            actor.View = cv;
            actor.Inv.Slots.Clear(); foreach (var kv in pv.Slots) actor.Inv.Slots[kv.Key] = kv.Value;
            foreach (var line in plan.TakeIn)
            {
                var pos = new SlotPos(line.X, line.Y); var v = actor.Inv.Get(line.X, line.Y);
                Check(v != null && v.Key == line.Key && v.Stack >= line.Count, "withheld items are present at the slot that received them");
                actor.Inv.Set(line.X, line.Y, line.Key, v.Stack - line.Count);
            }
            var pending = actor.Ledger.Open("chest", owner, now + deadlineIn, plan.Changes, plan.TakeIn, plan.GiveOut);
            Send(pending, actor.Id);
        }
        void Send(SharedPendingTx tx, int from)
        {
            int to = (int)tx.Owner;
            var payload = new SharedPendingTx { Id = tx.Id, Deadline = tx.Deadline, Changes = tx.Changes };
            // Occasionally a request stalls far past the owner's deadline (lag spike): it must then be refused.
            long delay = rng.NextDouble() < 0.02 ? deadlineIn + grace + 20 + rng.Next(50) : Lat();
            if (delay > deadlineIn) Count("request-stalled");
            net.Add(new Msg { At = now + delay, Kind = 1, From = from, To = to, Tx = payload });
            if (rng.NextDouble() < dupRequest)
            {
                Count("duplicate-sent");
                net.Add(new Msg { At = now + Lat() + maxLatency, Kind = 1, From = from, To = to, Tx = payload });
            }
        }
        var final = Totals();
        foreach (var k in initial.Keys.Union(final.Keys))
            Check(initial.GetValueOrDefault(k) == final.GetValueOrDefault(k), $"seed {seed}: {k} conserved ({initial.GetValueOrDefault(k)} -> {final.GetValueOrDefault(k)})");
        Check(chest.Slots.Values.All(v => v.Stack > 0) && peers.All(p => p.Inv.Slots.Values.All(v => v.Stack > 0)), "no empty or negative stacks");
        return performed;
    }

    // Random UI-like actions on copies: take, put, swap, take-all, sort, internal player moves.
    static void Mutate(Random rng, int kind, Grid chest, Grid inv)
    {
        SlotPos Any(Grid g) => new SlotPos(rng.Next(g.W), rng.Next(g.H));
        switch (kind)
        {
            case 0: case 1: case 2: Move(chest, inv); break;
            case 3: case 4: case 5: Move(inv, chest); break;
            case 6: // take all that fits
                foreach (var kv in chest.Slots.ToList())
                {
                    var free = Enumerable.Range(0, inv.W * inv.H).Select(i => new SlotPos(i % inv.W, i / inv.W)).FirstOrDefault(p => inv.Get(p.X, p.Y) == null || inv.Get(p.X, p.Y).Key == kv.Value.Key);
                    if (inv.Get(free.X, free.Y) != null && inv.Get(free.X, free.Y).Key != kv.Value.Key) break;
                    var cur = inv.Get(free.X, free.Y);
                    inv.Set(free.X, free.Y, kv.Value.Key, (cur?.Stack ?? 0) + kv.Value.Stack); chest.Slots.Remove(kv.Key);
                }
                break;
            case 7: // sort / stack chest
                var items = chest.Slots.Values.GroupBy(v => v.Key).OrderBy(g => g.Key).Select(g => new SlotValue(g.Key, g.Sum(v => v.Stack))).ToList();
                chest.Slots.Clear(); for (int i = 0; i < items.Count; i++) chest.Slots[new SlotPos(i % chest.W, i / chest.W)] = items[i];
                break;
            default: Move(inv, inv); break;
        }
        void Move(Grid from, Grid to)
        {
            if (from.Slots.Count == 0) return;
            var src = from.Slots.Keys.ElementAt(rng.Next(from.Slots.Count)); var v = from.Slots[src];
            var dst = Any(to); if (ReferenceEquals(from, to) && dst.Equals(src)) return;
            int n = rng.Next(3) == 0 ? 1 + rng.Next(v.Stack) : v.Stack;
            var target = to.Get(dst.X, dst.Y);
            if (target == null) { to.Set(dst.X, dst.Y, v.Key, n); from.Set(src.X, src.Y, v.Key, v.Stack - n); }
            else if (target.Key == v.Key) { to.Set(dst.X, dst.Y, v.Key, target.Stack + n); from.Set(src.X, src.Y, v.Key, v.Stack - n); }
            else if (n == v.Stack) { to.Slots[dst] = v; from.Slots[src] = target; } // swap
        }
    }
}
