using System;

namespace Quartermaster;

internal static class GullTossLife
{
    internal static float Alpha(float age,float sinceFloorBounce)
    {
        float fade=Math.Max(0,(age-3f)/.75f);
        if(sinceFloorBounce>=0) fade=Math.Max(fade,(sinceFloorBounce-.25f)/.75f);
        return 1-Math.Min(1,Math.Max(0,fade));
    }
}
