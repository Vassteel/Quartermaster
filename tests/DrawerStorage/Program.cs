using System.Reflection;
using Quartermaster;
using Quartermaster.Cosmetics;
using UnityEngine;
int checks=0;void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args);
var network=new GameObject().AddComponent<DrawerNetwork>();Call(network,"Awake");Call(network,"Update");
var root=new GameObject();var view=root.AddComponent<ZNetView>();view.Data=ZDOMan.instance.CreateNewZDO(new(),1);view.Data.Owner=10;view.Data.SetPrefab("Quartermaster_DrawerCabinet0".GetStableHashCode());ZNetScene.instance.Loaded[view.Data.m_uid]=root;root.AddComponent<Piece>();var wear=root.AddComponent<WearNTear>();var cabinet=root.AddComponent<DrawerCabinet>();Call(cabinet,"Awake");
var ghost=new GameObject();var gv=ghost.AddComponent<ZNetView>();gv.Data=view.Data;gv.m_ghost=true;var gc=ghost.AddComponent<DrawerCabinet>();Call(gc,"Awake");Call(gc,"EnsureDrawers");Check(ZDOMan.instance.All.Count==1,"placement preview never creates saved drawers");
view.Peer=11;Call(cabinet,"EnsureDrawers");Check(ZDOMan.instance.All.Count==1,"nonowner cannot create inventories");
view.Peer=10;Call(cabinet,"EnsureDrawers");Check(ZDOMan.instance.All.Count==13,"one saved container per drawer");
var ids=Enumerable.Range(0,12).Select(i=>view.Data.GetZDOID(DrawerLayout.Key(i))).ToArray();Check(ids.Distinct().Count()==12,"independent persistent identities");
for(int i=0;i<12;i++){var d=ZDOMan.instance.GetZDO(ids[i]);Check(d.GetPrefab()==DrawerFurniture.DrawerPrefab.GetStableHashCode(),"native CreateNewZDO requires explicit SetPrefab");Check(d.Persistent&&d.GetZDOID(DrawerFurniture.ParentKey)==view.Data.m_uid&&d.GetInt(DrawerFurniture.IndexKey,-1)==i,"correct durable parent/index");d.Set("items","contents"+i);}
Call(cabinet,"EnsureDrawers");Check(ZDOMan.instance.All.Count==13,"repeated load does not duplicate drawers");
ZNet.instance.Server=false;var saved=ZDOMan.instance.All[ids[0]];ZDOMan.instance.All.Remove(ids[0]);Call(cabinet,"EnsureDrawers");Check(view.Data.GetZDOID(DrawerLayout.Key(0))==ids[0]&&ZDOMan.instance.All.Count==12,"missing streamed child is not recreated");ZDOMan.instance.All[ids[0]]=saved;
ZNet.instance.Server=true;
var bad=ids[1];ZDOMan.instance.All.Remove(bad);
Call(network,"CheckMissingRequest",999L,view.Data.m_uid,1,bad);Check(view.Data.GetZDOID(DrawerLayout.Key(1))==bad,"nonowner cannot repair a drawer");
Call(network,"CheckMissingRequest",10L,view.Data.m_uid,1,bad);var repaired=view.Data.GetZDOID(DrawerLayout.Key(1));Check(repaired!=bad&&ZDOMan.instance.GetZDO(repaired).GetPrefab()==DrawerFurniture.DrawerPrefab.GetStableHashCode(),"server-confirmed deleted drawer is recreated with valid prefab");
Call(network,"CheckMissingRequest",10L,view.Data.m_uid,1,bad);Check(view.Data.GetZDOID(DrawerLayout.Key(1))==repaired,"stale reply cannot overwrite new link");
ZDOMan.instance.GetZDO(repaired).Set("items","contents1");ids[1]=repaired;
Call(cabinet,"RepairMissing",0,ids[0]);Check(view.Data.GetZDOID(DrawerLayout.Key(0))==ids[0],"existing inventory never replaced by repair");
view.Data.Owner=11;view.Peer=11;Call(cabinet,"EnsureDrawers");Check(ids.All(id=>ZDOMan.instance.GetZDO(id).Data.ContainsKey("items")),"ownership handoff keeps drawer contents and identities");
var shape=ModularCabinetPlacement.DrawerScale;Check(shape.x==2&&shape.y==1.5f&&shape.z==2.5f,"drawer width preserved, height reduced 25 percent, depth increased 25 percent");
for(int i=0;i<12;i++){var at=ModularCabinetPlacement.DrawerPosition(i);Check(at.y>0&&at.y<1.5&&Math.Abs(at.x)<1&&at.z>.5&&at.z<.625,"all drawer click surfaces match resized fronts");}
var child=new GameObject();var cv=child.AddComponent<ZNetView>();cv.Data=saved;var chest=child.AddComponent<Container>();chest.Items=17;var storage=child.AddComponent<DrawerStorage>();Call(storage,"Awake");
Call(storage,"RemoveRequested",999L);Check(chest.Drops==0,"unrelated peer cannot drop drawer contents");
// Reproduce the reported race: cabinet exists only in the client's stale cache.
ZNet.instance.Server=false;Time.time=0;Call(storage,"CheckParent");
int before=ZRoutedRpc.instance.Requests;Time.time=15;Call(storage,"CheckParent");
Check(ZDOMan.instance.GetZDO(view.Data.m_uid)!=null&&ZRoutedRpc.instance.Requests==before+1&&ZRoutedRpc.instance.LastMethod=="QM_CheckDrawerParent","cached parent cannot suppress server validation");
Call(storage,"CheckParent");Check(ZRoutedRpc.instance.Requests==before+1,"parent checks are throttled");
// The server has the full database, including parents with no loaded scene object.
ZNet.instance.Server=true;ZNetScene.instance.Loaded.Remove(view.Data.m_uid);
before=ZRoutedRpc.instance.Requests;Call(network,"Check",cv.Data.Owner,cv.Data.m_uid);
Check(ZRoutedRpc.instance.Requests==before&&chest.Drops==0,"unloaded existing cabinet does not orphan drawers");
ZDOMan.instance.All.Remove(view.Data.m_uid);
Call(network,"Check",999L,cv.Data.m_uid);Check(ZRoutedRpc.instance.Requests==before,"nonowner cannot request orphan deletion");
Call(network,"Check",cv.Data.Owner,cv.Data.m_uid);Check(ZRoutedRpc.instance.Requests==before+1&&ZRoutedRpc.instance.LastMethod=="QM_DrawerParentGone","server confirms deleted parent to drawer owner");
// A client may remove only after the real server confirms; cached parent is irrelevant.
ZNetScene.instance.Loaded[cv.Data.m_uid]=child;ZDOMan.instance.All[view.Data.m_uid]=view.Data;
ZNet.instance.Server=false;Call(network,"Reply",999L,cv.Data.m_uid);Check(chest.Drops==0,"untrusted deletion reply ignored");
cv.Data.Owner=99;Call(network,"Reply",10L,cv.Data.m_uid);Check(chest.Drops==0,"stale reply after ownership handoff cannot delete");
cv.Data.Owner=cv.Peer;Call(network,"Reply",10L,cv.Data.m_uid);
Check(chest.Drops==17&&ZNetScene.instance.Destroyed.Count==1,"confirmed phantom drawer drops contents despite cached parent");
Call(network,"Reply",10L,cv.Data.m_uid);Check(chest.Drops==17,"repeated reply does not duplicate recovery drops");
// Dedicated/server-owned orphan recovery needs no client RPC or parent scene instance.
var orphan=new GameObject();var ov=orphan.AddComponent<ZNetView>();ov.Data=ZDOMan.instance.CreateNewZDO(new(),1);ov.Data.Set(DrawerFurniture.ParentKey,view.Data.m_uid);ov.Data.Set(DrawerFurniture.IndexKey,0);ov.Data.Owner=99;
var oc=orphan.AddComponent<Container>();oc.Items=23;var os=orphan.AddComponent<DrawerStorage>();Call(os,"Awake");
ZDOMan.instance.All.Remove(view.Data.m_uid);ZNet.instance.Server=true;Time.time=40;Call(os,"CheckParent");Check(oc.Drops==0,"server does not steal remote-owned orphan");
ov.Data.Owner=ov.Peer;Call(os,"CheckParent");Check(oc.Drops==23,"server-owned orphan contents recovered");Call(os,"CheckParent");Check(oc.Drops==23,"server cleanup is idempotent");
Check(DrawerLayout.Columns*DrawerLayout.Rows==24&&DrawerLayout.Count==12,"twelve independent 24-slot inventories");
for(int i=0;i<3;i++){using var stream=File.OpenRead($"assets/drawer-cabinets/{i}.bin");var model=OwlModelData.Read(stream,true,5);Check(model.Levels[0].Length==5,"five shared native and solid material surfaces");foreach(var part in model.Levels[0])for(int k=0;k<part.Vertices.Length;k+=8)Check(Math.Abs(part.Vertices[k])<=.505&&part.Vertices[k+1]>=-.01&&part.Vertices[k+1]<=1.01&&Math.Abs(part.Vertices[k+2])<=.26,"one metre cabinet envelope");}
Console.WriteLine($"PASS {checks} drawer ownership, persistence links, destruction recovery and geometry checks");
