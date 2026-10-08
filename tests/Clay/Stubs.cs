using UnityEngine;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:Object {public string name;public ItemDrop Item=new();public Pickable Pick=new();public ZNetView View=new();public GameObject(string n){name=n;}public T GetComponent<T>()where T:class => (typeof(T)==typeof(ItemDrop)?(object)Item:typeof(T)==typeof(Pickable)?Pick:View)as T;}
 public class Sprite:Object {}
}
public class ItemDrop {public ItemData m_itemData=new();public class ItemData {public GameObject m_dropPrefab;public int m_stack;public SharedData m_shared=new();public class SharedData {public string m_name,m_description;public Sprite[] m_icons;public float m_weight;public int m_maxStackSize,m_value;public bool m_teleportable;}}}
public class Pickable:UnityEngine.Object {public GameObject m_itemPrefab,m_hideWhenPicked;public string m_overrideName;public int m_amount,m_minAmountScaled;public bool m_dontScale,m_defaultPicked,m_defaultEnabled,m_harvestable;public float m_respawnTimeMinutes,m_respawnTimeInitMin,m_respawnTimeInitMax,m_spawnOffset,m_hoverOffset;public DropTable m_extraDrops;}
public class ZNetView {public bool m_syncInitialScale;}
public class DropTable {}
public class Heightmap { [Flags]public enum Biome {Meadows=1,BlackForest=2,Swamp=4,Mountain=8,Plains=16};public enum BiomeArea {Everything}}
namespace Jotunn.Configs {public class VegetationConfig {public Heightmap.Biome Biome;public Heightmap.BiomeArea BiomeArea;public float Min,Max,MinAltitude,MaxAltitude,MinTilt,MaxTilt,GroupRadius,ScaleMin,ScaleMax,GroundOffset;public int GroupSizeMin,GroupSizeMax;public bool BlockCheck,ForcePlacement;}}
namespace Jotunn.Entities {public class CustomItem {public GameObject ItemPrefab;public CustomItem(GameObject p,bool fix){ItemPrefab=p;}}public class CustomVegetation {public GameObject Prefab;public Jotunn.Configs.VegetationConfig Config;public CustomVegetation(GameObject p,bool fix,Jotunn.Configs.VegetationConfig c){Prefab=p;Config=c;}}}
namespace Jotunn.Managers {
 public class PrefabManager {public static PrefabManager Instance=new();public static event Action OnVanillaPrefabsAvailable;public List<(string name,string source)> Clones=new();public GameObject CreateClonedPrefab(string name,string source){Clones.Add((name,source));return new(name);}public static void Fire()=>OnVanillaPrefabsAvailable?.Invoke();}
 public class ItemManager {public static ItemManager Instance=new();public List<Jotunn.Entities.CustomItem> Items=new();public bool AddItem(Jotunn.Entities.CustomItem i){Items.Add(i);return true;}}
 public class ZoneManager {public static ZoneManager Instance=new();public List<Jotunn.Entities.CustomVegetation> Entries=new();public bool AddCustomVegetation(Jotunn.Entities.CustomVegetation v){Entries.Add(v);return true;}}
 public class LocalizationManager {public static LocalizationManager Instance=new();public Dictionary<string,string> Words;public LocalizationManager GetLocalization()=>this;public void AddTranslation(string lang,Dictionary<string,string>w){Words=w;}}
}
namespace Quartermaster {
 public static class Plugin {public static Logger Log=new();public class Logger {public List<string> Errors=new();public void LogError(string s)=>Errors.Add(s);public void LogInfo(string s){}}}
 internal static class ClayModel {internal static List<(GameObject prefab,bool patch)> Replaced=new();internal static GameObject Replace(GameObject p,bool patch){Replaced.Add((p,patch));return new("original clay art");}internal static Sprite Icon()=>new();}
 internal static class ItemStacks {public static int Applied;public static void Apply(ItemDrop.ItemData.SharedData s){Applied++;s.m_maxStackSize=500;}}
}
