using UnityEngine;

namespace Quartermaster;

internal static class InventoryUiCompatibility
{
    internal static bool OwnsStatLayout(Transform parent) =>
        ExternalSlotCompatibility.HasInventorySlots && parent && parent.name == "InventorySlots_PlayerStatPanelHost";

    internal static RectTransform ActionPanel(InventoryGui gui)
    {
        if (!ExternalSlotCompatibility.HasInventorySlots || !gui || !gui.m_playerGrid || !gui.m_playerGrid.m_gridRoot) return null;
        var panel = gui.m_playerGrid.m_gridRoot.Find("InventorySlots_CustomSlotPanel") as RectTransform;
        return panel && panel.gameObject.activeInHierarchy ? panel : null;
    }
}
