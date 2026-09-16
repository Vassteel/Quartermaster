using System;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster;

// Local NPC feedback only. Never sends a player chat message or creates a network object.
internal static class GullSpeech
{
    private static readonly AccessTools.FieldRef<Chat,float> HideTimer=AccessTools.FieldRefAccess<Chat,float>("m_hideTimer");
    internal static void Say(Transform speaker,string text)
    {
        if(!speaker || !Player.m_localPlayer || Player.m_localPlayer.IsDead())return;
        try
        {
            text=text.Replace('<',' ').Replace('>',' ');
            if(Chat.instance)
            {
                Chat.instance.AddString("<color=#83c9e8>Gull:</color> "+text);
                HideTimer(Chat.instance)=0;
            }
            else Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,"Gull: "+text);
            // Read native clips; do not clone the effect's behaviours or ZNetView.
            var prefab=ZNetScene.instance ? ZNetScene.instance.GetPrefab("Seagal") : null;
            var bird=prefab ? prefab.GetComponent<RandomFlyingBird>() : null;
            if(bird==null || bird.m_randomNoise==null)return;
            foreach(var entry in bird.m_randomNoise.m_effectPrefabs)
            {
                if(!entry.m_prefab)continue;
                foreach(var sfx in entry.m_prefab.GetComponentsInChildren<ZSFX>(true))
                {
                    if(sfx.m_audioClips==null || sfx.m_audioClips.Length==0)continue;
                    var clip=sfx.m_audioClips[UnityEngine.Random.Range(0,sfx.m_audioClips.Length)];
                    if(!clip)continue;
                    var go=new GameObject("Gull announcement squawk");go.transform.position=speaker.position;
                    var audio=go.AddComponent<AudioSource>();audio.playOnAwake=false;
                    var original=sfx.GetComponent<AudioSource>();
                    if(original)audio.outputAudioMixerGroup=original.outputAudioMixerGroup;
                    audio.clip=clip;audio.volume=.65f;audio.spatialBlend=1;
                    audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=3;audio.maxDistance=35;
                    audio.Play();UnityEngine.Object.Destroy(go,clip.length+.5f);return;
                }
            }
        }
        catch(Exception error){Debug.LogWarning("Gull announcement: "+error.Message);}
    }
}
