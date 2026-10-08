using System;

namespace Quartermaster;

internal static class PatchCleanup
{
    // Unpatching rebuilds shared game methods and can itself fail in another
    // transpiler. Always attempt restoration and retain the original error log.
    internal static void Run(Action unpatch, Action restore, Action<string> logError)
    {
        try { unpatch(); }
        catch (Exception e) { logError("Quartermaster patch cleanup failed: " + e); }
        try { restore(); }
        catch (Exception e) { logError("Quartermaster stack restoration failed: " + e); }
    }
}
