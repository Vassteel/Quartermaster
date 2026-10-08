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
                Chat.instance.AddString("<color=#83c9e8>Owl:</color> "+text);
                HideTimer(Chat.instance)=0;
            }
            else Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,"Owl: "+text);
            OwlVoice.Call(speaker,true);
        }
        catch(Exception error){Debug.LogWarning("Gull announcement: "+error.Message);}
    }
}
