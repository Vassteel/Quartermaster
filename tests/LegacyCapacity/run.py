"""Exercise the production capacity migration against existing persisted field IDs."""
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
source = (root / 'src/RuntimePatches.cs').read_text()
start = source.index('    private static void PreserveExistingCapacity(')
brace = source.index('{', start)
end, depth = brace + 1, 1
while depth:
    depth += (source[end] == '{') - (source[end] == '}')
    end += 1
method = source[start:end]
code = '''using System;
using System.Collections.Generic;
class Program {
''' + method + '''
 static int checks;
 static void Check(Container c, int width, int height, string message) {
  PreserveExistingCapacity(c); checks++;
  if(c.m_width!=width || c.m_height!=height) throw new Exception(message);
 }
 static void Main() {
  Check(new Container(),4,2,"Unmodified native chest");
  var c=new Container();
  // IDs verified with the installed game's stable hash function against the
  // pre-cleanup DLL's original save-key strings (not runtime GetHashCode).
  c.View.Data.Ints[-503974627]=8;c.View.Data.Ints[1546878126]=6;
  Check(c,8,6,"Existing saved enlargement must survive");
  Check(c,8,6,"Repeated Awake must preserve capacity");
  c.m_width=10;c.m_height=8;
  Check(c,10,8,"Legacy dimensions must not shrink larger current storage");
  c=new Container();c.View.Data.Ints[1546878126]=6;
  Check(c,4,6,"Partially saved dimensions");
  c.View.Data.Ints[-503974627]=-1;c.View.Data.Ints[1546878126]=-1;
  Check(c,4,6,"Invalid saved dimensions cannot shrink storage");
  c=new Container();c.View.Data.Ints[-503974627]=20;c.View.Valid=false;
  Check(c,4,2,"Invalid network view must not be read");
  c.View=null;Check(c,4,2,"Missing network view must be safe");
  Console.WriteLine($"PASS: {checks} legacy capacity regressions.");
 }
}
class Container {public int m_width=4,m_height=2;public View View=new();}
static class ContainerRegistry {public static View GetView(Container c)=>c.View;}
class View {
 public bool Valid=true;public Zdo Data=new();
 public static implicit operator bool(View v)=>v!=null;
 public bool IsValid()=>Valid;
 public Zdo GetZDO(){if(!Valid)throw new Exception("Invalid view read");return Data;}
}
class Zdo {
 public Dictionary<int,int> Ints=new();
 public int GetInt(int key,int fallback)=>Ints.TryGetValue(key,out var value)?value:fallback;
}
'''
with tempfile.TemporaryDirectory(prefix='quartermaster-capacity-') as tmp:
    p = Path(tmp)
    (p / 'Capacity.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    (p / 'Program.cs').write_text(code)
    subprocess.run([os.environ.get('DOTNET', 'dotnet'), 'run', '--project', str(p / 'Capacity.csproj'), '-c', 'Release'], check=True)
