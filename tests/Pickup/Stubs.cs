namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:Object {public object Component;public T GetComponent<T>() where T:class=>Component as T;}
}
public class Humanoid:UnityEngine.Object {
 public List<string> Messages=new();public void Message(MessageHud.MessageType type,string message)=>Messages.Add(message);
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 public bool Pickup(UnityEngine.GameObject go,bool autoequip=true,bool autoPickupDelay=true){ Received++; return true; }
 public int Received;
}
public class Player:Humanoid {public static Player m_localPlayer;public Dictionary<string,string> m_customData=new();public ItemDrop Target; public bool Throw;
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 public void AutoPickup(float dt){ if(Throw) throw new InvalidOperationException("pickup failure"); if(Target.m_autoPickup && Target.CanPickup()) Pickup(new UnityEngine.GameObject{Component=Target}); } }
public class ItemDrop:UnityEngine.Object {
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 public bool CanPickup()=>true;
 public bool m_autoPickup=true;public ItemData m_itemData;
 public class ItemData {public UnityEngine.GameObject m_dropPrefab;public int m_stack=10;public SharedData m_shared=new();}
 public class SharedData {public string m_name;}
}
public class MessageHud {public enum MessageType {TopLeft}}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance;public UnityEngine.GameObject GetItemPrefab(string id)=>null;}
public class Localization {public static Localization instance=new();public string Localize(string name)=>name;}
namespace Quartermaster {public class Plugin {public static Setting Enabled=new(); public static Logger Log=new(); public class Logger {public int Warnings; public void LogWarning(string message){Warnings++;}}public class Setting {public bool Value=true;}}}
