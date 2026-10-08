using System.Reflection;
using Quartermaster;
using Quartermaster.Cosmetics;
using UnityEngine;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
ItemDrop.ItemData Item(string id,bool consumable=true){var p=new GameObject{name=id};var i=new ItemDrop.ItemData{m_dropPrefab=p,m_shared=new(){m_name=id,m_itemType=consumable?ItemDrop.ItemData.ItemType.Consumable:ItemDrop.ItemData.ItemType.Material}};p.Component=new ItemDrop{name=id,m_itemData=i};ObjectDB.instance.Items[id]=p;return i;}
object Call(string n,params object[] args)=>typeof(MeadCabinet).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
var bottle=Item("MeadHealthMinor");var stone=Item("Stone",false);var baseItem=Item("MeadBaseHealthMinor",false);var food=Item("CookedMeat");
Check(MeadStoragePolicy.Columns==6&&MeadStoragePolicy.Rows==4,"24 slots");
Check(MeadCabinet.IsMead(bottle)&&MeadCabinet.IsMead("MeadHealthMinor"),"mead item and prefab metadata supported");
Check(!MeadCabinet.IsMead(stone)&&!MeadCabinet.IsMead(baseItem)&&!MeadCabinet.IsMead(food),"reject materials bases food");
Check(!MeadStoragePolicy.Accepts("MeadBaseTest",true,true)&&!MeadStoragePolicy.Accepts(null,true),"base and null never admitted");
var drink=Item("ModdedHealingDrink");drink.m_shared.m_consumeStatusEffect=new();ZNetScene.instance=new();var fermenter=new Fermenter();fermenter.m_conversion.Add(new(){m_to=(ItemDrop)drink.m_dropPrefab.Component});ZNetScene.instance.m_prefabs.Add(new GameObject{Component=fermenter});
Check(MeadCabinet.IsMead(drink),"fermenter output with consumable effect supports modded drinks");
var shelf=new Inventory();var bag=new Inventory();ContainerRegistry.Owners[shelf]=new Container{name=MeadCabinet.Prefab};
Check(MeadCabinet.Allows(shelf,bottle)&&!MeadCabinet.Allows(shelf,stone)&&MeadCabinet.Allows(bag,stone),"restriction scoped to mead cabinet");
foreach(string method in new[]{"Add","AddAt","AddAmount"}){
 object[] a={shelf,stone,true};Check(!(bool)Call(method,a)&&!(bool)a[2],method+" rejects invalid item");
 object[] b={shelf,bottle,false};Check((bool)Call(method,b),method+" allows mead");}
shelf.Item=bottle;bag.Item=stone;
object[] inward={shelf,bag,stone,1,new Vector2i(0,0),true};Check(!(bool)Call("Drop",inward)&&!(bool)inward[5]&&shelf.Item==bottle&&bag.Item==stone,"inward swap refused before mutation");
object[] outward={bag,shelf,bottle,1,new Vector2i(0,0),true};Check(!(bool)Call("Drop",outward)&&!(bool)outward[5],"reverse swap cannot put stone into shelf");
bag.Item=null;object[] withdrawal={bag,shelf,bottle,1,new Vector2i(0,0),false};Check((bool)Call("Drop",withdrawal),"withdrawal to empty player slot allowed");
Call("Loading");Check(MeadCabinet.Allows(shelf,stone),"saved non-mead retained during load");var failure=new Exception("load error");Check(Call("Loaded",failure)==failure&&!MeadCabinet.Allows(shelf,stone),"load exception restores admission guard");
bottle.m_gridPos=new Vector2i(5,3);
Check(MeadCabinet.DisplayItems(new[]{stone,bottle,bottle}).SequenceEqual(new[]{bottle}),"last inventory row displayed and duplicate stacks coalesced");
Check(MeadCabinet.DisplayItems(Array.Empty<ItemDrop.ItemData>()).Length==0,"empty shelf clears bottles");
Check(MeadCabinet.DisplayItems(Enumerable.Range(0,20).Select(n=>Item("MeadTest"+n))).Length==12,"display bounded to twelve native models");
using(var stream=File.OpenRead("assets/mead-cabinet/model.bin")){
 var model=OwlModelData.Read(stream,true);Check(model.Levels.Count==2&&model.Levels[0].Length==3,"bounded native-material asset");
 foreach(var p in model.Levels[0])for(int i=0;i<p.Vertices.Length;i+=8)Check(Math.Abs(p.Vertices[i])<=.505&&p.Vertices[i+1]>=-.01&&p.Vertices[i+1]<=1.01&&Math.Abs(p.Vertices[i+2])<=.16,"mesh fits snap envelope");
}
Console.WriteLine($"PASS {checks} mead classification, admission, swap, persistence and asset checks");
