using HarmonyLib;

namespace Quartermaster;

[HarmonyPatch]
internal static class ItemTagMessages
{
    internal const string MessageKey = "$achievements_cheated_item_inventory";
    private static bool Enabled => Plugin.Enabled.Value && Plugin.HideCheatItemMessages.Value;

    internal static string RemoveNotice(string tooltip, string localizedMessage)
    {
        if (string.IsNullOrEmpty(tooltip) || string.IsNullOrEmpty(localizedMessage)) return tooltip;
        return tooltip.Replace("\n<color=#808080><i>" + localizedMessage + "</i></color>", "");
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) }), HarmonyPostfix]
    internal static void Tooltip(ref string __result)
    {
        if (Enabled && Localization.instance != null)
            __result = RemoveNotice(__result, Localization.instance.Localize(MessageKey));
    }

    [HarmonyPatch(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) }), HarmonyPrefix]
    internal static void InventoryMessages(ref bool cheatedStateChanged, ref bool ___m_cheatedPopup)
    {
        if (!Enabled) return;
        // Suppress both pickup and subsequent removal notices without suppressing
        // normal inventory updates, save callbacks or unrelated game messages.
        cheatedStateChanged = false;
        ___m_cheatedPopup = false;
    }
}
