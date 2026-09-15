namespace Quartermaster.Cosmetics;

public enum GullMood { Calm, RoughSeas, Storm, Fog, Combat }

/// <summary>Combat is immediate and lingers; weather must settle before the animation state changes.</summary>
public sealed class GullMoodState
{
    public GullMood Current { get; private set; }
    private GullMood candidate;
    private double candidateSince, combatUntil = double.NegativeInfinity;

    public GullMood Update(double now, bool combat, bool storm, bool fog, bool rough)
    {
        if(combat) combatUntil=now+6;
        if(now<combatUntil) {Current=GullMood.Combat;candidate=GullMood.Combat;candidateSince=now;return Current;}
        var next=storm ? GullMood.Storm : fog ? GullMood.Fog : rough ? GullMood.RoughSeas : GullMood.Calm;
        if(next!=candidate) {candidate=next;candidateSince=now;}
        if(now-candidateSince>=3) Current=candidate;
        return Current;
    }
}
