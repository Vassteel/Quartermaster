"""Exercise the production native-harvest entry point with a controllable game fixture."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2]
s=(root/'src/Automation.cs').read_text()
methods=s[s.index('    internal static int HoneyLevel('):s.index('    private static void ProcessFermenter(')]
code='''using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
namespace Quartermaster {
class BaseNetwork{}
static class Automation {
 internal static bool Allowed=true;internal static int Batch;internal static string Status;
 static ZNetView View(Beehive h)=>h.View;
 static bool ProductionAllowed(BaseNetwork n,Beehive h,ItemDrop item,int batch){Batch=batch;return Allowed;}
 static void SetStatus(Beehive h,string status)=>Status=status;
 internal static void Run(Beehive h)=>ProcessHive(new(),h);
'''+methods+'''
}
static class ProductionOutput {
 internal static int Depth;
 internal class Scope{}
 internal static Scope Begin(Beehive h,ItemDrop d){Depth++;return new();}
 internal static void End(Scope s){Depth--;}
}
}
class ItemDrop {internal object m_itemData=new();public static implicit operator bool(ItemDrop d)=>d!=null;}
class ZNetView {internal ZDO Zdo=new();public static implicit operator bool(ZNetView v)=>v!=null;internal bool IsValid()=>true;internal ZDO GetZDO()=>Zdo;}
class ZDO {internal int Honey;internal long Visit;internal int GetInt(int key)=>Honey;internal void Set(string key,long value)=>Visit=value;}
static class ZDOVars {internal const int s_level=1;}
class Game {internal static Game instance=new();internal int Rate=1;internal int ScaleDrops(object item,int count)=>count*Rate;}
class ZNet {internal static ZNet instance=new();internal DateTime GetTime()=>new DateTime(1234567);}
class Beehive {
 internal ZNetView View=new();internal ItemDrop m_honeyItem=new();internal int Drops,Calls;internal bool Throw;
 private void RPC_Extract(long sender){Calls++;if(Throw)throw new Exception("native extraction failed");Drops+=View.Zdo.Honey*Game.instance.Rate;View.Zdo.Honey=0;}
}
namespace HarmonyLib {static class AccessTools {internal static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);}}
class Program {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Main(){
 var h=new Beehive();h.View.Zdo.Honey=4;
 Quartermaster.Automation.Allowed=false;Quartermaster.Automation.Run(h);
 Check(h.Calls==0&&h.View.Zdo.Honey==4,"unconfigured, capped or full storage never extracts honey");
 Quartermaster.Automation.Allowed=true;Game.instance.Rate=3;Quartermaster.Automation.Run(h);
 Check(Quartermaster.Automation.Batch==12&&h.Drops==12,"storage preflight accounts for native world drop multiplier");
 Check(h.Calls==1&&h.View.Zdo.Honey==0,"extracts a ready hive exactly once");
 Check(h.View.Zdo.Visit==1234567,"successful harvest leaves a recent owl visit cue");
 Check(Quartermaster.ProductionOutput.Depth==0,"tagging scope ends after native harvest");
 Quartermaster.Automation.Run(h);Check(h.Calls==1,"empty hive does not respawn honey");
 h.View.Zdo.Honey=2;h.Throw=true;
 try{Quartermaster.Automation.Run(h);}catch(TargetInvocationException){}
 Check(Quartermaster.ProductionOutput.Depth==0,"native exception cannot leak honey tagging into player drops");
 Console.WriteLine($"PASS: {checks} native honey harvest checks.");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='quartermaster-honey-') as temp:
 p=Path(temp);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'Program.cs').write_text(code)
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'-c','Release'],check=True)
