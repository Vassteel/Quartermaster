using System;
using System.Linq;
using UnityEngine;
using TMPro;

namespace Quartermaster;

// Bounded display sockets; polling is staggered and distant interiors are hidden.
// Cosmetic reports are derived from native inventory snapshots, including remote reloads.
internal sealed class ApothecaryDisplay : MonoBehaviour
{
    private Container chest;
    private ApothecaryDefinition definition;
    private Transform[] jars,lids;
    private Transform coverHinge;
    private Transform[] doorHinges;
    private FurnitureDoors doors;
    private bool inventoryOpen;
    private int coverSounds;
    private Vector3[] positions;
    private TextMeshPro[] labels;
    private GameObject[] fills;
    private MeshRenderer[] loads;
    private NativeHangingItem[] hanging;
    private int[] variants;
    private MaterialPropertyBlock properties;
    private string[] ids,assigned;
    private int[] counts,limits;
    private bool initialized,visible;
    private OwlWork pending;
    private float expires,animationUntil,nextSound;
    private int activeSlot=-1;
    private void Start()
    {
        chest=GetComponent<Container>();definition=Apothecary.Definition(ContainerRegistry.PrefabName(chest));
        if(definition==null||!chest){enabled=false;return;}
        positions=ApothecaryArt.Slots(definition.Model);int n=positions.Length;
        jars=new Transform[n];lids=new Transform[n];labels=new TextMeshPro[n];fills=new GameObject[n];ids=new string[n];counts=new int[n];
        hanging=new NativeHangingItem[n];variants=new int[n];
        loads=new MeshRenderer[n];assigned=new string[n];limits=new int[n];properties=new MaterialPropertyBlock();
        var art=transform.Find("Quartermaster furniture");
        var cover=art.Find("lid");
        if(cover&&definition.LidHeight>0)
        {
            coverHinge=new GameObject("Furniture cover hinge").transform;coverHinge.SetParent(art,false);
            coverHinge.localPosition=new Vector3(0,definition.LidHeight,definition.LidBack);
            cover.SetParent(coverHinge,false);cover.localPosition=-coverHinge.localPosition;
            if(definition.PlayerLid)doors=new FurnitureDoors();
        }
        if(definition.Doors>0)
        {
            doors=new FurnitureDoors();doorHinges=new Transform[definition.Doors];
            for(int i=0;i<doorHinges.Length;i++)
            {
                string prefix=i==0?"door_left":"door_right";
                var hinge=new GameObject("Wardrobe hinge "+i).transform;hinge.SetParent(art,false);
                hinge.localPosition=new Vector3((i==0?-1:1)*definition.DoorHingeX,0,definition.DoorFront);
                foreach(var part in art.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==art&&t.name.StartsWith(prefix,StringComparison.Ordinal)).ToArray())
                {part.SetParent(hinge,false);part.localPosition=-hinge.localPosition;}
                doorHinges[i]=hinge;
            }
        }
        for(int i=0;i<n;i++)
        {
            if(NativeItems)
            {
                var root=new GameObject("Stored hanging item "+i);root.layer=art.gameObject.layer;root.transform.SetParent(art,false);root.transform.localPosition=positions[i];
                jars[i]=root.transform;hanging[i]=FittedNativeItems?new NativeHangingItem(jars[i],new Vector3(definition.DisplayWidth,definition.DisplayHeight,definition.DisplayDepth),definition.HorizontalDisplay,Handling==FurnitureHandling.Wardrobe||FurnitureMotion.IsDisplay(Handling),FurnitureMotion.IsDisplay(Handling)):new NativeHangingItem(jars[i]);root.SetActive(false);continue;
            }
            if(definition.Handling!=FurnitureHandling.Jar)
            {
                var load=ApothecaryArt.Create(art,BulkPresentation.Model(definition.Category,"",1,definition.Model),"load_"+i);
                load.transform.localPosition=positions[i];jars[i]=load.transform;
                loads[i]=load.GetComponentInChildren<MeshRenderer>();load.SetActive(false);continue;
            }
            jars[i]=art.Find("jar_"+i);lids[i]=jars[i].Find("lid");ApothecaryArt.StyleJar(jars[i],definition.Glass,i);
            var label=new GameObject("Stored item label");label.layer=gameObject.layer;label.transform.SetParent(jars[i],false);
            label.transform.localPosition=definition.Glass?new Vector3(0,.21f,.133f):new Vector3(0,.16f,.151f);
            if(definition.Model.StartsWith("modular_",StringComparison.Ordinal))label.transform.localPosition=definition.Glass?new Vector3(0,.10f,.063f):new Vector3(0,.082f,.079f);
            label.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=label.AddComponent<TextMeshPro>();text.alignment=TextAlignmentOptions.Center;
            text.font=Hud.instance?Hud.instance.m_hoverName.font:TMP_Settings.defaultFontAsset;
            text.rectTransform.sizeDelta=new Vector2(.15f,.07f);text.fontSize=.24f;
            text.enableAutoSizing=true;text.fontSizeMin=.10f;text.fontSizeMax=.24f;
            text.color=new Color(.15f,.11f,.065f);text.richText=false;text.raycastTarget=false;labels[i]=text;
            if(definition.Glass){fills[i]=ApothecaryArt.Create(jars[i],"fill","contents");fills[i].SetActive(false);}
        }
        InvokeRepeating(nameof(Poll),.2f+(Math.Abs(GetInstanceID())%11)*.07f,.8f);
    }
    private void Poll()
    {
        if(!chest||chest.GetInventory()==null)return;
        bool near=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-transform.position).sqrMagnitude<625;
        if(near!=visible)
        {
            visible=near;
            for(int i=0;i<jars.Length;i++)jars[i].gameObject.SetActive(near&&InteriorVisible&&(Handling==FurnitureHandling.Jar||counts[i]>0));
            if(!near)ResetInteraction();
        }
        if(!near){initialized=false;pending=null;ResetDoors();return;}
        inventoryOpen=chest.IsInUse();
        var inventory=chest.GetInventory();
        var items=inventory.GetAllItems();
        var meads=Handling==FurnitureHandling.Mead?MeadCabinet.DisplayItems(items):null;
        for(int i=0;i<jars.Length;i++)
        {
            int x=i%definition.Columns,y=i/definition.Columns;
            var item=meads!=null?(i<meads.Length?meads[i]:null):items.FirstOrDefault(v=>v.m_gridPos.x==x&&v.m_gridPos.y==y);
            string id=item==null?"":InventoryTransfers.ItemId(item);int count=item?.m_stack??0;
            int limit=item?.m_shared.m_maxStackSize??1;
            string category=Handling==FurnitureHandling.Jar?"":FurnitureAssignment.Classify(id,definition.Category);
            bool changed=id!=ids[i]||count!=counts[i];
            int variant=item?.m_variant??0;
            if(!changed&&variant==variants[i]&&limit==limits[i]&&category==assigned[i])continue;
            // Coalesce rapid operations into a single representative event per cabinet.
            if(changed&&initialized&&Time.time>animationUntil&&!chest.IsInUse())
            {
                bool retrieval=id==""||(id==ids[i]&&count<counts[i]);
                var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab(retrieval?ids[i]:id):null;
                if(prefab)
                {
                    pending=new OwlWork{Machine=this,Furniture=this,Slot=i,Retrieving=retrieval,Input=prefab.GetComponent<ItemDrop>(),InputPoint=ContactPoint(i,retrieval?counts[i]:count,retrieval?limits[i]:limit)};
                    expires=Time.time+90;
                }
            }
            variants[i]=variant;ids[i]=id;counts[i]=count;limits[i]=limit;assigned[i]=category;
            if(NativeItems)
            {
                bool hasModel=hanging[i].Set(item);jars[i].gameObject.SetActive(count>0&&hasModel&&InteriorVisible);continue;
            }
            if(Handling!=FurnitureHandling.Jar)
            {
                jars[i].gameObject.SetActive(count>0);
                if(count>0)
                {
                    bool matches=category==definition.Category;
                    ApothecaryArt.SetModel(loads[i].gameObject,matches?BulkPresentation.Model(definition.Category,id,BulkPresentation.Level(count,item.m_shared.m_maxStackSize),definition.Model):"bulk_parcel");
                    var tint=matches?BulkPresentation.Tint(id,definition.Category):new StorageTint(1,1,1);
                    properties.SetColor("_Color",new Color(tint.R,tint.G,tint.B));loads[i].SetPropertyBlock(properties);
                }
                continue;
            }
            string name=item==null?"":Automation.Label(item).Replace("\n"," ");
            // Small world labels remain bounded; full localized names stay in inventory/hover UI.
            labels[i].text=name.Length>16?name.Substring(0,15)+"…":name;
            labels[i].transform.localScale=Vector3.one*(definition.Glass?.55f:1f)*(definition.Model.StartsWith("modular_",StringComparison.Ordinal)?.45f:1f);
            if(fills[i])
            {
                fills[i].SetActive(count>0);
                if(count>0)fills[i].transform.localScale=new Vector3(1,.35f+.65f*Mathf.Clamp01(count/(float)Math.Max(1,item.m_shared.m_maxStackSize)),1)*(definition.Model.StartsWith("modular_",StringComparison.Ordinal)?.5f:1f);
            }
        }
        initialized=true;
        if(activeSlot>=0&&(Time.time>animationUntil||chest.IsInUse()))ResetInteraction();
    }
    private bool FittedNativeItems=>FurnitureMotion.IsRack(Handling)||Handling==FurnitureHandling.Wardrobe||FurnitureMotion.IsDisplay(Handling);
    private bool NativeItems=>Handling==FurnitureHandling.Hanging||FittedNativeItems;
    private bool InteriorVisible=>doors==null||doors.Amount>.12f;
    private void Update()
    {
        if(doors==null||!visible)return;
        float old=doors.Amount;bool wasVisible=InteriorVisible;
        int sound=doors.Step(Time.deltaTime,inventoryOpen);
        if(old==doors.Amount)return;
        if(doorHinges!=null)for(int i=0;i<doorHinges.Length;i++)doorHinges[i].localRotation=Quaternion.Euler(0,(i==0?-1:1)*100*doors.Amount,0);
        if(definition.PlayerLid&&coverHinge)coverHinge.localRotation=Quaternion.Euler(-68*doors.Amount,0,0);
        if(wasVisible!=InteriorVisible)for(int i=0;i<jars.Length;i++)jars[i].gameObject.SetActive(InteriorVisible&&counts[i]>0);
        if(sound!=0)FurnitureAudio.Door(transform.position,sound<0);
    }
    private void ResetDoors()
    {
        doors?.Reset();inventoryOpen=false;
        if(doors!=null&&jars!=null)foreach(var slot in jars)if(slot)slot.gameObject.SetActive(false);
        if(doorHinges!=null)foreach(var hinge in doorHinges)if(hinge)hinge.localRotation=Quaternion.identity;
        if(definition!=null&&definition.PlayerLid&&coverHinge)coverHinge.localRotation=Quaternion.identity;
    }
    internal FurnitureHandling Handling=>definition?.Handling??FurnitureHandling.Jar;
    internal float ContactTime=>FurnitureMotion.Contact(Handling);
    internal float Duration=>FurnitureMotion.Duration(Handling);
    internal float Lean(float time)=>FurnitureMotion.Lean(Handling,time);
    private Vector3 ContactPoint(int slot,int count,int limit)=>Socket(slot)+(Handling==FurnitureHandling.Jar?transform.forward*(definition.DrawDistance+.01f)+Vector3.up*.32f:Vector3.up*(FittedNativeItems?0:BulkPresentation.ContactHeight(definition.Model,BulkPresentation.Level(count,limit))));
    internal string DisplayName=>definition?.Name??"Apothecary cabinet";
    internal Vector3 Approach(OwlWork work)=>Handling==FurnitureHandling.Jar?Socket(work.Slot)+transform.forward*(definition.DrawDistance+.47f)-Vector3.up*.16f:
        transform.TransformPoint(new Vector3(positions[work.Slot].x,positions[work.Slot].y,definition.Depth*.5f+.48f));
    internal Vector3 Socket(int slot)=>transform.TransformPoint(positions[Mathf.Clamp(slot,0,positions.Length-1)]);
    internal OwlWork Work(Container home)
    {
        if(!home||!Plugin.Enabled.Value||!ContainerRegistry.GetSettings(home).Deposit)return null;
        if(!visible||pending==null||Time.time>expires||!chest||chest.IsInUse()||!ContainerRegistry.Accessible(chest))return null;
        var n=Automation.Network(home);if(n==null||!n.Contains(transform.position)||!Policy.SameGroup(n.Group,ContainerRegistry.GetSettings(chest).Group))return null;
        var nearest=n.Hubs.Where(h=>h&&ContainerRegistry.GetView(h)&&ContainerRegistry.GetView(h).IsValid()).OrderBy(h=>(h.transform.position-transform.position).sqrMagnitude)
            .ThenBy(h=>ContainerRegistry.GetView(h).GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
        return nearest==home?pending:null;
    }
    internal void Animate(OwlWork work,float time)
    {
        if(!chest||chest.IsInUse()||work.Slot<0||work.Slot>=jars.Length){ResetInteraction();return;}
        activeSlot=work.Slot;animationUntil=Time.time+1;
        doors?.Request(FurnitureDoors.OwlOpening(time));
        if(Handling!=FurnitureHandling.Jar)
        {
            if(coverHinge&&!definition.PlayerLid)
            {
                coverHinge.localRotation=Quaternion.Euler(-FurnitureMotion.Cover(time),0,0);
                if(time>=.25f&&time<.65f&&(coverSounds&1)==0){coverSounds|=1;FurnitureAudio.Contact(coverHinge.position,FurnitureHandling.Lumber);}
                if(time>=3.9f&&(coverSounds&2)==0){coverSounds|=2;FurnitureAudio.Contact(coverHinge.position,FurnitureHandling.Lumber);}
            }
            float press=FurnitureMotion.Press(Handling,time);
            if(definition.Model=="hide_rail")jars[activeSlot].localRotation=Quaternion.Euler(0,0,press*28);
            else if(Handling==FurnitureHandling.Hanging)
            {float sway=FurnitureMotion.HangingSway(time);jars[activeSlot].localRotation=Quaternion.Euler(0,0,sway);jars[activeSlot].localPosition=positions[activeSlot]+Vector3.right*(.51f*Mathf.Sin(sway*Mathf.Deg2Rad))+Vector3.up*(.51f*(1-Mathf.Cos(sway*Mathf.Deg2Rad)));}
            else if(FittedNativeItems)
            {
                float lift=FurnitureMotion.IsDisplay(Handling)?FurnitureMotion.DisplayLift(Handling,time):FurnitureMotion.RackLift(Handling==FurnitureHandling.Wardrobe?FurnitureHandling.Weapon:Handling,time);
                jars[activeSlot].localPosition=positions[activeSlot]+Vector3.up*lift+Vector3.forward*(lift*.35f);
                jars[activeSlot].localRotation=Quaternion.Euler(0,0,definition.HorizontalDisplay?0:-lift*25);
            }
            else jars[activeSlot].localScale=new Vector3(1,1-press,1);
            if(time>=ContactTime+.65f&&time<ContactTime+1.0f&&Time.time>nextSound)
            {FurnitureAudio.Contact(Socket(activeSlot),Handling);nextSound=Time.time+2;}
            return;
        }
        var pose=ApothecaryMotion.Sample(time);
        jars[activeSlot].localPosition=positions[activeSlot]+Vector3.forward*(pose.Draw*definition.DrawDistance/.23f);
        lids[activeSlot].localPosition=Vector3.up*pose.Lid;
        lids[activeSlot].localRotation=Quaternion.Euler(0,0,pose.Tilt);
        if((time>.6f&&time<1.1f||time>3.6f&&time<4.1f)&&Time.time>nextSound)
        {FurnitureAudio.Contact(jars[activeSlot].position,definition.Glass);nextSound=Time.time+2;}
    }
    internal void Complete(OwlWork work)
    {if(ReferenceEquals(pending,work))pending=null;ResetInteraction();}
    private void ResetInteraction()
    {
        if(activeSlot>=0&&jars!=null&&jars[activeSlot])
        {jars[activeSlot].localPosition=positions[activeSlot];if(Handling!=FurnitureHandling.Jar){jars[activeSlot].localScale=Vector3.one;jars[activeSlot].localRotation=Quaternion.identity;}if(lids[activeSlot]){lids[activeSlot].localPosition=Vector3.zero;lids[activeSlot].localRotation=Quaternion.identity;}}
        if(coverHinge&&!definition.PlayerLid)coverHinge.localRotation=Quaternion.identity;
        doors?.Request(0);
        activeSlot=-1;animationUntil=0;coverSounds=0;
    }
    private void OnDestroy(){if(hanging!=null)foreach(var item in hanging)item?.Dispose();}
    private void OnDisable(){CancelInvoke();ResetInteraction();ResetDoors();pending=null;}
    private void OnEnable()
    {
        // A pooled/re-enabled furniture instance resumes its existing inventory polling.
        if(jars!=null&&!IsInvoking(nameof(Poll)))InvokeRepeating(nameof(Poll),.2f,.8f);
    }
}
