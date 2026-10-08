using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;

namespace Quartermaster;

// Read the optional manager's public window state; never patch its internals.
internal static class ConfigurationManagerCompatibility
{
    private static readonly List<Func<bool>> Windows = new List<Func<bool>>();
    private static bool connected;
    private static bool failed;
    internal static bool IsOpen
    {
        get
        {
            if (!connected)
            {
                connected = true;
                foreach (var id in new[] { "_shudnal.ConfigurationManager", "com.bepis.bepinex.configurationmanager" })
                {
                    if (!Chainloader.PluginInfos.TryGetValue(id, out var plugin) || plugin.Instance == null) continue;
                    try
                    {
                        var getter = plugin.Instance.GetType().GetProperty("DisplayingWindow", BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod();
                        if (getter == null || getter.ReturnType != typeof(bool)) continue;
                        Windows.Add((Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), plugin.Instance, getter));
                    }
                    catch (Exception error) { Plugin.Log?.LogWarning("Configuration Manager window integration unavailable: " + error.GetBaseException().Message); }
                }
            }
            if (failed) return true;
            try
            {
                foreach (var window in Windows) if (window()) return true;
            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Log?.LogWarning("Quartermaster menus paused because Configuration Manager window state failed: " + error.GetBaseException().Message);
                return true;
            }
            return false;
        }
    }
}
