using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace Quartermaster;

internal sealed class DeliveryEntry
{
    public string Origin, Player;
    public long UtcTicks;
    public int Count;
}
internal static class DeliveryHistory
{
    private const string Key="QM.mail.received.v1";
    internal static List<DeliveryEntry> Decode(string json)
    {
        try { return (JsonConvert.DeserializeObject<List<DeliveryEntry>>(json) ?? new List<DeliveryEntry>())
            .Where(e=>e!=null&&e.Count>0&&e.UtcTicks>=DateTime.MinValue.Ticks&&e.UtcTicks<=DateTime.MaxValue.Ticks).Take(100).ToList(); }
        catch { return new List<DeliveryEntry>(); }
    }
    internal static IEnumerable<DeliveryEntry> Read(Container home)
    {
        var view=ContainerRegistry.GetView(home);
        return view&&view.IsValid()?Decode(view.GetZDO().GetString(Key,"")):new List<DeliveryEntry>();
    }
    internal static void Record(Container home,ZDO parcel,int count)
    {
        var view=ContainerRegistry.GetView(home);if(count<=0||!NativeStorageAccess.CanWrite(view))return;
        var entries=Read(home).ToList();
        entries.Insert(0,new DeliveryEntry { Origin=Short(parcel.GetString("QM.mail.originName","Unknown origin")),
            Player=Short(parcel.GetString("QM.mail.playerName","Unknown sender")),UtcTicks=DateTime.UtcNow.Ticks,Count=count });
        // Bound persistent network data; partial deliveries log only actual arrivals.
        view.GetZDO().Set(Key,JsonConvert.SerializeObject(entries.Take(100).ToList()));
    }
    private static string Short(string value)=>value.Length>100?value.Substring(0,100):value;
}
