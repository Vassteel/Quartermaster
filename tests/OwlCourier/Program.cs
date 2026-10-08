using Quartermaster;
using Quartermaster.Cosmetics;
using UnityEngine;
int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
Transform root=null;OwlCourier courier=null;Vector3 homePerch=Vector3.zero;
var seen=new HashSet<string>();
int walkingFrames=0;
void Reset()
{
    OwlCleanup.Request=false;seen.Clear();walkingFrames=0;homePerch=Vector3.zero;Time.time=0;Time.frameCount=0;OwlNavigation.GroundEnabled=true;OwlNavigation.FrontStand=false;GullToss.Targets.Clear();OwlLedger.Current=null;OwlWork.Jobs.Clear();OwlCleanup.Collected=0;OwlCleanup.Capacity=100;OwlCleanup.Access=true;
    OwlCleanup.Drop=new ItemDrop{transform={position=new Vector3(4,0,0)}};
    GullToss.Tosses=GullToss.Placed=GullToss.Carried=GullToss.Returned=0;Physics.BlockSight=false;
    OwlNavigation.Permit=(_,_)=>true;OwlNavigation.Hops=0;Plugin.OwlCollectDroppedItems.Value=true;
    root=new Transform();courier=new OwlCourier(new Container(),root,new QuartermasterOwl());
}
void Frames(float seconds,bool sorting=false)
{for(int f=0;f<(int)(seconds/Time.deltaTime);f++){Time.time+=Time.deltaTime;Time.frameCount++;courier.Tick(homePerch,Quaternion.identity,sorting);seen.Add(courier.Status);if(courier.Walking)walkingFrames++;}}
void FinishTrip(float budget=45){for(int i=0;i<(int)(budget/Time.deltaTime)&&courier.Away;i++)Frames(Time.deltaTime);Check(!courier.Away,"trip completes within its bounded walking-time allowance");}
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlCleanup.Request=true;Frames(1);
Check(courier.Away,"explicit request begins without idle wait or automatic collection enabled");
FinishTrip();Check(OwlCleanup.Collected==3,"requested trip keeps three-item limit");
Reset();OwlCleanup.Request=true;Frames(1);OwlCleanup.Request=false;Frames(1);FinishTrip();
Check(OwlCleanup.Collected==0,"cancelled request leaves ground items alone");
Reset();OwlCleanup.Drop.Count=0;OwlCleanup.Request=true;Frames(1);
Check(!OwlCleanup.Request&&!courier.Away,"empty requested area completes without repeated trips");
Reset();Frames(19);
Check(!courier.Away&&OwlCleanup.Collected==0,"idle cleanup waits at home for 20 seconds");
Check(courier.Status=="Resting on the Deposit Chest"&&courier.Workstation==null,"home status identifies its perch without a stale station");
Frames(2);FinishTrip();
Check(OwlCleanup.Collected==3&&OwlCleanup.Drop.Count==7,"one trip takes exactly three individual items");
Check(!courier.Away&&root.position.sqrMagnitude<.01f&&GullToss.Returned==3,"cleanup flies home with its cosmetic cargo");
Check(seen.Contains("Collecting dropped items")&&seen.Contains("Picking up dropped items")&&seen.Contains("Returning with collected items"),"cleanup status follows travel, gathering and cargo return");
Frames(10);Check(OwlCleanup.Collected==3,"next cleanup trip has another idle wait");
Reset();OwlWork.Jobs.Add(new(){InputPoint=new Vector3(5,0,0)});Frames(1);FinishTrip();
Check(GullToss.Tosses==6&&OwlCleanup.Collected==0,"active workstation gets six props while cleanup waits");
Check(!courier.Away,"work visit returns to its Deposit Chest");
Check(seen.Contains("Visiting")&&seen.Contains("Working at")&&seen.Contains("Returning to the Deposit Chest"),"work status follows actual station visit phases");
Check(courier.Workstation==null,"returned owl does not retain workstation hover label");
Reset();var job=new OwlWork{InputPoint=new Vector3(14,0,0)};OwlWork.Jobs.Add(job);Frames(1);job.Active=false;Frames(8);
Check(!courier.Away&&GullToss.Tosses==0,"a paused or finished target cancels its visit and returns home");
Reset();Frames(22);OwlCleanup.Capacity=0;Frames(10);
Check(OwlCleanup.Collected==0&&!courier.Away,"a chest filled during travel cancels collection");
Reset();Frames(22);OwlCleanup.Access=false;Frames(10);
Check(OwlCleanup.Collected==0&&!courier.Away,"lost access during travel leaves drops alone");
Reset();Frames(22);Physics.BlockSight=true;Frames(10);
Check(OwlCleanup.Collected==0&&!courier.Away,"a newly blocked pickup cannot reach through walls");
Reset();Frames(22);OwlCleanup.Drop.Destroyed=true;Frames(10);
Check(OwlCleanup.Collected==0&&!courier.Away,"another player taking the drop cancels cleanup");
Reset();Frames(25);int before=OwlCleanup.Collected;courier.Reset();Frames(2);
Check(OwlCleanup.Collected==before,"culling/reset never recollects cosmetic carried items");
Reset();OwlNavigation.GroundEnabled=false;OwlWork.Jobs.Add(new(){InputPoint=new Vector3(14,0,0)});Frames(1.5f);
OwlNavigation.Permit=(_,_)=>false;Frames(12);
Check(!courier.Away&&root.position.sqrMagnitude<.01f,"closed routes eventually recover at home");
Check(seen.Contains("Finding a way back to the Deposit Chest"),"blocked route reports recovery");
Check(OwlNavigation.Hops>0,"blocked movement tries a clearance-checked hop before recovery");
Reset();Plugin.OwlCollectDroppedItems.Value=false;Frames(45);
Check(OwlCleanup.Collected==0&&!courier.Away,"disabled cleanup never starts a trip");
Reset();OwlWork.Jobs.Add(new(){InputPoint=new Vector3(5,0,0)});Frames(20,true);
Check(GullToss.Tosses>0,"continuous chest sorting cannot starve workstation visits");
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlLedger.Current=new(){transform={position=new Vector3(4,1,0)}};
Frames(55);
Check(seen.Contains("Visiting the ledger")&&seen.Contains("Reading the ledger"),"book visit and reading statuses follow the animation lifecycle");
Check(OwlLedger.Current.Pages==1,"occasional ledger visit turns one page");
Check(!courier.Away&&OwlCleanup.Collected==0&&GullToss.Tosses==0,"reading returns home without moving any real items");
OwlWork.Jobs.Add(new(){InputPoint=new Vector3(5,0,0)});Frames(15);
Check(GullToss.Tosses>0,"work resumes after reading the ledger");
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlLedger.Current=new(){transform={position=new Vector3(4,1,0)}};
Frames(18);for(int i=0;i<1300&&!courier.Away;i++)Frames(.04f);
OwlLedger.Current.Destroyed=true;Frames(14);
Check(!courier.Away,"destroyed ledger cancels an owl visit safely");

