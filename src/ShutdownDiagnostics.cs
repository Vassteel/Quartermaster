using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
namespace Quartermaster;

// Stage markers only: do not bypass saves, cloud checks, worker joins, or shutdown.
// A missing completion marker in the next hang log identifies where to investigate.
[HarmonyPatch]
internal static class ShutdownDiagnostics
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Menu),"OnQuitYes",Type.EmptyTypes);
        yield return AccessTools.Method(typeof(Game),"Shutdown",new[]{typeof(bool)});
        yield return AccessTools.Method(typeof(ZNetScene),"Shutdown",Type.EmptyTypes);
        yield return AccessTools.Method(typeof(ZNet),"Shutdown",new[]{typeof(bool)});
        yield return AccessTools.Method(typeof(HeightmapBuilder),"Dispose",Type.EmptyTypes);
    }
    private static void Prefix(MethodBase __originalMethod,out long __state)
    {
        __state=Stopwatch.GetTimestamp();
        Plugin.Log.LogInfo("Shutdown stage begin: "+__originalMethod.DeclaringType.Name+"."+__originalMethod.Name);
    }
    private static void Postfix(MethodBase __originalMethod,long __state)
    {
        double ms=(Stopwatch.GetTimestamp()-__state)*1000d/Stopwatch.Frequency;
        Plugin.Log.LogInfo("Shutdown stage end: "+__originalMethod.DeclaringType.Name+"."+__originalMethod.Name+" ("+ms.ToString("F0")+" ms)");
    }
}
