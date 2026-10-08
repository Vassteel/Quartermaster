using System.Collections.Generic;

namespace Quartermaster;

// Completed transfers queue only visual props. Busy bases coalesce overflow
// reports into one final batch; no items or ownership are retained here.
internal sealed class OwlSortReplay<T>
{
    private readonly Queue<T> pending=new Queue<T>();
    private readonly SlotThrows throws=new SlotThrows();
    private bool overflow,paused;
    private T overflowSample;
    internal T Sample { get; private set; }
    internal int Remaining=>throws.Remaining;
    internal bool Pending=>Remaining>0||pending.Count>0||overflow;
    internal void Report(T sample)
    {
        if(!overflow&&pending.Count<32)pending.Enqueue(sample);
        else {overflow=true;overflowSample=sample;}
    }
    internal bool BeginNext(float now)
    {
        if(Remaining>0)return false;
        if(pending.Count>0)Sample=pending.Dequeue();
        else if(overflow){Sample=overflowSample;overflow=false;overflowSample=default(T);}
        else return false;
        throws.Begin(now);paused=false;return true;
    }
    internal void Pause()=>paused=true;
    internal void Resume(float now)
    {if(paused){throws.Delay(now+.34f);paused=false;}}
    internal bool Take(float now)=>!paused&&throws.Take(now);
    internal void Clear(){pending.Clear();throws.Clear();Sample=overflowSample=default(T);overflow=paused=false;}
}
