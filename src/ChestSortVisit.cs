using System;
namespace Quartermaster;
internal enum ChestVisitPhase { Perched, Opening, Entering, Throwing, Returning }
// Local presentation follows an already completed transfer. It never moves items or
// changes Container.m_inUse, and cannot keep an unloaded chest from sorting.
internal sealed class ChestSortVisit
{
    internal const float OpenSeconds=.22f, HopSeconds=.65f;
    internal const float ArrivalDelay=OpenSeconds+HopSeconds;
    internal ChestVisitPhase Phase { get; private set; }
    private float since;
    internal bool Busy=>Phase!=ChestVisitPhase.Perched;
    internal bool CanThrow=>Phase==ChestVisitPhase.Throwing;
    internal void Begin(float now){Phase=ChestVisitPhase.Opening;since=now;}
    internal void Tick(float now,int remaining)
    {
        if(Phase==ChestVisitPhase.Opening && now-since>=OpenSeconds){Phase=ChestVisitPhase.Entering;since+=OpenSeconds;}
        if(Phase==ChestVisitPhase.Entering && now-since>=HopSeconds){Phase=ChestVisitPhase.Throwing;since+=HopSeconds;}
        if(Phase==ChestVisitPhase.Throwing && remaining==0){Phase=ChestVisitPhase.Returning;since=now;}
        if(Phase==ChestVisitPhase.Returning && now-since>=HopSeconds)Reset();
    }
    internal float Progress(float now)=>Math.Max(0,Math.Min(1,(now-since)/HopSeconds));
    internal void Reset(){Phase=ChestVisitPhase.Perched;since=0;}
}
