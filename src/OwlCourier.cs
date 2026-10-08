using System;
using System.Collections.Generic;
using Quartermaster.Cosmetics;
using UnityEngine;

namespace Quartermaster;

// Presentation owns travel, never machine work. Ground pickup is a checked,
// single-item transfer into the permanent chest before the return animation.
internal sealed class OwlCourier
{
    private enum Phase { Home, GroundPlanning, Walking, Planning, Takeoff, Flying, Landing, Working, Reading, Gathering, Recovery }
    private readonly Container home;
    private readonly Transform root;
    private readonly QuartermasterOwl rig;
    private readonly Dictionary<int,float> visited=new Dictionary<int,float>();
    private readonly HashSet<int> tour=new HashSet<int>();
    private readonly HashSet<int> skipped=new HashSet<int>();
    private readonly List<Vector3> route=new List<Vector3>();
    private readonly List<ItemDrop.ItemData> collected=new List<ItemDrop.ItemData>();
    private OwlFlightPath search;
    private OwlGroundPath groundSearch;
    private readonly List<Vector3> groundRoute=new List<Vector3>();
    private Vector3 flightTarget;
    private bool walkAfterLanding,groundArrival,furnitureCarry;
    private OwlWork job;
    private OwlBeeMotes bees;
    private Vector3 hiveStand;
    private ItemDrop drop;
    private OwlLedger book;
    private Component perch;
    private float nextBook;
    private Phase phase;
    private Vector3 destination,homePoint,scale,lastPosition;
    private Quaternion homeRotation;
    private bool returning,grounded=true,hopTried,narrow,requestedTrip;
    private float since,idleSince,nextSense,nextThrow,nextWidth,lastProgress,speed,turn,homeSince;
    private float walkTravel,verticalSpeed;
    private int waypoint,groundWaypoint,tosses,replans;
    internal bool Away=>phase!=Phase.Home;
    internal bool Walking=>phase==Phase.Walking;
    internal Component Workstation=>!returning&&job!=null?job.Machine:null;
    internal string Status
    {
        get
        {
            if(phase==Phase.Home)return "Resting on the Deposit Chest";
            if(phase==Phase.Recovery)return "Finding a way back to the Deposit Chest";
            if(returning)return collected.Count>0?"Returning with collected items":"Returning to the Deposit Chest";
            if(phase==Phase.Reading)return "Reading the ledger";
            if(book)return "Visiting the ledger";
            if(drop)return phase==Phase.Gathering?"Picking up dropped items":"Collecting dropped items";
            if(job!=null&&job.Machine is Beehive&&phase==Phase.Working)return "Shooing angry bees at";
            if(job!=null)return phase==Phase.Working?(job.Furniture?(job.Retrieving?"Taking an item from":"Stowing an item in"):"Working at"):"Visiting";
            return "Returning to the Deposit Chest";
        }
    }
    internal OwlCourier(Container chest,Transform bird,QuartermasterOwl owl)
    {home=chest;perch=chest;root=bird;rig=owl;idleSince=homeSince=Time.time;scale=root.localScale;nextBook=Time.time+20+Math.Abs(home.GetInstanceID()%16);}
    internal bool Tick(Vector3 homePerch,Quaternion rotation,bool sorting)
    {
        homePoint=homePerch;homeRotation=rotation;
        float now=Time.time,dt=Mathf.Min(Time.deltaTime,.05f);
        if(!Away)
        {
            if(sorting)idleSince=now;
            if(now<nextSense)return false;nextSense=now+1;
            if(sorting&&now-homeSince<8)return false;
            requestedTrip=false;
            tour.Clear();
            if(OwlCleanup.CanStart(home))
            {
                skipped.Clear();collected.Clear();drop=OwlCleanup.Find(home,skipped);
                if(drop){requestedTrip=true;GoToDrop();return true;}
                OwlCleanup.Complete(home);
            }
            if(TryBook()) { }
            else if(TryWork())idleSince=now;
            else if(now-idleSince>=20&&Plugin.OwlCollectDroppedItems.Value)
            {
                skipped.Clear();collected.Clear();
                drop=OwlCleanup.Find(home,skipped);
                if(drop)GoToDrop();else idleSince=now-15;
            }
            if(!Away)return false;
        }
        root.localScale=scale;
        if(now>=nextSense)
        {
            nextSense=now+.6f;
            if(returning&&(destination-homePoint).sqrMagnitude>.16f&&phase!=Phase.Recovery)Begin(homePoint,true);
            if(!returning&&job!=null)
            {
                var current=OwlWork.Read(home,job.Machine);
                if(current==null){if(!(job.Machine is Beehive&&phase==Phase.Working))ContinueRound();}else job=current;
            }
            if(!returning&&requestedTrip&&!OwlCleanup.Requested(home)){requestedTrip=false;ReturnHome();}
            if(!returning&&drop&&!OwlCleanup.Eligible(home,drop))ReturnHome();
            if(!returning&&drop&&(drop.transform.position-destination).sqrMagnitude>16)ReturnHome();
            if(!returning&&book&&!OwlLedger.Allowed(book)){book=null;ContinueRound();}
            if(!returning&&job==null&&!drop&&!book)ContinueRound();
        }
        switch(phase)
        {
            case Phase.GroundPlanning:
                Pose(OwlAction.Idle,now-since,0,dt);
                if(now-since>6){StartFlight(destination);break;}
                if(!OwlNavigation.SearchTurn())break;
                groundSearch.Step(8);
                if(groundSearch.State==OwlFlightPath.Result.Unreachable){StartFlight(destination);break;}
                if(groundSearch.State==OwlFlightPath.Result.Found)
                {
                    groundRoute.Clear();foreach(var p in groundSearch.Points)groundRoute.Add(OwlNavigation.Vector(p));
                    groundWaypoint=1;groundSearch=null;
                    if(Vector3.Distance(root.position,groundRoute[0])>.15f)
                    {walkAfterLanding=true;StartFlight(groundRoute[0]);}
                    else StartWalking();
                }
                break;
            case Phase.Walking:
                Walk(dt);break;
            case Phase.Planning:
                var support=OwlNavigation.Floor(OwlNavigation.Point(root.position));
                bool onSurface=support.HasValue&&Mathf.Abs(support.Value.Y-root.position.y)<.08f;
                Pose(onSurface?OwlAction.Idle:OwlAction.Waiting,now-since,0,dt);
                if(now-since>8){Failed();break;}
                if(search==null||!OwlNavigation.SearchTurn())break;
                search.Step(8);
                if(search.State==OwlFlightPath.Result.Unreachable){Failed();break;}
                if(search.State==OwlFlightPath.Result.Found)
                {
                    route.Clear();foreach(var p in search.Points)route.Add(OwlNavigation.Vector(p));
                    waypoint=1;phase=Phase.Takeoff;since=now;search=null;
                }
                break;
            case Phase.Takeoff:
                Face(flightTarget,dt);Pose(OwlAction.Takeoff,now-since,(now-since)/.4f,dt);
                if(now-since>=.4f){phase=Phase.Flying;since=lastProgress=now;lastPosition=root.position;speed=0;}
                break;
            case Phase.Flying:
                Fly(dt);break;
            case Phase.Landing:
                Face(returning?homePoint+homeRotation*Vector3.forward:book?book.Book:job!=null?job.InputPoint:drop?drop.transform.position:destination,dt);
                Pose(groundArrival?OwlAction.Idle:OwlAction.Landing,now-since,(now-since)/.55f,dt);
                if(now-since>=.55f)
                {
                    if(walkAfterLanding){walkAfterLanding=false;StartWalking();break;}
                    if(returning){ArriveHome();return false;}
                    perch=book;
                    phase=book?Phase.Reading:job!=null?Phase.Working:Phase.Gathering;since=now;tosses=0;furnitureCarry=false;nextThrow=now+.43f;
                    if(job?.Machine is Beehive)hiveStand=root.position;
                    // Arrival is quiet; ambient calls come from the occasional perch timer.
                }
                break;
            case Phase.Reading:
                if(!book){ContinueRound();break;}
                Face(book.Book,dt);Pose(OwlAction.Reading,now-since,0,dt);
                if(tosses==0&&now-since>=1.2f){book.TurnPage();tosses=1;}
                if(now-since>=4.6f){book=null;ContinueRound();}
                break;
            case Phase.Working:
                if(job.Machine is Beehive)
                {
                    float elapsed=now-since;
                    Face(job.InputPoint,dt);
                    Pose(elapsed<.45f?OwlAction.Working:OwlAction.BeePanic,Mathf.Max(0,elapsed-.45f),0,dt);
                    if(elapsed>=.45f)
                    {
                        if(!bees)bees=OwlBeeMotes.Create(root);
                        bees.Show(rig.BeakWorld,job.InputPoint,elapsed-.45f);
                        var away=hiveStand-job.InputPoint;away.y=0;
                        var next=hiveStand+away.normalized*(.22f*Mathf.Sin(Mathf.Clamp01((elapsed-.45f)/3.6f)*Mathf.PI));
                        if(grounded)
                        {
                            var floor=OwlNavigation.Floor(OwlNavigation.Point(next));
                            if(floor.HasValue&&OwlNavigation.WalkClear(OwlNavigation.Point(root.position),floor.Value))root.position=OwlNavigation.Vector(floor.Value);
                        }
                        else if(OwlNavigation.Clear(root.position,next))root.position=next;
                    }
                    if(elapsed>=4.05f)ContinueRound();
                    break;
                }
                Face(job.InputPoint,dt);Pose(OwlAction.Working,now-since,0,dt);
                if(job.Furniture)
                {
                    job.Furniture.Animate(job,now-since);
                    if(!furnitureCarry&&!job.Retrieving&&job.Input&&now-since>=.35f&&FurnitureMotion.Carries(job.Furniture.Handling))
                    {GullToss.Carry(root,Automation.NewOutput(job.Input,false),rig.BeakWorld);furnitureCarry=true;}
                    if(tosses==0&&now-since>=job.Furniture.ContactTime)
                    {
                        if(job.Input)
                        {
                            if(job.Retrieving)GullToss.Place(root,Automation.NewOutput(job.Input,false),job.InputPoint,rig.BeakWorld);
                            else if(FurnitureMotion.Carries(job.Furniture.Handling))GullToss.Place(root,Automation.NewOutput(job.Input,false),rig.BeakWorld,job.InputPoint);
                            else GullToss.SpawnToward(root,Automation.NewOutput(job.Input,false),rig.BeakWorld,job.InputPoint);
                        }
                        tosses=1;
                    }
                    if(now-since>=job.Furniture.Duration)ContinueRound();
                    break;
                }
                if(now>=nextThrow&&tosses<6)
                {
                    bool fuel=(tosses%2)==1&&job.Fuel;var item=job.Prop(fuel);
                    if(item)GullToss.SpawnToward(root,Automation.NewOutput(item,false),rig.BeakWorld,job.Aim(fuel));
                    tosses++;nextThrow=now+.85f;
                }
                if(now-since>=5.2f)
                {
                    ContinueRound();
                }
                break;
            case Phase.Gathering:
                Pose(OwlAction.Working,now-since,0,dt);
                if(now>=nextThrow)
                {
                    nextThrow=now+.8f;
                    var item=drop&&(drop.transform.position-root.position).sqrMagnitude<6.25f&&
                        !Physics.Linecast(rig.BeakWorld,drop.transform.position,OwlNavigation.Mask,QueryTriggerInteraction.Ignore)
                        ?OwlCleanup.TakeOne(home,drop):null;
                    if(item!=null){collected.Add(item);GullToss.Carry(root,item,rig.BeakWorld);}
                    if(collected.Count>=3||item==null||!drop||!OwlCleanup.Eligible(home,drop))
                    {
                        if(drop)skipped.Add(drop.GetInstanceID());
                        drop=collected.Count<3?OwlCleanup.Find(home,skipped):null;
                        if(drop)GoToDrop();else ReturnHome();
                    }
                }
                break;
            case Phase.Recovery:
                // Last-resort local rescue after a closed/removed route: no wall
                // clipping, no owned cargo to lose and no production dependency.
                Pose(OwlAction.Takeoff,now-since,1,dt);
                root.localScale=scale*Mathf.Clamp01(1-(now-since)/.45f);
                if(now-since>=.5f){root.position=homePoint;root.localScale=scale;ArriveHome();return false;}
                break;
        }
        return Away;
    }
    private void Pose(OwlAction action,float time,float progress,float dt)
    {
        var pose=OwlMotion.Sample(action,time,progress,speed,turn,action==OwlAction.Walking?walkTravel:-1);
        if(action==OwlAction.Flying)
        {
            pose.BodyPitch-=Mathf.Clamp(verticalSpeed,-2,2)*7;
            float approach=Mathf.Clamp01(Vector3.Distance(root.position,flightTarget)/.7f);
            pose.FeetTuck*=approach;
            pose.LeftWing+=10*(1-approach);pose.RightWing+=10*(1-approach);
            pose.BodyPitch-=18*(1-approach);
        }
        if(action==OwlAction.Walking)
        {
            pose.LeftFootLift+=FootHeight(-.099f,pose.LeftFootForward);
            pose.RightFootLift+=FootHeight(.099f,pose.RightFootForward);
        }
        if(action==OwlAction.Working&&!grounded)
        {float flap=55+22*(float)Math.Sin(time*20);pose.LeftWing+=flap;pose.RightWing+=flap;pose.FeetTuck=.6f;pose.BodyLift+=.012f*(1+(float)Math.Sin(time*20));}
        if(narrow&&action==OwlAction.Flying){pose.LeftWing*=.35f;pose.RightWing*=.35f;}
        if(action==OwlAction.Working&&job!=null&&job.Furniture)
        {
            float lean=job.Furniture.Lean(time);pose.BodyPitch+=lean*12;pose.HeadForward+=lean*.035f;
            pose.LeftWing+=lean*15;pose.HeadPitch-=lean*9;
            if(job.Furniture.Handling==FurnitureHandling.Hide||job.Furniture.Handling==FurnitureHandling.Textile)
            {float press=FurnitureMotion.Press(job.Furniture.Handling,time);pose.HeadPitch+=press*65;pose.RightWing+=lean*10;pose.BodyLift-=press*.13f;}
            if(job.Furniture.Handling==FurnitureHandling.Wardrobe||job.Furniture.Handling==FurnitureHandling.Treasure)
            {
                float reach=FurnitureDoors.OwlOpening(time);pose.LeftWing+=reach*14;pose.HeadForward+=reach*.02f;
                pose.RightWing+=lean*13;pose.BodyPitch+=lean*4;
            }
            if(FurnitureMotion.IsDisplay(job.Furniture.Handling))
            {
                float lift=FurnitureMotion.DisplayLift(job.Furniture.Handling,time);
                // Brace a larger trophy with both wings; give small gems a close inspection.
                pose.RightWing+=lean*(job.Furniture.Handling==FurnitureHandling.Trophy?18:7);
                pose.HeadPitch-=lift*75;pose.HeadForward+=lift*.25f;
            }
            if(FurnitureMotion.IsRack(job.Furniture.Handling))
            {
                pose.HeadForward+=lean*.028f;pose.RightWing+=lean*(job.Furniture.Handling==FurnitureHandling.Shield?22:12);
                pose.BodyLift-=lean*.016f;pose.HeadPitch-=lean*6;
            }
            if(job.Furniture.Handling==FurnitureHandling.Hanging){pose.HeadPitch-=lean*7;pose.HeadForward+=lean*.02f;pose.RightWing+=lean*14;}
            if(job.Furniture.Handling==FurnitureHandling.Grain){pose.HeadPitch+=lean*6;pose.RightWing+=lean*8;}
            if(job.Furniture.Handling==FurnitureHandling.Feather)pose.HeadPitch+=lean*5;
            if(job.Furniture.Handling==FurnitureHandling.Masonry){pose.BodyLift-=lean*.022f;pose.RightWing+=lean*17;pose.HeadPitch+=lean*4;}
            if(job.Furniture.Handling==FurnitureHandling.Ingot){pose.BodyLift-=lean*.015f;pose.RightWing+=lean*12;}
        }
        rig.Perform(pose,dt);rig.Rest(false,Time.time,dt);
    }
    private float FootHeight(float side,float forward)
    {
        var point=root.TransformPoint(new Vector3(side,0,forward+.055f));
        var ground=OwlNavigation.Floor(OwlNavigation.Point(point));
        return ground.HasValue?Mathf.Clamp((ground.Value.Y-root.position.y)/Mathf.Max(.1f,scale.y),-.12f,.12f):0;
    }
    private void Begin(Vector3 target,bool goHome)
    {
        destination=target;returning=goHome;replans=0;hopTried=false;walkAfterLanding=false;groundArrival=false;
        groundRoute.Clear();groundSearch=null;
        // Walk wherever there is continuous support. Short flights connect the
        // chest/book perches to the floor; a failed ground search falls back to air.
        if(grounded&&OwlNavigation.GroundNear(root.position,target,perch,out var start,perch)&&
            OwlNavigation.GroundNear(target,root.position,book?(Component)book:goHome?home:job?.Machine,out var end,book||goHome))
        {
            groundSearch=new OwlGroundPath(OwlNavigation.Point(start),OwlNavigation.Point(end),OwlNavigation.Floor,OwlNavigation.WalkClear);
            phase=Phase.GroundPlanning;since=Time.time;speed=0;
        }
        else StartFlight(target);
    }
    private void StartFlight(Vector3 target)
    {groundArrival=false;flightTarget=target;replans=0;Plan();}
    private void Plan()
    {
        phase=Phase.Planning;since=Time.time;speed=0;groundSearch=null;
        search=new OwlFlightPath(OwlNavigation.Point(root.position),OwlNavigation.Point(flightTarget),OwlNavigation.Clear,replans>0?.4f:.75f);
    }
    private void StartWalking()
    {perch=null;phase=Phase.Walking;since=lastProgress=Time.time;lastPosition=root.position;speed=0;walkTravel=0;verticalSpeed=0;}
    private void Walk(float dt)
    {
        float now=Time.time;
        if(groundWaypoint>=groundRoute.Count)
        {
            if(Vector3.Distance(root.position,destination)>.15f){StartFlight(destination);return;}
            groundArrival=true;phase=Phase.Landing;since=now-.55f;speed=0;return;
        }
        var target=groundRoute[groundWaypoint];
        if(Vector3.Distance(root.position,target)<.04f){groundWaypoint++;return;}
        float heading=Mathf.Abs(Vector3.SignedAngle(root.forward,target-root.position,Vector3.up));
        Face(target,dt);
        float desired=Mathf.Min(.85f,Mathf.Sqrt(Mathf.Max(.02f,Vector3.Distance(root.position,groundRoute[groundRoute.Count-1]))*3));
        desired*=Mathf.Clamp01((70-heading)/45);
        if(groundWaypoint+1<groundRoute.Count&&Vector3.Distance(root.position,target)<.4f)
        {
            float corner=Mathf.Abs(Vector3.SignedAngle(target-root.position,groundRoute[groundWaypoint+1]-target,Vector3.up));
            desired*=Mathf.Clamp(1-corner/140,.2f,1);
        }
        speed=Mathf.MoveTowards(speed,desired,dt*2.5f);
        var next=Vector3.MoveTowards(root.position,target,speed*dt);
        var floor=OwlNavigation.Floor(OwlNavigation.Point(next));
        if(floor.HasValue)next=OwlNavigation.Vector(floor.Value);
        if(!floor.HasValue||!OwlNavigation.WalkClear(OwlNavigation.Point(root.position),OwlNavigation.Point(next)))
        {
            // A moved door or obstacle cancels walking immediately. The same
            // swept flight/hop recovery handles the remaining leg without clipping.
            StartFlight(destination);return;
        }
        float moved=Vector3.Distance(root.position,next);
        walkTravel+=moved/Mathf.Max(.1f,scale.x);
        root.position=next;speed=dt>0?moved/dt:0;
        Pose(OwlAction.Walking,now-since,0,dt);
        if(Vector3.Distance(lastPosition,root.position)>.15f){lastPosition=root.position;lastProgress=now;}
        if(now-lastProgress>3||now-since>150)StartFlight(destination);
    }
    private bool TryBook()
    {
        if(Time.time<nextBook)return false;
        book=OwlLedger.VisitFor(home);
        // A missing/inaccessible book gets a short retry, not another full interval.
        nextBook=Time.time+(book?45+Math.Abs((home.GetInstanceID()+Time.frameCount)%31):10);
        if(!book)return false;
        job=null;drop=null;grounded=true;Begin(book.Perch,false);return true;
    }
    private bool TryWork()
    {
        // Mark every selected station in this round, including blocked targets,
        // so a finished or inaccessible machine cannot send him home early.
        while((job=OwlWork.Next(home,visited,tour,root.position))!=null)
        {
            tour.Add(job.Machine.GetInstanceID());
            if(job.Furniture)
            {
                // Prefer the authored open face, never a shortcut through the back boards.
                var approach=job.Furniture.Approach(job);
                if(job.Furniture.Handling!=FurnitureHandling.Jar&&OwlNavigation.StandNear(job.InputPoint,approach,job.Machine,out var floor,out var onGround)&&onGround&&Mathf.Abs(job.InputPoint.y-floor.y)<.95f&&Vector3.Dot(floor-job.InputPoint,approach-job.InputPoint)>0)
                {grounded=true;Begin(floor,false);return true;}
                if(OwlNavigation.Clear(approach,approach)){grounded=false;Begin(approach,false);return true;}
                visited[job.Machine.GetInstanceID()]=Time.time+12;continue;
            }
            if(OwlNavigation.StandNear(job.InputPoint,root.position,job.Machine,out var stand,out grounded))
            {Begin(stand,false);return true;}
            visited[job.Machine.GetInstanceID()]=Time.time+12;
        }
        return false;
    }
    private void ContinueRound()
    {
        if(bees)bees.Hide();
        if(job!=null&&job.Machine)visited[job.Machine.GetInstanceID()]=Time.time;
        if(job!=null&&job.Furniture){job.Furniture.Complete(job);GullToss.ClearFor(root);}
        job=null;
        if(TryBook()||TryWork())return;
        ReturnHome();
    }
    private void GoToDrop()
    {
        if(OwlNavigation.StandNear(drop.transform.position,root.position,null,out var point,out grounded))Begin(point,false);
        else {skipped.Add(drop.GetInstanceID());ReturnHome();}
    }
    private void Fly(float dt)
    {
        float now=Time.time;
        if(waypoint>=route.Count){phase=Phase.Landing;since=now;speed=0;return;}
        var target=route[waypoint];float distance=Vector3.Distance(root.position,target);
        if(distance<.06f){waypoint++;return;}
        float finalDistance=Vector3.Distance(root.position,flightTarget);
        float desired=Mathf.Min(4.2f,Mathf.Sqrt(Mathf.Max(.02f,finalDistance)*5));
        // Brake into route corners and let the head/body turn before accelerating.
        if(waypoint+1<route.Count&&distance<1)
            desired=Mathf.Min(desired,1.2f+distance*2);
        float yaw=Mathf.Abs(Vector3.SignedAngle(root.forward,target-root.position,Vector3.up));
        if(yaw>50)desired*=Mathf.Clamp(1-yaw/220,.25f,1);
        speed=Mathf.MoveTowards(speed,desired,dt*5);
        var next=Vector3.MoveTowards(root.position,target,speed*dt);
        if(!OwlNavigation.Clear(root.position,next))
        {
            if(!hopTried&&OwlNavigation.Hop(root.position,target,out var hop))
            {
                hopTried=true;route.Insert(waypoint,hop);route.Insert(waypoint+1,target+Vector3.up*(hop.y-root.position.y));return;
            }
            if(++replans<=2){Plan();return;}ReturnOrRecover();return;
        }
        verticalSpeed=dt>0?(next.y-root.position.y)/dt:0;
        root.position=next;Face(target,dt);Pose(OwlAction.Flying,now-since,0,dt);
        if(now>=nextWidth){nextWidth=now+.3f;narrow=!OwlNavigation.Clear(root.position,root.position,.42f);}
        if(Vector3.Distance(lastPosition,root.position)>.2f){lastPosition=root.position;lastProgress=now;}
        if(now-lastProgress>3||now-since>60)ReturnOrRecover();
    }
    private void Face(Vector3 point,float dt)
    {
        var direction=point-root.position;direction.y=0;if(direction.sqrMagnitude<.0001f)return;
        var target=Quaternion.LookRotation(direction);
        turn=Mathf.Clamp(Vector3.SignedAngle(root.forward,direction,Vector3.up)/70,-1,1);
        root.rotation=Quaternion.RotateTowards(root.rotation,target,dt*240);
    }
    private void Failed()
    {
        // A finer search can find offset doorways that the coarse grid misses.
        if(replans++<1){Plan();return;}
        if(job!=null&&job.Machine)visited[job.Machine.GetInstanceID()]=Time.time+20;
        if(drop)skipped.Add(drop.GetInstanceID());
        ReturnOrRecover();
    }
    private void ReturnOrRecover()
    {
        if(!returning&&job!=null)ContinueRound();
        else if(!returning)ReturnHome();
        else {phase=Phase.Recovery;since=Time.time;search=null;}
    }
    private void ReturnHome()
    {
        if(bees)bees.Hide();
        if(job!=null&&job.Machine)visited[job.Machine.GetInstanceID()]=Time.time;
        if(job!=null&&job.Furniture){job.Furniture.Complete(job);GullToss.ClearFor(root);}
        job=null;drop=null;book=null;grounded=true;Begin(homePoint,true);
    }
    private void ArriveHome()
    {
        perch=home;root.position=homePoint;root.rotation=homeRotation;root.localScale=scale;
        GullToss.ClearFor(root);
        foreach(var item in collected)GullToss.Spawn(root,item,rig.BeakWorld);
        collected.Clear();job=null;drop=null;book=null;phase=Phase.Home;search=null;groundSearch=null;groundRoute.Clear();route.Clear();
        since=idleSince=homeSince=Time.time;nextSense=Time.time+2;
    }
    internal void Reset()
    {
        if(bees)bees.Hide();
        if(job!=null&&job.Furniture){job.Furniture.Complete(job);GullToss.ClearFor(root);}
        perch=home;phase=Phase.Home;search=null;groundSearch=null;groundRoute.Clear();route.Clear();job=null;drop=null;book=null;collected.Clear();
        since=idleSince=homeSince=Time.time;nextSense=Time.time+2;root.localScale=scale;
    }
}
