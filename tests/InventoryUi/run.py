"""Run production layout methods against both native and InventorySlots hierarchies."""
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
chest = (root / 'src/ChestUi.cs').read_text()
layout = chest[chest.index('    private static void LayoutActions()'):chest.index('    private static void DisposeActions()')]
pickup = (root / 'src/PickupTab.cs').read_text()
pickup = pickup[pickup.index('    internal static void Layout(bool visible)'):pickup.index('    private static Sprite LoadIcon()')]
code = r'''
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Quartermaster;
namespace Quartermaster {
 static class ChestUi {
  internal static GameObject dock;
  internal static List<Button> dockButtons = new();
  internal static NativeInventoryFooter chestFooter,playerFooter;
  internal static bool IsOpen;
  internal static Component lastHovered;
  static bool Near(Component obj)=>true;
  internal static void Run()=>LayoutActions();
''' + layout + r'''
 }
 static class PickupTab {
  internal static GameObject root;
  internal static Text count=new();
  internal static Tooltip tooltip=new();
  internal static RectTransform armor,weight;
  internal static int lastCount=-1;
''' + pickup + r'''
 }
 static class ExternalSlotCompatibility { internal static bool HasInventorySlots; }
 static class Plugin {internal static Setting Enabled=new(){Value=true},ShowSortInventory=new(){Value=true};internal static Number SortInventoryOffsetX=new(),SortInventoryOffsetY=new();internal static Container OpenContainer;}
 static class ConfigurationManagerCompatibility {internal static bool IsOpen;}
 static class ContainerRegistry {internal static Container GetSettings(Container c)=>c;}
 static class PickupFilter {internal static int Count(Player p)=>2;}
 class Setting {internal bool Value;}
 class Number {internal int Value;}
 class Text {internal string text;internal bool richText,raycastTarget;}
 class Tooltip : UnityEngine.Object {internal string m_text;}
}
class Container : UnityEngine.Object {internal bool Deposit;}
class Player : UnityEngine.Object {internal static Player m_localPlayer=new();}
class InventoryGui : UnityEngine.Object {
 internal static InventoryGui instance;internal static bool visible=true;
 internal static bool IsVisible()=>visible;
 internal RectTransform m_player,m_container;
 internal InventoryGrid m_playerGrid=new();
}
class InventoryGrid : UnityEngine.Object {internal RectTransform m_gridRoot;}
class Button : Component {internal Button(GameObject go){gameObject=go;} }
namespace UnityEngine {
 class Object {public static implicit operator bool(Object o)=>o!=null;}
 struct Vector2 {
  internal float x,y;internal Vector2(float x,float y){this.x=x;this.y=y;}
  internal static Vector2 zero=>new(0,0);
 }
 struct Vector3 {
  internal float x,y,z;internal Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
  internal static Vector3 one=>new(1,1,1);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);
 }
 struct Rect {internal float width,height;internal Vector2 center=>new(width/2,height/2);}
 class Component : Object {internal GameObject gameObject;internal Transform transform=>gameObject.transform;}
 class GameObject : Object {
  internal readonly RectTransform transform;internal bool activeSelf=true;
  internal bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
  internal GameObject(string name){transform=new RectTransform{gameObject=this,name=name};}
  internal void SetActive(bool active)=>activeSelf=active;
 }
 class Transform : Object {
  internal GameObject gameObject;internal string name;internal Transform parent;
  internal List<Transform> children=new();internal Vector3 position,localScale;
  internal void SetParent(Transform p,bool world){parent?.children.Remove(this);parent=p;p.children.Add(this);}
  internal Transform Find(string path){Transform t=this;foreach(var part in path.Split('/')){t=t?.children.FirstOrDefault(x=>x.name==part);}return t;}
  internal Vector3 TransformPoint(Vector2 point)=>position+new Vector3(point.x,point.y);
 }
 class RectTransform : Transform {
  internal Rect rect;internal Vector2 offsetMin,anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;
 }
 static class Mathf {
  internal static int Max(int a,int b)=>Math.Max(a,b);internal static float Max(float a,float b)=>Math.Max(a,b);
  internal static int Min(int a,int b)=>Math.Min(a,b);internal static int FloorToInt(float v)=>(int)Math.Floor(v);
  internal static bool Approximately(float a,float b)=>Math.Abs(a-b)<.001;
 }
}
class Program {
 static int checks;
 static void Check(bool condition,string why){checks++;if(!condition)throw new Exception(why);}
 static RectTransform Rect(string name,Transform parent=null,float width=560,float height=300){var r=new GameObject(name).transform;r.rect=new(){width=width,height=height};if(parent!=null)r.SetParent(parent,false);return r;}
 static RectTransform Panel(string name){var r=Rect(name);Rect("Bkg",r);Rect("Darken",r);return r;}
 static void Main(){
  var gui=InventoryGui.instance=new InventoryGui{m_player=Panel("Player"),m_container=Panel("Container")};
  gui.m_playerGrid.m_gridRoot=Rect("Grid",gui.m_player);
  ChestUi.dock=Rect("Quartermaster_Actions",gui.m_player).gameObject;
  for(int i=0;i<5;i++)ChestUi.dockButtons.Add(new Button(Rect("button"+i,ChestUi.dock.transform).gameObject));
  ChestUi.playerFooter=new(gui.m_player);ChestUi.chestFooter=new(gui.m_container);
  PickupTab.armor=Rect("Armor",gui.m_player,70,70);PickupTab.weight=Rect("Weight",gui.m_player,70,70);PickupTab.weight.position=new(280,0);
  PickupTab.root=Rect("Quartermaster_Pickup",gui.m_player,70,70).gameObject;
  var bg=(RectTransform)gui.m_player.Find("Bkg");var chestBg=(RectTransform)gui.m_container.Find("Bkg");
  ChestUi.Run();
  Check(ChestUi.dock.transform.parent==gui.m_player,"Vanilla actions below player panel");
  Check(ChestUi.dockButtons.Count(b=>b.gameObject.activeSelf)==1&&ChestUi.dockButtons[3].gameObject.activeSelf,"Only Sort Inventory without a chest");
  Check(bg.offsetMin.y==-56&&chestBg.offsetMin.y==0,"Vanilla footer extends player artwork only");
  Check(PickupTab.root.transform.position.x==175&&PickupTab.root.transform.position.y==35,"Vanilla pickup midpoint preserved");
  for(int frame=0;frame<300;frame++)ChestUi.Run();
  Check(bg.offsetMin.y==-56,"Repeated vanilla layout never accumulates footer height");
  Plugin.OpenContainer=new Container{Deposit=true};ChestUi.Run();
  Check(ChestUi.dock.transform.parent==gui.m_container&&bg.offsetMin.y==0&&chestBg.offsetMin.y==-56,"Opening chest transfers footer without leaving player extension");
  Check(ChestUi.dockButtons.Count(b=>b.gameObject.activeSelf)==4,"Deposit chest shows all four actions");
  ExternalSlotCompatibility.HasInventorySlots=true;
  ChestUi.Run();
  Check(!ChestUi.dock.activeSelf&&bg.offsetMin.y==0&&chestBg.offsetMin.y==0,"Wait for equipment panel without stale footer or overlap");
  var equipment=Rect("InventorySlots_CustomSlotPanel",gui.m_playerGrid.m_gridRoot,210,210);
  var statHost=Rect("InventorySlots_PlayerStatPanelHost",equipment);
  PickupTab.root.transform.SetParent(statHost,false);PickupTab.root.transform.position=new(230,-144);
  PickupTab.armor.SetParent(statHost,false);PickupTab.weight.SetParent(statHost,false);
  for(int frame=0;frame<300;frame++){
   // Simulate changing row count and the other mod rewriting native backgrounds.
   bg.offsetMin=new(0,-frame);chestBg.offsetMin=new(0,-frame/2f);
   ChestUi.Run();
   Check(bg.offsetMin.y==-frame&&chestBg.offsetMin.y==-frame/2f,"Never change InventorySlots backgrounds");
   Check(PickupTab.root.transform.position.x==230&&PickupTab.root.transform.position.y==-144,"Never fight external stat placement");
  }
  Check(ChestUi.dock.activeSelf&&ChestUi.dock.transform.parent==equipment,"Chest actions stay beneath equipment");
  Check(ChestUi.dock.transform.anchoredPosition.y==-24&&ChestUi.dock.transform.sizeDelta.x==210,"Action bank leaves 24-unit equipment gap and follows its width");
  Check(ChestUi.dock.transform.sizeDelta.y==178,"Narrow equipment panel stacks four action buttons");
  for(int i=0;i<4;i++)Check(ChestUi.dockButtons[i].gameObject.transform.anchoredPosition.y==-46*i,"Buttons do not overlap");
  Plugin.OpenContainer=null;ChestUi.Run();
  Check(ChestUi.dock.transform.parent==equipment&&ChestUi.dock.transform.sizeDelta.y==40,"Closing chest retains equipment location and shrinks actions");
  equipment.rect=new(){width=350,height=280};ChestUi.Run();
  Check(ChestUi.dock.transform.sizeDelta.x==350,"Resized equipment panel updates action width");
  ChestUi.lastHovered=new Component();ChestUi.Run();
  Check(ChestUi.dockButtons[4].gameObject.activeSelf&&ChestUi.dockButtons[3].gameObject.activeSelf,"Machine and sort remain reachable");
  ConfigurationManagerCompatibility.IsOpen=true;ChestUi.Run();
  Check(!ChestUi.dock.activeSelf&&!PickupTab.root.activeSelf,"Settings menu hides both controls");
  ConfigurationManagerCompatibility.IsOpen=false;ChestUi.Run();
  equipment.gameObject.SetActive(false);ChestUi.Run();
  Check(!ChestUi.dock.activeSelf,"Inactive equipment panel cannot leave floating controls");
  equipment.gameObject.SetActive(true);ChestUi.Run();
  Check(ChestUi.dock.activeSelf,"Equipment panel reopening restores controls");
  ExternalSlotCompatibility.HasInventorySlots=false;PickupTab.root.transform.SetParent(gui.m_player,false);ChestUi.lastHovered=null;ChestUi.Run();
  Check(ChestUi.dock.transform.parent==gui.m_player&&PickupTab.root.transform.position.x==175,"Native layout restored when optional mod absent");
  Plugin.SortInventoryOffsetX.Value=37;Plugin.SortInventoryOffsetY.Value=62;ChestUi.Run();
  Check(((RectTransform)ChestUi.dockButtons[3].transform).anchoredPosition.x==37&&((RectTransform)ChestUi.dockButtons[3].transform).anchoredPosition.y==-62,"Sort offsets apply relative to existing layout");
  float expectedBackground=bg.offsetMin.y+56;
  Plugin.ShowSortInventory.Value=false;ChestUi.Run();
  Check(!ChestUi.dock.activeSelf&&bg.offsetMin.y==expectedBackground,"Hiding the only action restores footer without negative sizes");
  Plugin.OpenContainer=new Container{Deposit=true};ChestUi.Run();
  Check(ChestUi.dock.activeSelf&&ChestUi.dockButtons.Count(b=>b.gameObject.activeSelf)==3,"Hide Sort Inventory preserves chest controls");
  Check(((RectTransform)ChestUi.dockButtons[0].transform).anchoredPosition.x==0&&((RectTransform)ChestUi.dockButtons[0].transform).anchoredPosition.y==0,"Offset never moves Chest Config");
  Plugin.ShowSortInventory.Value=true;Plugin.SortInventoryOffsetX.Value=0;Plugin.SortInventoryOffsetY.Value=0;Plugin.OpenContainer=null;ChestUi.Run();
  Check(((RectTransform)ChestUi.dockButtons[3].transform).anchoredPosition.x==0&&((RectTransform)ChestUi.dockButtons[3].transform).anchoredPosition.y==0,"Reset restores established layout");
  InventoryGui.visible=false;ChestUi.Run();
  Check(!ChestUi.dock.activeSelf&&!PickupTab.root.activeSelf,"Closing inventory hides both controls");
  Console.WriteLine($"PASS: {checks} native and InventorySlots UI layout checks (including 600 update frames).");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='quartermaster-inventory-ui-') as tmp:
    project = Path(tmp)
    (project / 'Ui.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
    (project / 'Program.cs').write_text(code)
    for name in ('InventoryUiCompatibility.cs', 'NativeInventoryFooter.cs'):
        (project / name).write_text((root / 'src' / name).read_text())
    subprocess.run([os.environ.get('DOTNET', 'dotnet'), 'run', '--project', str(project/'Ui.csproj'), '-c', 'Release'], check=True)
