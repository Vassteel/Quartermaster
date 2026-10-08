using Quartermaster;
int checks=0;
void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
bool Box(OwlPoint a,OwlPoint b,OwlPoint lo,OwlPoint hi)
{
    float enter=0,exit=1;
    for(int axis=0;axis<3;axis++)
    {
        float from=axis==0?a.X:axis==1?a.Y:a.Z,to=axis==0?b.X:axis==1?b.Y:b.Z;
        float low=axis==0?lo.X:axis==1?lo.Y:lo.Z,high=axis==0?hi.X:axis==1?hi.Y:hi.Z;
        float d=to-from;
        if(Math.Abs(d)<.000001f){if(from<low||from>high)return false;continue;}
        float x=(low-from)/d,y=(high-from)/d;if(x>y)(x,y)=(y,x);
        enter=Math.Max(enter,x);exit=Math.Min(exit,y);if(enter>exit)return false;
    }
    return true;
}
OwlFlightPath Solve(OwlPoint a,OwlPoint b,Func<OwlPoint,OwlPoint,bool> clear)
{
    var path=new OwlFlightPath(a,b,clear);int frames=0;
    while(path.State==OwlFlightPath.Result.Searching)
    {
        int previous=path.Expanded;path.Step(8);
        Check(path.Expanded-previous<=8,"search respects its per-frame expansion budget");
        Check(++frames<2000,"search terminates");
    }
    if(path.State==OwlFlightPath.Result.Found)
    {
        Check(OwlPoint.Distance(path.Points.First(),a)<.001f&&OwlPoint.Distance(path.Points.Last(),b)<.001f,"exact route endpoints");
        for(int i=1;i<path.Points.Count;i++)Check(clear(path.Points[i-1],path.Points[i]),"every smoothed segment clears the entire obstacle");
    }
    return path;
}
var a=new OwlPoint(0,1,0);var b=new OwlPoint(8,1,0);
var direct=Solve(a,b,(_,_)=>true);Check(direct.Points.Count==2,"open travel uses a direct route");
bool Wall(OwlPoint x,OwlPoint y)=>!Box(x,y,new(3,-10,-2),new(4,9,2));
var around=Solve(a,b,Wall);Check(around.State==OwlFlightPath.Result.Found&&around.Points.Any(p=>Math.Abs(p.Z)>2),"detours around tall walls");
bool Low(OwlPoint x,OwlPoint y)=>!Box(x,y,new(3,-10,-20),new(4,1.6f,20));
var over=Solve(a,b,Low);Check(over.State==OwlFlightPath.Result.Found&&over.Points.Any(p=>p.Y>1.6f),"flies over low barriers");
bool Door(OwlPoint x,OwlPoint y)=>!Box(x,y,new(3,-10,-20),new(4,20,-.65f))&&!Box(x,y,new(3,-10,.65f),new(4,20,20))&&
    !Box(x,y,new(3,2,-.65f),new(4,20,.65f));
