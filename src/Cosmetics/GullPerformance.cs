using System;

namespace Quartermaster.Cosmetics;

public struct GullPose
{
    public double HeadPitch, HeadYaw, HeadRoll;
    public double BodyPitch, BodyYaw, BodyRoll, Crouch;
    public double TailYaw, TailFan, Hop;
}

/// <summary>Timed gestures in degrees/metres, evaluated from a fresh rest pose each frame.</summary>
public static class GullPerformance
{
    public static GullPose Blend(GullPose a,GullPose b,double amount)
    {
        double t=Clamp(amount,0,1);
        double L(double x,double y)=>x+(y-x)*t;
        return new GullPose {
            HeadPitch=L(a.HeadPitch,b.HeadPitch),HeadYaw=L(a.HeadYaw,b.HeadYaw),HeadRoll=L(a.HeadRoll,b.HeadRoll),
            BodyPitch=L(a.BodyPitch,b.BodyPitch),BodyYaw=L(a.BodyYaw,b.BodyYaw),BodyRoll=L(a.BodyRoll,b.BodyRoll),
            Crouch=L(a.Crouch,b.Crouch),TailYaw=L(a.TailYaw,b.TailYaw),TailFan=L(a.TailFan,b.TailFan),Hop=b.Hop
        };
    }
    public static GullPose Sample(GullMood mood,double elapsed,double threatYaw=0,double windYaw=0,double shipRoll=0,double shipPitch=0)
    {
        var p=new GullPose();
        double t=Math.Max(0,elapsed);
        switch(mood)
        {
            case GullMood.Calm:
                t%=14;
                p.HeadYaw=28*Window(t,.7,3)-32*Window(t,5.5,7.3)+78*Window(t,9,11.8);
                p.HeadPitch=38*(Pulse(t,3.5,.65)+Pulse(t,4.35,.55))+24*Window(t,9.5,11.5);
                p.HeadRoll=12*Window(t,1.5,2.6)-8*Window(t,6,7);
                p.BodyPitch=6*(Pulse(t,3.5,.65)+Pulse(t,4.35,.55));
                p.Crouch=.012*(1+Math.Sin(t*Math.PI*2/3.5));
                p.TailYaw=13*Math.Sin(t*19)*Window(t,8,8.8);
                p.TailFan=.18*Window(t,10,11.5);
                break;
            case GullMood.RoughSeas:
                t%=7;
                p.BodyRoll=Clamp(-shipRoll*.7,-14,14)+4*Math.Sin(t*Math.PI*2/7);
                p.BodyPitch=5+Clamp(-shipPitch*.5,-8,8);
                p.Crouch=.06+.025*(1+Math.Sin(t*Math.PI*4/7));
                p.HeadRoll=-p.BodyRoll*.7;p.HeadPitch=-p.BodyPitch*.5;
                p.HeadYaw=24*Window(t,1.2,2.7)-24*Window(t,4,5.7);
                p.TailFan=.3;p.TailYaw=-p.BodyRoll;
                p.Hop=.12*Pulse(t,5.8,.65);
                break;
            case GullMood.Storm:
                t%=10;
                p.BodyYaw=Clamp(windYaw,-65,65);
                p.BodyPitch=13+3*Math.Sin(t*Math.PI*4/10);p.Crouch=.14;
                p.HeadPitch=24-32*Window(t,6.6,8.1);
                p.HeadYaw=-20*Window(t,6.7,7.5)+20*Window(t,7.5,8.3);
                double shake=Window(t,2.5,3.7);
                p.BodyRoll=7*Math.Sin(t*32)*shake;
                p.HeadRoll=-10*Math.Sin(t*32)*shake;
                p.TailYaw=18*Math.Sin(t*26)*shake;p.TailFan=.25*shake;
                break;
            case GullMood.Fog:
                t%=12;
                p.BodyPitch=-5;p.Crouch=-.025;
                p.HeadPitch=-12+22*Window(t,8.5,10);
                p.HeadYaw=-55*Window(t,.7,3.2)+55*Window(t,4.2,7.4);
                p.HeadRoll=18*Window(t,2,3)-18*Window(t,5.8,7);
                p.BodyYaw=p.HeadYaw*.2;
                p.TailYaw=12*Window(t,9,10.8);
                p.Hop=.16*Pulse(t,10.4,.8);
                break;
            case GullMood.Combat:
                t%=6;
                p.BodyYaw=Clamp(threatYaw,-80,80)*Window(t,0,5.8,.15);
                p.HeadYaw=Clamp(threatYaw-p.BodyYaw,-55,55)+9*Math.Sin(t*22)*Window(t,1.5,2.2);
                p.HeadPitch=-18+30*Pulse(t,2.5,.35)+30*Pulse(t,2.95,.35);
                p.BodyPitch=-7+14*Window(t,2.4,3.4);
                p.Crouch=.09*Pulse(t,0,.28)+.05*Window(t,2.3,3.5);
                p.Hop=.4*Pulse(t,.28,1.05)+.25*Pulse(t,4.15,.8);
                p.TailFan=.6;p.TailYaw=15*Math.Sin(t*16)*Window(t,1.4,2.2);
                break;
        }
        return p;
    }
    private static double Clamp(double x,double a,double b)=>Math.Max(a,Math.Min(b,x));
    private static double Smooth(double x){x=Clamp(x,0,1);return x*x*(3-2*x);}
    private static double Window(double t,double start,double end,double edge=.35)=>Smooth((t-start)/edge)*Smooth((end-t)/edge);
    private static double Pulse(double t,double start,double duration)
    {if(t<=start || t>=start+duration)return 0;double s=Math.Sin((t-start)/duration*Math.PI);return s*s;}
}
