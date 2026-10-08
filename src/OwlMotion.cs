using System;

namespace Quartermaster;

internal enum OwlAction { Idle, Walking, Takeoff, Flying, Landing, Working, Reading, Alert, Waiting, BeePanic }
internal struct OwlPose
{
    internal float HeadPitch,HeadYaw,HeadRoll,BodyPitch,BodyRoll,Crouch,LeftWing,RightWing,FeetTuck,Step;
    internal float LeftFootForward,RightFootForward,LeftFootLift,RightFootLift,LeftFootPitch,RightFootPitch,FootPlant,BodySway,BodyLift,HeadForward;
}

// Deterministic layered motion, independent of the frame rate and the game runtime.
internal static class OwlMotion
{
    private static float Sin(float x)=>(float)Math.Sin(x);
    private static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));
    private static float Pulse(float t,float start,float duration)
    { if(t<start||t>start+duration)return 0;float s=Sin((t-start)/duration*(float)Math.PI);return s*s; }
    private static void Foot(float cycle,float weight,out float forward,out float lift,out float pitch)
    {
        float phase=cycle-(float)Math.Floor(cycle);
        // The planted foot moves backward at exactly the root's travel speed.
        // The remaining 40% swings it forward with toe clearance and a soft set-down.
        if(phase<.6f){forward=(.156f-.52f*phase)*weight;lift=pitch=0;return;}
        float t=(phase-.6f)/.4f,ease=t*t*(3-2*t);
        forward=(-.156f+.312f*ease)*weight;
        lift=.085f*Sin(t*(float)Math.PI)*weight;
        pitch=-16*Sin(t*(float)Math.PI)*weight;
    }
    internal static OwlPose Sample(OwlAction action,float time,float progress=0,float speed=0,float turn=0,float travel=-1)
    {
        var p=new OwlPose();float phase=time%23;
        p.HeadPitch=3*Sin(time*1.7f);p.Crouch=.004f*(1+Sin(time*2));
        if(action==OwlAction.Walking)
        {
            float cycle=(travel>=0?travel:time*speed)/.52f;
            float weight=Clamp(speed/.25f,0,1),angle=cycle*2*(float)Math.PI;
            Foot(cycle,weight,out p.LeftFootForward,out p.LeftFootLift,out p.LeftFootPitch);
            Foot(cycle+.5f,weight,out p.RightFootForward,out p.RightFootLift,out p.RightFootPitch);
            p.Step=Sin(angle)*weight;p.FootPlant=weight;
            p.Crouch=(.008f+.012f*(1-(float)Math.Cos(angle*2)))*weight;
            p.BodyPitch=(7+2*Sin(angle*2))*weight;p.BodyRoll=2.5f*Sin(angle)*weight;
            p.BodySway=.018f*Sin(angle)*weight;
            p.HeadPitch=(-5+6*Sin(angle*2))*weight;p.HeadYaw=Clamp(turn,-1,1)*28;
            p.HeadForward=.022f*Sin(angle*2)*weight;
            p.LeftWing=p.RightWing=3*weight;
        }
        else if(action==OwlAction.Flying)
        {
            float flap=Sin(time*(speed<1.5f?22:18));float spread=68+27*flap;
            p.BodyLift=.018f*(1+flap);
            // Short glides between flapping runs; toes fold behind the body.
            if(speed>2.5f&&time%3.7f>2.65f)spread=66+4*Sin(time*7);
            p.LeftWing=spread+Clamp(turn,-1,1)*12;p.RightWing=spread-Clamp(turn,-1,1)*12;
            p.BodyPitch=18+Clamp(speed,0,5)*4;p.BodyRoll=Clamp(turn,-1,1)*-23;
            p.HeadPitch=-p.BodyPitch*.72f;p.HeadYaw=Clamp(turn,-1,1)*25;p.FeetTuck=1;
        }
        else if(action==OwlAction.Takeoff)
        {
            float t=Clamp(progress,0,1);p.Crouch=.12f*Sin(t*(float)Math.PI);
            p.BodyPitch=18*t;p.HeadPitch=-12*t;p.LeftWing=p.RightWing=85*t;
            p.BodyLift=.06f*Clamp((t-.65f)/.35f,0,1);p.FeetTuck=.3f*Clamp((t-.7f)/.3f,0,1);
        }
        else if(action==OwlAction.Landing)
        {
            float t=Clamp(progress,0,1);p.LeftWing=p.RightWing=80*(1-t);
            p.BodyPitch=-15*(1-t);p.HeadPitch=8*(1-t);p.Crouch=.1f*Pulse(t,.15f,.75f);
            p.Step=Sin(t*12)*.2f*(1-t);
        }
        else if(action==OwlAction.Working)
        {
            float t=time%.85f;float scoop=Pulse(t,0,.43f),release=Pulse(t,.43f,.32f);
            p.HeadPitch=55*scoop-26*release;p.BodyPitch=23*scoop-8*release;
            p.Crouch=.065f*scoop;p.HeadYaw=8*Sin(time*2);p.LeftWing=14*release;p.RightWing=24*release;
            p.Step=Sin(time*7)*.13f;
        }
        else if(action==OwlAction.BeePanic)
        {
            float alarm=Clamp(time/.18f,0,1)*Clamp((3.6f-time)/.7f,0,1);
            p.HeadPitch=-22*alarm;p.HeadYaw=36*Sin(time*17)*alarm;
            p.HeadRoll=12*Sin(time*23)*alarm;p.BodyPitch=-14*alarm;
            p.BodyRoll=9*Sin(time*19)*alarm;p.Crouch=.035f*alarm;
            p.LeftWing=(65+30*Sin(time*29))*alarm;
            p.RightWing=(65+30*Sin(time*29+1.1f))*alarm;
            p.BodyLift=.035f*(1+Sin(time*22))*alarm;
            p.HeadForward=-.045f*alarm;p.Step=.6f*Sin(time*23)*alarm;
        }
        else if(action==OwlAction.Reading)
        {
            p.HeadPitch=42+9*Sin(time*2.7f);p.HeadYaw=22*Sin(time*1.6f);
            p.HeadRoll=9*Sin(time*2);p.BodyPitch=12;p.Crouch=.018f;
            p.RightWing=38*Pulse(time,1.2f,1.4f);p.Step=.08f*Sin(time*4);
        }
        else if(action==OwlAction.Alert)
        {
            float t=time%3.6f;float bob=Pulse(t,.2f,.4f)+Pulse(t,.8f,.35f);
            p.HeadPitch=27*bob;p.Crouch=.055f*bob;p.HeadRoll=t>1.7f?30*Sin((t-1.7f)*2):0;
            p.HeadYaw=25*Sin(time*1.1f);
        }
        else
        {
            // Binocular inspection bobs, a sideways look, preening, stretch,
            // feather shake and a small alternating-foot weight shift.
            float bob=Pulse(phase,.4f,.38f)+Pulse(phase,1.15f,.45f);
            p.HeadPitch+=22*bob;p.Crouch+=.035f*bob;
            p.HeadYaw=42*Sin(time*.43f);p.HeadRoll=30*Pulse(phase,3,1.6f);
            float preen=Pulse(phase,7,3);
            p.HeadYaw+=55*preen;p.HeadPitch+=49*preen;p.BodyPitch=9*preen;
            p.RightWing=24*preen;
            float stretch=Pulse(phase,13,2.5f);p.LeftWing=75*stretch;p.RightWing+=35*stretch;
            float shake=Pulse(phase,18,1);p.BodyRoll=7*Sin(time*34)*shake;p.HeadRoll+=12*Sin(time*30)*shake;
            p.Step=.24f*Pulse(phase,21,1.2f)*Sin(time*9);
            if(action==OwlAction.Waiting){p.LeftWing=p.RightWing=65+28*Sin(time*22);p.FeetTuck=1;p.BodyPitch=8;p.HeadPitch=-6;p.BodyLift=.018f*(1+Sin(time*22));p.HeadYaw=Clamp(turn,-1,1)*20;}
        }
        p.Crouch=Clamp(p.Crouch,0,.13f);
        return p;
    }
}