Check(Solve(a,b,Door).State==OwlFlightPath.Result.Found,"uses an open doorway below its lintel");
bool Roofed(OwlPoint x,OwlPoint y)=>Low(x,y)&&!Box(x,y,new(-20,1.6f,-20),new(20,20,20));
Check(Solve(a,b,Roofed).State==OwlFlightPath.Result.Unreachable,"does not fly through a low ceiling to cross a barrier");
Check(Solve(a,b,(x,y)=>false).State==OwlFlightPath.Result.Unreachable,"occupied endpoints reject a route");
Check(Solve(a,new(float.NaN,0,0),(_,_)=>true).State==OwlFlightPath.Result.Unreachable,"non-finite targets are rejected");
Check(Solve(a,new(1000,0,0),(_,_)=>true).State==OwlFlightPath.Result.Unreachable,"unloaded distant targets are bounded");
// A route found before a door closes must be invalidated by the movement sweep.
Check(!Wall(direct.Points[0],direct.Points[1]),"runtime clearance detects a newly closed route");
// Ground routes must detour without taking the airborne shortcut, and never bridge gaps.
OwlGroundPath Ground(OwlPoint start,OwlPoint end,Func<OwlPoint,OwlPoint?> floor,Func<OwlPoint,OwlPoint,bool> clear)
{
    var path=new OwlGroundPath(start,end,floor,clear);int frames=0;
    while(path.State==OwlFlightPath.Result.Searching)
    {int before=path.Expanded;path.Step(8);Check(path.Expanded-before<=8,"ground search stays within frame budget");Check(++frames<200,"ground search terminates");}
    if(path.State==OwlFlightPath.Result.Found)
        for(int i=1;i<path.Points.Count;i++)Check(clear(path.Points[i-1],path.Points[i]),"ground edges and final approach retain clearance and support");
    return path;
}
OwlPoint? Flat(OwlPoint p)=>new(p.X,0,p.Z);
var ga=new OwlPoint(0,0,0);var gb=new OwlPoint(8,0,0);
bool GroundWall(OwlPoint x,OwlPoint y)=>x.Y==0&&y.Y==0&&!Box(x,y,new(3,-1,-2),new(4,3,2));
var walking=Ground(ga,gb,Flat,GroundWall);
Check(walking.State==OwlFlightPath.Result.Found&&walking.Points.Any(p=>Math.Abs(p.Z)>2)&&walking.Points.All(p=>p.Y==0),"walk around obstacles instead of flying over them");
bool Gap(OwlPoint x,OwlPoint y)=>!Box(x,y,new(3,-10,-20),new(4,10,20));
Check(Ground(ga,gb,p=>p.X>=3&&p.X<=4?null:Flat(p),Gap).State==OwlFlightPath.Result.Unreachable,"unsupported gap requires flight fallback");
OwlPoint? Ramp(OwlPoint p)=>new(p.X,p.X*.15f,p.Z);
bool SupportedRamp(OwlPoint x,OwlPoint y)=>Math.Abs(x.Y-x.X*.15f)<.001f&&Math.Abs(y.Y-y.X*.15f)<.001f&&
    !Box(x,y,new(3,-1,-1),new(4,4,1));
