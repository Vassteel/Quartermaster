using Quartermaster;

internal static class GullTests
{
    internal static void Run(Action<bool,string> check)
    {
        var a=new UnityEngine.Vector3(-1,1,-1);
        var b=new UnityEngine.Vector3(1,1,-1);
        var c=new UnityEngine.Vector3(0,1,1);
        check(PerchGeometry.Height(a,b,c,0,0,out float y) && Math.Abs(y-1)<.00001f,"closed lid surface gives exact contact height");
        check(PerchGeometry.Height(c,b,a,0,0,out y) && Math.Abs(y-1)<.00001f,"lid winding does not affect height");
        check(!PerchGeometry.Height(a,b,c,2,0,out y),"nearby side geometry cannot raise the perch");
        c.y=2;
        check(PerchGeometry.Height(a,b,c,0,0,out y) && Math.Abs(y-1.5f)<.00001f,"sloping open lid uses surface intersection, not highest bound");
        check(PerchGeometry.Height(a,b,c,-1,-1,out y) && Math.Abs(y-1)<.00001f,"triangle edge contact remains supported");
        check(!PerchGeometry.Height(a,a,a,0,0,out y),"degenerate geometry cannot produce invalid perch heights");
        check(!PerchGeometry.Height(new(0,0,0),new(0,1,0),new(0,0,1),0,0,out y),"vertical lid edge is not a standing surface");
        var state=new DepositGullState();
        check(state.Get(10,false,true)==DepositGullMood.Idle,"empty chest gull idles");
        check(state.Get(10,true,true)==DepositGullMood.Idle,"new deposits await a real routing result");
        state.Report(10,3,true);
        check(state.Get(10.1f,true,true)==DepositGullMood.Sorting,"partial success plays sorting before blocked gesture");
        check(state.Get(12.5f,true,true)==DepositGullMood.NeedsAttention,"leftovers receive annoyed gesture after sorting");
        check(state.Get(12.5f,false,true)==DepositGullMood.Idle,"manual removal immediately clears annoyed state");
        check(state.Get(12.5f,true,false)==DepositGullMood.Idle,"open or inaccessible chest does not act busy or blocked");
        state.Report(13,0,true);
        check(state.Get(13,true,true)==DepositGullMood.NeedsAttention,"zero transfers never produce sorting effects");
        state.Report(14,2,false);
        check(state.Get(14,false,true)==DepositGullMood.Sorting,"successful final transfer remains visible after chest empties");
        check(state.Get(16.5f,false,true)==DepositGullMood.Idle,"successful completion returns to idle");
        state.Report(17,1,false); state.Report(19,0,false);
        check(state.Get(19.5f,false,true)==DepositGullMood.Idle,"empty polling does not extend the sorting flourish");
        state.Report(20,1,true); state.Reset();
        check(state.Get(20.1f,true,true)==DepositGullMood.Idle,"unmark or disable discards old effects and blockage");
        check(GullTossLife.Alpha(.4f,-1)==1,"airborne props remain visible");
        check(GullTossLife.Alpha(1,.1f)==1,"first floor bounce remains visible briefly");
        check(Math.Abs(GullTossLife.Alpha(1.5f,.625f)-.5f)<.001f,"props visibly fade after bounce");
        check(GullTossLife.Alpha(2,1)==0,"props disappear one second after floor bounce");
        check(GullTossLife.Alpha(3.75f,-1)==0,"props without a floor still expire");
        check(GullTossLife.Alpha(3.5f,.1f)<1,"late collision cannot reset overall lifetime");
        for(int i=0;i<=500;i++)
        {
            float airborne=GullTossLife.Alpha(i*.01f,-1);
            float bounced=GullTossLife.Alpha(i*.01f,i*.01f);
            check(airborne>=0 && airborne<=1 && bounced>=0 && bounced<=1,"fade remains within valid alpha range");
        }
    }
}
