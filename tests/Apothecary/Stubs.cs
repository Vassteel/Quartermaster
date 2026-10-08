using UnityEngine;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:Object {
  public string name;readonly Dictionary<Type,object> components=new();public GameObject(string n){name=n;}
  public T GetComponent<T>()where T:class=>components.TryGetValue(typeof(T),out var c)?c as T:null;
  public T AddComponent<T>()where T:new(){var c=new T();components[typeof(T)]=c;if(c is Component v)v.gameObject=this;return c;}
 }
 public class Component:Object {public GameObject gameObject;public string name=>gameObject.name;}
 public class Sprite:Object {}
}
public class ItemDrop:Component {public ItemData m_itemData=new();public class ItemData {public enum ItemType {Material,Consumable,OneHandedWeapon,Fish,Bow,Shield,Ammo,TwoHandedWeapon,TwoHandedWeaponLeft,Tool,Helmet,Chest,Legs,Shoulder,Hands,Trophy,AmmoNonEquipable};public GameObject m_dropPrefab;public int m_stack;public SharedData m_shared=new();public class SharedData {public string m_name,m_description,m_ammoType;public Skills.SkillType m_skillType;public Sprite[] m_icons;public float m_weight,m_food,m_foodStamina,m_foodEitr;public int m_maxStackSize,m_value;public object m_consumeStatusEffect;public ItemType m_itemType;}}}
public class Container:Component {public Inventory GetInventory()=>new Inventory{ Name=m_name };public string m_name;public int m_width,m_height;public DropTable m_defaultItems;public Quartermaster.ChestSettings Settings=new();}
public class Piece:Component {
 public bool m_groundPiece=true,m_groundOnly=true,m_cultivatedGroundOnly=true,m_waterPiece=true,m_notOnWood=true,m_notOnTiltingSurface=true;
 public bool m_inCeilingOnly,m_notOnFloor;
}
public class DropTable{}
public class ZNetScene:UnityEngine.Object {public static ZNetScene instance;public List<GameObject> m_prefabs=new();}
public class CookingStation:Component {public List<Smelter.ItemConversion> m_conversion=new();}
public class Smelter:Component {public ItemDrop m_fuelItem;public string m_name,m_addOreTooltip;public int m_maxOre,m_maxFuel,m_fuelPerProduct;public float m_secPerProduct;public List<ItemConversion> m_conversion=new();public class ItemConversion{public ItemDrop m_from,m_to;}}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance;public List<Recipe> m_recipes=new();public List<GameObject> m_items=new();}
public class Recipe:UnityEngine.Object {public ItemDrop m_item;public Requirement[] m_resources;public class Requirement {public ItemDrop m_resItem;}}
namespace Jotunn.Configs {
 public class RequirementConfig {public string Item;public int Amount;public bool Recover;public RequirementConfig(string item,int amount,int per=0,bool recover=false){Item=item;Amount=amount;Recover=recover;}}
 public class ItemConfig {public string Name,Description,CraftingStation;public int MinStationLevel,Amount;public RequirementConfig[] Requirements;}
 public class PieceConfig {public bool Enabled=true;public string Name,Description,PieceTable,Category,CraftingStation;public Sprite Icon;public RequirementConfig[] Requirements;}
}
namespace Jotunn.Entities {
 public class CustomItem {public GameObject Prefab;public Jotunn.Configs.ItemConfig Config;public CustomItem(GameObject p,bool f){Prefab=p;}public CustomItem(GameObject p,bool f,Jotunn.Configs.ItemConfig c):this(p,f){Config=c;}}
 public class CustomPiece {public GameObject Prefab;public Jotunn.Configs.PieceConfig Config;public CustomPiece(GameObject p,bool f,Jotunn.Configs.PieceConfig c){Prefab=p;Config=c;}}
}
namespace Jotunn.Managers {
 public class PrefabManager {
  public static PrefabManager Instance=new();public static event Action OnVanillaPrefabsAvailable;public List<(string name,string source)> Clones=new();public Dictionary<string,GameObject> Prefabs=new();
  public GameObject GetPrefab(string id)=>Prefabs.TryGetValue(id,out var p)?p:null;
  public GameObject CreateClonedPrefab(string name,string source){Clones.Add((name,source));var p=new GameObject(name);p.AddComponent<ItemDrop>();p.AddComponent<Container>();p.AddComponent<Smelter>();p.AddComponent<Piece>();Prefabs[name]=p;return p;}public static void Fire()=>OnVanillaPrefabsAvailable?.Invoke();
 }
 public class ItemManager {public static ItemManager Instance=new();public List<Jotunn.Entities.CustomItem> Items=new();public bool AddItem(Jotunn.Entities.CustomItem item){Items.Add(item);return true;}}
 public class PieceManager {public static PieceManager Instance=new();public List<Jotunn.Entities.CustomPiece> Pieces=new();public bool AddPiece(Jotunn.Entities.CustomPiece item){Pieces.Add(item);return true;}}
}
namespace Quartermaster {
 internal static class Plugin {internal static Logger Log=new();internal static Setting StorageCategoryOverrides=new();internal class Setting{internal string Value="";}internal class Logger{internal List<string> Errors=new();internal void LogError(string s)=>Errors.Add(s);internal void LogInfo(string s){}}}
 internal static class ClayResource{internal const string ItemName="Quartermaster_RawClay";}
 internal static class ApothecaryArt {internal static void Initialize(GameObject p){}internal static Sprite Icon(string s)=>new();internal static void Item(GameObject p,string s){}internal static void Cabinet(GameObject p,ApothecaryDefinition d){}}
 internal class ApothecaryDisplay{}
 internal static class BuildMenuCategory {internal static HashSet<string> Registered=new();internal static void Register(string s)=>Registered.Add(s);}
 internal static class ItemStacks {internal static int Applied;internal static void Apply(ItemDrop.ItemData.SharedData s){Applied++;s.m_maxStackSize=500;}}
 internal static class ContainerRegistry {internal static string PrefabName(Container c)=>c.name;internal static ChestSettings GetSettings(Container c)=>c.Settings;}
 internal static class InventoryTransfers {internal static string ItemId(ItemDrop.ItemData i)=>i.m_dropPrefab.name;}
}

public class Skills {public enum SkillType {None,Bows,Crossbows,Spears,Polearms,ElementalMagic,BloodMagic,Fishing,Swords,Axes,Knives,Clubs,Pickaxes}}

namespace Quartermaster { internal static class MeadCabinet { internal const string Prefab="Quartermaster_MeadCabinet"; internal static bool IsMead(string id)=>id.StartsWith("Mead")&&!id.StartsWith("MeadBase"); internal static bool IsMead(ItemDrop.ItemData item)=>item!=null&&IsMead(item.m_dropPrefab.name); } }

namespace Quartermaster { internal static class DrawerFurniture { internal const string DrawerPrefab="Quartermaster_apothecary_chest_drawer"; internal static readonly ApothecaryDefinition Definition=new(){Prefab=DrawerPrefab,Columns=6,Rows=4}; } }

public class Inventory { public string Name; }
namespace Quartermaster {
 internal static class ExternalStorageCompatibility {
  internal static ExternalStorageRules Rules=new(null,null);
  internal static bool Recognizes(Container c)=>c&&Rules.Recognizes(c.name,c.m_name??"");
  internal static IEnumerable<string> Assigned(Container c)=>Rules.Assigned(c.name,c.m_name??"");
  internal static bool Allows(Inventory inv,ItemDrop.ItemData item)=>Rules.Allows(inv.Name??"",InventoryTransfers.ItemId(item));
 }
}
