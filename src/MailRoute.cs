namespace Quartermaster;
// Persisted wire values; terminal states are never reactivated by retries.
internal enum MailPhase { Preparing=0, Waiting=1, Returning=2, Delivered=3, Returned=4, Cancelled=5 }
internal static class MailRoute
{
    internal static bool Terminal(MailPhase p)=>p==MailPhase.Delivered||p==MailPhase.Returned||p==MailPhase.Cancelled;
    internal static MailPhase Cancel(MailPhase p)=>Terminal(p)?p:MailPhase.Returning;
    internal static MailPhase Finish(MailPhase p,int remaining)=>remaining>0?p:p==MailPhase.Returning?MailPhase.Returned:MailPhase.Delivered;
}
