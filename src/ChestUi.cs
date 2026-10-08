using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quartermaster;

internal static partial class ChestUi
{
    private static GameObject dock, modal, blocker;
    private static bool pickupMode;
    private static readonly List<Button> dockButtons = new List<Button>();
    private static NativeInventoryFooter chestFooter, playerFooter;
    private static RectTransform body;
    private static TMP_FontAsset font;
    private static TMP_Text status;
    private static Container chest;
    private static OwlLedger ledger;
    private static Component machine, lastHovered;
    private static int tab, page;
    private static string query = "", notice = "", clipboard;
    private static float statusAt;
    private static readonly List<Selectable> controls = new List<Selectable>();
    private static int selected;
    internal static bool IsOpen => modal && modal.activeSelf;
    private static Color Gold = new Color(.92f, .73f, .38f);
    private static Color Muted = new Color(.83f, .80f, .72f);
    private static bool refreshing;
    internal static void Attach()
    {
        var gui = InventoryGui.instance;
        if (!gui || !gui.m_stackAllButton) return;
        font = gui.m_stackAllButton.GetComponentInChildren<TMP_Text>(true)?.font;
        NativeMenuTheme.Capture(gui);
        DisposeActions();
        chestFooter = new NativeInventoryFooter(gui.m_container);
        playerFooter = new NativeInventoryFooter(gui.m_player);
        dock = Rect("Quartermaster_Actions", gui.m_container, 0, 0, 0, 40).gameObject;
        dockButtons.Add(NativeAction(gui, "Chest Config", () => OpenChest(Plugin.OpenContainer)));
        dockButtons.Add(NativeAction(gui, "Deposit All", DepositAll));
        dockButtons.Add(NativeAction(gui, "Sort Chest", SortChest));
        dockButtons.Add(NativeAction(gui, "Sort Inventory", () =>
        {
            if (!ConfigurationManagerCompatibility.IsOpen && Player.m_localPlayer) WarehouseService.SortInventory(Player.m_localPlayer.GetInventory(), true, true);
        }));
        dockButtons.Add(NativeAction(gui, "Machine Config", OpenHoveredMachine));
        PickupTab.Attach(gui, OpenPickup);
        LayoutActions();
    }
    private static Button NativeAction(InventoryGui gui, string label, Action action)
    {
        var button = UnityEngine.Object.Instantiate(gui.m_stackAllButton, dock.transform, false);
        button.name = "Quartermaster_" + label.Replace(" ", "");
        // Never inherit Place stacks' input binding or click callbacks.
        var shortcut = button.GetComponent<UIGamePad>();
        if (shortcut) { shortcut.enabled = false; UnityEngine.Object.Destroy(shortcut); }
        foreach (Transform child in button.transform)
            if (child.name.StartsWith("gamepad_hint", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => action());
        button.interactable = true;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13;
        text.fontSizeMax = text.fontSize;
        text.raycastTarget = false;
        // Cloned native controls can carry tooltips; these footer labels stand alone.
        foreach (var tip in button.GetComponentsInChildren<UITooltip>(true))
        {
            tip.enabled = false;
            UnityEngine.Object.Destroy(tip);
        }
        return button;
    }
    private static void LayoutActions()
    {
        if (!dock) return;
        bool visible = Plugin.Enabled.Value && InventoryGui.IsVisible() && Player.m_localPlayer && !IsOpen && !ConfigurationManagerCompatibility.IsOpen;
        PickupTab.Layout(visible);
        dock.SetActive(visible);
        if (!visible) { chestFooter?.Extend(0); playerFooter?.Extend(0); return; }
        var gui = InventoryGui.instance;
        var openChest = Plugin.OpenContainer;
        bool hasChest = openChest && gui.m_container.gameObject.activeInHierarchy;
        bool isDeposit = hasChest && ContainerRegistry.GetSettings(openChest).Deposit;
        bool hasMachine = !hasChest && lastHovered && Near(lastHovered);
        var sidePanel = InventoryUiCompatibility.ActionPanel(gui);
        // InventorySlots independently owns the player background, its sliding
        // action tabs, and the chest position. Keep our controls with equipment
        // instead of competing for the changing space between player and chest.
        bool separateActions = ExternalSlotCompatibility.HasInventorySlots;
        if (separateActions) { chestFooter?.Extend(0); playerFooter?.Extend(0); }
        if (separateActions && !sidePanel) { dock.SetActive(false); return; }
        var parent = separateActions ? sidePanel : hasChest ? gui.m_container : gui.m_player;
        var rect = (RectTransform)dock.transform;
        if (rect.parent != parent) rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = separateActions ? new Vector2(0, -24) : new Vector2(6, -8);
        rect.localScale = Vector3.one;
        int shown = 0;
        for (int i = 0; i < dockButtons.Count; i++)
        {
            bool show = i == 3 ? Plugin.ShowSortInventory.Value : (i == 4 ? hasMachine : i == 1 ? isDeposit : hasChest);
            dockButtons[i].gameObject.SetActive(show);
            if (show) shown++;
        }
        if(shown==0){dock.SetActive(false);chestFooter?.Extend(0);playerFooter?.Extend(0);return;}
        float width = Mathf.Max(120, parent.rect.width - (separateActions ? 0 : 12));
        int columns = Mathf.Max(1, Mathf.Min(shown, Mathf.FloorToInt((width + 6) / 126)));
        int rows = (shown + columns - 1) / columns;
        float buttonWidth = (width - 6 * (columns - 1)) / columns;
        int slot = 0;
        foreach (var button in dockButtons)
        {
            if (!button.gameObject.activeSelf) continue;
            var br = (RectTransform)button.transform;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 1);
            br.anchoredPosition = new Vector2((slot % columns) * (buttonWidth + 6), -(slot / columns) * 46);
            if(button==dockButtons[3])br.anchoredPosition=new Vector2(br.anchoredPosition.x+Plugin.SortInventoryOffsetX.Value,br.anchoredPosition.y-Plugin.SortInventoryOffsetY.Value);
            br.sizeDelta = new Vector2(buttonWidth, 40);
            slot++;
        }
        rect.sizeDelta = new Vector2(width, rows * 46 - 6);
        float extension = rows * 46 + 10;
        if (!separateActions)
        {
            chestFooter?.Extend(hasChest ? extension : 0);
            playerFooter?.Extend(hasChest ? 0 : extension);
        }
    }
    private static void DisposeActions()
    {
        chestFooter?.Extend(0); playerFooter?.Extend(0);
        chestFooter = null; playerFooter = null;
        if (dock) { dock.SetActive(false); UnityEngine.Object.Destroy(dock); }
        dock = null; dockButtons.Clear();
    }
    private static void DepositAll()
    {
        if (ConfigurationManagerCompatibility.IsOpen) return;
        var c = Plugin.OpenContainer;
        if (!c || !ContainerRegistry.GetSettings(c).Deposit || !Near(c)) return;
        int count = 0;
        if (!SharedChests.Run(c, () => count = WarehouseService.StoreAllInOpenChest(Player.m_localPlayer, c)))
            count = WarehouseService.StoreAllInOpenChest(Player.m_localPlayer, c);
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Deposited " + count + " items (hotbar and equipment kept)");
    }
    private static void SortChest()
    {
        if (ConfigurationManagerCompatibility.IsOpen) return;
        var c = Plugin.OpenContainer;
        if (!c || !Near(c)) return;
        void Sort() { InventoryTransfers.StackWithin(c.GetInventory()); WarehouseService.SortInventory(c.GetInventory(), false); }
        if (ContainerRegistry.IsUsable(c, true)) Sort();
        else SharedChests.Run(c, Sort);
    }
    internal static void Tick()
    {
        if (!InventoryGui.instance) return;
        if (ConfigurationManagerCompatibility.IsOpen) { if (IsOpen) Close(); LayoutActions(); return; }
        if (!IsOpen && !InventoryGui.IsVisible())
        {
            var hover = Player.m_localPlayer?.GetHoverObject();
            lastHovered = hover ? (Component)hover.GetComponentInParent<Smelter>() ?? hover.GetComponentInParent<Fermenter>() ?? (Component)hover.GetComponentInParent<CookingStation>() ?? (Component)hover.GetComponentInParent<Fireplace>() ?? hover.GetComponentInParent<Beehive>() : null;
            if (lastHovered is CookingStation rack && !rack.m_requireFire) lastHovered = null;
        }
        LayoutActions();
        if (!IsOpen) return;
        if (!Plugin.Enabled.Value || !Player.m_localPlayer || Player.m_localPlayer.IsDead() || !InventoryGui.IsVisible() || (!pickupMode && !Near(ledger ? (Component)ledger : chest ? (Component)chest : machine) || ledger && !OwlLedger.Allowed(ledger))) { Close(); return; }
        if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
        { ZInput.ResetButtonStatus("JoyButtonB"); Close(); return; }
        if (Time.unscaledTime >= statusAt)
        {
            statusAt = Time.unscaledTime + .5f;
            RetryPendingEdit();
            status.text = pendingEdit!=null?notice:StatusText();
            RefreshLedgerRows();
        }
        Controller();
    }
    internal static bool TryControllerShortcut()
    {
        if (ConfigurationManagerCompatibility.IsOpen) return false;
        if (!Plugin.Enabled.Value || !InventoryGui.IsVisible() || !ZInput.GetButton("JoyLStick")) return false;
        if (ZInput.GetButtonDown("JoyButtonY"))
        { ZInput.ResetButtonStatus("JoyButtonY"); OpenPickup(); return true; }
        if (!ZInput.GetButtonDown("JoyButtonA")) return false;
        ZInput.ResetButtonStatus("JoyButtonA");
        if (Plugin.OpenContainer) OpenChest(Plugin.OpenContainer); else OpenHoveredMachine();
        return true;
    }
    private static bool Near(Component c) => c && Player.m_localPlayer && (c.transform.position - Player.m_localPlayer.transform.position).sqrMagnitude <= Mathf.Pow(Player.m_localPlayer.m_maxInteractDistance + 2f, 2);
    internal static void OpenHoveredMachine()
    {
        if (ConfigurationManagerCompatibility.IsOpen) return;
        if (!lastHovered || !Near(lastHovered) || !PrivateArea.CheckAccess(lastHovered.transform.position, 0, false, true)) return;
        if (!InventoryGui.IsVisible()) InventoryGui.instance.Show(null);
        ledger = null; pickupMode = false; machine = lastHovered; chest = null; tab = 0; page = 0; notice = ""; Build();
    }
    private static void OpenChest(Container c)
    {
        if (ConfigurationManagerCompatibility.IsOpen) return;
        if (!c || !Near(c)) return;
        if (!ContainerRegistry.IsUsable(c, true) && !SharedChests.Viewing(c))
        {
            var view=ContainerRegistry.GetView(c);
            string reason=!Plugin.Enabled.Value?"Quartermaster is disabled.":
                !ContainerRegistry.Accessible(c)?"Chest Config unavailable: chest type or ward permissions do not allow access.":
                !view.IsOwner()?"Waiting for chest ownership. Close and reopen the chest, then try Chest Config again.":
                "Chest is currently in use. Close and reopen it before configuring.";
            Player.m_localPlayer.Message(MessageHud.MessageType.Center,reason);return;
        }
        ledger = null; pickupMode = false; chest = c; machine = null; tab = 0; page = 0; query = ""; notice = ""; Build();
    }
    private static void OpenPickup()
    {
        if (ConfigurationManagerCompatibility.IsOpen) return;
        if (!Plugin.Enabled.Value || !Player.m_localPlayer || !InventoryGui.IsVisible()) return;
        ledger = null; pickupMode = true; chest = null; machine = null; page = 0; query = ""; notice = ""; Build();
    }
    private static string PickupStatus() => PickupFilter.Count(Player.m_localPlayer) + " item types ignored · Saved with your character";
    private static void PickupBody()
    {
        Text(body, "Ignore an item type to stop collecting it automatically.\nPick one up manually from the ground to enable it again.", 0, 0, 672, 54, 20, Color.white);
        Input(body, query, 0, -66, 672, value => { query = value; page = 0; Build(); }, "Search carried and ignored items");
        var player = Player.m_localPlayer;
        var types = player.GetInventory().GetAllItems().Select(PickupFilter.ItemId).Where(id => !string.IsNullOrEmpty(id))
            .Concat(PickupFilter.Types(player)).Distinct().Where(id => PickupFilter.Label(id).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(PickupFilter.Label).ThenBy(id => id, StringComparer.Ordinal).ToList();
        int pages = Math.Max(1, (types.Count + 3) / 4); page = Mathf.Clamp(page, 0, pages - 1);
        if (types.Count == 0) Text(body, "No matching carried or ignored items.", 0, -132, 672, 48, 20, Muted);
        for (int i = 0; i < 4 && page * 4 + i < types.Count; i++)
        {
            string id = types[page * 4 + i]; float y = -122 - i * 49;
            var prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(id) : null;
            var item = prefab ? prefab.GetComponent<ItemDrop>() : null;
            if (item)
            {
                var icon = Rect("ItemIcon", body, 0, y, 36, 36).gameObject.AddComponent<Image>();
                icon.sprite = item.m_itemData.GetIcon(); icon.preserveAspect = true; icon.raycastTarget = false;
            }
            bool blocked = PickupFilter.IsIgnored(player, id);
            Text(body, PickupFilter.Label(id), 48, y, 450, 38, 20, blocked ? Muted : Color.white);
            if (blocked) Text(body, "Ignored", 516, y, 156, 38, 18, Gold);
            else Button(body, "Ignore", 516, y, 156, () =>
            {
                PickupFilter.SetIgnored(Player.m_localPlayer, id, true);
                notice = "Ignoring " + PickupFilter.Label(id); Build();
            });
        }
        Button(body, "Previous", 0, -334, 175, () => { page = Math.Max(0, page - 1); Build(); });
        Text(body, (page + 1) + " / " + pages, 194, -334, 285, 38, 17, Muted);
        Button(body, "Next", 497, -334, 175, () => { page = Math.Min(pages - 1, page + 1); Build(); });
    }
    internal static void Close()
    {
        pendingEdit=null;
        // InventoryGui.Hide also runs every frame before a player exists (including
        // the server password screen). Release only focus owned by our own window.
        var events = EventSystem.current;
        var focused = events ? events.currentSelectedGameObject : null;
        if (modal && focused && focused.transform.IsChildOf(modal.transform))
            events.SetSelectedGameObject(null);
        if (modal) { modal.SetActive(false); UnityEngine.Object.Destroy(modal); }
        if (blocker) { blocker.SetActive(false); UnityEngine.Object.Destroy(blocker); }
        blocker = null; modal = null; ledger = null; ledgerRows.Clear(); ledgerMachines.Clear(); chest = null; machine = null; controls.Clear();
    }
    internal static void Dispose() { Close(); PickupTab.Dispose(); DisposeActions(); NativeMenuTheme.Clear(); }
    private static void Build()
    {
        refreshing = true;
        NativeMenuTheme.Capture(InventoryGui.instance);
        ledgerRows.Clear(); ledgerMachines.Clear();
        if (modal) { modal.SetActive(false); UnityEngine.Object.Destroy(modal); }
        controls.Clear(); selected = 0;
        var canvas = InventoryGui.instance.m_player.GetComponentInParent<Canvas>();
        if (blocker) { blocker.SetActive(false); UnityEngine.Object.Destroy(blocker); }
        blocker = Rect("Quartermaster_InputShield", canvas.transform, 0, 0, 0, 0).gameObject;
        var shieldRect = (RectTransform)blocker.transform;
        shieldRect.anchorMin = Vector2.zero; shieldRect.anchorMax = Vector2.one; shieldRect.offsetMin = shieldRect.offsetMax = Vector2.zero;
        blocker.AddComponent<Image>().color = new Color(0, 0, 0, .45f);
        float panelHeight = ledger && tab == LedgerStockTab ? 810 : 620;
        modal = Rect("Quartermaster_Config", canvas.transform, 0, 0, 720, panelHeight).gameObject;
        var rect = (RectTransform)modal.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        var cr = (RectTransform)canvas.transform;
        float scale = Mathf.Min(1, Mathf.Min((cr.rect.width - 24) / 720, (cr.rect.height - 24) / panelHeight));
        rect.localScale = Vector3.one * scale;
        NativeMenuTheme.Panel(modal.AddComponent<Image>());
        var title=Text(modal.transform, ledger ? "QUARTERMASTER LEDGER" : pickupMode ? "AUTO PICKUP" : chest ? PostalMailbox.IsMailbox(chest) ? "MAILBOX" : "CHEST CONFIG" : "MACHINE CONFIG", 24, -16, 566, 40, 32, Gold);
        NativeMenuTheme.Text(title,true);title.enableAutoSizing=true;title.fontSizeMin=24;title.fontSizeMax=32;
        Button(modal.transform, "Close", 602, -18, 94, Close);
        var subtitle=Text(modal.transform, ledger ? "Inventory, limits, stack sizes and machine status" : pickupMode ? "Choose which item types to leave on the ground" : chest ? PostalMailbox.IsMailbox(chest) ? "Send items and configure your mailbox connection" : Tier(chest) + " · Changes save immediately" : DisplayMachine(machine) + " · Caps: Enter or Save; other controls save immediately", 24, -60, 670, 32, 18, Muted);
        subtitle.enableAutoSizing=true;subtitle.fontSizeMin=14;subtitle.fontSizeMax=18;
        var tabs = ledger ? new[] { "Items & Limits", "Stack sizes", "Machines", "Deliveries", "Interface" } : pickupMode ? Array.Empty<string>() : chest ? PostalMailbox.IsMailbox(chest)?new[]{"Destinations","Settings"}:new[] { "Storage", "Supply", "Base & Copy", "Owl" } : new[] { "Production", "Base & Inputs" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int t = i; float width = (672f - 8f * (tabs.Length - 1)) / tabs.Length; var button = Button(modal.transform, tabs[i], 24 + i * (width + 8), -101, width, () => { if(ledger&&(tab==LedgerMachinesTab||t==LedgerMachinesTab))query=""; tab = t; page = 0; notice = ""; Build(); });
            NativeMenuTheme.Button(button,true);
            if (tab == i) NativeMenuTheme.ActiveTab(button);
        }
        var divider=Rect("Header divider",modal.transform,24,-144,672,1).gameObject.AddComponent<Image>();divider.color=new Color(.65f,.43f,.17f,.65f);divider.raycastTarget=false;
        body = Rect("Contents", modal.transform, 24, -155, 672, panelHeight - 238);
        if (ledger) LedgerBody(); else if (pickupMode) PickupBody(); else if (chest) ChestBody(); else MachineBody();
        status = Text(modal.transform, StatusText(), 24, -(panelHeight - 72), 672, 28, 17, Gold);
        status.enableAutoSizing=true;status.fontSizeMin=12;status.fontSizeMax=17;status.overflowMode=TextOverflowModes.Ellipsis;
        Text(modal.transform, pickupMode ? "D-pad: select   A: activate   B: close    •    L-stick + Y in inventory: pickup" : "D-pad: select   A: activate   B: close    •    L-stick + A in inventory: config", 24, -(panelHeight - 36), 672, 20, 15, Muted);
        refreshing = false;
        if (controls.Count > 0 && ZInput.IsGamepadActive()) controls[0].Select();
    }
    private static void Edit(Action<ChestSettings> action)
    {
        bool shared = chest && !ContainerRegistry.IsUsable(chest, true) && SharedChests.Viewing(chest);
        if (!chest || !(shared || ContainerRegistry.IsUsable(chest, true))) { notice = "Chest is no longer available"; return; }
        var s = JsonUtility.FromJson<ChestSettings>(JsonUtility.ToJson(ContainerRegistry.GetSettings(chest)));
        action(s);
        if (shared)
        {
            // Another player owns this open chest: its owner saves the change.
            notice = SharedChests.RequestSettings(chest, s, result => { notice = result; if (IsOpen) Build(); }) ? "Sending to the chest's owner…" : "Chest is no longer available";
            Build(); return;
        }
        notice = ContainerRegistry.SaveSettings(chest, s) ? "Saved" : "Could not save: chest access changed";
        Build();
    }
    private static void ChestBody()
    {
        if(PostalMailbox.IsMailbox(chest)){MailBody();return;}
        var s = ContainerRegistry.GetSettings(chest);
        if (tab == 3)
        {
            if(!s.Deposit){Text(body,"Enable Deposit Chest to request an owl collection trip.",0,0,672,70,21,Muted);return;}
            Text(body,"Collection radius (m)",0,0,240,38,21,Gold);
            Input(body,OwlCleanup.Radius(chest).ToString("0"),260,0,160,value=>{
                if(float.TryParse(value,out var radius)&&radius>=1&&radius<=Math.Min(100,Plugin.Range.Value))Edit(x=>x.PickupRange=radius);
                else notice="Enter a radius within this base's coverage (up to 100 metres)";
            },"25");
            Button(body,"Show pickup area",0,-65,320,()=>RangeVisual.Show(chest.transform.position,OwlCleanup.Radius(chest)));
            Button(body,"Collect nearby items",0,-126,320,()=>Edit(x=>x.PickupRequestedUntil=DateTime.UtcNow.AddMinutes(10).Ticks));
            Button(body,"Cancel request",346,-126,326,()=>Edit(x=>x.PickupRequestedUntil=0));
            Text(body,OwlCleanup.Requested(chest)?"Collection requested · close the chest to let the owl begin.":"No collection request active.",0,-188,672,60,21,Gold);
            Text(body,"The owl brings back up to three items per trip.\nStops when no eligible items remain or after ten minutes.\nRespects wards, pickup filters, full storage and animal feed.\nOnly loaded ground items can be collected.",0,-255,672,116,19,Muted);
            return;
        }
        if (tab == 0)
        {
            Toggle("Use as Deposit Chest", s.Deposit, 0, () => Edit(x => { x.Deposit = !x.Deposit; if (x.Deposit) x.Overflow = false; }));
            if(!s.Deposit&&FurnitureAssignment.IsFurniture(chest))
                Button(body,"Auto assignment: "+(s.AutoAssign?FurnitureAssignment.Mode(s,chest):"OFF"),0,-45,672,()=>Edit(x=>FurnitureAssignment.Cycle(chest,x)));
            else Text(body, s.Deposit ? "Place items here. Matching storage receives them when you close the chest." : "Place an item in this chest to teach it what belongs here—even when empty.", 0, -45, 672, 34, 17, Muted);
            if (s.Deposit)
            {
                Text(body, "BASE COVERAGE", 0, -102, 672, 30, 23, Gold);
                Text(body, Plugin.Range.Value.ToString("0") + " metres from each Deposit Chest in this group.\n" + (Plugin.ExtendStationCoverage.Value ? "Stations in this area support building and structure repairs.\n" : "") + "Unmatched items stay here. Chest capacity and cost stay the same.", 0, -143, 672, 98, 20, Color.white);
                Button(body, "Deposit All", 0, -265, 215, DepositAll);
                Button(body, "Show range · 60s", 228, -265, 215, () => { RangeVisual.Show(chest.transform.position, Plugin.Range.Value); notice = "Range shown for 60 seconds"; });
                Button(body, "Sort this chest", 456, -265, 215, SortChest);
                return;
            }
            Toggle("Receive matching items", s.AcceptStorage, 83, () => Edit(x => x.AcceptStorage = !x.AcceptStorage), 324);
            Toggle("Preferred destination", s.Preferred, 83, () => Edit(x => x.Preferred = !x.Preferred), 324, 346);
            Input(body, query, 0, -133, 440, value => { query = value; page = 0; Build(); }, FurnitureAssignment.IsFurniture(chest)?"Search assigned items":"Search remembered items");
            Button(body, s.Learn ? "Learning: ON" : "Learning: OFF", 456, -133, 216, () => Edit(x => x.Learn = !x.Learn));
            var types = s.Remembered.Concat(s.Forgotten).Concat(string.IsNullOrWhiteSpace(query)?Enumerable.Empty<string>():FurnitureAssignment.Types(chest)).Distinct().Where(id => ItemLabel(id).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(ItemLabel).ToList();
            int pages = Math.Max(1, (types.Count + 2) / 3); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < 3 && page * 3 + i < types.Count; i++)
            {
                string id = types[page * 3 + i]; float y = -184 - i * 49;
                var prefab = ObjectDB.instance.GetItemPrefab(id); var item = prefab ? prefab.GetComponent<ItemDrop>() : null;
                if (item) { var icon = Rect("ItemIcon", body, 0, y, 36, 36).gameObject.AddComponent<Image>(); icon.sprite = item.m_itemData.GetIcon(); icon.preserveAspect = true; }
                bool forgotten = s.Forgotten.Contains(id);
                Text(body, ItemLabel(id) + (forgotten ? " (ignored)" : ""), 48, y, 462, 38, 20, forgotten ? Muted : Color.white);
                Button(body, forgotten ? "Restore" : "Forget", 534, y, 138, () => Edit(x => { if (forgotten) x.Restore(id); else x.Forget(id); }));
            }
            if (types.Count == 0) Text(body, "No remembered items yet. Put an item into this chest to teach it.", 0, -193, 660, 72, 21, Muted);
            Button(body, "Previous", 0, -338, 142, () => { page = Math.Max(0, page - 1); Build(); });
            Text(body, (page + 1) + " / " + pages, 164, -338, 102, 38, 20, Muted);
            Button(body, "Next", 278, -338, 142, () => { page = Math.Min(pages - 1, page + 1); Build(); });
            Button(body, "Forget all", 456, -338, 216, () => Edit(x => { foreach (var id in x.Remembered.ToArray()) x.Forget(id); }));
        }
        else if (tab == 1)
        {
            if(!s.Deposit&&FurnitureAssignment.IsFurniture(chest))
                Button(body,"Auto assignment: "+(s.AutoAssign?FurnitureAssignment.Mode(s,chest):"OFF"),0,-45,672,()=>Edit(x=>FurnitureAssignment.Cycle(chest,x)));
            else Text(body, s.Deposit ? "Deposit Chests distribute supplies; machines draw from ordinary storage." : "Choose which systems may take items from this chest.", 0, 0, 672, 45, 19, Muted);
            Toggle("Crafting, building & upgrades", s.CraftingSupply, 62, () => Edit(x => x.CraftingSupply = !x.CraftingSupply));
            Toggle("Fire & machine fuel", s.FuelSupply, 106, () => Edit(x => x.FuelSupply = !x.FuelSupply));
            Toggle("Production ingredients", s.ProcessingSupply, 150, () => Edit(x => x.ProcessingSupply = !x.ProcessingSupply));
            Toggle("Feed tamed animals", s.LivestockFeed, 194, () => Edit(x => x.LivestockFeed = !x.LivestockFeed));
            Toggle("Also feed animals being tamed", s.FeedUntamed, 238, () => Edit(x => x.FeedUntamed = !x.FeedUntamed));
            Text(body, "Feeding radius (m)", 0, -284, 200, 36, 19, Muted);
            Input(body, Policy.FeedingRadius(s.FeedingRange).ToString("0"), 210, -284, 130, value => {
                if(float.TryParse(value,out var radius)&&radius>=1&&radius<=100)Edit(x=>x.FeedingRange=radius);
                else notice="Enter a feeding radius from 1 to 100 metres";
            }, "10");
            Button(body,"Show feeding range",360,-284,312,()=>RangeVisual.Show(chest.transform.position,Policy.FeedingRadius(s.FeedingRange)));
            Button(body, "Keep private · block all supply", 0, -338, 672, () => Edit(x => { x.CraftingSupply = x.FuelSupply = x.ProcessingSupply = x.LivestockFeed = x.FeedUntamed = false; }));
        }
        else
        {
            Text(body, "Base group", 0, 0, 170, 35, 21, Gold);
            Input(body, s.Group, 180, 0, 492, value => Edit(x => x.Group = string.IsNullOrWhiteSpace(value) ? "Home" : value.Trim()), "Home");
            Text(body, "Use the same group for a base’s chests and machines. Separate groups keep nearby bases apart.", 0, -48, 672, 55, 18, Muted);
            Toggle("Overflow · accept unmatched items", s.Overflow, 112, () => Edit(x => { x.Overflow = !x.Overflow; if (x.Overflow) x.Deposit = false; }));
            Toggle("Sort and combine stacks in this chest", s.AutoSort, 162, () => Edit(x => x.AutoSort = !x.AutoSort));
            Button(body, "Copy config", 0, -222, 212, () => { clipboard = JsonUtility.ToJson(s); notice = "Copied chest settings and item memory"; });
            Button(body, "Paste settings", 230, -222, 212, () => Paste(false));
            Button(body, "Paste + item memory", 460, -222, 212, () => Paste(true));
            if(s.Deposit)
            {
                Text(body,"Base name",0,-282,170,35,20,Gold);
                Input(body,s.BaseName,180,-282,492,value=>Edit(x=>x.BaseName=(value??"").Trim().Substring(0,Math.Min(40,(value??"").Trim().Length))),"Name for mail deliveries");
            }
            else Text(body, "Pasting keeps this chest’s Deposit/Storage role.\nItem memory is copied only with “Paste + item memory”.\nStorage has no item quantity targets or stack-size overrides.", 0, -282, 672, 96, 18, Muted);
        }
    }
    private static void Paste(bool memory)
    {
        if (string.IsNullOrEmpty(clipboard)) { notice = "Copy a chest config first"; return; }
        var saved = JsonUtility.FromJson<ChestSettings>(clipboard);
        Edit(x => {
            x.AcceptStorage = saved.AcceptStorage; x.CraftingSupply = saved.CraftingSupply; x.FuelSupply = saved.FuelSupply;
            x.ProcessingSupply = saved.ProcessingSupply; x.LivestockFeed = saved.LivestockFeed; x.FeedUntamed = saved.FeedUntamed; x.FeedingRange = Policy.FeedingRadius(saved.FeedingRange); x.PickupRange = saved.PickupRange; 
            x.AutoSort = saved.AutoSort; x.Preferred = saved.Preferred; x.Group = saved.Group; x.Learn = saved.Learn; x.AutoAssign=saved.AutoAssign; x.AutoCategories=saved.AutoCategories;
            if (memory) { x.Remembered = saved.Remembered; x.Forgotten = saved.Forgotten; }
        });
    }
    private static (Component Target,Action<MachineSettings> Action,bool Rebuild,float Deadline)? pendingEdit;
    private static void RetryPendingEdit()
    {
        if(pendingEdit==null)return;
        var edit=pendingEdit.Value;
        if(!IsOpen||machine!=edit.Target||!edit.Target){pendingEdit=null;return;}
        if(ChestOwnership.Request(Automation.View(edit.Target),"machine settings")){pendingEdit=null;EditMachine(edit.Action,edit.Rebuild);return;}
        if(Time.unscaledTime>edit.Deadline)
        {
            pendingEdit=null;notice="Station owner did not hand over ownership; try again in a moment";
            if(status)status.text=notice;
        }
    }
    private static void EditMachine(Action<MachineSettings> action, bool rebuild = true)
    {
        var target=machine;
        bool Valid()=>target&&Near(target)&&PrivateArea.CheckAccess(target.transform.position,0,false,true);
        bool saved=false;
        void Save()
        {
            // Apply the edit to the latest replicated settings AFTER acquisition,
            // preserving unrelated changes made by another player meanwhile.
            var s=JsonUtility.FromJson<MachineSettings>(JsonUtility.ToJson(Automation.Settings(target)));
            action(s);saved=Automation.Save(target,s);
        }
        if(!NativeStorageAccess.Networked){Save();notice=saved?"Saved":"Machine access changed; settings not saved";if(rebuild)Build();return;}
        // Stations never change owner through normal play. Ask the current owner (or
        // claim an unserved station) and retry this edit once ownership has arrived.
        if(!ChestOwnership.Request(Automation.View(target),"machine settings"))
        {
            pendingEdit=(target,action,rebuild,Time.unscaledTime+8f);
            notice="Requesting station ownership ("+ChestOwnership.Describe(Automation.View(target))+")…";
            if(status)status.text=notice;return;
        }
        pendingEdit=null;
        notice="Waiting to save machine settings…";
        if(!NativeStorageAccess.Start("machine settings",new[]{Automation.View(target)},Valid,Save,result=>{
            if(!IsOpen||machine!=target)return;
            notice=result=="Saved"?(saved?"Saved":"Could not save machine settings"):result;
            if(rebuild)Build();else if(status)status.text=notice;
        }))notice="Machine is not locally owned; try again when ownership updates";
    }
    private static void SaveProductionCap(string id, TMP_InputField field)
    {
        if (!IsOpen || !machine || !field) return;
        if (string.IsNullOrEmpty(id)) { notice = "Could not identify this output item; cap not saved"; return; }
        if (!int.TryParse(field.text, out int amount) || amount < 0 || amount > 100000)
        { notice = "Enter a whole number from 0 to 100000, then Save"; return; }
        // Keep the active input and button alive after saving. Rebuilding the modal in
        // a pointer/input callback can interrupt focus and subsequent interaction.
        var network=Automation.Network(machine);var book=OwlLedger.Authority(network);
        if(book&&book.HasCap(id)){notice=book.SaveCap(id,amount);return;}
        EditMachine(x => x.SetCap(id, amount), false);
        if (notice == "Saved") notice = "Saved production cap: " + amount;
    }
    private static void MachineBody()
    {
        if(machine is Fermenter&&FermenterCompatibility.External)
        {Text(body,FermenterCompatibility.Notice+".\n\nDisable Automatic Fermenters to use Quartermaster’s fermenter limits and collection.",0,0,672,200,21,Muted);return;}
        var s = Automation.Settings(machine);
        if (tab == 0)
        {
            Toggle("Automation", !s.Paused, 0, () => EditMachine(x => x.Paused = !x.Paused));
            var products = Automation.Products(machine).ToList();
            if (products.Count == 0)
            {
                Text(body, "Refuels from eligible base storage when fuel falls below 25%.\nUses the normal fuel item. A switched-off fire stays off.", 0, -70, 672, 112, 22, Muted); return;
            }
            Text(body, "Stop loading when stored + queued output reaches the cap.\nThe ledger overrides machine caps; otherwise the lowest cap applies.", 0, -51, 672, 61, 18, Muted);
            int pages = Math.Max(1, (products.Count + 2) / 3); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < 3 && page * 3 + i < products.Count; i++)
            {
                var item = products[page * 3 + i]; string id = InventoryTransfers.PrefabId(item); float y = -131 - i * 64;
                Text(body, Automation.Label(item.m_itemData), 0, y, 290, 42, 21, Color.white);
                Text(body, "Cap", 300, y, 48, 38, 18, Muted);
                var input = Input(body, (OwlLedger.Cap(Automation.Network(machine),id,s.Cap(id))).ToString(), 354, y, 166, null, "Enter cap", true, false);
                input.onFocusSelectAll = true;
                input.onSubmit.AddListener(_ => SaveProductionCap(id, input));
                Button(body, "Save", 542, y, 130, () => SaveProductionCap(id, input));
            }
            Text(body, "Type a cap, then press Enter or Save. 0 stops new production.", 0, -321, 672, 24, 17, Muted);
            Button(body, "Previous", 0, -351, 175, () => { page = Math.Max(0, page - 1); Build(); });
            Text(body, (page + 1) + " / " + pages, 194, -351, 285, 38, 17, Muted);
            Button(body, "Next", 497, -351, 175, () => { page = Math.Min(pages - 1, page + 1); Build(); });
        }
        else
        {
            Text(body, "Base group", 0, 0, 170, 38, 21, Gold);
            Input(body, s.Group, 180, 0, 492, value => EditMachine(x => x.Group = string.IsNullOrWhiteSpace(value) ? "Home" : value.Trim()), "Home");
            if (machine is CookingStation)
            {
                Text(body, "Draws raw food from chests with Production Ingredients enabled.\nThe fire underneath uses fuel-enabled base storage.\n\nNormal cooking time and a lit fire are required. Cooked food\nis removed from the rack and ejected for player pickup.\nAfter 60 seconds, base storage collects it when there is room.\n\nPausing automation leaves the rack under manual control.", 0, -80, 672, 280, 20, Muted);
                return;
            }
            Toggle("Allow valuable wood as a production input", s.AllowValuableWood, 80, () => EditMachine(x => x.AllowValuableWood = !x.AllowValuableWood));
            Text(body, "Ordinary wood is allowed by default. Fine wood, core wood,\nYggdrasil wood and ashwood require this opt-in.\n\nNormal processing times, recipes, fuel costs and shelter\nrequirements remain in effect.\n\nPause a machine from Production to stop its automation.\nMaterials already queued continue processing normally.", 0, -144, 672, 226, 20, Muted);
        }
    }
    private static string ItemLabel(string id)
    {
        var prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(id) : null;
        var label = prefab && prefab.GetComponent<ItemDrop>() ? Automation.Label(prefab.GetComponent<ItemDrop>().m_itemData) : id;
        return string.IsNullOrWhiteSpace(label) ? id : label;
    }
    private static string Tier(Container c)
    {
        string id = ContainerRegistry.PrefabName(c);
        string tier = id.Contains("blackmetal") ? "Black metal" : id.Contains("ashwood") ? "Ashwood" : id.Contains("private") ? "Personal" : id == "piece_chest" ? "Reinforced / iron" : id.Contains("wood") ? "Wooden" : "Storage";
        return tier + " chest · " + c.GetInventory().GetWidth() + " × " + c.GetInventory().GetHeight();
    }
    internal static string DisplayMachine(Component c) => c is Beehive hive ? Localize(hive.m_name) : c is ApothecaryDisplay cabinet ? cabinet.DisplayName : c is Smelter s ? Localize(s.m_name) : c is Fermenter f ? Localize(f.m_name) : c is Fireplace fire ? Localize(fire.m_name) : c is CookingStation rack ? Localize(rack.m_name) : "Machine";
    private static string Localize(string s) => Localization.instance.Localize(s);
    private static void Toggle(string label, bool value, float y, Action action, float width = 672, float x = 0)
    {
        var b = Button(body, label + (value ? "  · ON" : "  · OFF"), x, -y, width, action);
        var box=Rect("Checkbox",b.transform,10,-7,24,24).gameObject.AddComponent<Image>();
        var mark=Rect("Checkmark",box.transform,0,0,24,24).gameObject.AddComponent<Image>();
        if(NativeMenuTheme.Checkmark(box,mark))
        {
            mark.gameObject.SetActive(value);
            var text=b.GetComponentInChildren<TMP_Text>();text.rectTransform.anchoredPosition=new Vector2(43,0);text.rectTransform.sizeDelta=new Vector2(width-55,38);text.alignment=TextAlignmentOptions.MidlineLeft;
        }
        else box.gameObject.SetActive(false);
    }
    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)obj.transform;
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(width, height); return r;
    }
    private static TMP_Text Text(Transform parent, string text, float x, float y, float width, float height, float size, Color color)
    {
        var label = Rect("Label", parent, x, y, width, height).gameObject;
        // Assign the native font before TMP Awake tries to load its default font.
        label.SetActive(false);
        var t = label.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size;
        // Do not inherit TMP project defaults for labels that later enable auto-sizing.
        t.fontSizeMin = size; t.fontSizeMax = size;
        t.color = color; t.raycastTarget = false;
        NativeMenuTheme.Text(t);
        t.alignment = TextAlignmentOptions.MidlineLeft; t.textWrappingMode = TextWrappingModes.Normal; t.richText = false; label.SetActive(true); return t;
    }
    private static Button Button(Transform parent, string label, float x, float y, float width, Action action, bool track = true)
    {
        var go = Rect(label, parent, x, y, width, 38).gameObject;
        var img = go.AddComponent<Image>(); img.color = new Color(.23f, .18f, .12f);
        var b = go.AddComponent<ConfigButton>(); b.targetGraphic = img;
        var t = Text(go.transform, label, 9, 0, width - 18, 38, 18, Color.white); t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 18;
        NativeMenuTheme.Button(b);
        b.onClick.AddListener(() => action()); if (track) controls.Add(b); return b;
    }
    private static TMP_InputField Input(Transform parent, string value, float x, float y, float width, Action<string> save, string placeholder, bool number = false, bool saveOnEndEdit = true)
    {
        var go = Rect("Input", parent, x, y, width, 38).gameObject;
        go.SetActive(false); // TMP builds its caret on enable, after text/viewport assignment.
        var image = go.AddComponent<Image>(); image.color = new Color(.15f, .11f, .075f);
        var field = go.AddComponent<TMP_InputField>(); field.targetGraphic = image;
        var viewport = Rect("Viewport", go.transform, 10, -2, width - 20, 34); viewport.gameObject.AddComponent<RectMask2D>();
        var text = Text(viewport, "", 0, 0, width - 20, 34, 20, Color.white);
        field.textViewport = viewport; field.textComponent = text; field.fontAsset = font; field.text = value;
        field.placeholder = Text(viewport, placeholder, 0, 0, width - 20, 34, 18, Muted);
        field.contentType = number ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
        NativeMenuTheme.Input(field);
        field.customCaretColor = true;
        field.caretColor = new Color(1f, .85f, .45f, 1f);
        field.caretWidth = 2;
        field.caretBlinkRate = .85f;
        field.selectionColor = new Color(.35f, .60f, 1f, .55f);
        field.characterLimit = number ? 6 : 60;
        go.SetActive(true);
        if (saveOnEndEdit) field.onEndEdit.AddListener(v => { if (!refreshing && IsOpen && v != value) save(v); });
        controls.Add(field); return field;
    }
    private static void Controller()
    {
        if (!ZInput.IsGamepadActive() || controls.Count == 0) return;
        var current = EventSystem.current?.currentSelectedGameObject;
        if (current && current.GetComponent<TMP_InputField>()?.isFocused == true) return;
        int delta = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyDPadRight") ? 1 : ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyDPadLeft") ? -1 : 0;
        if (delta != 0) { selected = (selected + delta + controls.Count) % controls.Count; controls[selected].Select(); }
        if (ZInput.GetButtonDown("JoyButtonA"))
        {
            ZInput.ResetButtonStatus("JoyButtonA");
            var control = controls[Mathf.Clamp(selected, 0, controls.Count - 1)];
            if (control is Button b) b.onClick.Invoke(); else if (control is TMP_InputField input) input.ActivateInputField();
        }
    }
}

