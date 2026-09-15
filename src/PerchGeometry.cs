using System;
using UnityEngine;

namespace Quartermaster;

internal static class PerchGeometry
{
    // Vertical intersection with a triangle, independent of its winding or world orientation.
    internal static bool Height(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float height)
    {
        height = 0;
        float denominator = (b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
        if (Math.Abs(denominator)<.0000001f) return false;
        float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/denominator;
        float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/denominator;
        float w=1-u-v;
        if(u<-.0001f || v<-.0001f || w<-.0001f) return false;
        height=u*a.y+v*b.y+w*c.y; return true;
    }
}
