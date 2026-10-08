using Quartermaster;
using Jotunn.Managers;
using UnityEngine;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
BuildPieces.Initialize();PrefabManager.Fire();
Check(Plugin.Log.Errors.Count==0,string.Join("\n",Plugin.Log.Errors));
var old=PrefabManager.Instance.GetPrefab(ChestDefaults.DepositPrefab);
Check(old!=null&&PrefabManager.Instance.Registered.Contains(ChestDefaults.DepositPrefab),"Saved chest network prefab still registered");
Check(old.GetComponent<Container>().m_width==8&&old.GetComponent<Container>().m_height==4,"Native blackmetal capacity retained for existing chests");
Check(PrefabManager.Instance.Clones.Single(x=>x.id==ChestDefaults.DepositPrefab).source=="piece_chest_blackmetal","Old chest retains native prefab behavior");
Check(PieceManager.Instance.Pieces.Count==1&&PieceManager.Instance.Pieces[0].Prefab.name==OwlLedger.PrefabName,"Only ledger has a new Hammer recipe");
Check(PieceManager.Instance.Pieces[0].Config.Category=="Quartermaster","Ledger stays in Quartermaster category");
PrefabManager.Fire();Check(PieceManager.Instance.Pieces.Count==1&&PrefabManager.Instance.Registered.Count==1,"Registration unsubscribes and cannot duplicate entries");
Console.WriteLine($"PASS: {checks} saved chest and build-recipe registration checks.");
namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;public static void DestroyImmediate(Object o){}}
 public class GameObject:Object {readonly Dictionary<Type,object> components=new();public T GetComponent<T>() where T:new(){if(!components.ContainsKey(typeof(T)))components[typeof(T)]=new T();return (T)components[typeof(T)];}public T AddComponent<T>() where T:new()=>GetComponent<T>();}
}
public class Container {public string m_name;public int m_width=8,m_height=4;}
public class Widget:UnityEngine.Object {public GameObject gameObject=new();}
public class Sign:UnityEngine.Object {public Widget m_textWidget;}
namespace Jotunn.Configs {
 public class RequirementConfig {public RequirementConfig(string id,int quantity,int per,bool recover){}}
 public class PieceConfig {public string Name,Description,PieceTable,Category,CraftingStation;public object Icon;public RequirementConfig[] Requirements;}
}
namespace Jotunn.Entities {
 public class CustomPrefab {public GameObject Prefab;public CustomPrefab(GameObject p,bool fix){Prefab=p;}}
 public class CustomPiece {public GameObject Prefab;public Jotunn.Configs.PieceConfig Config;public CustomPiece(GameObject p,bool fix,Jotunn.Configs.PieceConfig c){Prefab=p;Config=c;}}
}
namespace Jotunn.Managers {
 public class PrefabManager {
  public static PrefabManager Instance=new();public static event Action OnVanillaPrefabsAvailable;public static void Fire()=>OnVanillaPrefabsAvailable?.Invoke();
  readonly Dictionary<string,GameObject> prefabs=new();public List<(string id,string source)> Clones=new();public HashSet<string> Registered=new();
  public GameObject CreateClonedPrefab(string id,string source){Clones.Add((id,source));return new GameObject{name=id};}
  public void AddPrefab(Jotunn.Entities.CustomPrefab prefab){prefabs.Add(prefab.Prefab.name,prefab.Prefab);Registered.Add(prefab.Prefab.name);}
  public GameObject GetPrefab(string id)=>prefabs.GetValueOrDefault(id);
 }
 public class PieceManager {public static PieceManager Instance=new();public List<Jotunn.Entities.CustomPiece> Pieces=new();public bool AddPiece(Jotunn.Entities.CustomPiece piece){Pieces.Add(piece);return true;}}
}
namespace Quartermaster {
 class Plugin {internal static Logger Log=new();internal class Logger {internal List<string> Errors=new();internal void LogError(string message)=>Errors.Add(message);internal void LogInfo(string message){}}}
 class OwlLedger {internal const string PrefabName="piece_quartermaster_ledger";}
 static class ChestDefaults {internal const string DepositPrefab="quartermaster_deposit_chest";}
 static class LedgerModel {internal static void Build(GameObject prefab){}internal static object Icon()=>new();}
}
