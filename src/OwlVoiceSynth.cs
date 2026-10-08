using System;

namespace Quartermaster;

// Original synthesized calls, not borrowed recordings. Soft paired coos and
// brief breathy clucks with continuous phase and click-free envelopes.
internal static class OwlVoiceSynth
{
    internal const int Rate=22050;
    internal static float[] Create(bool cluck)
    {
        float duration=cluck?.48f:1.45f;var data=new float[(int)(Rate*duration)];
        var random=new Random(cluck?491:217);double phase=0;float breath=0;
        for(int i=0;i<data.Length;i++)
        {
            float time=(float)i/Rate;
            float local=cluck?time:time<.55f?time:time-.69f;
            float length=cluck?.28f:time<.55f?.40f:.59f;
            float env=local>=0&&local<length?(float)Math.Pow(Math.Sin(local/length*Math.PI),1.6):0;
            float frequency=cluck?900-900*local:440+25*(float)Math.Sin(local*8)-48*local;
            frequency+=8*(float)Math.Sin(time*37);
            phase+=2*Math.PI*frequency/Rate;
            breath=breath*.82f+((float)random.NextDouble()*2-1)*.18f;
            float voiced=(float)(Math.Sin(phase)+.16*Math.Sin(phase*2)+.035*Math.Sin(phase*3));
            data[i]=env*(voiced*.42f+breath*(cluck?.48f:.07f));
        }
        return data;
    }
}
