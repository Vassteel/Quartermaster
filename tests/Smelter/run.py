"""Run the actual smelter cycle against deterministic queue, stock and fuel fixtures."""
import os
import subprocess
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = (root / 'src/Automation.cs').read_text()
start = source.index('    private static void ProcessSmelter(')
end = source.index('    internal static void RecoverProcessorClock(', start)
code = r'''
using System;
using System.Collections.Generic;
using Quartermaster;
class BaseNetwork {}
class ItemDrop {
 public string name; public object m_itemData;
 public static implicit operator bool(ItemDrop item)=>item!=null;
}
class Smelter {
 public int m_maxOre=10; public float m_maxFuel=20; public ItemDrop m_fuelItem;
 public List<Conversion> m_conversion=new(); public ZDO Data=new();
 public class Conversion { public ItemDrop m_from,m_to; }
}
class ZDO {
 public int Queued; public float Fuel;
 public int GetInt(int key)=>Queued; public float GetFloat(int key)=>Fuel;
 public ZDO GetZDO()=>this;
}
static class ZDOVars { public const int s_queued=1,s_fuel=2; }
class ItemData { public bool m_cheated; }
class Program {
 static string status;
 static bool allowProduction,haveFuel,haveOre,rejectFuel;
 static int oreTaken,fuelTaken,checks;
 static MachineSettings settings=new();
 static ZDO View(Smelter s)=>s.Data;
 static MachineSettings Settings(Smelter s)=>settings;
 static void SetStatus(Smelter s,string value)=>status=value;
 static string Label(object item)=>"Coal";
 static string ProcessingStatus(Smelter s)=>s.Data.Fuel<=0?"Processing blocked: waiting for fuel":"Processing";
 static bool ProductionAllowed(BaseNetwork n,Smelter s,ItemDrop output,int batch) {
  if(!allowProduction)SetStatus(s,"Metal cap reached");return allowProduction;
 }
 static bool Supply(BaseNetwork n,Smelter s,ItemDrop input,Func<ChestSettings,bool> permission,
  string method,Func<ItemData,object[]> args,Func<bool> accepted) {
  if(!permission(new ChestSettings{FuelSupply=method=="RPC_AddFuel",ProcessingSupply=method=="RPC_AddOre"}))throw new Exception("Wrong supply permission");
  if(method=="RPC_AddOre") { if(!haveOre)return false;oreTaken++;s.Data.Queued++; }
  else { if(!haveFuel||rejectFuel)return false;fuelTaken++;s.Data.Fuel++; }
  return accepted();
 }
'''+source[start:end]+r'''
 static Smelter Setup(int queued,bool production,bool coal=true,bool ore=false) {
  allowProduction=production;haveFuel=coal;haveOre=ore;rejectFuel=false;oreTaken=fuelTaken=0;
  var s=new Smelter{m_fuelItem=new ItemDrop(),Data=new ZDO{Queued=queued}};
  s.m_conversion.Add(new(){m_from=new ItemDrop{name="CopperOre"},m_to=new ItemDrop()});return s;
 }
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Main() {
  var n=new BaseNetwork();var s=Setup(1,false);ProcessSmelter(n,s);
  Check(fuelTaken==8&&oreTaken==0,"Existing ore receives coal even at production cap");
  Check(status=="Processing","A production cap must not mask processing of queued ore");
  s=Setup(1,false,false);ProcessSmelter(n,s);
  Check(status.Contains("Waiting for Coal"),"Missing fuel takes priority over production cap");
  Check(fuelTaken==0&&s.Data.Queued==1,"Missing coal preserves queued ore");
  s=Setup(0,false);ProcessSmelter(n,s);
  Check(fuelTaken==0&&oreTaken==0,"Capped idle machines consume no ingredients or fuel");
  Check(status.Contains("cap reached"),"An idle capped machine still explains its cap");
  s=Setup(0,true);ProcessSmelter(n,s);
  Check(fuelTaken==0,"An empty smelter without ore does not draw coal");
  s=Setup(0,true,false,true);ProcessSmelter(n,s);
  Check(oreTaken==8&&status.Contains("Waiting for Coal"),"Adding ore must not overwrite the missing-coal explanation");
  s=Setup(10,false);s.Data.Fuel=19;ProcessSmelter(n,s);
  Check(fuelTaken==0&&status=="Processing","Full ore queue with sufficient fuel still reports processing");
  s=Setup(1,true);rejectFuel=true;ProcessSmelter(n,s);
  Check(status.Contains("Waiting for Coal")&&fuelTaken==0,"Rejected fuel leaves queued work waiting");
  s=Setup(1,false);s.m_fuelItem=null;s.m_maxFuel=0;ProcessSmelter(n,s);
  Check(fuelTaken==0,"Fuel-free processors never request coal");
  Console.WriteLine($"PASS: {checks} smelter queue, cap and fuel regressions.");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='quartermaster-smelter-') as tmp:
    p = Path(tmp)
    (p / 'Smelter.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
    (p / 'Program.cs').write_text(code)
    (p / 'Policy.cs').write_text((root / 'src/Policy.cs').read_text())
    subprocess.run([os.environ.get('DOTNET', 'dotnet'), 'run', '--project', str(p / 'Smelter.csproj'), '-c', 'Release'], check=True)
