using System;
using System.Collections.Generic;

namespace Quartermaster;

// Pure, incremental 3D A*. The caller supplies swept-body clearance, so a route
// cannot squeeze diagonally through a wall or depend on the humanoid navmesh.
internal struct OwlPoint
{
    internal float X, Y, Z;
    internal OwlPoint(float x,float y,float z) { X=x;Y=y;Z=z; }
    internal static float Distance(OwlPoint a,OwlPoint b)
    { float x=a.X-b.X,y=a.Y-b.Y,z=a.Z-b.Z;return (float)Math.Sqrt(x*x+y*y+z*z); }
}

internal sealed class OwlFlightPath
{
    internal enum Result { Searching, Found, Unreachable }
    internal Result State { get; private set; }
    internal readonly List<OwlPoint> Points=new List<OwlPoint>();
    internal int Expanded { get; private set; }
    private readonly float Cell;
    private const int Limit=4096;
    private readonly OwlPoint start,goal;
    private readonly Func<OwlPoint,OwlPoint,bool> clear;
    private readonly Dictionary<(int,int,int),Node> nodes=new Dictionary<(int,int,int),Node>();
    private readonly List<(Node node,float score)> heap=new List<(Node,float)>();
    private sealed class Node
    {
        internal int X,Y,Z;
        internal float Cost=float.MaxValue;
        internal Node Parent;
        internal bool Closed;
    }
    internal OwlFlightPath(OwlPoint start,OwlPoint goal,Func<OwlPoint,OwlPoint,bool> clear,float resolution=.75f)
    {
        this.start=start;this.goal=goal;this.clear=clear;Cell=Math.Max(.35f,Math.Min(1.5f,resolution));
        if(!Finite(start)||!Finite(goal)||OwlPoint.Distance(start,goal)>210||!clear(start,start)||!clear(goal,goal))
        { State=Result.Unreachable;return; }
        if(clear(start,goal)) { Points.Add(start);Points.Add(goal);State=Result.Found;return; }
        var first=new Node{Cost=0};nodes.Add((0,0,0),first);Push(first,OwlPoint.Distance(start,goal));
    }
    private static bool Finite(OwlPoint p)=>!float.IsNaN(p.X+p.Y+p.Z)&&!float.IsInfinity(p.X+p.Y+p.Z);
    private OwlPoint Position(Node n)=>new OwlPoint(start.X+n.X*Cell,start.Y+n.Y*Cell,start.Z+n.Z*Cell);
    internal void Step(int budget)
    {
        if(State!=Result.Searching)return;
        for(int i=0;i<budget&&heap.Count>0;i++)
        {
            var n=Pop();if(n.Closed)continue;n.Closed=true;Expanded++;
            var p=Position(n);
            if(OwlPoint.Distance(p,goal)<Cell*2.5f&&clear(p,goal)) { Finish(n);return; }
            if(Expanded>=Limit) { State=Result.Unreachable;return; }
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int x=n.X+(axis==0?sign:0),y=n.Y+(axis==1?sign:0),z=n.Z+(axis==2?sign:0);
                var point=new OwlPoint(start.X+x*Cell,start.Y+y*Cell,start.Z+z*Cell);
                if(point.X<Math.Min(start.X,goal.X)-12||point.X>Math.Max(start.X,goal.X)+12||
                   point.Z<Math.Min(start.Z,goal.Z)-12||point.Z>Math.Max(start.Z,goal.Z)+12||
                   point.Y<Math.Min(start.Y,goal.Y)-6||point.Y>Math.Max(start.Y,goal.Y)+9)continue;
                var key=(x,y,z);
                if(nodes.TryGetValue(key,out var next)&&next.Closed)continue;
                float cost=n.Cost+Cell*(axis==1?1.2f:1);
                if(next!=null&&cost>=next.Cost)continue;
                if(!clear(p,point))continue;
                if(next==null)
                {
                    if(nodes.Count>=Limit*2) { State=Result.Unreachable;return; }
                    next=new Node{X=x,Y=y,Z=z};nodes.Add(key,next);
                }
                next.Cost=cost;next.Parent=n;Push(next,cost+OwlPoint.Distance(point,goal));
            }
        }
        if(heap.Count==0)State=Result.Unreachable;
    }
    private void Finish(Node n)
    {
        var raw=new List<OwlPoint>{goal};
        for(;n!=null;n=n.Parent)raw.Add(Position(n));
        raw.Reverse();Points.Add(raw[0]);
        for(int i=0;i<raw.Count-1;)
        {
            // Bounded look-ahead smoothing. Every shortcut is clearance checked.
            int next=Math.Min(i+8,raw.Count-1);
            while(next>i+1&&!clear(raw[i],raw[next]))next--;
            Points.Add(raw[next]);i=next;
        }
        State=Result.Found;
    }
    private void Push(Node n,float score)
    {
        heap.Add((n,score));int i=heap.Count-1;
        while(i>0)
        { int parent=(i-1)/2;if(heap[parent].score<=score)break;heap[i]=heap[parent];i=parent; }
        heap[i]=(n,score);
    }
    private Node Pop()
    {
        var result=heap[0].node;var last=heap[heap.Count-1];heap.RemoveAt(heap.Count-1);
        if(heap.Count==0)return result;
        int i=0;
        while(i*2+1<heap.Count)
        {
            int child=i*2+1;if(child+1<heap.Count&&heap[child+1].score<heap[child].score)child++;
            if(heap[child].score>=last.score)break;heap[i]=heap[child];i=child;
        }
        heap[i]=last;return result;
    }
}
