using Quartermaster;
internal static class ChestVisitTests
{
    internal static void Run(Action<bool,string> check)
    {
        var visit=new ChestSortVisit();var throws=new SlotThrows();
        check(!visit.Busy && !visit.CanThrow,"perched owl leaves lid and next slot free");
        visit.Begin(10);throws.Begin(10+ChestSortVisit.ArrivalDelay);
        visit.Tick(10.1f,throws.Remaining);
        check(visit.Phase==ChestVisitPhase.Opening && !visit.CanThrow,"lid opens before owl enters");
        visit.Tick(10.4f,throws.Remaining);
        check(visit.Phase==ChestVisitPhase.Entering && !visit.CanThrow && visit.Progress(10.4f)>0,"hop-in precedes all throws");
        visit.Tick(10.9f,throws.Remaining);
        check(visit.CanThrow && !throws.Take(10.9f),"scoop waits until owl is inside");
        int tossed=0;float now=10.9f;
        for(int i=0;i<50;i++,now+=.05f)
        {
            visit.Tick(now,throws.Remaining);
            if(visit.CanThrow && throws.Take(now))tossed++;
            if(tossed==3)break;
        }
        visit.Tick(now,throws.Remaining);
        check(tossed==3 && visit.Phase==ChestVisitPhase.Returning && visit.Busy,"three throws finish before hop back; lid stays open");
        visit.Tick(now+.3f,0);
        check(visit.Phase==ChestVisitPhase.Returning && !visit.CanThrow,"return hop cannot emit extra items");
        visit.Tick(now+1,0);
        check(!visit.Busy && visit.Phase==ChestVisitPhase.Perched,"return completes before next slot starts");
        visit.Begin(100);visit.Tick(110,3);
        check(visit.CanThrow && visit.Busy,"slow frames do not skip unplayed throws");
        visit.Reset();check(!visit.Busy && !visit.CanThrow,"hide or unload releases cosmetic lid immediately");
    }
}
