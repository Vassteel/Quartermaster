"""Run production Plugin.Update/world teardown through death and world transitions."""
import os, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=(root/'src/Plugin.cs').read_text()
def method(signature):
 start=source.index(signature);brace=source.index('{',start);depth=1;end=brace+1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
update=method('    private void Update()')
cleanup=method('    internal static void WorldUnloaded()') if '    internal static void WorldUnloaded()' in source else 'internal static void WorldUnloaded() {}'
code='''using System;
using UnityEngine;
namespace Quartermaster {
static class ChestSearch { public static void Clear() {} public static void Tick() {} }
static class SharedChests { public static int Ticks, Clears; public static void Tick()=>Ticks++; public static void Clear()=>Clears++; }
class Plugin:UnityEngine.Object {
 internal static Plugin Instance;
 internal static Setting Enabled=new();internal static Shortcut MachineKey=new();internal static Logger Log=new();
 internal static Container OpenContainer;
 private float next;private bool playerReady;
 public void Frame()=>Update();
'''+update+cleanup+'''
}
class Program {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Seed(){ContainerRegistry.Count=3;Automation.Devices=2;Automation.Animals=1;Automation.Outputs=1;ItemTagCleanup.Pending=3;}
 static void Main(){
  var plugin=Plugin.Instance=new Plugin();Seed();Player.m_localPlayer=new Player();plugin.Frame();
  Check(Automation.Ticks==1,"live player starts automation");
  Plugin.OpenContainer=new Container();ChestUi.Open=true;
  Player.m_localPlayer=null;Time.time+=3;plugin.Frame();
  Check(ContainerRegistry.Count==3&&Automation.Devices==2&&Automation.Animals==1&&Automation.Outputs==1,"respawn gap preserves loaded chest, machine, animal and output registries");
  Check(ItemTagCleanup.Pending==3,"pending chest scans survive respawn");
  Check(!ChestUi.Open&&Plugin.OpenContainer==null,"respawn clears stale player UI and open-container reference");
  Check(Automation.Ticks==1&&ChestStackOverflow.Ticks==1,"missing player pauses transfers and overflow");
  int closes=ChestUi.Closes;
  for(int i=0;i<100;i++){Time.time+=.1f;plugin.Frame();}
  Check(ContainerRegistry.Clears==0&&Automation.Clears==0&&ChestUi.Closes==closes,"long respawn pauses never erase world objects or repeatedly close UI");
  Player.m_localPlayer=new Player();plugin.Frame();
  Check(Automation.Ticks==2&&Automation.LastChestCount==3&&Automation.LastDeviceCount==2,"new player resumes with the original loaded network without reopening chests");
  Plugin.OpenContainer=new Container();ChestUi.Open=true;Player.m_localPlayer.Dead=true;Time.time+=3;plugin.Frame();
  Check(Automation.Ticks==2&&ChestStackOverflow.Ticks==2,"dead player pauses world mutations even before its object is destroyed");
  Check(!ChestUi.Open&&Plugin.OpenContainer==null&&ContainerRegistry.Count==3,"death closes stale UI but keeps the registry");
  Player.m_localPlayer=new Player();plugin.Frame();
  Check(Automation.Ticks==3,"respawn without a missing-player frame also resumes");
  Plugin.Enabled.Value=false;Time.time+=3;plugin.Frame();
  Check(Automation.Ticks==3&&ContainerRegistry.Count==3,"disabled automation stays disabled after respawn");
  Plugin.WorldUnloaded();
  Check(ContainerRegistry.Count==0&&Automation.Devices==0&&Automation.Animals==0&&Automation.Outputs==0,"actual world teardown clears every registry");
  Check(NativeHangingItem.Clears==1,"world teardown resets bounded native-display warning cache");
  Check(SharedChests.Clears==1,"world teardown settles shared-chest state");
  Check(SharedChests.Ticks>100,"shared chests keep ticking through respawn gaps (pending items are delivered after respawn)");
  Check(ItemTagCleanup.Pending==0&&Plugin.OpenContainer==null,"world teardown clears tag scans and player UI state");
  Player.m_localPlayer=null;plugin.Frame();Seed();Plugin.Enabled.Value=true;Time.time=0;Player.m_localPlayer=new Player();plugin.Frame();
  Check(Automation.Ticks==4&&Automation.LastChestCount==3,"next world starts promptly even when its clock resets");
  Plugin.WorldUnloaded();Plugin.WorldUnloaded();
  Check(ContainerRegistry.Count==0&&Automation.Devices==0,"repeated shutdown remains safe");
  Check(Plugin.Log.Errors==0,"lifecycle does not throw or rely on exception recovery");
  Console.WriteLine($"PASS: {checks} production respawn/world lifecycle regressions.");
 }
}
class Setting { public bool Value=true; }
class Shortcut { public Shortcut Value=>this;public bool IsDown()=>false; }
class Logger { public int Errors;public void LogError(string text){Errors++;} }
class Container:UnityEngine.Object {}
static class ContainerRegistry {public static int Count,Clears;public static void Clear(){Count=0;Clears++;}}
static class Automation {
 public static int Devices,Animals,Outputs,Ticks,Clears,LastChestCount,LastDeviceCount;
 public static void Clear(){Devices=Animals=Outputs=0;Clears++;}
 public static void Tick(){Ticks++;LastChestCount=ContainerRegistry.Count;LastDeviceCount=Devices;}
}
static class ItemTagCleanup {public static int Pending;public static void Clear()=>Pending=0;public static void Tick(){} }
static class ChestStackOverflow {public static int Ticks;public static void Tick()=>Ticks++;}
static class ChestUi {public static bool Open;public static int Closes;public static void Close(){Open=false;Closes++;}public static void Tick(){}public static void OpenHoveredMachine(){} }
static class PickupFilter {public static void Clear(){} }
static class NativeHangingItem {public static int Clears;public static void ClearWarnings()=>Clears++;}
class Player:UnityEngine.Object {public static Player m_localPlayer;public bool Dead;public bool IsDead()=>Dead;public bool IsTeleporting()=>false;}
static class InventoryGui {public static bool IsVisible()=>false;}
class Chat:UnityEngine.Object {public static Chat instance;public bool HasFocus()=>false;}
static class Menu {public static bool IsVisible()=>false;}
static class TextInput {public static bool IsVisible()=>false;}
}
namespace UnityEngine {
 class Object {public static implicit operator bool(Object o)=>o!=null;}
 static class Time {public static float time;}
}
'''
# Console is a game type, while test output remains System.Console.
code=code.replace('!Console.IsVisible()', '!GameConsole.IsVisible()')+'\nstatic class GameConsole { public static bool IsVisible()=>false; }\n'
with tempfile.TemporaryDirectory(prefix='quartermaster-respawn-') as tmp:
 p=Path(tmp)
 (p/'Respawn.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
 (p/'Program.cs').write_text(code)
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Respawn.csproj'),'-c','Release'],check=True)
