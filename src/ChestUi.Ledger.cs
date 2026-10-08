using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Quartermaster;

internal static partial class ChestUi
{
    private const int LedgerStockTab = 0, LedgerStackTab = 1, LedgerMachinesTab = 2, LedgerDeliveriesTab = 3, LedgerInterfaceTab = 4;
    private static bool stackCustom, stackPending;
    private static string stackDraft;
    private static readonly List<(string id,TMP_Text text)> ledgerRows=new List<(string,TMP_Text)>();
    private static readonly List<(Component machine,TMP_Text text)> ledgerMachines=new List<(Component,TMP_Text)>();
    private static ServerSettings StackSettings=>Plugin.Instance.GetComponent<ServerSettings>();
    internal static void OpenLedger(OwlLedger book)
    {
        if(ConfigurationManagerCompatibility.IsOpen)return;
        if(!OwlLedger.Allowed(book)||!Near(book)||!InventoryGui.instance)return;
        if(!InventoryGui.IsVisible())InventoryGui.instance.Show(null);
        ledger=book;chest=null;machine=null;pickupMode=false;tab=page=0;query=notice="";
        stackCustom=StackSettings.CustomStacks;stackDraft=StackSettings.StackMaximum.ToString();stackPending=false;
        var authority=OwlLedger.Authority(book.Network);
        if(authority)Plugin.Log.LogInfo("Ledger opened: book="+Automation.View(book).GetZDO().m_uid+
            " authority="+Automation.View(authority).GetZDO().m_uid+" limits="+
            string.Join(",",authority.Settings.Caps.Select(c=>c.Item+"="+c.Amount)));
        Build();
    }
    private static string StatusText()
    {
        if(ledger&&tab==LedgerStackTab&&stackPending)return StackSettings.StackStatus;
        if(notice.Length>0)return notice;
        if(ledger&&tab==LedgerInterfaceTab)return "Local interface settings";
        if(ledger){var n=ledger.Network;return n==null?"Place near a Deposit Chest in the same group":"Base: "+ledger.Group+" · "+n.Chests.Count+" chests · "+n.Machines.Count+" machines";}
        return pickupMode?PickupStatus():Automation.Status(chest?(Component)chest:machine);
    }
    private static void RefreshLedgerRows()
    {
        if(!ledger)return;var network=ledger.Network;
        if(network==null)
        {
            foreach(var row in ledgerMachines)if(row.text)row.text.text="No Deposit Chest in this base group and range";
            return;
        }
        foreach(var row in ledgerMachines)if(row.text)row.text.text=row.machine&&network.Machines.Contains(row.machine)?Automation.Status(row.machine):"Machine no longer in this base · Refresh";
        foreach(var row in ledgerRows)if(row.text)row.text.text=Automation.Stock(network,row.id)+" / "+Automation.Queued(network,row.id);
    }
    private static void SaveLedgerCap(string id,TMP_InputField input)
    {
        if(!ledger||!Near(ledger)||!input)return;
        if(!int.TryParse(input.text,out var amount)){notice="Enter a whole number from 0 to 100000";return;}
        notice=ledger.SaveCap(id,amount);
        if(notice.StartsWith("Saved",StringComparison.Ordinal))
            input.SetTextWithoutNotify(Automation.EffectiveCap(ledger.Network,id).ToString());
        if(status)status.text=notice;
    }
    private static void LedgerBody()
    {
        if(tab==LedgerInterfaceTab){InterfaceBody();return;}
        if(tab==LedgerStackTab){StackBody();return;}
        Text(body,"Base group",0,0,130,38,19,Gold);
        Input(body,ledger.Group,138,0,325,value=>{
            if(!ledger||!Near(ledger))return;
            notice=ledger.SaveGroup(value)?"Base group saved":"Could not save base group";page=0;Build();
        },"Home");
        Button(body,"Refresh",480,0,192,()=>{notice="";Build();});
        var n=ledger.Network;
        if(n==null){Text(body,"Place this lectern within a Deposit Chest’s range.\nUse the same base group as its chests and machines.",0,-65,672,110,22,Muted);return;}
        if(tab==LedgerDeliveriesTab){LedgerDeliveriesBody(n);return;}
        Input(body,query,0,-48,tab==LedgerMachinesTab?672:370,value=>{query=value;page=0;ChestSearch.Clear();Build();},tab==LedgerMachinesTab?"Search machines":"Search items");
        if(tab!=LedgerMachinesTab)
        {
            Button(body,"Find chests",382,-48,170,()=>{
                if(string.IsNullOrWhiteSpace(query)){notice="Enter an item name first.";return;}
                int count=ChestSearch.Start(ledger,query);
                notice=count+" matching chests · chests glow until opened. Busy chests are skipped.";
                if(status)status.text=notice;
            });
            Button(body,"Clear",564,-48,108,()=>{ChestSearch.Clear();query="";notice="Search cleared";page=0;Build();});
        }
        if(tab==LedgerMachinesTab){LedgerMachinesBody(n);return;}
        var products=new HashSet<string>(n.Machines.SelectMany(Automation.Products).Select(InventoryTransfers.PrefabId));
        // A workstation's recipe list is not evidence that an item has entered
        // this base. Remembered storage types keep depleted items editable.
        var introduced=new HashSet<string>(n.Chests.SelectMany(c=>StorageObservation.Read(c).GetAllItems()).Select(InventoryTransfers.ItemId)
            .Concat(n.Chests.SelectMany(c=>ContainerRegistry.GetSettings(c).Remembered))
            .Concat(Automation.Outputs.Where(d=>d&&n.Contains(d.transform.position)).Select(d=>InventoryTransfers.ItemId(d.m_itemData)))
            .Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
        introduced.UnionWith(products.Where(id=>!string.IsNullOrEmpty(id)&&Automation.Queued(n,id)>0));
        // Expose outputs of installed machines before the first batch is made.
        introduced.UnionWith(products.Where(id=>!string.IsNullOrEmpty(id)));
        var ids=introduced
            .Where(id=>ItemLabel(id).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)
            .OrderBy(ItemLabel).ThenBy(id=>id,StringComparer.Ordinal).ToList();
        for(int column=0;column<2;column++)
        {
            float x=column*344;
            Text(body,"ITEM",x,-94,45,25,14,Gold);
            Text(body,"STOCK / QUEUED",x+47,-94,112,25,13,Gold);
            Text(body,"MAX STOCK",x+164,-94,164,25,14,Gold);
        }
        var itemName=Text(body,"Hover or select an item to see its name",0,-470,672,24,17,Gold);
        itemName.overflowMode=TextOverflowModes.Ellipsis;
        const int perPage=16;
        int pages=Math.Max(1,(ids.Count+perPage-1)/perPage);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<perPage&&page*perPage+i<ids.Count;i++)
        {
            string id=ids[page*perPage+i];float x=(i/8)*344,y=-126-(i%8)*43;
            string name=ItemLabel(id);
            var iconButton=Button(body,"",x,y,40,()=>itemName.text=name);
            var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab(id):null;
            var item=prefab?prefab.GetComponent<ItemDrop>():null;
            var sprite=item?item.m_itemData.GetIcon():null;
            if(sprite)
            {
                var icon=Rect("Item icon",iconButton.transform,3,-3,34,32).gameObject.AddComponent<Image>();
                icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            }
            else iconButton.GetComponentInChildren<TMP_Text>().text="?";
            LedgerItemHint.Attach(iconButton.gameObject,itemName,name);
            var stock=Text(body,"",x+47,y,112,38,16,Muted);
            stock.enableAutoSizing=true;stock.fontSizeMin=12;stock.fontSizeMax=16;
            ledgerRows.Add((id,stock));
            var input=Input(body,Automation.HasProductionLimit(n,id)?Automation.EffectiveCap(n,id).ToString():"",x+164,y,92,null,"Set qty",true,false);
            input.textComponent.fontSize=16;
            input.onFocusSelectAll=true;input.onSubmit.AddListener(_=>SaveLedgerCap(id,input));
            LedgerItemHint.Attach(input.gameObject,itemName,name);
            var save=Button(body,"Save",x+260,y,68,()=>SaveLedgerCap(id,input));
            LedgerItemHint.Attach(save.gameObject,itemName,name);
        }
        if(ids.Count==0)Text(body,query.Length>0?"No items match your search.":"No items added yet. Add items to this base’s chests, then Refresh.",0,-143,670,80,20,Muted);
        Text(body,"Stock: chests + production drops. Queued output counts toward limits.\nSave a quantity to enable production; 0 pauses it. Stored items are kept.",0,-496,672,36,14,Muted);
        Button(body,"Previous",0,-536,170,()=>{page=Math.Max(0,page-1);Build();});
        Text(body,(page+1)+" / "+pages,198,-536,260,38,18,Muted);
        Button(body,"Next",502,-536,170,()=>{page=Math.Min(pages-1,page+1);Build();});
        RefreshLedgerRows();
    }
    private static void LedgerDeliveriesBody(BaseNetwork network)
    {
        var entries=network.Hubs.SelectMany(DeliveryHistory.Read).OrderByDescending(e=>e.UtcTicks).ToList();
        const int perPage=4;
        int pages=Math.Max(1,(entries.Count+perPage-1)/perPage);page=Mathf.Clamp(page,0,pages-1);
        Text(body,"Received mail · automatically queued for sorting · times in UTC",0,-48,672,28,17,Gold);
        for(int i=0;i<perPage&&page*perPage+i<entries.Count;i++)
        {
            var e=entries[page*perPage+i];float y=-84-i*55;
            var from=Text(body,"From "+e.Origin+" · "+e.Player,0,y,672,25,18,Gold);
            from.enableAutoSizing=true;from.fontSizeMin=12;from.fontSizeMax=18;
            Text(body,new DateTime(e.UtcTicks,DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm")+" · "+e.Count+" items received",0,y-25,672,23,16,Muted);
        }
        if(entries.Count==0)Text(body,"No deliveries recorded yet.",0,-90,672,50,20,Muted);
        Button(body,"Previous",0,-325,170,()=>{page=Math.Max(0,page-1);Build();});
        Text(body,(page+1)+" / "+pages,198,-325,260,38,18,Muted);
        Button(body,"Next",502,-325,170,()=>{page=Math.Min(pages-1,page+1);Build();});
    }
    private static void LedgerMachinesBody(BaseNetwork network)
    {
        var machines=network.Machines.Where(m=>m&&DisplayMachine(m).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)
            .OrderBy(DisplayMachine).ThenBy(m=>(m.transform.position-ledger.transform.position).sqrMagnitude)
            .ThenBy(m=>m.GetInstanceID()).ToList();
        Text(body,"MACHINE",0,-94,245,25,16,Gold);
        Text(body,"PRODUCTION STATUS",248,-94,424,25,16,Gold);
        int pages=Math.Max(1,(machines.Count+3)/4);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<4&&page*4+i<machines.Count;i++)
        {
            var machine=machines[page*4+i];float y=-124-i*51;
            var name=Text(body,DisplayMachine(machine),0,y,240,27,19,Gold);
            name.fontSizeMin=14;name.fontSizeMax=19;name.enableAutoSizing=true;name.overflowMode=TextOverflowModes.Ellipsis;
            Text(body,Vector3.Distance(machine.transform.position,ledger.transform.position).ToString("0")+" m from book",0,y-27,240,20,14,Muted);
            var text=Text(body,"",248,y,424,48,18,Color.white);
            text.fontSizeMin=14;text.fontSizeMax=18;text.enableAutoSizing=true;text.overflowMode=TextOverflowModes.Ellipsis;
            ledgerMachines.Add((machine,text));
        }
        if(machines.Count==0)Text(body,query.Length>0?"No machines match your search.":"No loaded machines in this base group and range.",0,-143,672,80,20,Muted);
        Text(body,"Status updates automatically. Refresh to find newly added machines.",0,-327,672,20,15,Muted);
        Button(body,"Previous",0,-346,170,()=>{page=Math.Max(0,page-1);Build();});
        Text(body,(page+1)+" / "+pages,198,-346,260,38,18,Muted);
        Button(body,"Next",502,-346,170,()=>{page=Math.Min(pages-1,page+1);Build();});
        RefreshLedgerRows();
    }
    private static void StackBody()
    {
        var settings=StackSettings;bool allowed=settings.CanEditStacks;
        Text(body,"STACK SIZES",0,0,672,32,24,Gold);
        Text(body,"Choose normal item limits or a custom maximum for stackable items.\nThis setting applies to the whole world, across all base groups.",0,-43,672,63,19,Muted);
        TMP_InputField maximum=null;
        Button(body,stackCustom?"Use normal stack sizes":"Use custom stack sizes",0,-121,672,()=>{
            if(!allowed)return;stackDraft=maximum.text;stackCustom=!stackCustom;stackPending=false;notice="Press Save to apply";Build();
        });
        Text(body,stackCustom?"Custom maximum":"Normal item limits selected",0,-176,350,38,21,Color.white);
        maximum=Input(body,stackDraft,452,-176,220,null,"2–100000",true,false);
        maximum.interactable=allowed&&stackCustom;
        Action save=()=>{
            if(!ledger||!Near(ledger)||!OwlLedger.Allowed(ledger))return;
            if(!int.TryParse(maximum.text,out int count)||count<2||count>100000){stackPending=false;notice="Enter a maximum from 2 to 100000";return;}
            stackDraft=maximum.text;notice=settings.SetStacks(stackCustom,count);
            stackPending=notice==settings.StackStatus;
        };
        maximum.onSubmit.AddListener(_=>save());
        Button(body,"Save stack sizes",0,-241,672,save).interactable=allowed;
        Text(body,allowed?"Fills available chest space with normal stacks; excess drops in front.\nBusy, blocked or unloaded chests keep their stock until ready.":"Only the server host or an admin can change stack sizes.\nThe server’s settings apply to everyone.",0,-300,672,70,19,Muted);
    }
}

// Keep names available to both pointer users and the existing controller navigation.
public sealed class LedgerItemHint : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private TMP_Text target;
    private string itemName;
    internal static void Attach(GameObject control,TMP_Text target,string name)
    {
        var hint=control.AddComponent<LedgerItemHint>();hint.target=target;hint.itemName=name;
    }
    private void Show(){if(target)target.text=itemName;}
    public void OnPointerEnter(PointerEventData eventData)=>Show();
    public void OnSelect(BaseEventData eventData)=>Show();
}
