using UnityEngine;
namespace Quartermaster;
// Only the server can distinguish a destroyed cabinet from an unloaded one.
internal sealed class DrawerNetwork:MonoBehaviour
{
    private ZRoutedRpc registered;
    private static DrawerNetwork instance;
    private void Awake()=>instance=this;
    private void Update()
    {
        if(ZRoutedRpc.instance==null||registered==ZRoutedRpc.instance)return;
        registered=ZRoutedRpc.instance;
        registered.Register<ZDOID>("QM_CheckDrawerParent",Check);
        registered.Register<ZDOID>("QM_DrawerParentGone",Reply);
        registered.Register<ZDOID,int,ZDOID>("QM_CheckMissingDrawer",CheckMissingRequest);
        registered.Register<ZDOID,int,ZDOID>("QM_RepairMissingDrawer",RepairReply);
    }
    internal static void CheckOrphan(ZDOID id)
    {
        var peer=ZNet.instance.GetServerPeer();
        if(peer!=null&&peer.IsReady())ZRoutedRpc.instance?.InvokeRoutedRPC(peer.m_uid,"QM_CheckDrawerParent",id);
    }
    internal static void CheckMissing(ZDOID cabinet,int index,ZDOID child)
    {
        if(!instance||!ZNet.instance||ZRoutedRpc.instance==null)return;
        if(ZNet.instance.IsServer())instance.CheckMissingRequest(ZDOMan.GetSessionID(),cabinet,index,child);
        else
        {
            var peer=ZNet.instance.GetServerPeer();
            if(peer!=null&&peer.IsReady())ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid,"QM_CheckMissingDrawer",cabinet,index,child);
        }
    }
    private void CheckMissingRequest(long sender,ZDOID id,int index,ZDOID expected)
    {
        if(!ZNet.instance.IsServer()||index<0||index>=DrawerLayout.Count||expected==ZDOID.None)return;
        var parent=ZDOMan.instance.GetZDO(id);
        if(parent==null||parent.GetOwner()!=sender||parent.GetZDOID(DrawerLayout.Key(index))!=expected||ZDOMan.instance.GetZDO(expected)!=null)return;
        bool cabinet=false;for(int i=0;i<3;i++)cabinet|=parent.GetPrefab()==("Quartermaster_DrawerCabinet"+i).GetStableHashCode();
        if(!cabinet)return;
        if(sender==ZDOMan.GetSessionID())ApplyRepair(id,index,expected);
        else registered.InvokeRoutedRPC(sender,"QM_RepairMissingDrawer",id,index,expected);
    }
    private void RepairReply(long sender,ZDOID id,int index,ZDOID expected)
    {
        if(!ZNet.instance.IsServer()&&ZNet.instance.GetServerPeer()?.m_uid==sender)ApplyRepair(id,index,expected);
    }
    private static void ApplyRepair(ZDOID id,int index,ZDOID expected)
    {
        var go=ZNetScene.instance?ZNetScene.instance.FindInstance(id):null;
        if(go)go.GetComponent<DrawerCabinet>()?.RepairMissing(index,expected);
    }
    private void Check(long sender,ZDOID id)
    {
        if(!ZNet.instance.IsServer())return;
        var child=ZDOMan.instance.GetZDO(id);
        if(child==null||child.GetPrefab()!=DrawerFurniture.DrawerPrefab.GetStableHashCode()||child.GetOwner()!=sender)return;
        var parent=child.GetZDOID(DrawerFurniture.ParentKey);
        if(parent!=ZDOID.None&&ZDOMan.instance.GetZDO(parent)==null)
            registered.InvokeRoutedRPC(sender,"QM_DrawerParentGone",id);
    }
    private void Reply(long sender,ZDOID id)
    {
        if(ZNet.instance.IsServer()||ZNet.instance.GetServerPeer()?.m_uid!=sender)return;
        var go=ZNetScene.instance.FindInstance(id);
        if(go&&go.GetComponent<ZNetView>().IsOwner())go.GetComponent<DrawerStorage>()?.Remove();
    }
}
