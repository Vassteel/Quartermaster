using BepInEx.Bootstrap;

namespace Quartermaster;

internal static class FermenterCompatibility
{
    internal const string Notice="Automatic Fermenters is not compatible with Quartermaster fermenter automation; Quartermaster control is disabled for this station";
    internal static bool External { get; private set; }
    internal static void Initialize()
    {
        External=Chainloader.PluginInfos.ContainsKey("TastyChickenLegs.AutomaticFermenters");
        if(External)Plugin.Log.LogWarning(Notice+". Other Quartermaster features remain enabled.");
    }
}
