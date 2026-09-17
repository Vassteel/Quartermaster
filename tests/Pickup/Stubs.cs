namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:Object {public object Component;public T GetComponent<T>() where T:class=>Component as T;}
}
public class Humanoid:UnityEngine.Object {
 public List<string> Messages=new();public void Message(MessageHud.MessageType type,string message)=>Messages.Add(message);
 public bool Pickup(UnityEngine.GameObject go,bool autoequip=true,bool autoPickupDelay=true)=>true;
}
public class Player:Humanoid {public static Player m_localPlayer;public Dictionary<string,string> m_customData=new();private void AutoPickup(float dt){} }
public class ItemDrop:UnityEngine.Object {
 public bool m_autoPickup=true;public ItemData m_itemData;
 public class ItemData {public UnityEngine.GameObject m_dropPrefab;public int m_stack=10;public SharedData m_shared=new();}
 public class SharedData {public string m_name;}
}
public class MessageHud {public enum MessageType {TopLeft}}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance;public UnityEngine.GameObject GetItemPrefab(string id)=>null;}
public class Localization {public static Localization instance=new();public string Localize(string name)=>name;}
namespace Quartermaster {public class Plugin {public static Setting Enabled=new();public class Setting {public bool Value=true;}}}

// The game's Mono Harmony build cannot initialize under .NET 8. Model the instruction
// data only; ApiCheck independently verifies the installed game's actual call sites.
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)] public class HarmonyPatch:Attribute {public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}}
 public class HarmonyPrefix:Attribute {} public class HarmonyPostfix:Attribute {} public class HarmonyFinalizer:Attribute {} public class HarmonyTranspiler:Attribute {}
 public static class AccessTools {
  public static System.Reflection.FieldInfo Field(Type type,string name)=>type.GetField(name,System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static);
  public static System.Reflection.MethodInfo Method(Type type,string name)=>type.GetMethod(name,System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static);
 }
 public class CodeInstruction {
  public System.Reflection.Emit.OpCode opcode;public object operand;public List<System.Reflection.Emit.Label> labels=new();
  public CodeInstruction(System.Reflection.Emit.OpCode op,object value=null){opcode=op;operand=value;}
  public bool LoadsField(System.Reflection.FieldInfo field)=>opcode==System.Reflection.Emit.OpCodes.Ldfld&&Equals(operand,field);
 }
}
