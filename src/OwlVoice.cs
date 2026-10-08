using UnityEngine;

namespace Quartermaster;

internal static class OwlVoice
{
    private static AudioClip coo,cluck;
    private static float nextCall;
    internal static void Call(Transform owl,bool attention)
    {
        if(!owl||!Player.m_localPlayer||!Plugin.Instance||Plugin.OwlVolume.Value<=0||Time.time<nextCall||
            (Player.m_localPlayer.transform.position-owl.position).sqrMagnitude>324)return;
        nextCall=Time.time+Random.Range(90f,150f);
        var clip=attention?cluck:coo;
        if(!clip)
        {
            var samples=OwlVoiceSynth.Create(attention);
            clip=AudioClip.Create(attention?"Quartermaster owl cluck":"Quartermaster owl paired coo",samples.Length,1,OwlVoiceSynth.Rate,false);
            clip.SetData(samples,0);if(attention)cluck=clip;else coo=clip;
        }
        var source=owl.GetComponent<AudioSource>();
        if(!source)
        {
            source=owl.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=18;source.dopplerLevel=0;
            // Follow the game's SFX volume mixer without reusing a seagull call.
            var prefab=ZNetScene.instance?ZNetScene.instance.GetPrefab("sfx_seagull_idle"):null;
            var native=prefab?prefab.GetComponentInChildren<AudioSource>(true):null;
            if(native)source.outputAudioMixerGroup=native.outputAudioMixerGroup;
        }
        source.pitch=Random.Range(.96f,1.04f);source.volume=Plugin.OwlVolume.Value;source.PlayOneShot(clip);
    }
    internal static void Release()
    {if(coo)Object.Destroy(coo);if(cluck)Object.Destroy(cluck);coo=cluck=null;nextCall=0;}
}
