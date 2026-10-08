using System;
namespace Quartermaster;
internal struct JarPose {internal float Draw,Lid,Tilt,Lean;}
internal static class ApothecaryMotion
{
    private static float Ease(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    internal static JarPose Sample(float time)
    {
        float draw=Ease(time/.8f)*(1-Ease((time-4)/.8f));
        float open=Ease((time-.8f)/.6f)*(1-Ease((time-3.3f)/.6f));
        return new JarPose{Draw=.23f*draw,Lid=.14f*open,Tilt=-18*open,Lean=draw*(1-.5f*open)};
    }
}
