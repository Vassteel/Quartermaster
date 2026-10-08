using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Quartermaster;

// Each drawer has its own native Container and ZDO: independent inventory,
// settings, locks and transaction identity. The cabinet only owns their links.
internal static class DrawerFurniture
{
    internal const string DrawerPrefab="Quartermaster_apothecary_chest_drawer",ParentKey="Quartermaster.drawerParent",IndexKey="Quartermaster.drawerIndex";
    internal static readonly ApothecaryDefinition Definition=new ApothecaryDefinition{Prefab=DrawerPrefab,Name="Apothecary Drawer",Columns=6,Rows=4,Category="ingredients,reagents"};
    internal static void Initialize()=>PrefabManager.OnVanillaPrefabsAvailable+=Register;
    internal static void Shutdown()=>PrefabManager.OnVanillaPrefabsAvailable-=Register;
    private static void Register()
    {
        try
        {
            var drawer=PrefabManager.Instance.CreateClonedPrefab(DrawerPrefab,"piece_chest_wood");
            foreach(var r in drawer.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
            foreach(var c in drawer.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var l in drawer.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(l);
            Object.DestroyImmediate(drawer.GetComponent<WearNTear>());
            var piece=drawer.GetComponent<Piece>();piece.m_canBeRemoved=false;piece.m_resources=Array.Empty<Piece.Requirement>();
            var ctn=drawer.GetComponent<Container>();ctn.m_width=6;ctn.m_height=4;ctn.m_name="Apothecary Drawer";ctn.m_defaultItems=new DropTable();ctn.m_open=null;ctn.m_closed=null;ctn.m_destroyedLootPrefab=null;
            drawer.AddComponent<DrawerStorage>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(drawer,false));
            if(PrefabManager.Instance.GetPrefab(DrawerPrefab)!=drawer)throw new Exception("Cannot register drawer inventory");
            for(int i=0;i<3;i++)
            {
                // Keep all three saved prefab IDs and their recipes for existing
                // cabinets/refunds; only the first is available for new builds.
                string name="Apothecary Drawer Cabinet";
                var cabinet=PrefabManager.Instance.CreateClonedPrefab("Quartermaster_DrawerCabinet"+i,"piece_chest_wood");
                Object.DestroyImmediate(cabinet.GetComponent<Container>());
                DrawerCabinetArt.Build(cabinet,i);
                cabinet.AddComponent<DrawerCabinet>();
                if(!PieceManager.Instance.AddPiece(new CustomPiece(cabinet,false,new PieceConfig{Enabled=i==0,Name=name,Description="Twelve separate drawers, each with 24 slots. Aim at a drawer to open or configure it.",Icon=ApothecaryArt.Icon("drawers_"+i),PieceTable="Hammer",Category="Quartermaster",CraftingStation="piece_workbench",Requirements=new[]{new RequirementConfig("Wood",20,0,true),new RequirementConfig("FineWood",6,0,true),new RequirementConfig("RoundLog",4,0,true),new RequirementConfig("Bronze",2,0,true)}})))throw new Exception("Cannot register "+name);
                if(i==0)BuildMenuCategory.Register(cabinet.name);
            }
            Shutdown();
        }
        catch(Exception e){Plugin.Log.LogError("Drawer furniture registration: "+e);}
    }
}

internal sealed class DrawerCabinet:MonoBehaviour
{
    private ZNetView view;
    private bool destroyed,preview;
    private static readonly FieldInfo Ghost=AccessTools.Field(typeof(ZNetView),"m_ghost");
    private void Awake()
    {
        view=GetComponent<ZNetView>();if(!view||!view.IsValid())return;
        preview=(bool)Ghost.GetValue(view);if(preview)return;
        var wear=GetComponent<WearNTear>();if(wear)wear.m_onDestroyed+=OnBroken;
        InvokeRepeating(nameof(EnsureDrawers),.3f,2f);
    }
    private void EnsureDrawers()
    {
        if(preview||destroyed||!view||!view.IsValid()||!view.IsOwner())return;
        var parent=view.GetZDO();
        for(int i=0;i<DrawerLayout.Count;i++)
        {
            // A nonempty link is never replaced just because a remote ZDO has
            // not arrived yet. This also avoids recreating emptied/damaged drawers.
            var linked=parent.GetZDOID(DrawerLayout.Key(i));
            if(linked!=ZDOID.None)
            {
                if(ZDOMan.instance.GetZDO(linked)==null)DrawerNetwork.CheckMissing(parent.m_uid,i,linked);
                continue;
            }
            var child=ZDOMan.instance.CreateNewZDO(transform.position,DrawerFurniture.DrawerPrefab.GetStableHashCode());
            child.SetPrefab(DrawerFurniture.DrawerPrefab.GetStableHashCode());
            child.Persistent=true;child.SetRotation(transform.rotation);child.SetOwner(parent.GetOwner());
            child.Set(DrawerFurniture.ParentKey,parent.m_uid);child.Set(DrawerFurniture.IndexKey,i);
            var piece=GetComponent<Piece>();if(piece)child.Set(ZDOVars.s_creator,piece.GetCreator());
            parent.Set(DrawerLayout.Key(i),child.m_uid);
            // ZNetScene creates these saved objects normally, including on remote
            // peers and dedicated servers. No nested ZNetView initialization.
        }
    }
    internal void RepairMissing(int index,ZDOID expected)
    {
        if(preview||destroyed||!view||!view.IsValid()||!view.IsOwner()||index<0||index>=DrawerLayout.Count)return;
        var parent=view.GetZDO();if(parent.GetZDOID(DrawerLayout.Key(index))!=expected)return;
        if(ZDOMan.instance.GetZDO(expected)!=null)return;
        parent.Set(DrawerLayout.Key(index),ZDOID.None);EnsureDrawers();
    }
    private void OnBroken()
    {
        destroyed=true;if(!view||!view.IsValid()||!view.IsOwner())return;
        for(int i=0;i<DrawerLayout.Count;i++)
        {
            var go=ZNetScene.instance.FindInstance(view.GetZDO().GetZDOID(DrawerLayout.Key(i)));
            if(go)go.GetComponent<ZNetView>().InvokeRPC("QM_RemoveDrawer");
        }
    }
}

internal sealed class DrawerStorage:MonoBehaviour
{
    private ZNetView view;
    private Container chest;
    private float missingSince=-1;
    private bool removing;
    private static readonly MethodInfo Drop=AccessTools.Method(typeof(Container),"DropAllItems",Type.EmptyTypes);
    private void Awake()
    {
        view=GetComponent<ZNetView>();chest=GetComponent<Container>();
        if(!view||!view.IsValid())return;
        int index=view.GetZDO().GetInt(DrawerFurniture.IndexKey,-1);
        if(index<0||index>=DrawerLayout.Count)return;
        // Saved indices start at the bottom-right when facing the cabinet.
        // Reverse only the displayed number; inventory identities stay unchanged.
        chest.m_name="Apothecary Drawer "+(DrawerLayout.Count-index);
        var hit=gameObject.AddComponent<BoxCollider>();hit.center=DrawerCabinetArt.Position(index);hit.size=Vector3.Scale(new Vector3(.203f,.260f,.045f),ModularCabinetPlacement.DrawerScale);
        view.Register("QM_RemoveDrawer",RemoveRequested);
        InvokeRepeating(nameof(CheckParent),2f,3f);
    }
    private void RemoveRequested(long sender)
    {
        var parent=ZDOMan.instance.GetZDO(view.GetZDO().GetZDOID(DrawerFurniture.ParentKey));
        // Only the cabinet owner may request removal. A missing parent is handled
        // by the server, which has the complete world database.
        if(parent!=null&&parent.GetOwner()==sender&&view.IsOwner())Remove();
    }
    private void CheckParent()
    {
        if(removing||!view||!view.IsValid()||!ZNet.instance)return;
        var id=view.GetZDO().GetZDOID(DrawerFurniture.ParentKey);
        if(id==ZDOID.None)return;
        if(ZDOMan.instance.GetZDO(id)!=null){missingSince=-1;return;}
        if(missingSince<0){missingSince=Time.time;return;}
        if(Time.time-missingSince<12)return;
        // Also recovers destruction while drawers were unloaded or during a
        // disconnect. Clients ask the server before treating streamed data as deleted.
        if(ZNet.instance.IsServer()){if(view.IsOwner())Remove();}
        else if(view.IsOwner())DrawerNetwork.CheckOrphan(view.GetZDO().m_uid);
    }
    internal void Remove()
    {
        if(removing||!view.IsOwner())return;
        removing=true;ContainerRegistry.Refresh(chest);
        Drop.Invoke(chest,null);ZNetScene.instance.Destroy(gameObject);
    }
}
