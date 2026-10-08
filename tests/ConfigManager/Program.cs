using Quartermaster;
using BepInEx.Bootstrap;
int checks=0;
void Check(bool b,string name){if(!b)throw new Exception(name);checks++;}
if(args.Contains("absent")){Check(!ConfigurationManagerCompatibility.IsOpen,"No manager leaves UI available");}
else {
 var a=new Manager();var b=new Manager();
 Chainloader.PluginInfos["_shudnal.ConfigurationManager"]=new(){Instance=a};
 Chainloader.PluginInfos["com.bepis.bepinex.configurationmanager"]=new(){Instance=b};
 Check(!ConfigurationManagerCompatibility.IsOpen,"Closed managers");
 a.Open=true;Check(ConfigurationManagerCompatibility.IsOpen,"Shudnal open");a.Open=false;
 b.Open=true;Check(ConfigurationManagerCompatibility.IsOpen,"Standard open");b.Open=false;
 Check(!ConfigurationManagerCompatibility.IsOpen,"Closing restores access");
 a.Throw=true;Check(ConfigurationManagerCompatibility.IsOpen,"Throwing state blocks UI safely");int calls=a.Calls;
 Check(ConfigurationManagerCompatibility.IsOpen&&a.Calls==calls&&Plugin.Log.Warnings==1,"Failure latched and logged once");
}
Console.WriteLine($"PASS: {checks} configuration manager compatibility checks.");
class Manager {public bool Open,Throw;public int Calls;public bool DisplayingWindow {get{Calls++;if(Throw)throw new Exception("test");return Open;}}}
namespace BepInEx.Bootstrap {static class Chainloader {public static Dictionary<string,Info> PluginInfos=new();}class Info{public object Instance;}}
namespace Quartermaster {static class Plugin{public static Logger Log=new();}class Logger{public int Warnings;public void LogWarning(string s){Warnings++;}}}
