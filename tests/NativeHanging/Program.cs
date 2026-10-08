using Quartermaster;
using UnityEngine;
int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
GameObject Child(GameObject parent,string name){var g=new GameObject(name);g.transform.SetParent(parent.transform,false);return g;}
MeshRenderer Model(GameObject parent,string name,Vector3 size){var g=Child(parent,name);g.AddComponent<MeshFilter>().sharedMesh=new Mesh{bounds=new Bounds(Vector3.zero,size)};return g.AddComponent<MeshRenderer>();}
var fish=new GameObject("Fish1");fish.SetActive(false);fish.AddComponent<Fish>();fish.AddComponent<ZNetView>();fish.AddComponent<ItemDrop>();
var attach=Child(fish,"attach");attach.transform.localPosition=new Vector3(3,2,-1);attach.transform.localScale=new Vector3(2,1,1);
var model=Model(attach,"fish body",new Vector3(2,.3f,.2f));model.Props.SetInt("native",7);
var lod=Model(attach,"lod1",new Vector3(10,10,10));var group=attach.AddComponent<LODGroup>();group.levels=new[]{new LOD{renderers=new Renderer[]{model}},new LOD{renderers=new Renderer[]{lod}}};
var hidden=Model(attach,"inactive",Vector3.one);hidden.gameObject.SetActive(false);
Model(fish,"world only",Vector3.one);
var parent=new GameObject("Hook");using var hook=new NativeHangingItem(parent.transform);var item=new ItemDrop.ItemData{m_dropPrefab=fish,m_stack=8,m_variant=2};
Check(hook.Set(item),"inactive native prefab can render its attach");
var displays=parent.GetComponentsInChildren<MeshRenderer>(true);Check(displays.Length==1,"only attach LOD0 and active branch copied");
var display=displays.Single();var nativeMesh=model.GetComponent<MeshFilter>().sharedMesh;
Check(display.GetComponent<MeshFilter>().sharedMesh==nativeMesh&&display.sharedMaterials[0]==model.sharedMaterials[0],"native meshes and materials borrowed unchanged");
Check(display.Props.Values["native"]==7&&display.Props.Values["_Style"]==2&&!model.Props.Values.ContainsKey("_Style"),"variant changes local block only");
Check(parent.GetComponentsInChildren<Fish>(true).Length==0&&parent.GetComponentsInChildren<ZNetView>(true).Length==0&&parent.GetComponentsInChildren<ItemDrop>(true).Length==0,"display contains no gameplay components");
var points=NativeHangingItem.Corners(nativeMesh.bounds).Select(p=>display.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
Check(Math.Abs(points.Max(p=>p.y)-.505f)<.0001f,"native transform offsets normalized to hook contact");
Check(points.Max(p=>p.y)-points.Min(p=>p.y)<=.4201f&&points.Max(p=>p.x)-points.Min(p=>p.x)<=.3201f&&points.Max(p=>p.z)-points.Min(p=>p.z)<=.2401f,"native model fits hook bounds");
int created=GameObject.Created;item.m_stack=80;Check(hook.Set(item)&&GameObject.Created==created,"quantity update does not recreate display");
item.m_variant=3;Check(hook.Set(item)&&display.Props.Values["_Style"]==3&&GameObject.Created==created,"variant update reuses model");
var meat=new GameObject("DeerMeat");var meatRenderer=Model(meat,"meat",new Vector3(.3f,1,.3f));item.m_dropPrefab=meat;
Check(hook.Set(item)&&display.gameObject.destroyed,"changing item type replaces previous model");Check(!nativeMesh.destroyed&&!model.sharedMaterials[0].destroyed,"borrowed native assets survive type change");
Check(parent.GetComponentsInChildren<MeshRenderer>(true).Single().GetComponent<MeshFilter>().sharedMesh==meatRenderer.GetComponent<MeshFilter>().sharedMesh,"fallback prefab without attach uses actual item model");
item.m_stack=0;Check(!hook.Set(item)&&parent.GetComponentsInChildren<MeshRenderer>(true).Length==0,"empty inventory clears model");
var skinPrefab=new GameObject("SkinnedFish");var skin=Child(skinPrefab,"attach").AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=new Mesh();item=new(){m_dropPrefab=skinPrefab};
var parent2=new GameObject("Second hook");using var hook2=new NativeHangingItem(parent2.transform);
Check(hook.Set(item)&&hook2.Set(item)&&SkinnedMeshRenderer.Bakes==1,"two hooks share one baked pose");var baked=SkinnedMeshRenderer.LastBake;
hook.Dispose();Check(!baked.destroyed,"shared pose remains until last hook releases it");hook2.Dispose();Check(baked.destroyed&&!skin.sharedMesh.destroyed,"last hook frees only owned baked pose");
var oversized=new GameObject("TooManyParts");for(int i=0;i<9;i++)Model(oversized,"part"+i,Vector3.one);
created=GameObject.Created;Check(!hook.Set(new(){m_dropPrefab=oversized})&&GameObject.Created==created,"oversized model rejected without partial display");
var invalid=new GameObject("BadSkin");var bad=Child(invalid,"attach").AddComponent<SkinnedMeshRenderer>();bad.sharedMesh=new Mesh{bounds=new Bounds(new Vector3(float.NaN,0,0),Vector3.one)};
Check(!hook.Set(new(){m_dropPrefab=invalid})&&SkinnedMeshRenderer.LastBake.destroyed,"failed baked bounds free temporary mesh");
var empty=new GameObject("NoVisual");Check(!hook.Set(new(){m_dropPrefab=empty}),"missing item model is empty, never generic wrong meat");int warnings=Plugin.Log.Warnings.Count;
hook.Set(new(){m_dropPrefab=empty});Check(Plugin.Log.Warnings.Count==warnings,"invalid prefab warns once");
NativeHangingItem.ClearWarnings();hook.Set(new(){m_dropPrefab=empty});Check(Plugin.Log.Warnings.Count==warnings+1,"world unload resets warning state");
Check(!hook.Set(null),"null item clears safely");hook.Dispose();hook.Dispose();
var rackParent=new GameObject("Gear rack");
using(var rack=new NativeHangingItem(rackParent.transform,new Vector3(1.3f,.6f,.22f),true))
{
 var gear=new GameObject("Crossbow");var sourceGear=Model(gear,"attach",new Vector3(.7f,2,.08f));
 Check(rack.Set(new(){m_dropPrefab=gear}),"rack reuses native item extraction");
 var gearModel=rackParent.GetComponentsInChildren<MeshRenderer>(true).Single();
 var pts=NativeHangingItem.Corners(sourceGear.GetComponent<MeshFilter>().sharedMesh.bounds).Select(p=>gearModel.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
 Check(Math.Abs(pts.Max(p=>p.x)-.65f)<.0001f&&Math.Abs(pts.Min(p=>p.x)+.65f)<.0001f,"horizontal rack places longest axis across the rack");
 Check(pts.Max(p=>p.y)<=.3001f&&pts.Min(p=>p.y)>=-.3001f&&pts.Max(p=>p.z)<=.1101f&&pts.Min(p=>p.z)>=-.1101f,"rack model centered within authored display limits");
}
using(var rack=new NativeHangingItem(rackParent.transform,new Vector3(.85f,.94f,.22f),false))
{
 var gear=new GameObject("SidewaysShield");var sourceGear=Model(gear,"attach",new Vector3(.06f,1,1));
 Check(rack.Set(new(){m_dropPrefab=gear}),"side-facing native shield accepted");
 var gearModel=rackParent.GetComponentsInChildren<MeshRenderer>(true).Single();
 var pts=NativeHangingItem.Corners(sourceGear.GetComponent<MeshFilter>().sharedMesh.bounds).Select(p=>gearModel.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
 Check(pts.Max(p=>p.x)-pts.Min(p=>p.x)>.8f&&pts.Max(p=>p.z)-pts.Min(p=>p.z)<.06f,"shield broad face presented outward without flattening native proportions");
}
using(var wardrobe=new NativeHangingItem(rackParent.transform,new Vector3(.74f,.49f,.38f),false,true))
{
 var helmet=new GameObject("WideHelmet");var helmetSource=Model(helmet,"attach",new Vector3(1,.4f,.4f));
 Check(wardrobe.Set(new(){m_dropPrefab=helmet}),"wardrobe uses existing native display extraction");
 var displayed=rackParent.GetComponentsInChildren<MeshRenderer>(true).Single();
 var pts=NativeHangingItem.Corners(helmetSource.GetComponent<MeshFilter>().sharedMesh.bounds).Select(p=>displayed.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
 Check(pts.Max(p=>p.x)-pts.Min(p=>p.x)>.70f&&pts.Max(p=>p.y)-pts.Min(p=>p.y)<.31f,"wide helmets retain upright native orientation instead of becoming vertical weapons");
}
// A broad low trophy or gem must rest on its shelf, not float at the fitting-box center.
using(var shelf=new NativeHangingItem(rackParent.transform,new Vector3(.66f,.66f,.39f),false,true,true))
{
 var trophy=new GameObject("WideFlatTrophy");var source=Model(trophy,"attach",new Vector3(2,.2f,.4f));
 Check(shelf.Set(new(){m_dropPrefab=trophy}),"display shelf borrows the stored native item");
 var shown=rackParent.GetComponentsInChildren<MeshRenderer>(true).Single();
 var pts=NativeHangingItem.Corners(source.GetComponent<MeshFilter>().sharedMesh.bounds).Select(p=>shown.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
 Check(Math.Abs(pts.Min(p=>p.y)+.33f)<.0001f,"wide short item rests exactly on envelope base");
 Check(pts.Max(p=>p.y)-pts.Min(p=>p.y)<.07f&&pts.Max(p=>p.x)-pts.Min(p=>p.x)>.65f,"resting item preserves upright shape and proportions");
 var tall=new GameObject("TallCrystal");var tallSource=Model(tall,"attach",new Vector3(.2f,2,.2f));
 Check(shelf.Set(new(){m_dropPrefab=tall}),"swapping to taller item reuses normal lifecycle");
 shown=rackParent.GetComponentsInChildren<MeshRenderer>(true).Single();
 pts=NativeHangingItem.Corners(tallSource.GetComponent<MeshFilter>().sharedMesh.bounds).Select(p=>shown.transform.localToWorldMatrix.MultiplyPoint3x4(p)).ToArray();
 Check(Math.Abs(pts.Min(p=>p.y)+.33f)<.0001f&&pts.Max(p=>p.y)<=.3301f,"tall crystal shares same base without exceeding height");
}
Console.WriteLine($"Native hanging: {checks} checks passed (host stubs; live Unity pose checks pending).");
