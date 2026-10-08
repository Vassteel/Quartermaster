"""Hives harvest without a desired quantity; a set limit (including 0) is still honored."""
import os,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
src=(root/'src/Automation.cs').read_text()
def section(a,b): return src[src.index(a):src.index(b,src.index(a))]
allowed=section('    private static bool ProductionAllowed(','    private static bool Take(')
unlimited=section('    // Hives consume nothing','    internal static void Register(')
policy=(root/'src/Policy.cs').read_text()
batches=policy[policy.index('    public static int BatchesAllowed('):policy.index('    public static string Group(')]
code=r'''
using System;
using System.Collections.Generic;
using System.Linq;
class Vector3{} class Transform{public Vector3 position=new();}
class Component{public Transform transform=new();} class Beehive:Component{} class Smelter:Component{}
class ItemDrop{public ItemData m_itemData=new();public class ItemData{public string Name="Honey";}}
class Container{public Inventory GetInventory()=>new();} class Inventory{}
class BaseNetwork{}
static class InventoryTransfers{public static string PrefabId(ItemDrop d)=>"Honey";public static int CapacityFor(Inventory i,ItemDrop.ItemData item,bool m)=>Program.Room;}
static class Policy{'''+batches+r'''}
class Program{
 internal static int Room=50; static bool limited; static int cap; static long stock,queued; static string status;
 static bool HasProductionLimit(BaseNetwork n,string id)=>limited;
 static int EffectiveCap(BaseNetwork n,string id)=>cap;
 static long Queued(BaseNetwork n,string id)=>queued;
 static long Stock(BaseNetwork n,string id)=>stock;
 static ItemDrop.ItemData NewOutput(ItemDrop d,bool cheated)=>d.m_itemData;
 static List<Container> Destinations(BaseNetwork n,ItemDrop.ItemData item,Vector3 p,Container exclude,bool fallback)=>new(){new Container()};
 static void SetStatus(Component c,string s)=>status=s;
 static string Label(ItemDrop.ItemData i)=>i.Name;
'''+allowed+unlimited+r'''
 static int checks; static void Check(bool ok,string m){checks++;if(!ok)throw new Exception(m);}
 static void Main(){
  var hive=new Beehive();var smelter=new Smelter();var honey=new ItemDrop();var n=new BaseNetwork();
  limited=false;
  Check(!ProductionAllowed(n,smelter,honey,1)&&status.StartsWith("Set a desired quantity"),"smelters still require a desired quantity");
  Check(ProductionAllowed(n,hive,honey,4),"a hive harvests with no limit set");
  stock=1_000_000; Check(ProductionAllowed(n,hive,honey,4),"unlimited hive ignores existing stock");
  Room=3; Check(!ProductionAllowed(n,hive,honey,4)&&status=="Output storage full or busy","unlimited hive still needs storage room for the whole batch");
  Room=50; stock=0; limited=true; cap=0;
  Check(!ProductionAllowed(n,hive,honey,4)&&status.Contains("cap reached"),"an explicit limit of 0 disables hive harvest");
  cap=10; stock=8; Check(!ProductionAllowed(n,hive,honey,4),"batch exceeding the remaining limit is refused");
  stock=6; Check(ProductionAllowed(n,hive,honey,4),"batch within the limit is allowed");
  Console.WriteLine($"PASS: {checks} hive production-limit checks.");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='qm-hive-limit-') as tmp:
    p=Path(tmp)
    (p/'Hive.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
    (p/'Program.cs').write_text(code)
    subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Hive.csproj'),'-c','Release'],check=True)
