using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Quartermaster;

public sealed class PostalMailbox : MonoBehaviour
{
    internal const string PrefabName="Quartermaster_mail_chest";
    internal static readonly List<PostalMailbox> All=new List<PostalMailbox>();
    internal Container Chest;
    private Transform eagle;
    private PostalEagle motion;
    private Vector3 perch;
    private long seenFlight;
    private float flightStart;
    private bool flying;
    private PostalParcel outgoing;
    internal const string HomeKey="Quartermaster.mailHome.v1";
    private float sendAt;
    private bool queued;
    internal static bool IsMailbox(Container c)=>c&&Utils.GetPrefabName(c.gameObject)==PrefabName;
    internal static IEnumerable<Container> Deposits=>ContainerRegistry.All.Where(c=>c&&!IsMailbox(c)&&Plugin.Enabled.Value&&ContainerRegistry.Accessible(c)&&ContainerRegistry.GetSettings(c).Deposit);
    internal IEnumerable<Container> NearbyHomes=>Deposits.Where(c=>Vector3.Distance(c.transform.position,transform.position)<=Plugin.Range.Value).OrderBy(c=>Vector3.Distance(c.transform.position,transform.position));
    internal Container Home
    {
        get
        {
            var view=Automation.View(this);
            string selected=view&&view.IsValid()?view.GetZDO().GetString(HomeKey,""):"";
            // An explicit choice must not silently switch bases when it unloads.
            return selected.Length==0?NearbyHomes.FirstOrDefault():NearbyHomes.FirstOrDefault(c=>ContainerRegistry.GetView(c).GetZDO().m_uid.ToString()==selected);
        }
    }
    internal static string Name(Container home)
    {
        if(!home)return "Unassigned mailbox";
        var settings=ContainerRegistry.GetSettings(home);
        string name=string.IsNullOrWhiteSpace(settings.BaseName)?"Unnamed base":settings.BaseName;
        return name+" ("+Mathf.RoundToInt(home.transform.position.x)+", "+Mathf.RoundToInt(home.transform.position.z)+")";
    }
    internal string BaseName=>Name(Home);
    internal string Label=>BaseName;
    internal bool SelectHome(Container home)
    {
        if(queued||PostalDirectory.Pending(Chest)||flying||!ContainerRegistry.IsUsable(Chest,true)||!NearbyHomes.Contains(home))return false;
        ContainerRegistry.GetView(Chest).GetZDO().Set(HomeKey,ContainerRegistry.GetView(home).GetZDO().m_uid.ToString());return Home==home;
    }
    internal bool RenameBase(string name,Action<string> result)
    {
        var home=Home;if(!home||queued||PostalDirectory.Pending(Chest)||flying)return false;
        name=(name??"").Trim();if(name.Length>40)name=name.Substring(0,40);
        bool Valid()=>this&&home&&Home==home&&ContainerRegistry.IsUsable(home,false);
        void Work()
        {
            var settings=JsonUtility.FromJson<ChestSettings>(JsonUtility.ToJson(ContainerRegistry.GetSettings(home)));
            settings.BaseName=name;
            if(!ContainerRegistry.SaveSettings(home,settings))throw new InvalidOperationException("Base name could not be saved");
        }
        return NativeStorageAccess.Start("mail base name",new[]{ContainerRegistry.GetView(home)},Valid,Work,result);
    }
    internal Container Receiver(Container home,out bool hasMailbox)
    {
        var mailbox=All.Where(m=>m&&m!=this&&m.Chest&&ContainerRegistry.GetView(m.Chest)&&ContainerRegistry.GetView(m.Chest).IsValid()&&m.Home==home)
            .OrderBy(m=>(m.transform.position-home.transform.position).sqrMagnitude)
            .ThenBy(m=>ContainerRegistry.GetView(m.Chest).GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
        hasMailbox=mailbox;
        return mailbox?mailbox.Chest:home;
    }
    internal static string Availability(Container receiver)
    {
        if(!Plugin.Enabled.Value||!receiver||!ContainerRegistry.Accessible(receiver))return "Unavailable";
        if(receiver.IsInUse()||ContainerRegistry.GetView(receiver).GetZDO().GetInt(ZDOVars.s_inUse)!=0)return "Busy";
        return "Available";
    }
    internal static void Initialize()=>PrefabManager.OnVanillaPrefabsAvailable+=Register;
    internal static void Shutdown()=>PrefabManager.OnVanillaPrefabsAvailable-=Register;
    private static void Register()
    {
        try{PostalParcel.Register();RegisterPiece();Shutdown();}
        catch(Exception error){Plugin.Log.LogError("Mailbox registration failed: "+error);}
    }
    private static void RegisterPiece()
    {
        var prefab=PrefabManager.Instance.CreateClonedPrefab(PrefabName,"piece_chest_wood");
        if(!prefab)throw new InvalidOperationException("Native mailbox chest template missing");
        MailboxModel.Build(prefab);
        // This flag means wear when there is no roof, not immunity. Disable
        // rain/submersion wear only; health and normal damage remain native.
        prefab.GetComponent<WearNTear>().m_noRoofWear=false;
        var c=prefab.GetComponent<Container>();c.m_name="Quartermaster Mailbox";c.m_width=4;c.m_height=2;c.m_defaultItems=new DropTable();
        c.m_open=null;c.m_closed=null;
        prefab.AddComponent<PostalMailbox>();
        if(!PieceManager.Instance.AddPiece(new CustomPiece(prefab,false,new PieceConfig {
            Name="Quartermaster Mailbox",Description="Load items, then use Chest Config to queue delivery to another base's mailbox or Deposit Chest.",
            PieceTable="Hammer",Category="Quartermaster",CraftingStation="piece_workbench",
            Requirements=new[]{new RequirementConfig("Wood",15,0,true),new RequirementConfig("Bronze",2,0,true)}
        })))throw new InvalidOperationException("Mailbox registration failed");
        BuildMenuCategory.Register(PrefabName);
    }
    private void Awake()
    {
        Chest=GetComponent<Container>();eagle=transform.Find("QuartermasterPostalEagle");
        if(eagle){motion=eagle.GetComponent<PostalEagle>();perch=eagle.localPosition;eagle.gameObject.SetActive(false);}
        if(!All.Contains(this))All.Add(this);
    }
    internal bool Request(MailAddress address)
    {
        var origin=Home;
        if(queued||PostalDirectory.Pending(Chest)||!origin||address==null||ContainerRegistry.GetView(origin).GetZDO().m_uid==address.Id||
            !ContainerRegistry.IsUsable(Chest,true)||Chest.GetInventory().NrOfItems()==0)return false;
        outgoing=PostalParcel.Create(this,address);queued=true;sendAt=Time.time+.3f;
        Automation.View(this).GetZDO().Set("Quartermaster.mailFlight",(long)(ZNet.instance.GetTimeSeconds()*1000));return true;
    }
    private void Update()
    {
        var view=Automation.View(this);if(!view||!view.IsValid())return;
        long flight=view.GetZDO().GetLong("Quartermaster.mailFlight",0);
        if(eagle&&flight!=seenFlight)
        {
            seenFlight=flight;
            // Do not replay old arrivals when a base streams back in.
            double age=ZNet.instance.GetTimeSeconds()-flight/1000d;
            if(age>=0&&age<11){flying=true;flightStart=Time.time-(float)age;eagle.gameObject.SetActive(true);}
        }
        if(eagle&&flying)
        {
            float t=Time.time-flightStart;
            if(t>=11){flying=false;eagle.gameObject.SetActive(false);}
            else
            {
                bool approach=t<4,depart=t>6;
                float distance=approach?1-Mathf.SmoothStep(0,1,t/4):depart?Mathf.SmoothStep(0,1,(t-6)/5):0;
                eagle.localPosition=perch+new Vector3(approach?-distance*9:distance*9,distance*8,-distance*5);
                eagle.localRotation=Quaternion.Euler(0,approach?60:depart?120:0,0);
                if(motion)motion.Flight=approach||depart;
            }
        }
        if(queued&&Time.time>=sendAt)
        {
            queued=false;var parcel=outgoing;outgoing=null;
            bool Valid()=>this&&parcel&&parcel.View&&parcel.View.IsValid()&&PostalParcel.Phase(parcel.View.GetZDO())==MailPhase.Preparing&&
                ContainerRegistry.IsUsable(Chest,false)&&ContainerRegistry.IsUsable(parcel.Chest,false);
            void Work()
            {
                foreach(var item in Chest.GetInventory().GetAllItems().ToArray())InventoryTransfers.Move(Chest.GetInventory(),parcel.Chest.GetInventory(),item,item.m_stack,false);
                parcel.Activate();
            }
            void Result(string message)
            {
                if(message!="Saved"&&parcel&&parcel.View&&parcel.View.IsValid()&&parcel.View.IsOwner())parcel.Activate();
                if(Player.m_localPlayer)Player.m_localPlayer.Message(MessageHud.MessageType.Center,message=="Saved"?"Mail queued. You can leave the base.":"Mail reservation stopped; unsent items remain in the mailbox.");
            }
            if(!parcel||!NativeStorageAccess.Start("reserve mail",new[]{view,parcel.View},Valid,Work,Result))Result("Unavailable");
        }
    }
    private void OnDestroy(){All.Remove(this);}
}
