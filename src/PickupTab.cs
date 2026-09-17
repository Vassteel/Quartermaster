using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Quartermaster;

// Reuse the stat tab itself: its wooden child, font material, outline and dimensions
// are different from the generic inventory action buttons.
internal static class PickupTab
{
    private static GameObject root;
    private static TMP_Text count;
    private static UITooltip tooltip;
    private static RectTransform armor, weight;
    private static int lastCount = -1;

    internal static void Attach(InventoryGui gui, Action open)
    {
        Dispose();
        if (!gui.m_armor || !gui.m_weight) return;
        armor = gui.m_armor.transform.parent as RectTransform;
        weight = gui.m_weight.transform.parent as RectTransform;
        var background = armor ? armor.Find("bkg")?.GetComponent<Image>() : null;
        if (!background || !background.sprite || !weight)
        {
            Plugin.Log.LogWarning("Pickup tab could not find the native inventory stat background.");
            return;
        }
        root = Object.Instantiate(armor.gameObject, armor.parent, false);
        root.name = "Quartermaster_Pickup";
        var icon = root.transform.Find("armor_icon");
        if (icon) icon.gameObject.SetActive(false);
        count = root.GetComponentInChildren<TMP_Text>(true);
        count.richText = false;
        count.raycastTarget = false;
        count.text = "0";
        count.rectTransform.anchoredPosition = new Vector2(count.rectTransform.anchoredPosition.x, -10);
        var title = Object.Instantiate(count, root.transform, false);
        title.name = "PickupLabel";
        title.text = "Pickup";
        title.fontSize = count.fontSize * .8f;
        title.color = Color.white;
        title.enableAutoSizing = false;
        title.rectTransform.anchoredPosition = new Vector2(count.rectTransform.anchoredPosition.x, 14);
        var button = root.AddComponent<ConfigButton>();
        button.targetGraphic = root.transform.Find("bkg").GetComponent<Image>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.85f, .85f, .85f);
        button.colors = colors;
        button.onClick.AddListener(() => open());
        var nativeTooltip = gui.m_craftButton ? gui.m_craftButton.GetComponent<UITooltip>() : null;
        if (nativeTooltip && nativeTooltip.m_tooltipPrefab)
        {
            tooltip = root.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = nativeTooltip.m_tooltipPrefab;
            tooltip.m_topic = "Auto pickup";
        }
        lastCount = -1;
        Layout(false);
    }

    internal static void Layout(bool visible)
    {
        if (!root) return;
        visible = visible && armor && weight;
        root.SetActive(visible);
        if (!visible) return;
        root.transform.position = (armor.TransformPoint(armor.rect.center) + weight.TransformPoint(weight.rect.center)) * .5f;
        int ignored = PickupFilter.Count(Player.m_localPlayer);
        if (ignored == lastCount) return;
        lastCount = ignored;
        count.text = ignored.ToString();
        if (tooltip) tooltip.m_text = ignored + " item types ignored\nClick to choose items.\nManually pick up an item to enable it again.\nController: L-stick + Y";
    }

    internal static void Dispose()
    {
        if (root) { root.SetActive(false); Object.Destroy(root); }
        root = null; count = null; tooltip = null; armor = null; weight = null; lastCount = -1;
    }
}