var ramp=Ground(ga,new(8,1.2f,0),Ramp,SupportedRamp);
Check(ramp.State==OwlFlightPath.Result.Found&&ramp.Points.All(p=>Math.Abs(p.Y-p.X*.15f)<.001f),"ground search follows changing terrain height around an obstruction");
Check(Ground(ga,gb,Flat,(_,_)=>false).State==OwlFlightPath.Result.Unreachable,"blocked landing points reject ground travel");
var gaitA=OwlMotion.Sample(OwlAction.Walking,.1f,0,1.4f);
var gaitB=OwlMotion.Sample(OwlAction.Walking,.35f,0,1.4f);
Check(gaitA.Step*gaitB.Step<0&&gaitA.FeetTuck==0&&gaitA.LeftWing<10,"walking alternates feet with grounded toes and folded wings");
// Walk phase follows measured travel, not the frame clock or desired velocity.
var plantedA=OwlMotion.Sample(OwlAction.Walking,1,0,.7f,0,.04f);
var plantedB=OwlMotion.Sample(OwlAction.Walking,20,0,.7f,0,.09f);
Check(Math.Abs((plantedA.LeftFootForward+.04f)-(plantedB.LeftFootForward+.09f))<.00001f,"stance foot counters root movement instead of skating");
Check(plantedA.LeftFootLift==0&&plantedB.LeftFootLift==0&&plantedA.LeftFootPitch==0,"planted toes stay level on the floor");
var sameTravel=OwlMotion.Sample(OwlAction.Walking,99,0,.7f,0,.04f);
Check(sameTravel.LeftFootForward==plantedA.LeftFootForward&&sameTravel.RightFootLift==plantedA.RightFootLift,"pausing path progress cannot advance the foot cycle");
var stopped=OwlMotion.Sample(OwlAction.Walking,7,0,0,0,.04f);
Check(stopped.Step==0&&stopped.LeftFootForward==0&&stopped.RightFootLift==0&&stopped.BodySway==0,"stopped movement settles to a planted stance");
for(int i=0;i<100;i++)
{
 var p=OwlMotion.Sample(OwlAction.Walking,i*.03f,0,.7f,0,i*.0052f);
 Check(p.LeftFootLift>=0&&p.RightFootLift>=0&&p.LeftFootLift<=.086f&&p.RightFootLift<=.086f,"swing feet lift without passing below the floor");
 Check(p.LeftFootLift==0||p.RightFootLift==0,"at least one foot supports every walking pose");
}
var random=new Random(213);
for(int i=0;i<35;i++)
{
    float width=.2f+(float)random.NextDouble()*2,height=.5f+(float)random.NextDouble()*4;
    bool Clear(OwlPoint x,OwlPoint y)=>!Box(x,y,new(3,-10,-width),new(4,height,width));
    Check(Solve(a,b,Clear).State==OwlFlightPath.Result.Found,"varied workyard obstacles have safe detours");
}
foreach(var action in Enum.GetValues<OwlAction>())for(int frame=0;frame<1400;frame++)
{
    var pose=OwlMotion.Sample(action,frame/60f,(frame%60)/59f,4,(float)Math.Sin(frame*.1));
    foreach(var value in new[]{pose.HeadPitch,pose.HeadYaw,pose.HeadRoll,pose.BodyPitch,pose.BodyRoll,pose.Crouch,pose.LeftWing,pose.RightWing,pose.FeetTuck,pose.Step})
        Check(float.IsFinite(value),"all animation phases remain finite");
    Check(pose.Crouch>=0&&pose.Crouch<=.13f,"body stays inside leg compression range");
    Check(Math.Abs(pose.LeftWing)<115&&Math.Abs(pose.RightWing)<115,"wings stay inside articulation limits");
}
Check(OwlMotion.Sample(OwlAction.Flying,.5f).FeetTuck==1,"flight tucks toes");
Check(OwlMotion.Sample(OwlAction.Landing,.5f,1).LeftWing==0,"landing finishes with folded wings");
foreach(bool cluck in new[]{false,true})
{
    var samples=OwlVoiceSynth.Create(cluck);
    Check(samples.All(float.IsFinite)&&samples.Max(Math.Abs)<1,"original audio has no invalid or clipped samples");
    Check(Math.Abs(samples[0])<.001&&Math.Abs(samples[^1])<.001,"audio fades to silence at both ends");
    Check(samples.Any(x=>Math.Abs(x)>.1),"audio is audible");
    if(!cluck)Check(samples.Skip((int)(.42*OwlVoiceSynth.Rate)).Take((int)(.2*OwlVoiceSynth.Rate)).All(x=>x==0),"paired coos have a silent gap");
}
Console.WriteLine($"PASS: {checks:N0} owl navigation, movement pose and original audio checks.");
if(args.Length==2&&args[0]=="--preview")
{
    var frames=new List<object>();
    for(int i=0;i<96;i++)
    {
        float t=i/12f;var action=t<1.5f?OwlAction.Idle:t<2?OwlAction.Takeoff:t<4.5f?OwlAction.Flying:t<5.1f?OwlAction.Landing:t<7?OwlAction.Working:OwlAction.Idle;
        float local=action==OwlAction.Takeoff?t-1.5f:action==OwlAction.Landing?t-4.5f:action==OwlAction.Working?t-5.1f:t>=7?t+6:t;
        float progress=action==OwlAction.Takeoff?local/.5f:action==OwlAction.Landing?local/.6f:0;
        var pose=OwlMotion.Sample(action,local,progress,4,action==OwlAction.Flying?.2f:0);
        var fields=typeof(OwlPose).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).ToDictionary(f=>f.Name,f=>f.GetValue(pose));
        frames.Add(new{action=action.ToString(),pose=fields,lift=action==OwlAction.Flying?.5f:action==OwlAction.Takeoff?.5f*progress:action==OwlAction.Landing?.5f*(1-progress):0});
    }
    File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(frames));
    foreach(bool cluck in new[]{false,true})
    {
        var samples=OwlVoiceSynth.Create(cluck);
        using var writer=new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(args[1])!,cluck?"owl-cluck.wav":"owl-coo.wav")));
        void Tag(string s)=>writer.Write(System.Text.Encoding.ASCII.GetBytes(s));
        Tag("RIFF");writer.Write(36+samples.Length*2);Tag("WAVEfmt ");writer.Write(16);writer.Write((short)1);writer.Write((short)1);
        writer.Write(OwlVoiceSynth.Rate);writer.Write(OwlVoiceSynth.Rate*2);writer.Write((short)2);writer.Write((short)16);
        Tag("data");writer.Write(samples.Length*2);foreach(float value in samples)writer.Write((short)(value*32767));
    }
}

if(args.Length==2&&args[0]=="--walking-preview")
{
    var frames=new List<object>();
    for(int i=0;i<20;i++)
    {
        var pose=OwlMotion.Sample(OwlAction.Walking,i/20f,0,.7f,.1f,i*.026f);
        var fields=typeof(OwlPose).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).ToDictionary(f=>f.Name,f=>f.GetValue(pose));
        frames.Add(new{action="Walking",pose=fields,lift=0});
    }
    File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(frames));
}
