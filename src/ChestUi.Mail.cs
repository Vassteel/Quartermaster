using System;
using System.Linq;
using UnityEngine;

namespace Quartermaster;
internal static partial class ChestUi
{
    private static void MailBody()
    {
        var source=chest.GetComponent<PostalMailbox>();
        if(!source){Text(body,"Mailbox is not ready.",0,0,672,40,20,Muted);return;}
        if(tab==1){MailboxSettingsBody(source);return;}
        Text(body,"From: "+source.BaseName,0,0,505,38,20,Gold);
        Button(body,"Refresh",520,0,152,()=>{notice="";Build();});
        Text(body,"Choose a destination. Mail goes to its mailbox, or directly to its\nDeposit Chest when no mailbox is present.",0,-43,672,55,17,Muted);
        var home=source.Home;
        var state=PostalDirectory.Progress(chest);bool pending=state!=null&&!MailRoute.Terminal(state.Phase);
        if(pending)
        {
            Text(body,state.Status+" · "+state.Count+" items",0,-110,672,65,20,Gold);
            Button(body,"Cancel / return remaining mail",0,-195,480,()=>{notice=PostalDirectory.Cancel(chest)?"Cancellation requested; remaining mail will return.":"Mailbox busy; try again.";Build();});
            Text(body,"Reserved items travel with the saved parcel. Returned items wait\nfor room in this mailbox. New items here are not added to that parcel.",0,-265,672,65,17,Muted);
            return;
        }
        var targets=PostalDirectory.Addresses.Where(a=>!home||a.Id!=ContainerRegistry.GetView(home).GetZDO().m_uid)
            .OrderBy(a=>(a.Position-source.transform.position).sqrMagnitude).ThenBy(a=>a.Id.ToString(),StringComparer.Ordinal).ToList();
        int pages=Math.Max(1,(targets.Count+2)/3);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<3&&page*3+i<targets.Count;i++)
        {
            var target=targets[page*3+i];float y=-105-i*61;
            var go=ZNetScene.instance.FindInstance(target.Receiver);var receiver=go?go.GetComponent<Container>():null;
            string available=receiver?PostalMailbox.Availability(receiver):"Unloaded · delivery queued";
            var label=Text(body,target.Name,0,y,485,28,18,Color.white);label.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            Text(body,Vector3.Distance(source.transform.position,target.Position).ToString("0")+" m · "+(target.Mailbox?"Mailbox → sorting":"Deposit Chest → sorting")+" · "+available,0,y-28,485,25,15,Muted);
            var send=Button(body,"Send",506,y,166,()=>{
                if(!source.Request(target)){notice="Cannot queue: check contents and the local base connection.";Build();return;}
                Close();InventoryGui.instance.Hide();
            });send.interactable=home;
        }
        if(targets.Count==0)Text(body,"Discovering known bases. Use Refresh shortly.",0,-140,672,50,20,Gold);
        Text(body,state!=null?state.Status:home?"Mail waits for the destination to load and have space.":"Choose a local Deposit Chest in Settings before sending.",0,-293,672,40,17,Muted);
        Button(body,"Previous",0,-340,180,()=>{page=Math.Max(0,page-1);Build();});
        Text(body,(page+1)+" / "+pages,220,-340,220,38,18,Muted);
        Button(body,"Next",492,-340,180,()=>{page=Math.Min(pages-1,page+1);Build();});
    }
    private static void MailboxSettingsBody(PostalMailbox source)
    {
        var home=source.Home;
        Text(body,"Base name",0,0,150,38,20,Gold);
        var name=Input(body,home?ContainerRegistry.GetSettings(home).BaseName:"",155,0,365,null,"Optional base name",false,false);
        name.characterLimit=40;name.interactable=home;
        void SaveName()
        {
            if(!source.RenameBase(name.text,message=>{notice=message=="Saved"?"Base name saved":message;if(IsOpen)Build();}))
            {notice="Base unavailable or another operation is pending";if(status)status.text=notice;}
        }
        name.onSubmit.AddListener(_=>SaveName());
        var save=Button(body,"Save",532,0,140,SaveName);save.interactable=home;
        Text(body,"Local Deposit Chest · connection range "+Plugin.Range.Value.ToString("0")+" m",0,-49,672,30,18,Gold);
        Text(body,"Select the chest this mailbox belongs to. Names are optional.",0,-79,672,25,16,Muted);
        var homes=source.NearbyHomes.ToList();
        int pages=Math.Max(1,(homes.Count+2)/3);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<3&&page*3+i<homes.Count;i++)
        {
            var candidate=homes[page*3+i];float y=-115-i*52;
            var label=Text(body,PostalMailbox.Name(candidate)+" · "+Vector3.Distance(source.transform.position,candidate.transform.position).ToString("0")+" m",0,y,486,38,17,Color.white);
            label.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            var select=Button(body,candidate==home?"Selected":"Select",506,y,166,()=>{
                notice=source.SelectHome(candidate)?"Mailbox connected":"Could not connect: mailbox busy or unavailable";Build();
            });select.interactable=candidate!=home;
        }
        if(homes.Count==0)Text(body,"No accessible Deposit Chest within connection range.",0,-128,672,65,20,Gold);
        Button(body,"Show connection range",0,-285,328,()=>RangeVisual.Show(source.transform.position,Plugin.Range.Value));
        Button(body,"Refresh",344,-285,328,()=>{notice="";Build();});
        Button(body,"Previous",0,-340,180,()=>{page=Math.Max(0,page-1);Build();});
        Text(body,(page+1)+" / "+pages,220,-340,220,38,18,Muted);
        Button(body,"Next",492,-340,180,()=>{page=Math.Min(pages-1,page+1);Build();});
    }
}
