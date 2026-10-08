using System;
using UnityEngine;
namespace Quartermaster;

// Short, original contact sounds. SFX volume follows the native effects mixer.
internal static class FurnitureAudio
{
    private static AudioClip clay,glass,hinge,latch;
    private static readonly AudioClip[] bulk=new AudioClip[11];
    private static float next;
    private static AudioClip Make(bool crystal)
    {
        const int rate=22050;int n=(int)(rate*.24f);var samples=new float[n];var random=new System.Random(crystal?920:481);
        for(int i=0;i<n;i++)
        {
            float t=i/(float)rate;
            double tap=crystal?(Math.Sin(t*2380*2*Math.PI)+.4*Math.Sin(t*3610*2*Math.PI))*Math.Exp(-t*27):
                (Math.Sin(t*310*2*Math.PI)*.6+(random.NextDouble()-.5)*.7)*Math.Exp(-t*33);
            samples[i]=(float)tap*.15f*Math.Min(1,t/.002f);
        }
        var clip=AudioClip.Create(crystal?"Quartermaster flask contact":"Quartermaster clay contact",n,1,rate,false);clip.SetData(samples,0);return clip;
    }
    internal static void Contact(Vector3 point,FurnitureHandling style)
    {
        if(style==FurnitureHandling.Gem){Contact(point,true);return;}
        int index=style==FurnitureHandling.Treasure?1:style==FurnitureHandling.Trophy?10:style==FurnitureHandling.Wardrobe?3:style==FurnitureHandling.Ammunition?9:style==FurnitureHandling.Weapon||style==FurnitureHandling.Shield?10:style==FurnitureHandling.Hanging?7:style==FurnitureHandling.Grain?8:style==FurnitureHandling.Pantry?3:style==FurnitureHandling.Masonry?6:style==FurnitureHandling.Lumber?0:style==FurnitureHandling.Ingot?1:style==FurnitureHandling.Hide||style==FurnitureHandling.Textile?3:style==FurnitureHandling.Feather?4:style==FurnitureHandling.Bone?5:2;
        if(!Ready(point))return;
        if(!bulk[index])
        {
            const int rate=22050;var samples=new float[(int)(rate*.28f)];var random=new System.Random(930+index);
            for(int i=0;i<samples.Length;i++)
            {
                double t=i/(double)rate,noise=random.NextDouble()-.5;
                double tone=index==0?Math.Sin(t*155*2*Math.PI)*.65+noise*.22:
                    index==1?Math.Sin(t*680*2*Math.PI)*.4+Math.Sin(t*1130*2*Math.PI)*.15+noise*.08:
                    index==9?Math.Sin(t*420*2*Math.PI)*.18+noise*.12:
                    index==10?Math.Sin(t*190*2*Math.PI)*.3+Math.Sin(t*870*2*Math.PI)*.07+noise*.08:
                    index==3?noise*.30*(.6+.4*Math.Sin(t*65)):
                    index==4?noise*.16:
                    index==7?Math.Sin(t*980*2*Math.PI)*.12+Math.Sin(t*1610*2*Math.PI)*.06+noise*.05:
                    index==8?noise*.27*(.6+.4*Math.Sin(t*160)):
                    index==6?(Math.Sin(t*180*2*Math.PI)*.32+noise*.26):
                    index==5?(Math.Sin(t*950*2*Math.PI)*.25+noise*.22):
                    noise*(.6+.25*Math.Sin(t*110));
                samples[i]=(float)(tone*Math.Exp(-t*(index==1?22:30))*.17*Math.Min(1,t/.003));
            }
            bulk[index]=AudioClip.Create("Quartermaster "+new[]{"timber placement","ingot contact","loose material","cloth rustle","feather rustle","bone clack","stone placement","hanging contact","grain rustle","ammunition seating","equipment seating"}[index],samples.Length,1,rate,false);bulk[index].SetData(samples,0);
        }
        Play(point,bulk[index]);
    }
    internal static void Door(Vector3 point,bool closing)
    {
        if(!Ready(point))return;
        var clip=closing?latch:hinge;
        if(!clip)
        {
            const int rate=22050;var samples=new float[(int)(rate*.28f)];var random=new System.Random(closing?719:718);
            for(int i=0;i<samples.Length;i++)
            {
                double t=i/(double)rate,noise=random.NextDouble()-.5;
                double signal=closing?(Math.Sin(t*230*2*Math.PI)*.35+noise*.15)*Math.Exp(-t*32):
                    (Math.Sin((t*170+t*t*140)*2*Math.PI)*.15+noise*.09)*Math.Sin(Math.PI*t/.28);
                samples[i]=(float)(signal*.12*Math.Min(1,t/.008));
            }
            clip=AudioClip.Create(closing?"Quartermaster wardrobe latch":"Quartermaster wardrobe hinge",samples.Length,1,rate,false);clip.SetData(samples,0);
            if(closing)latch=clip;else hinge=clip;
        }
        Play(point,clip);
    }
    private static bool Ready(Vector3 point)=>Time.time>=next&&Player.m_localPlayer&&(Player.m_localPlayer.transform.position-point).sqrMagnitude<=144;
    internal static void Contact(Vector3 point,bool crystal)
    {
        if(!Ready(point))return;
        if(!clay)clay=Make(false);if(!glass)glass=Make(true);
        Play(point,crystal?glass:clay);
    }
    private static void Play(Vector3 point,AudioClip clip)
    {
        next=Time.time+1.5f;
        var go=new GameObject("Quartermaster furniture contact");go.transform.position=point;
        var audio=go.AddComponent<AudioSource>();audio.clip=clip;audio.spatialBlend=1;audio.minDistance=1;audio.maxDistance=12;audio.rolloffMode=AudioRolloffMode.Linear;audio.volume=.3f;
        // Use the game's mixer group when available; never alter global sound settings.
        var native=ZNetScene.instance?ZNetScene.instance.GetPrefab("sfx_seagull_idle"):null;
        var source=native?native.GetComponentInChildren<AudioSource>(true):null;
        if(source)audio.outputAudioMixerGroup=source.outputAudioMixerGroup;
        audio.Play();UnityEngine.Object.Destroy(go,.4f);
    }
    internal static void Release(){if(clay)UnityEngine.Object.Destroy(clay);if(glass)UnityEngine.Object.Destroy(glass);if(hinge)UnityEngine.Object.Destroy(hinge);if(latch)UnityEngine.Object.Destroy(latch);clay=glass=hinge=latch=null;for(int i=0;i<bulk.Length;i++){if(bulk[i])UnityEngine.Object.Destroy(bulk[i]);bulk[i]=null;}next=0;}
}
