namespace Quartermaster;

internal sealed class SlotThrows
{
    internal const int PerSlot=3;
    internal int Remaining { get; private set; }
    private float next;
    internal void Begin(float now) { Remaining=PerSlot; next=now+.34f; }
    internal bool Take(float now)
    {
        if(Remaining==0 || now<next) return false;
        Remaining--; next=now+.65f;
        return true;
    }
    internal void Clear() { Remaining=0; }
}
