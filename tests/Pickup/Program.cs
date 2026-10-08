using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Quartermaster;
using UnityEngine;
int checks=0;
void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
object Invoke(string name, params object[] arguments)=>typeof(PickupFilter).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,arguments);
ItemDrop Drop(string id)=>new(){m_itemData=new(){m_dropPrefab=new(){name=id}}};
void Pick(Player player,ItemDrop drop,bool success,int removed=0){
 object[] before={player,new GameObject{Component=drop},false,null};
 if((bool)Invoke("BeforePickup",before)) drop.m_itemData.m_stack-=removed;
 Invoke("AfterPickup",player,success,before[3]);
}
var local=Player.m_localPlayer=new Player();var remote=new Player();var resin=Drop("Resin");var wood=Drop("Wood");
Check(PickupFilter.AllowAutomatic(resin,local),"Unconfigured item collects normally");
Check(PickupFilter.SetIgnored(local,"Resin",true),"Block resin");
Check(!PickupFilter.SetIgnored(local,"Resin",true),"Duplicate block is idempotent");
Check(!PickupFilter.AllowAutomatic(resin,local)&&PickupFilter.AllowAutomatic(wood,local),"Only selected type is blocked");
Check(resin.m_autoPickup,"Shared drop stays eligible for other players");
Check(PickupFilter.AllowAutomatic(resin,remote),"Other player still collects resin");
Check(PickupFilter.Count(local)==1 && resin.m_itemData.m_stack==10,"Blocking does not consume items");
Plugin.Enabled.Value=false;Check(PickupFilter.AllowAutomatic(resin,local),"Disabling mod bypasses filter");
Pick(local,resin,true);Check(PickupFilter.IsIgnored(local,"Resin"),"Disabled feature leaves saved preference intact");Plugin.Enabled.Value=true;
resin.m_autoPickup=false;Check(!PickupFilter.AllowAutomatic(resin,remote),"Respect native no-pickup flag");resin.m_autoPickup=true;
Pick(local,resin,false);Check(PickupFilter.IsIgnored(local,"Resin"),"Failed manual pickup leaves blocked");
Pick(remote,resin,true);Check(PickupFilter.IsIgnored(local,"Resin"),"Remote pickup cannot clear local preference");
object[] outer={local,null};Invoke("BeginAutomatic",outer);
Pick(local,resin,true);Check(PickupFilter.IsIgnored(local,"Resin"),"Automatic pickup never clears preference");
object[] inner={remote,null};Invoke("BeginAutomatic",inner);Invoke("EndAutomatic",inner[1]);
Pick(local,resin,true);Check(PickupFilter.IsIgnored(local,"Resin"),"Nested auto-pickup restores outer context");
Invoke("EndAutomatic",outer[1]);
Pick(local,resin,true);Check(!PickupFilter.IsIgnored(local,"Resin")&&local.Messages.Count==1,"Successful manual pickup restores type and gives feedback");
Check(!local.m_customData.ContainsKey(PickupFilter.SaveKey),"Empty list removes only its own save key");
PickupFilter.SetIgnored(local,"Resin",true);Pick(local,resin,false,1);
Check(!PickupFilter.IsIgnored(local,"Resin"),"Successful partial stack pickup also restores type");
local.m_customData["OtherMod"]="keep";
PickupFilter.SetIgnored(local,"Wood",true);PickupFilter.SetIgnored(local,"Resin",true);
Check(local.m_customData[PickupFilter.SaveKey]=="Resin\nWood","Stable exact prefab IDs are persisted");
var reloaded=new Player{m_customData=new(local.m_customData)};PickupFilter.Clear();Player.m_localPlayer=reloaded;
Check(!PickupFilter.AllowAutomatic(Drop("Wood"),reloaded),"Reloaded character keeps blocked types");
Check(PickupFilter.Count(remote)==0 && PickupFilter.Count(reloaded)==2,"Switching characters isolates lists");
reloaded.m_customData[PickupFilter.SaveKey]="Coal\nCoal\n\n";
Check(PickupFilter.Count(reloaded)==1 && PickupFilter.IsIgnored(reloaded,"Coal"),"External save reload invalidates cache and normalizes duplicates");
Check(!PickupFilter.SetIgnored(reloaded,"bad\nid",true)&&!PickupFilter.SetIgnored(reloaded,null,true),"Invalid identities cannot corrupt save");
Check(reloaded.m_customData["OtherMod"]=="keep","Other custom character data is preserved");
Check(PickupFilter.ItemId(new())==null,"Missing prefab fails open");
Player.m_localPlayer=null;Check(PickupFilter.AllowAutomatic(Drop("Coal"),remote),"Headless server has no local-player filtering");
var field=AccessTools.Field(typeof(ItemDrop),"m_autoPickup");
var marker=new DynamicMethod("labels",typeof(void),Type.EmptyTypes).GetILGenerator().DefineLabel();
var original=new CodeInstruction(OpCodes.Ldfld,field);original.labels.Add(marker);
var patched=((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",new List<CodeInstruction>{original})).ToList();
Check(patched.Count==2&&patched[0].opcode==OpCodes.Ldarg_0&&patched[0].labels.Contains(marker)&&patched[1].opcode==OpCodes.Call,"Transpiler preserves branch labels and supplies the acting player");
Check(original.opcode==OpCodes.Ldfld && Equals(original.operand,field),"Original Harmony input must not be mutated");
var replay=((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",(object)new[]{original})).ToList();
Check(replay.Count==2 && replay[1].Calls(AccessTools.Method(typeof(PickupFilter),"AllowAutomatic")),"Replaying the same input remains valid");
var empty=((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",(object)new[]{new CodeInstruction(OpCodes.Ret)})).ToList();
Check(empty.Count==1 && empty[0].opcode==OpCodes.Ret,"Replaced eligibility preserves incoming IL");
((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",(object)Array.Empty<CodeInstruction>())).ToList();
Check(Plugin.Log.Warnings==1,"Missing hook warns only once across rebuilds");
var multiple=((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",(object)new[]{original,original})).ToList();
Check(multiple.Count==4,"Multiple native eligibility checks are all filtered");

// Exercise real Harmony patch rebuilding, fallback, manual pickup and finalizers.
Player.m_localPlayer=local; PickupFilter.Clear(); PickupFilter.SetIgnored(local,"Resin",true);
PickupFilter.SetIgnored(local,"Wood",false);
local.Target=resin;remote.Target=resin;
var ours=new Harmony("tests.quartermaster.pickup");
var other=new Harmony("tests.replaced.pickup");
ours.CreateClassProcessor(typeof(PickupFilter)).Patch();
local.AutoPickup(0);Check(local.Received==0,"Patched automatic ignored pickup is blocked");
remote.AutoPickup(0);Check(remote.Received==1,"Patched remote automatic pickup is unaffected");
other.Patch(AccessTools.Method(typeof(Player),"AutoPickup"),transpiler:new HarmonyMethod(typeof(Replacement),"Rewrite"){priority=Priority.First});
local.AutoPickup(0);Check(local.Received==0,"Replaced eligibility still blocks ignored automatic pickup");
Replacement.BypassAvailability=true;local.AutoPickup(0);
Check(local.Received==0,"Final transfer guard blocks replacement that bypasses CanPickup");
Replacement.BypassAvailability=false;
Check(resin.m_autoPickup && resin.CanPickup(),"Fallback does not mutate shared drop or manual eligibility");
other.UnpatchAll(other.Id);other.Patch(AccessTools.Method(typeof(Player),"AutoPickup"),transpiler:new HarmonyMethod(typeof(Replacement),"Rewrite"){priority=Priority.First});
local.AutoPickup(0);Check(local.Received==0,"Repeated patch removal and rebuilding remains safe");
Plugin.Enabled.Value=false;local.AutoPickup(0);Check(local.Received==1,"Disabled mod leaves replacement auto pickup intact");Plugin.Enabled.Value=true;
local.Throw=true;try{local.AutoPickup(0);}catch(InvalidOperationException){}local.Throw=false;
local.Pickup(new GameObject{Component=resin});
Check(local.Received==2 && !PickupFilter.IsIgnored(local,"Resin"),"Finalizer restores manual pickup after exception");
PickupFilter.SetIgnored(local,"Resin",true);local.Target=wood;local.AutoPickup(0);
Check(local.Received==3 && PickupFilter.IsIgnored(local,"Resin"),"Allowed items work with replacement hook");
ours.UnpatchAll(ours.Id);other.UnpatchAll(other.Id);
var events=new List<string>();
PatchCleanup.Run(()=>{events.Add("unpatch");throw new Exception("third party patch");},()=>events.Add("restore"),message=>events.Add("log"));
Check(string.Join(",",events)=="unpatch,log,restore","Failed unpatch still restores stack state");
events.Clear();PatchCleanup.Run(()=>throw new Exception("unpatch"),()=>throw new Exception("restore"),message=>events.Add(message));
Check(events.Count==2 && events[0].Contains("unpatch") && events[1].Contains("restore"),"Both cleanup failures are reported without escaping");
Console.WriteLine($"{checks} pickup and cleanup checks passed, including real Harmony patch rebuilding.");

static class Replacement {
 public static bool BypassAvailability;
 public static bool Eligible(ItemDrop item)=>item.m_autoPickup;
 public static bool Available(ItemDrop item)=>BypassAvailability || item.CanPickup();
 public static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions){
  foreach(var input in instructions){var code=new CodeInstruction(input);if(code.LoadsField(AccessTools.Field(typeof(ItemDrop),"m_autoPickup"))){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(Replacement),"Eligible");}else if(code.Calls(AccessTools.Method(typeof(ItemDrop),"CanPickup"))){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(Replacement),"Available");}yield return code;}
 }
}
