using System.Reflection;
using UnityEngine;
using Quartermaster;
int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
void Lifecycle(OwlHover owl,string name)=>typeof(OwlHover).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(owl,null);
var player=Player.m_localPlayer=new Player();player.transform.position=new(0,.5f,0);GameCamera.instance.transform.position=new(0,.5f,-2);
var hud=new Hud();var owl=new OwlHover{Owner=new DepositGull()};owl.transform.position=new(0,0,2);Lifecycle(owl,"OnEnable");
string Draw(string original=""){hud.m_hoverName.text=original;OwlHover.Draw(hud,player);return hud.m_hoverName.text;}
Check(Draw().Contains("Resting on the Deposit Chest"),"crosshair aimed at owl reports actual owner status");
Check(Draw("Deposit Chest [E] Open").EndsWith("Deposit Chest [E] Open"),"native chest controls remain visible and untouched");
owl.Owner.HoverStatus="Sorting deposits";Check(Draw().Contains("Sorting deposits"),"status refreshes without recreating hover target");
var wall=new Collider();Physics.Obstacles=new[]{new RaycastHit{collider=wall,distance=1}};Check(Draw("Wall")=="Wall","wall occludes owl and native label is preserved");
wall.transform.Parent=player.transform;Check(Draw().Contains("Quartermaster Owl"),"own player collider does not occlude owl");
Physics.Obstacles=Enumerable.Repeat(new RaycastHit{collider=wall,distance=1},32).ToArray();Check(Draw()=="","saturated hit buffer does not risk wall penetration");Physics.Obstacles=Array.Empty<RaycastHit>();
owl.transform.position=new(3,0,2);Check(Draw()=="","looking beside owl leaves native hover alone");
owl.transform.position=new(0,0,8);Check(Draw()=="","distant owl respects player interaction range");
owl.transform.position=new(0,0,2);InventoryGui.Visible=true;Check(Draw()=="","inventory suppresses owl hover");InventoryGui.Visible=false;
TextViewer.instance.Visible=true;Check(Draw()=="","book text viewer suppresses hover");TextViewer.instance.Visible=false;
ParticleMist.Blocked=true;Check(Draw()=="","mist blocks owl hover");ParticleMist.Blocked=false;
Plugin.Enabled.Value=false;Check(Draw()=="","disabled mod has no hover");Plugin.Enabled.Value=true;
owl.transform.lossyScale=new(.05f,.05f,.05f);Check(Draw()=="","recall shrink does not leave a hover target");owl.transform.lossyScale=new(1,1,1);
var farther=new OwlHover{Owner=new DepositGull{HoverStatus="Far owl"}};farther.transform.position=new(0,0,3);Lifecycle(farther,"OnEnable");Check(!Draw().Contains("Far owl"),"nearest owl wins");
Lifecycle(owl,"OnDisable");Check(Draw().Contains("Far owl"),"hidden or destroyed owl unregisters its target");Lifecycle(farther,"OnDisable");Check(Draw()=="","no stale label after all owls leave");
Console.WriteLine($"PASS: {checks} owl hover targeting and visibility regressions.");
