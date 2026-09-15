namespace Quartermaster;

internal enum DepositGullMood { Idle, Sorting, NeedsAttention }

// Driven by completed routing passes, never by presentation strings or inventory guesses.
internal sealed class DepositGullState
{
    internal const float SortDuration = 2.4f;
    private float sortingUntil;
    private bool blocked;
    internal void Report(float now, int moved, bool needsAttention)
    {
        blocked = needsAttention;
        if (moved > 0) sortingUntil = now + SortDuration;
    }
    internal DepositGullMood Get(float now, bool hasItems, bool canWork)
    {
        if (!canWork) return DepositGullMood.Idle;
        if (now < sortingUntil) return DepositGullMood.Sorting;
        return hasItems && blocked ? DepositGullMood.NeedsAttention : DepositGullMood.Idle;
    }
    internal void Reset() { sortingUntil = 0; blocked = false; }
}
