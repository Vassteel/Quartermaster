using System;
namespace Quartermaster;
internal static class MeadStoragePolicy
{
    internal const int Columns=6,Rows=4,DisplayCount=12;
    internal static bool Accepts(string id,bool consumable,bool fermentedWithEffect=false)=>consumable&&!string.IsNullOrEmpty(id)
        &&!id.StartsWith("MeadBase",StringComparison.OrdinalIgnoreCase)
        &&(id.StartsWith("Mead",StringComparison.OrdinalIgnoreCase)||fermentedWithEffect);
}