public sealed class RangeVisual : MonoBehaviour
{
    private float until;
    private Material mat;
    internal static void Show(Vector3 point, float radius)
    {
        var go = new GameObject("Quartermaster_RangePreview"); go.transform.position = point;
        var preview = go.AddComponent<RangeVisual>(); preview.until = Time.time + 60f;
        preview.mat = new Material(Shader.Find("Sprites/Default"));
        foreach (bool vertical in new[] { false, true })
        {
            var circle = new GameObject("Coverage"); circle.transform.SetParent(go.transform, false);
            var line = circle.AddComponent<LineRenderer>(); line.sharedMaterial = preview.mat; line.useWorldSpace = false; line.loop = true;
            line.widthMultiplier = .15f; line.startColor = line.endColor = new Color(.25f, .8f, 1f, .7f); line.positionCount = 128;
            for (int i = 0; i < 128; i++) { float a = i * Mathf.PI * 2 / 128; line.SetPosition(i, vertical ? new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0) : new Vector3(Mathf.Cos(a) * radius, .3f, Mathf.Sin(a) * radius)); }
        }
    }
    private void Update() { if (Time.time >= until || !Player.m_localPlayer) Destroy(gameObject); }
    private void OnDestroy() { if (mat) Destroy(mat); }
}

// Controller activation is handled once through ZInput; pointer activation stays native.
public sealed class ConfigButton : Button
{
    public override void OnSubmit(BaseEventData eventData) { }
}
