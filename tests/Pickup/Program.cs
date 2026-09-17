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
 object[] before={player,new GameObject{Component=drop},null};Invoke("BeforePickup",before);
 drop.m_itemData.m_stack-=removed;Invoke("AfterPickup",player,success,before[2]);
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
foreach(var count in new[]{0,2}){
 bool rejected=false;
 try{((IEnumerable<CodeInstruction>)Invoke("FilterAutomatic",Enumerable.Range(0,count).Select(_=>new CodeInstruction(OpCodes.Ldfld,field)))).ToList();}
 catch(InvalidOperationException){rejected=true;}
 Check(rejected,"Changed game eligibility shape fails safely");
}
Console.WriteLine($"{checks} pickup filter checks passed.");
