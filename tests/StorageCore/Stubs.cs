using Quartermaster;
public class GameObject { public string name;public static implicit operator bool(GameObject value)=>value!=null; }
public class Transform {public int position;}
public struct Vector2i {public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}}
public static class Game {public static int m_worldLevel;}
public class ItemDrop {
 public GameObject gameObject;public ItemData m_itemData;
 public static implicit operator bool(ItemDrop value)=>value!=null;
 public class SharedData {public string m_name;public int m_maxStackSize=50,m_itemType;public bool m_questItem,m_useDurability;}
 public class ItemData {
  public SharedData m_shared;public GameObject m_dropPrefab;
  public int m_stack,m_quality=1,m_worldLevel,m_variant;public long m_crafterID;public string m_crafterName;
  public float m_durability;public bool m_cheated,m_equipped;public Vector2i m_gridPos;
  public Dictionary<string,string> m_customData=new();
  public bool IsSameType(ItemData other)=>m_shared.m_name==other.m_shared.m_name;
  public ItemData Clone(){var copy=(ItemData)MemberwiseClone();copy.m_customData=new(m_customData);return copy;}
 }
}
public class Inventory {
 readonly List<ItemDrop.ItemData> items=new();readonly int width,height;
 public int Notifications;public Action OnChanged;public bool RejectRemovals;
 public Inventory(int width,int height){this.width=width;this.height=height;}
 public List<ItemDrop.ItemData> GetAllItems()=>items;
 public int GetWidth()=>width;public int GetHeight()=>height;public int NrOfItems()=>items.Count;
 public bool ContainsItem(ItemDrop.ItemData item)=>items.Contains(item);
 public int CountItems(string name,int quality,bool world)=>items.Where(i=>i.m_shared.m_name==name&&(quality<0||quality==i.m_quality)&&(!world||i.m_worldLevel>=Game.m_worldLevel)).Sum(i=>i.m_stack);
 public bool RemoveItem(ItemDrop.ItemData item,int count){if(RejectRemovals||!items.Contains(item)||count>item.m_stack)return false;item.m_stack-=count;if(item.m_stack==0)items.Remove(item);Changed();return true;}
 public ItemDrop.ItemData GetItem(string name,int quality,bool equipped)=>items.FirstOrDefault(i=>i.m_shared.m_name==name&&(quality<0||i.m_quality==quality));
 public void RemoveItem(string name,int count,int quality,bool world){foreach(var item in items.ToArray()){if(count<1)break;if(item.m_shared.m_name!=name||(quality>=0&&quality!=item.m_quality)||(world&&item.m_worldLevel<Game.m_worldLevel))continue;int n=Math.Min(count,item.m_stack);if(RemoveItem(item,n))count-=n;}}
 private void Changed(bool a=false,bool b=false){Notifications++;OnChanged?.Invoke();}
}
public class Player {
 public static Player m_localPlayer;public Inventory Inventory=new(4,3);public Transform transform=new();public CraftingStation Station;
 public enum RequirementMode {Known,CanBuild}
 public static implicit operator bool(Player value)=>value!=null;
 public Inventory GetInventory()=>Inventory;public bool IsItemEquiped(ItemDrop.ItemData item)=>item.m_equipped;
 public CraftingStation GetCurrentCraftingStation()=>Station;
}
public class CraftingStation {public bool m_upgrader;public static implicit operator bool(CraftingStation value)=>value!=null;}
public class Piece {public class Requirement {public ItemDrop m_resItem;public bool m_upgraderResource;public int Amount;public int GetAmount(int quality)=>Amount;}}
public class InventoryGui {}
public class Container {
 public Inventory Inventory=new(4,2);public bool Accessible=true,Owned=true,InUse;public int Distance;public ChestSettings Settings=new();public bool Reserved;
 public Inventory GetInventory()=>Inventory;
 public static implicit operator bool(Container value)=>value!=null;
}
public class ChestSettings {public bool CraftingSupply=true;}
namespace EquipmentAndQuickSlots {
 public static class API {
  public static bool Throw;public static int Calls;public static HashSet<(int,int)> Protected=new();public static int Rows=3;
  public static bool IsSlotCell(int x,int y,out string name){Calls++;name="test";if(Throw)throw new Exception("API unavailable");return Protected.Contains((x,y));}
  public static int GetVisibleRows()=>Rows;
 }
}
namespace Quartermaster {
 static class SharedChests { public static bool Viewing(Container c)=>false; }
 static class SharedOperations { public static bool Executing;public static bool Networked=true;public static bool Reserved(Container c)=>c.Reserved; }
 static class Plugin {
  public static Setting<bool> Enabled=new(true),CraftFromContainers=new(true);public static Setting<float> CraftRange=new(10);public static Logger Log=new();
 }
 class Setting<T>{public T Value;public Setting(T value){Value=value;}}
 class Logger {public int Warnings;public void LogInfo(string s){}public void LogWarning(string s){Warnings++;}public void LogError(string s)=>throw new Exception(s);}
 static class CraftStorageAccess {public static IEnumerable<Container> Nearby(Player p)=>ContainerRegistry.VisibleStores(p.transform.position,Plugin.CraftRange.Value);}
 static class ChestVisual {public static int Pulses;public static void Pulse(Container c)=>Pulses++;}
 static class ContainerRegistry {
  public static List<Container> Stores=new();public static Container GetView(Container c)=>c;public static ChestSettings GetSettings(Container c)=>c.Settings;
  public static Inventory SafeInventory(Container c)=>c?.Inventory;

  public static bool IsUsable(Container c,bool allowInUse)=>c&&c.Accessible&&c.Owned&&(allowInUse||!c.InUse);
  public static List<Container> VisibleStores(int position,float range)=>Stores.Where(c=>c&&c.Accessible&&!c.InUse&&c.Distance<=range).ToList();
  public static List<Container> Nearby(int position,float range,bool requireAccept,bool allowInUse)=>Stores.Where(c=>IsUsable(c,allowInUse)&&c.Distance<=range).ToList();
 }
}
