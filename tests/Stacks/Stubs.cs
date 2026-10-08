public class ItemDrop {
 public ItemData m_itemData;
 public class ItemData {
  public SharedData m_shared;public int m_stack;
  public ItemData Clone()=>(ItemData)MemberwiseClone();
  public class SharedData {public int m_maxStackSize;public float m_weight;}
 }
}
public class Prefab {
 public ItemDrop Item;
 public T GetComponent<T>() where T:class=>Item as T;
 public static implicit operator bool(Prefab value)=>value!=null;
}
public class ObjectDB {public List<Prefab> m_items=new();}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
 public class HarmonyPatch:Attribute {public HarmonyPatch(){} public HarmonyPatch(Type type,string name){} }
 public class HarmonyPostfix:Attribute {}
 public class HarmonyPriority:Attribute {public HarmonyPriority(int value){} }
 public class HarmonyAfter:Attribute {public HarmonyAfter(string value){} }
 public static class Priority {public const int Last=0;}
}
