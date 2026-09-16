using Quartermaster;
internal static class GullFeedbackTests
{
    internal static void Run(Action<bool,string> check)
    {
        string Say(params StorageRoom[] rooms)=>SortingFeedback.Explain("Stone",rooms);
        var empty=new StorageRoom{CanTakeItem=true,HasAnySpace=true};
        var full=new StorageRoom{Matching=true};
        var otherStack=new StorageRoom{HasAnySpace=true};
        var busy=new StorageRoom{Matching=true,CanTakeItem=true,HasAnySpace=true};
        check(Say(empty).Contains("Where should Stone go")&&!Say(empty).Contains("add a chest"),"unassigned item asks for a destination while room exists");
        check(Say(full,empty).Contains("Where else")&&!Say(full,empty).Contains("add a chest"),"full assigned chest does not imply the whole base is full");
        check(!Say(full,otherStack).Contains("add a chest"),"remaining stack capacity for a different item still prevents expansion advice");
        check(Say(full).Contains("add a chest"),"only entirely full receiving storage suggests expansion");
        check(!Say().Contains("add a chest")&&Say().Contains("assign"),"no receiving chests asks for configuration");
        check(Say(busy).Contains("check access")&&!Say(busy).Contains("add a chest"),"busy matching capacity never reports a full base");
        var gate=new GullNoticeGate();
        check(gate.Take(0,"Stone"),"first problem announces immediately");
        check(!gate.Take(300,"Stone"),"unchanged problem never repeats on a timer");
        check(!gate.Take(5,"Wood"),"changing problems respect cooldown");
        check(gate.Take(31,"Wood"),"changed problem announces after cooldown");
        gate.Resolved();check(!gate.Take(32,"Wood"),"resolution does not bypass cooldown");
        check(gate.Take(62,"Wood"),"resolved problem can be announced on recurrence");
        check(!gate.Take(100,""),"empty state stays silent");
    }
}
