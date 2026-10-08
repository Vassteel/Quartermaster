using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Quartermaster;

// Use the JSON library shipped with Valheim, independently of Unity's native
// field serializer. Keep the existing key and field names for saved worlds.
internal static class LedgerCodec
{
    private static JsonSerializer Serializer()=>JsonSerializer.Create(new JsonSerializerSettings
    {
        TypeNameHandling=TypeNameHandling.None,
        Culture=CultureInfo.InvariantCulture
    });
    internal static LedgerSettings Read(string json)
    {
        using(var reader=new JsonTextReader(new StringReader(json)))
        {
            var value=Serializer().Deserialize<LedgerSettings>(reader);
            if(value==null)throw new JsonSerializationException("Ledger settings were null");
            value.Group=value.Group??"";
            value.Caps=(value.Caps??new List<ProductCap>()).Where(c=>c!=null).ToList();
            return value;
        }
    }
    internal static string Write(LedgerSettings value)
    {
        using(var text=new StringWriter(CultureInfo.InvariantCulture))
        using(var writer=new JsonTextWriter(text))
        {
            Serializer().Serialize(writer,value);
            writer.Flush();return text.ToString();
        }
    }
    internal static LedgerSettings Copy(LedgerSettings value)=>new LedgerSettings
    {
        Group=value.Group,UpdatedTicks=value.UpdatedTicks,
        Caps=value.Caps.Select(c=>new ProductCap{Item=c.Item,Amount=c.Amount}).ToList()
    };
    internal static bool Equal(LedgerSettings a,LedgerSettings b)=>
        a.Group==b.Group&&a.UpdatedTicks==b.UpdatedTicks&&a.Caps.Count==b.Caps.Count&&
        a.Caps.Zip(b.Caps,(x,y)=>x.Item==y.Item&&x.Amount==y.Amount).All(equal=>equal);
}