// A whole round must finish even when it takes longer than the old 24-second cutoff.
Reset();Plugin.OwlCollectDroppedItems.Value=false;
for(int i=1;i<=5;i++)OwlWork.Jobs.Add(new(){InputPoint=new Vector3(i*7,0,0)});
Frames(1);float roundStarted=Time.time;
for(int i=0;i<6000&&courier.Away;i++)Frames(Time.deltaTime);
Check(!courier.Away&&Time.time-roundStarted>24,"long rounds finish and eventually return home");
Check(OwlWork.Jobs.All(j=>GullToss.Targets.Count(p=>Vector3.Distance(p,j.InputPoint)<.01f)==6),"all five active stations receive one complete visit before home");
Check(walkingFrames>300,"supported station rounds spend substantial time walking");

Reset();Plugin.OwlCollectDroppedItems.Value=false;homePerch=new Vector3(0,2,0);root.position=homePerch;
OwlWork.Jobs.Add(new(){InputPoint=new Vector3(10,0,0)});Frames(1);
for(int i=0;i<3000&&courier.Away;i++)Frames(Time.deltaTime);
Check(!courier.Away&&Vector3.Distance(root.position,homePerch)<.01f&&walkingFrames>100,"raised chest uses short flights joined by ground travel in both directions");
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlWork.Jobs.Add(new(){InputPoint=new Vector3(12,0,0)});Frames(2);
Check(courier.Walking,"owl starts the supported trip on foot");
OwlNavigation.Permit=(_,_)=>false;Frames(12);
Check(!courier.Away&&root.position.sqrMagnitude<.01f,"new obstruction during walking eventually recalls safely");

