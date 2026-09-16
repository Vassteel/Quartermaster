using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

internal sealed class StorageRoom
{
    internal bool Matching, CanTakeItem, HasAnySpace;
}
internal static class SortingFeedback
{
    internal static string Explain(string item,IEnumerable<StorageRoom> storage)
    {
        var rooms=storage.ToList();
        if(rooms.Any(r=>r.Matching && r.CanTakeItem))
            return "I can't use the assigned storage for "+item+" just now. Close its lid and check access, Viking.";
        if(rooms.Count>0 && rooms.All(r=>!r.HasAnySpace))
            return "Every receiving chest is full, Viking. Free some space or add a chest for "+item+".";
        return rooms.Any(r=>r.Matching)
            ? "The assigned storage has no room for "+item+". Where else should it go, Viking?"
            : "Where should "+item+" go, Viking? Put a sample in a receiving chest or assign it in the chest menu.";
    }
}

// An unchanged problem speaks once; resolving it rearms the same future problem.
internal sealed class GullNoticeGate
{
    private string last="";
    private float next;
    internal bool Take(float now,string message)
    {
        if(string.IsNullOrEmpty(message) || message==last || now<next)return false;
        last=message;next=now+30;return true;
    }
    internal void Resolved(){last="";}
}
