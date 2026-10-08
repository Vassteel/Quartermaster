using System.Collections.Generic;
namespace Quartermaster;

// Local scheduling hint only: never stored in world data or consulted by player actions.
internal sealed class AutomationCourtesy<T>
{
    private readonly Dictionary<T, float> Until = new Dictionary<T, float>();
    internal bool Yield(T chest, bool busy, float now)
    {
        if (busy) { Hold(chest, now + .5f); return true; }
        if (!Until.TryGetValue(chest, out var until)) return false;
        if (now < until) return true;
        Until.Remove(chest);
        return false;
    }
    // Extend (never shorten) the grace period, e.g. after another client announces a craft withdrawal.
    internal void Hold(T chest, float until)
    {
        if (!Until.TryGetValue(chest, out var current) || until > current) Until[chest] = until;
    }
    internal bool Holding(T chest, float now) => Until.TryGetValue(chest, out var until) && now < until;
    internal void Forget(T chest) => Until.Remove(chest);
    internal void Clear() => Until.Clear();
}