// Completing a target during travel should continue the round, not abandon the others.
Reset();Plugin.OwlCollectDroppedItems.Value=false;
var first=new OwlWork{InputPoint=new Vector3(7,0,0)};var second=new OwlWork{InputPoint=new Vector3(10,0,0)};
OwlWork.Jobs.Add(first);OwlWork.Jobs.Add(second);Frames(1);first.Active=false;Frames(25);
Check(GullToss.Targets.Count(p=>Vector3.Distance(p,second.InputPoint)<.01f)==6,"finished station does not cancel remaining work");

Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlLedger.Current=new(){transform={position=new Vector3(4,1.3f,0)}};
for(int i=1;i<=5;i++)OwlWork.Jobs.Add(new(){InputPoint=new Vector3(i*7,0,0)});
Frames(100);
Check(OwlLedger.Current.Pages>0,"busy workstation rounds still include a due ledger visit");
Check(GullToss.Targets.Any(p=>p.x>=21),"station round resumes after the book visit");

// Cosmetic reports survive a complete trip and keep the original item samples.
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlWork.Jobs.Add(new(){InputPoint=new Vector3(12,0,0)});
var replay=new OwlSortReplay<string>();Frames(1);replay.Report("Resin");replay.Report("Meat");replay.Pause();for(int i=0;i<3000&&courier.Away;i++)Frames(Time.deltaTime);
Check(!courier.Away&&replay.Pending,"away sorting remains pending on return");
var props=new List<string>();
for(int i=0;i<300&&replay.Pending;i++)
{
    Time.time+=Time.deltaTime;replay.Resume(Time.time);replay.BeginNext(Time.time);
    if(replay.Take(Time.time))props.Add(replay.Sample);
}
Check(props.SequenceEqual(new[]{"Resin","Resin","Resin","Meat","Meat","Meat"}),"each deferred batch keeps its item and all three throws");
replay.Report("Wood");replay.BeginNext(Time.time);Time.time+=.4f;Check(replay.Take(Time.time),"first throw starts before travel");
replay.Pause();Time.time+=100;Check(!replay.Take(Time.time)&&replay.Remaining==2,"travel pauses a partially played batch without consuming it");
replay.Resume(Time.time);Check(!replay.Take(Time.time),"resuming spaces the throws instead of bursting the backlog");
Time.time+=.4f;Check(replay.Take(Time.time)&&replay.Remaining==1,"partial batch resumes at the next throw");
replay.Clear();Check(!replay.Pending,"disabled decoration clears its cosmetic queue");
for(int i=0;i<1000;i++)replay.Report(i.ToString());
int batches=0;string last="";
while(replay.Pending)
{
    if(replay.BeginNext(Time.time)){batches++;last=replay.Sample;}
    Time.time+=1;replay.Take(Time.time);
}
Check(batches==33&&last=="999","long absences coalesce excess reports while preserving the latest activity");

// Furniture handling shares the trip lifecycle; no six-throw machine loop.
Reset();Plugin.OwlCollectDroppedItems.Value=false;
var furniture=new ApothecaryDisplay();
var jarJob=new OwlWork{Machine=furniture,Furniture=furniture,InputPoint=new Vector3(4,0,0),Fuel=null};
OwlWork.Jobs.Add(jarJob);Frames(20);
Check(furniture.Animations>0&&furniture.Completions==1,"cabinet visit drives and resets articulated props");
Check(GullToss.Tosses==1,"cabinet performs one representative deposit, not the smelter loop");
Reset();Plugin.OwlCollectDroppedItems.Value=false;furniture=new ApothecaryDisplay();
jarJob=new OwlWork{Machine=furniture,Furniture=furniture,Retrieving=true,InputPoint=new Vector3(4,0,0),Fuel=null};
OwlWork.Jobs.Add(jarJob);Frames(20);
Check(GullToss.Placed==1&&GullToss.Tosses==0,"retrieval moves one prop from the furniture to the beak");
Reset();Plugin.OwlCollectDroppedItems.Value=false;furniture=new ApothecaryDisplay();
jarJob=new OwlWork{Machine=furniture,Furniture=furniture,InputPoint=new Vector3(4,0,0),Fuel=null};
OwlWork.Jobs.Add(jarJob);Frames(2);courier.Reset();
Check(furniture.Completions==1,"reset during a furniture trip always resets moving props");

