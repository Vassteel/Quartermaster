using System.Reflection;
using System.Reflection.Emit;
using Quartermaster;
namespace UnityEngine {
 public class Object {public string name="test";public static implicit operator bool(Object o)=>o!=null;}
 public class Component:Object {public Transform transform=new();public bool IsPlant;public T GetComponent<T>() where T:new()=>IsPlant&&typeof(T)==typeof(Plant)?new T():default;}
 public class GameObject:Object {public Transform transform=new();}
 public class Transform {public Vector3 position;public Quaternion rotation;}
 public struct Vector3 {public float x,y,z;public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(){x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a-b).sqrMagnitude);}
 public struct Quaternion {public static float Angle(Quaternion a,Quaternion b)=>0;}
 public static class Time {public static float unscaledTime;}
}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true)]public class HarmonyPatch:Attribute{public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}public HarmonyPatch(Type t,string n,Type[] args){}}
 public class HarmonyPrefix:Attribute{}public class HarmonyFinalizer:Attribute{}public class HarmonyTranspiler:Attribute{}
 public class HarmonyPriority:Attribute {public HarmonyPriority(int p){}}public static class Priority{public const int First=800;}
 public static class AccessTools {const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;public static MethodInfo Method(Type t,string n)=>t.GetMethod(n,Flags);public static MethodInfo Method(Type t,string n,Type[] args)=>t.GetMethod(n,Flags,null,args,null);public static FieldInfo Field(Type t,string n)=>t.GetField(n,Flags);public static PropertyInfo Property(Type t,string n)=>t.GetProperty(n,Flags);}
 public class Harmony {public void Patch(MethodInfo original,HarmonyMethod transpiler){}}
 public class HarmonyMethod {public HarmonyMethod(Type t,string n){}}
 public class CodeInstruction {public OpCode opcode;public object operand;public CodeInstruction(CodeInstruction c){opcode=c.opcode;operand=c.operand;}public bool Calls(MethodInfo m)=>Equals(operand,m);}
}
public struct Vector2i {public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}}
public static class Game {public static int m_worldLevel;}
public class ItemDrop:UnityEngine.Object {
 public UnityEngine.GameObject gameObject=new();public ItemData m_itemData;
 public class SharedData {public string m_name;public int m_maxStackSize=50,m_maxQuality=1;public bool m_useDurability;}
 public class ItemData {public UnityEngine.GameObject m_dropPrefab;public SharedData m_shared;public int m_stack,m_quality=1,m_variant,m_worldLevel;public long m_crafterID;public string m_crafterName="";public float m_durability;public bool m_cheated,m_equipped;public Vector2i m_gridPos;public Dictionary<string,string> m_customData=new();public ItemData Clone(){var c=(ItemData)MemberwiseClone();c.m_customData=new(m_customData);return c;}public bool IsSameType(ItemData b)=>m_shared.m_name==b.m_shared.m_name;}
}
public class Inventory {
 readonly List<ItemDrop.ItemData> items=new();readonly int width,height;
 public Inventory(string name,object icon,int w,int h){width=w;height=h;}
 public List<ItemDrop.ItemData> GetAllItems()=>items;public int GetWidth()=>width;public int GetHeight()=>height;public int NrOfItems()=>items.Count;public bool ContainsItem(ItemDrop.ItemData item)=>items.Contains(item);
 public int CountItems(string name,int q,bool world)=>InventoryTransfers.CountType(this,name,q,world);
 public ItemDrop.ItemData GetItem(string name,int q,bool equipped)=>items.FirstOrDefault(i=>i.m_shared.m_name==name&&(q<0||i.m_quality==q));
 public bool RemoveItem(ItemDrop.ItemData item,int count){if(!items.Contains(item)||item.m_stack<count)return false;item.m_stack-=count;if(item.m_stack==0)items.Remove(item);return true;}
 public void RemoveItem(string name,int count,int q,bool world)=>InventoryTransfers.Remove(this,name,count,q,world);
 private void Changed(bool a,bool b){}
}
public static class ZDOVars {public const int s_inUse=1;}
public class ZDO {public int m_uid,InUse;public List<ItemDrop.ItemData> Saved;public int GetInt(int key)=>InUse;}
public class ZNetView:UnityEngine.Object {public ZDO z=new();public bool Unavailable,Owner=true;public ZDO GetZDO()=>z;}
public class Container:UnityEngine.Component {public Inventory Inventory=new("chest",null,8,4);public ZNetView View=new();public bool Open;public bool Supply=true,Denied;public int Saves;public bool IsInUse()=>Open;public Inventory GetInventory()=>Inventory;private void Save(){Saves++;View.z.Saved=Inventory.GetAllItems().Select(i=>i.Clone()).ToList();}}
public class Recipe:UnityEngine.Object {public bool m_requireOnlyOneIngredient;public Piece.Requirement[] m_resources;}
public class Plant:UnityEngine.Object {}
public class Pickable:UnityEngine.Object {}
public class Piece:UnityEngine.Component {
 public Requirement[] m_resources;public GlobalKeys FreeBuildKey()=>GlobalKeys.NoCraftCost;
 public class Requirement {public ItemDrop m_resItem;public int m_amount,m_amountPerLevel;public bool m_upgraderResource;public int GetAmount(int level)=>level<=1?m_amount:m_amountPerLevel*(level-1);}
}
public class CraftingStation:UnityEngine.Object {public bool m_upgrader;}
public enum GlobalKeys{NoCraftCost}
public class ZoneSystem {public static ZoneSystem instance=new();public bool GetGlobalKey(GlobalKeys k)=>false;}
public class MessageHud {public enum MessageType {TopLeft}}
public class Player:UnityEngine.Component {
 public enum RequirementMode {CanBuild}
 public static Player m_localPlayer;public Inventory Inventory=new("player",null,8,4);public CraftingStation Station;public bool m_noPlacementCost,Dead;public UnityEngine.GameObject m_placementGhost=new();public Piece Selected;public int Built;public List<string> Messages=new();
 public Inventory GetInventory()=>Inventory;public CraftingStation GetCurrentCraftingStation()=>Station;public bool IsDead()=>Dead;public bool IsTeleporting()=>false;public bool NoCostCheat()=>false;public Piece GetSelectedPiece()=>Selected;public void Message(MessageHud.MessageType t,string s)=>Messages.Add(s);
 public bool HaveRequirements(Recipe r,bool discover,int level,int amount)=>r.m_resources.Where(x=>x.m_upgraderResource==(Station?.m_upgrader??false)).All(x=>CraftingPatches.CountItemsIncludingWarehouse(Inventory,x.m_resItem.m_itemData.m_shared.m_name,-1,true)>=x.GetAmount(level)*amount);
 public bool HaveRequirements(Piece p,Player.RequirementMode m)=>p.m_resources.All(x=>CraftingPatches.CountItemsIncludingWarehouse(Inventory,x.m_resItem.m_itemData.m_shared.m_name,-1,true)>=x.m_amount);
 public ItemDrop.ItemData GetFirstRequiredItem(Inventory inv,Recipe r,int level,ref int amount,ref int extra,int multiplier){foreach(var x in r.m_resources){amount=x.GetAmount(level)*multiplier;if(CraftingPatches.CountItemsIncludingWarehouse(inv,x.m_resItem.m_itemData.m_shared.m_name,1,true)>=amount)return CraftingPatches.FindIngredientIncludingWarehouse(inv,x.m_resItem.m_itemData.m_shared.m_name,1,false);}return null;}
 public Action<Piece> PlaceExtras;public bool TryPlacePiece(Piece p){Built++;PlaceExtras?.Invoke(p);return true;}
}
public class InventoryGui:UnityEngine.Object {
 public Recipe m_craftRecipe;public ItemDrop.ItemData m_craftUpgradeItem;public bool m_multiCrafting;public int m_multiCraftAmount=1;public int Crafted;public static bool Visible=true;
 public static bool IsVisible()=>Visible;
 // Simulate Harmony entry/finalizer around native's create-then-consume order.
 public void DoCrafting(Player p){var args=new object[]{this,p,false};try {if(!(bool)Harness.Call("ReserveCraft",args))return;int level=m_craftUpgradeItem==null?1:m_craftUpgradeItem.m_quality+1;int amount=m_multiCrafting?m_multiCraftAmount:1;if(!p.HaveRequirements(m_craftRecipe,false,level,amount))return;Crafted++;if(m_craftUpgradeItem!=null)m_craftUpgradeItem.m_quality++;foreach(var r in m_craftRecipe.m_resources.Where(x=>x.m_upgraderResource==(p.Station?.m_upgrader??false)))p.Inventory.RemoveItem(r.m_resItem.m_itemData.m_shared.m_name,r.GetAmount(level)*amount,-1,true);}finally{Harness.Call("CraftScope",new object[]{null,args[2]});}}
}
static class Harness {public static object Call(string n,object[] args)=>typeof(SharedCrafting).GetMethod(n,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
namespace Quartermaster {
 static class Plugin {public static Setting<bool> Enabled=new(true),CraftFromContainers=new(true);public static Setting<float> CraftRange=new(100);public static Log Log=new();}
 class Setting<T>{public T Value;public Setting(T v){Value=v;}}
 class Log {public void LogInfo(string text){}public void LogWarning(string text){}public void LogError(string text)=>throw new Exception(text);}
 static class ContainerRegistry {public static List<Container> All=new();public static ZNetView GetView(Container c)=>c.View;public static bool IsUsable(Container c,bool open)=>!c.Open&&!c.View.Unavailable&&c.View.Owner;public static bool CanObserve(Container c)=>c&&!c.View.Unavailable&&!c.Denied;public static void Refresh(Container c){if(c.View.z.Saved!=null){c.Inventory.GetAllItems().Clear();c.Inventory.GetAllItems().AddRange(c.View.z.Saved.Select(i=>i.Clone()));}}public static Settings GetSettings(Container c)=>new(){CraftingSupply=c.Supply};public static Inventory SafeInventory(Container c)=>c.Inventory;}
 class Settings {public bool CraftingSupply;}
 static class WarehouseService {public static IEnumerable<Container> CraftStores(Player p,bool observe)=>CraftStorageAccess.Nearby(p);public static int CountAvailableNearPlayer(string n,int q,bool w)=>CraftStores(Player.m_localPlayer,true).Sum(c=>InventoryTransfers.CountType(c.Inventory,n,q,w));}
 static class SharedChests { public static HashSet<Container> ViewedBy=new(); public static bool Viewed(Container c)=>ViewedBy.Contains(c); }
 static class ChestOwnership { public static List<Container> Touched=new(); public static void Touch(IEnumerable<Container> sources)=>Touched.AddRange(sources); }
 static class NativeStorageAccess {
 public static bool Start(string name,IEnumerable<ZNetView> views,Func<bool> valid,Action work,Action<string> result=null,Action afterRelease=null){if(views.Any(v=>v.Unavailable)||!valid())return false;work();result?.Invoke("Saved");afterRelease?.Invoke();return true;}
 }
}

namespace BepInEx.Bootstrap {
 public class PluginInfo {public object Instance;}
 public static class Chainloader {public static Dictionary<string,PluginInfo> PluginInfos=new();}
}
namespace Advize_PlantEasily {
 public class PlantEasily {}
 public class ModConfig {public bool ModActive=>true;public bool PreventPartialPlanting {get;set;}public bool PreventInvalidPlanting {get;set;}=true;}
 public static class ModContext {public static ModConfig config=new();}
 public static class ModUtils {public static bool HoldingCultivator=>true;}
 public static class PlacementController {public static bool IsPlanting {get;set;}}
 public enum Status {Healthy,LackResources,NotCultivated,WrongBiome,NoSpace}
 public static class GhostGrid {
  public static List<UnityEngine.GameObject> ExtraGhosts=new();
  public static List<Status> GhostPlacementStatus=new();
  public static int MaxActiveGhosts=>ExtraGhosts.Count;
 }
 public static class GhostStatus {
  public static Dictionary<UnityEngine.GameObject,Status> Geometry=new();
  public static Status EvaluateStatus(UnityEngine.GameObject ghost,Status baseStatus=Status.Healthy)=>Geometry.TryGetValue(ghost,out var status)?status:baseStatus;
 }
 public static class InteractPatches {private static void Prefix(Pickable item,bool picked){}}
}
