using System;
namespace Quartermaster;

// Cosmetic only: inventory use is authoritative and no network/transfer depends on this state.
internal sealed class FurnitureDoors
{
    internal float Amount {get;private set;}
    private float requested;
    internal void Request(float value)=>requested=Finite(value)?Math.Max(0,Math.Min(1,value)):0;
    // 1 = hinge opening; -1 = latch closing; 0 = quiet. Called only on nearby door furniture.
    internal int Step(float seconds,bool inventoryOpen)
    {
        if(!Finite(seconds)||seconds<=0)return 0;
        float target=inventoryOpen?1:requested,old=Amount,step=Math.Min(seconds,.1f)*2;
        Amount=target>Amount?Math.Min(target,Amount+step):Math.Max(target,Amount-step);
        return old<=.08f&&Amount>.08f?1:old>.015f&&Amount<=.015f?-1:0;
    }
    internal void Reset(){Amount=0;requested=0;}
    internal static float OwlOpening(float time)=>Ease((time-.15f)/.65f)*(1-Ease((time-3.55f)/.7f));
    private static float Ease(float x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
    private static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
}
