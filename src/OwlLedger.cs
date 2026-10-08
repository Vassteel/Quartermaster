using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Quartermaster;

[Serializable]
public sealed class LedgerSettings
{
    public string Group="";
    public long UpdatedTicks;
    public List<ProductCap> Caps=new List<ProductCap>();
}

public sealed class OwlLedger:MonoBehaviour,Hoverable,Interactable
{
    internal const string PrefabName="piece_quartermaster_ledger";
    private const string Key="Quartermaster.ledger.v1";
    private static readonly List<OwlLedger> All=new List<OwlLedger>();
    private Transform page;
    private string cachedJson;
    private LedgerSettings cachedSettings;
    private string failedJson;
    private float pageStarted=-100;
    internal Vector3 Perch=>transform.TransformPoint(new Vector3(-.68f,1.305f,.05f));
    internal Vector3 Book=>transform.TransformPoint(new Vector3(0,1.28f,0));
    private void Awake(){All.Add(this);page=transform.Find("Quartermaster ledger page");}
    private void OnDestroy()=>All.Remove(this);
    private void LateUpdate()
    {
        if(page)page.localRotation=Quaternion.Euler(-20,0,165*Mathf.Sin(Mathf.Clamp01((Time.time-pageStarted)/1.4f)*Mathf.PI));
    }
    internal void TurnPage()=>pageStarted=Time.time;
    internal LedgerSettings Settings
    {
        get
        {
            var view=Automation.View(this);if(!view||!view.IsValid())return new LedgerSettings();
            string text=view.GetZDO().GetString(Key,"");
            try
            {
                if(cachedSettings!=null&&text==cachedJson)return cachedSettings;
                var settings=text.Length==0?new LedgerSettings():LedgerCodec.Read(text);
                settings=settings??new LedgerSettings();settings.Group=settings.Group??"";
                settings.Caps=(settings.Caps??new List<ProductCap>()).Where(c=>c!=null).ToList();
                failedJson=null;cachedJson=text;cachedSettings=settings;return settings;
            }
            catch(Exception error)
            {
                if(failedJson!=text)Plugin.Log.LogWarning("Ledger data could not be read: book="+view.GetZDO().m_uid+" bytes="+text.Length+" error="+error.GetType().Name+": "+error.Message);
                failedJson=text;return new LedgerSettings();
            }
        }
    }
    internal Container Home
    {
        get
        {
            string group=Settings.Group;
            return ContainerRegistry.All.Where(c=>c&&ContainerRegistry.Accessible(c)&&ContainerRegistry.GetSettings(c).Deposit&&
                (group.Length==0||Policy.SameGroup(group,ContainerRegistry.GetSettings(c).Group))&&
                (c.transform.position-transform.position).sqrMagnitude<=Plugin.Range.Value*Plugin.Range.Value)
                .OrderBy(c=>(c.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
        }
    }
    internal BaseNetwork Network=>Home?Automation.Network(Home):null;
    internal string Group=>Home?ContainerRegistry.GetSettings(Home).Group:Settings.Group.Length>0?Settings.Group:"Home";
    internal static OwlLedger Authority(BaseNetwork network)
    {
        if(network==null)return null;
        return All.Where(l=>l&&Automation.View(l)&&Automation.View(l).IsValid()&&network.Contains(l.transform.position)&&Policy.SameGroup(l.Group,network.Group))
            .OrderByDescending(l=>l.Settings.UpdatedTicks).ThenByDescending(l=>l.Settings.Caps.Count>0).ThenBy(l=>Automation.View(l).GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
    }
    internal static int Cap(BaseNetwork network,string id,int fallback)
    {
        var book=Authority(network);var cap=book?book.Settings.Caps.FirstOrDefault(c=>c.Item==id):null;
        return cap!=null&&cap.Amount>=0&&cap.Amount<=100000?cap.Amount:fallback;
    }
    internal bool HasCap(string id)=>Settings.Caps.Any(c=>c.Item==id);
    internal string SaveCap(string id,int amount)
    {
        if(string.IsNullOrEmpty(id)||amount<0||amount>100000)return "Enter a limit from 0 to 100000";
        var authority=Authority(Network);if(!authority)return "Place the ledger within a Deposit Chest's range";
        if(!Allowed(this)||!Allowed(authority))return "Ward access prevents changing this ledger";
        var settings=LedgerCodec.Copy(authority.Settings);settings.Group=Group;
        var cap=settings.Caps.FirstOrDefault(c=>c.Item==id);
        if(cap==null)settings.Caps.Add(new ProductCap{Item=id,Amount=amount});else cap.Amount=amount;
        // Store on the book being edited. Previously edits could land only on a
        // different loaded book, leaving this one blank when that authority unloaded.
        settings.UpdatedTicks=Math.Max(DateTime.UtcNow.Ticks,Math.Min(authority.Settings.UpdatedTicks,DateTime.MaxValue.Ticks-1)+1);
        if(!Save(settings))return "Could not verify the saved limit; try Save again";
        // Verify the same resolution path the UI and production use, not just that
        // a ZDO write was attempted. In particular, do not trust an old settings cache.
        int actual=Cap(Network,id,-1);
        if(actual!=amount)
        {
            Plugin.Log.LogWarning("Ledger limit verification failed: "+id+" requested="+amount+" effective="+actual+" stored="+Settings.Caps.Count+" network="+(Network?.Group??"none")+" book="+Automation.View(this).GetZDO().m_uid);
            return "Limit was not applied. Refresh the book and try Save again";
        }
        Plugin.Log.LogInfo("Ledger limit saved: "+id+"="+actual+" book="+Automation.View(this).GetZDO().m_uid);
        return "Saved base production limit: "+actual;
    }
    internal bool SaveGroup(string group)
    {var settings=LedgerCodec.Copy(Settings);settings.Group=string.IsNullOrWhiteSpace(group)?"Home":group.Trim();return Save(settings);}
    private bool Save(LedgerSettings settings)
    {
        if(!Allowed(this))return false;
        var view=Automation.View(this);if(!view||!view.IsValid())return false;
        // The sign-style shared book claims its own ZDO, never a storage inventory.
        if(!view.IsOwner())view.ClaimOwnership();if(!view.IsOwner())return false;
        try
        {
            string json=LedgerCodec.Write(settings);
            // Check the payload itself before replacing persistent data.
            if(!LedgerCodec.Equal(settings,LedgerCodec.Read(json)))return false;
            view.GetZDO().Set(Key,json);
            cachedJson=null;cachedSettings=null;
            string stored=view.GetZDO().GetString(Key,"");
            return stored==json&&LedgerCodec.Equal(settings,LedgerCodec.Read(stored));
        }
        catch(Exception error)
        {
            Plugin.Log.LogWarning("Ledger save failed: book="+view.GetZDO().m_uid+" error="+error.GetType().Name+": "+error.Message);
            return false;
        }
    }
    internal static bool Allowed(OwlLedger book)=>book&&Player.m_localPlayer&&Plugin.Enabled.Value&&PrivateArea.CheckAccess(book.transform.position,0,false,true);
    internal static OwlLedger VisitFor(Container home)
    {
        var n=Automation.Network(home);if(n==null)return null;
        return All.Where(l=>l&&Automation.View(l)&&Automation.View(l).IsValid()&&Allowed(l)&&n.Contains(l.transform.position)&&Policy.SameGroup(l.Group,n.Group))
            .OrderBy(l=>(l.transform.position-home.transform.position).sqrMagnitude).FirstOrDefault();
    }
    public string GetHoverName()=>"Quartermaster Ledger";
    public float GetHoverOffset()=>.3f;
    public string GetHoverText()=>Localization.instance.Localize("Quartermaster Ledger\n[<color=yellow><b>$KEY_Use</b></color>] Open book");
    public bool Interact(Humanoid user,bool hold,bool alt)
    {
        if(hold||user!=Player.m_localPlayer||!Allowed(this))return false;
        ChestUi.OpenLedger(this);TurnPage();return true;
    }
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
