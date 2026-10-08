using UnityEngine;
namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:UnityEngine.Object {public object Component;public T GetComponent<T>() where T:class=>Component as T;}
}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true)]public class HarmonyPatch:Attribute {public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}public HarmonyPatch(Type t,string n,Type[] a){}}
 public class HarmonyPrefix:Attribute{}public class HarmonyFinalizer:Attribute{}
}
public struct Vector2i {public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}}
public class ItemDrop:UnityEngine.Object {
 public ItemData m_itemData;
 public class ItemData {public Vector2i m_gridPos;public GameObject m_dropPrefab;public SharedData m_shared=new();public int m_stack=1,m_quality=1;public enum ItemType{Material,Consumable}public class SharedData{public string m_name;public ItemType m_itemType;public object m_consumeStatusEffect;public int m_maxQuality=1,m_maxStackSize=10;}}
}
public class Inventory {public ItemDrop.ItemData Item;public ItemDrop.ItemData GetItemAt(int x,int y)=>Item;}
public class InventoryGrid{}
public class ZPackage{}
public class Container:UnityEngine.Object{}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance=new();public Dictionary<string,GameObject> Items=new();public GameObject GetItemPrefab(string id)=>Items.GetValueOrDefault(id);}
public class ZNetScene:UnityEngine.Object {public static ZNetScene instance;public List<GameObject> m_prefabs=new();}
public class Fermenter:UnityEngine.Object {public List<Conversion> m_conversion=new();public class Conversion{public ItemDrop m_to;}}
namespace Quartermaster {
 internal static class ContainerRegistry {internal static Dictionary<Inventory,Container> Owners=new();internal static Container OwnerOf(Inventory i)=>i==null?null:Owners.GetValueOrDefault(i);internal static string PrefabName(Container c)=>c.name;}
 internal static class InventoryTransfers {internal static string ItemId(ItemDrop.ItemData item)=>item?.m_dropPrefab?.name??"";}
}
