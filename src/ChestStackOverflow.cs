using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Quartermaster;

internal static class ChestStackOverflow
{
    private static int cursor;
    internal static void Tick()
    {
        var chests=ContainerRegistry.All.Where(c=>c).ToArray();if(chests.Length==0)return;
        int budget=8;
        for(int i=0;i<Math.Min(chests.Length,Plugin.Budget.Value)&&budget>0;i++)
        {
            cursor%=chests.Length;var chest=chests[cursor++];
            // Never steal network ownership or alter an open chest.
            if(PostalParcel.IsParcel(chest)||!ContainerRegistry.IsUsable(chest,false))continue;
            var inventory=chest.GetInventory();
            if(!inventory.GetAllItems().Any(item=>item?.m_shared!=null&&item.m_stack>Math.Max(1,item.m_shared.m_maxStackSize)))continue;
            InventoryTransfers.SplitExcess(inventory);
            if(!inventory.GetAllItems().Any(item=>item?.m_shared!=null&&item.m_stack>Math.Max(1,item.m_shared.m_maxStackSize)))continue;
            if(!Front(chest,out var point))
            {Automation.SetStatus(chest,"Stack overflow waiting for clear space in front of chest");continue;}
            int count=InventoryTransfers.EjectExcess(inventory,Math.Min(4,budget),item=>
                ContainerRegistry.IsUsable(chest,false)&&Spawn(item,point,-chest.transform.forward));
            budget-=count;
            if(count>0){ChestVisual.Pulse(chest);DepositGull.Notice(chest,"Excess items dropped in front of chest",false);}
        }
    }
    private static bool Front(Container chest,out Vector3 point)
    {
        // Vanilla chest fronts face local -Z. Measure the real solid body so
        // reinforced/black-metal chests do not spawn drops inside their model.
        var bounds=new Bounds(new Vector3(0,.4f,0),new Vector3(1,.8f,1));bool found=false;
        foreach(var collider in chest.GetComponentsInChildren<Collider>())
        {
            if(!collider.enabled||collider.isTrigger)continue;
            var b=collider.bounds;
            for(int i=0;i<8;i++)
            {
                var world=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var local=chest.transform.InverseTransformPoint(world);
                if(!found){bounds=new Bounds(local,Vector3.zero);found=true;}else bounds.Encapsulate(local);
            }
        }
        for(int step=0;step<5;step++)
        {
            point=chest.transform.TransformPoint(new Vector3(bounds.center.x,Mathf.Max(.4f,bounds.center.y)+step*.35f,bounds.min.z-.35f));
            var start=chest.transform.TransformPoint(new Vector3(bounds.center.x,Mathf.Max(.4f,bounds.center.y),bounds.center.z));
            var direction=point-start;
            if(!Physics.CheckSphere(point,.23f,OwlNavigation.Mask,QueryTriggerInteraction.Ignore)&&
                Physics.SphereCastAll(start,.18f,direction.normalized,direction.magnitude,OwlNavigation.Mask,QueryTriggerInteraction.Ignore)
                    .All(hit=>hit.collider&&hit.collider.GetComponentInParent<Container>()==chest))return true;
        }
        point=default;return false;
    }
    private static bool Spawn(ItemDrop.ItemData item,Vector3 position,Vector3 forward)
    {
        if(!item.m_dropPrefab||!item.m_dropPrefab.GetComponent<ItemDrop>()||!item.m_dropPrefab.GetComponent<ZNetView>())return false;
        GameObject spawned=null;
        try
        {
            // Retain the handle before any item callback so a failed initialization
            // can remove its world object before the unchanged chest retries.
            spawned=Object.Instantiate(item.m_dropPrefab,position,Quaternion.identity);
            var drop=spawned.GetComponent<ItemDrop>();var view=spawned.GetComponent<ZNetView>();
            if(!view||!view.IsValid()||!view.IsOwner())throw new InvalidOperationException("Overflow drop is not locally owned");
            drop.m_itemData=item.Clone();
            if(item.m_quality>1)drop.SetQuality(item.m_quality);
            drop.m_onDrop?.Invoke(drop);
            ItemDrop.SaveToZDO(drop.m_itemData,view.GetZDO(),-1);
            var body=spawned.GetComponent<Rigidbody>();if(body)body.linearVelocity=forward*.8f+Vector3.up*1.2f;
            OwlCleanup.Track(drop);
            return true;
        }
        catch(Exception error)
        {
            if(spawned)
            {
                var view=spawned.GetComponent<ZNetView>();
                if(view&&view.IsValid()&&view.IsOwner())view.Destroy();else Object.Destroy(spawned);
            }
            Plugin.Log.LogWarning("Could not eject excess chest stack; contents retained: "+error.Message);
            return false;
        }
    }
}