foreach(var style in new[]{FurnitureHandling.Lumber,FurnitureHandling.Ingot,FurnitureHandling.Bin,FurnitureHandling.Hide,FurnitureHandling.Textile,FurnitureHandling.Feather,FurnitureHandling.Bone,FurnitureHandling.Masonry,FurnitureHandling.Pantry,FurnitureHandling.Hanging,FurnitureHandling.Grain})
{
    Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlNavigation.FrontStand=true;furniture=new ApothecaryDisplay{Handling=style};
    var bulkJob=new OwlWork{Machine=furniture,Furniture=furniture,InputPoint=new Vector3(4,.5f,0),Fuel=null};
    OwlWork.Jobs.Add(bulkJob);
    for(int f=0;f<900&&furniture.Animations==0;f++)Frames(Time.deltaTime);
    Check(walkingFrames>0&&furniture.Animations>0,"low open-face furniture is reached by walking before handling");
    Frames(20);
    Check(furniture.Completions==1&&furniture.Animations>0,"bulk furniture finishes one cosmetic visit");
    Check((style==FurnitureHandling.Bin||style==FurnitureHandling.Bone||style==FurnitureHandling.Grain)?GullToss.Tosses==1&&GullToss.Placed==0:GullToss.Placed==1&&GullToss.Tosses==0,"racks place carefully while bins toss once");
    Check((style==FurnitureHandling.Bin||style==FurnitureHandling.Bone||style==FurnitureHandling.Grain)?GullToss.Carried==0:GullToss.Carried==1,"rack delivery visibly holds one prop before placing it");
    Check(!courier.Away,"short bulk performance returns through normal tour lifecycle");
}
foreach(bool high in new[]{false,true})
{
    Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlNavigation.FrontStand=high;
    furniture=new ApothecaryDisplay{Handling=FurnitureHandling.Lumber};
    var approachJob=new OwlWork{Machine=furniture,Furniture=furniture,InputPoint=new Vector3(4,high?2.5f:.5f,0),Fuel=null};OwlWork.Jobs.Add(approachJob);
    for(int f=0;f<900&&furniture.Animations==0;f++)Frames(Time.deltaTime);
    Check(furniture.Animations>0&&walkingFrames==0,"high shelf or rear-only ground candidate uses the clear authored flight approach");
    Frames(20);Check(furniture.Completions==1,"flight fallback completes the same furniture interaction");
}
foreach(var style in new[]{FurnitureHandling.Hide,FurnitureHandling.Textile,FurnitureHandling.Feather,FurnitureHandling.Bone,FurnitureHandling.Masonry,FurnitureHandling.Pantry,FurnitureHandling.Hanging,FurnitureHandling.Grain})
{
    Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlNavigation.FrontStand=true;furniture=new ApothecaryDisplay{Handling=style};
    var retrieval=new OwlWork{Machine=furniture,Furniture=furniture,InputPoint=new Vector3(4,.5f,0),Fuel=null,Retrieving=true};OwlWork.Jobs.Add(retrieval);Frames(25);
    Check(furniture.Completions==1&&GullToss.Placed==1&&GullToss.Tosses==0&&GullToss.Carried==0,"soft furniture retrieval uses one inward prop movement and no deposit");
    Check(GullToss.Targets.Count==1&&GullToss.Targets[0].sqrMagnitude==0,"retrieved cosmetic prop targets the beak");
}
Reset();Plugin.OwlCollectDroppedItems.Value=false;OwlNavigation.FrontStand=true;
var hiveJob=new OwlWork{Machine=new Beehive(),InputPoint=new Vector3(3,.5f,0),Fuel=null};OwlWork.Jobs.Add(hiveJob);
OwlBeeMotes.Last=null;
for(int f=0;f<900&&OwlBeeMotes.Last==null;f++)Frames(Time.deltaTime);
Check(OwlBeeMotes.Last!=null&&OwlBeeMotes.Last.Visible,"beehive arrival starts an angry bee swarm");
hiveJob.Active=false; // Another owner harvests while the owl is performing.
Frames(1);
Check(OwlBeeMotes.Last.Visible,"an emptied hive does not interrupt the reaction");
Check(GullToss.Tosses==0&&GullToss.Carried==0,"hive animation never tosses honey back or transfers an item");
Frames(10);
Check(!OwlBeeMotes.Last.Visible&&!courier.Away,"swarm stops and owl returns through normal tour");
var calm=OwlMotion.Sample(OwlAction.BeePanic,3.6f);
Check(Math.Abs(calm.LeftWing)<.001f&&Math.Abs(calm.BodyLift)<.001f,"panic settles before departure");
var startled=OwlMotion.Sample(OwlAction.BeePanic,1f);
Check(startled.LeftWing>25&&startled.RightWing>25&&startled.HeadForward<0,"panic includes raised wings and recoil");
Console.WriteLine($"PASS: {checks} production owl courier lifecycle regressions.");
