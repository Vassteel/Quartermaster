"""Exercise the production Close method with Unity-like focus and deferred destruction.
The method is extracted verbatim so regressions in the UI entry point are covered.
"""
import os, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=(root/'src/ChestUi.cs').read_text()
start=source.index('    internal static void Close()')
end=source.index('    internal static void Dispose()',start)
method=source[start:end]
code='''using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
class ChestUi {
 public static GameObject modal,blocker,chest,machine;
 static List<object> controls=new();
'''+method+'''}
class Program {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Main(){
  var events=EventSystem.current=new EventSystem();
  var password=new GameObject();events.currentSelectedGameObject=password;
  for(int frame=0;frame<300;frame++)ChestUi.Close();
  Check(events.currentSelectedGameObject==password && events.Clears==0,"Joining frames must preserve password focus");
  var panel=ChestUi.modal=new GameObject();var blocker=ChestUi.blocker=new GameObject();
  ChestUi.Close();
  Check(events.currentSelectedGameObject==password,"Closing a stale QM panel must preserve another UI's focus");
  Check(!panel.Active && panel.DestroyQueued && !blocker.Active && blocker.DestroyQueued,"Panels stop intercepting clicks before deferred destruction");
  panel=ChestUi.modal=new GameObject();var input=new GameObject();input.transform.parent=panel.transform;events.currentSelectedGameObject=input;
  ChestUi.Close();
  Check(events.currentSelectedGameObject==null && events.Clears==1,"Closing QM clears its own selected child");
  events.currentSelectedGameObject=password;ChestUi.Close();
  Check(events.currentSelectedGameObject==password && events.Clears==1,"Repeated cleanup never deselects the next menu");
  panel=ChestUi.modal=new GameObject();events.currentSelectedGameObject=panel;ChestUi.Close();
  Check(events.currentSelectedGameObject==null,"Closing QM clears selection on the panel itself");
  EventSystem.current=null;panel=ChestUi.modal=new GameObject();ChestUi.Close();
  Check(panel.DestroyQueued,"Cleanup works after the EventSystem is destroyed");
  events=EventSystem.current=new EventSystem();events.currentSelectedGameObject=password;
  ChestUi.modal=new GameObject {Destroyed=true};ChestUi.Close();
  Check(events.currentSelectedGameObject==password,"Destroyed Unity modal behaves as null and cannot steal focus");
  Console.WriteLine($"{checks} UI focus regression checks passed (including 300 password-screen frames).");
 }
}
namespace UnityEngine {
 class Object { public bool Destroyed,DestroyQueued;public static implicit operator bool(Object o)=>o!=null&&!o.Destroyed;public static void Destroy(Object o){o.DestroyQueued=true;} }
 class Transform : Object {public Transform parent;public bool IsChildOf(Transform target)=>this==target||(parent!=null&&parent.IsChildOf(target));}
 class GameObject : Object {public Transform transform=new();public bool Active=true;public void SetActive(bool value){Active=value;}}
}
namespace UnityEngine.EventSystems {
 class EventSystem : UnityEngine.Object {public static EventSystem current;public UnityEngine.GameObject currentSelectedGameObject;public int Clears;public void SetSelectedGameObject(UnityEngine.GameObject go){currentSelectedGameObject=go;Clears++;}}
}
'''
with tempfile.TemporaryDirectory(prefix='quartermaster-ui-focus-') as tmp:
 p=Path(tmp)
 (p/'Focus.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><WarningLevel>0</WarningLevel></PropertyGroup></Project>')
 (p/'Program.cs').write_text(code)
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Focus.csproj'),'-c','Release'],check=True)
