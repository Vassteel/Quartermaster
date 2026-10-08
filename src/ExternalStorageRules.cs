using System;
using System.Collections.Generic;
using System.Reflection;

namespace Quartermaster;

// Bind to read-only metadata, not CanAddItem: those mods display a HUD error
// when CanAddItem rejects an item, which would spam during destination searches.
internal sealed class ExternalStorageRules
{
    internal delegate bool ItemRule(string name, out string item);
    internal delegate bool ItemSetRule(string name, out HashSet<string> items);
    private readonly ItemSetRule barrels;
    private readonly ItemRule piles, pileRestrictions;
    internal ExternalStorageRules(Assembly barrelAssembly, Assembly pileAssembly)
    {
        barrels=Bind<ItemSetRule>(barrelAssembly,"OdinsFoodBarrels.RestrictContainers","IsRestrictedContainer",typeof(HashSet<string>));
        piles=Bind<ItemRule>(pileAssembly,"DynamicStoragePiles.DynamicStoragePiles","IsStackPiece",typeof(string));
        pileRestrictions=Bind<ItemRule>(pileAssembly,"DynamicStoragePiles.RestrictContainers","IsRestrictedContainer",typeof(string));
    }
    private static T Bind<T>(Assembly assembly,string type,string method,Type output) where T:class
    {
        if(assembly==null)return null;
        var member=assembly.GetType(type)?.GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,
            null,new[]{typeof(string),output.MakeByRefType()},null);
        if(member==null)throw new MissingMethodException(type,method);
        return Delegate.CreateDelegate(typeof(T),member) as T;
    }
    internal bool Recognizes(string prefab,string name) =>
        (piles!=null&&piles(prefab,out _))||(barrels!=null&&barrels(name,out _));
    internal IEnumerable<string> Assigned(string prefab,string name)
    {
        if(piles!=null&&piles(prefab,out var item))return new[]{item};
        if(barrels!=null&&barrels(name,out var items))return items??(IEnumerable<string>)Array.Empty<string>();
        return Array.Empty<string>();
    }
    internal bool Allows(string inventoryName,string item)
    {
        if(barrels!=null&&barrels(inventoryName,out var items)&&(items==null||!items.Contains(item)))return false;
        // Calls the live rule so changes to Restrict Container Item Type take effect.
        return pileRestrictions==null||!pileRestrictions(inventoryName,out var only)||only==item;
    }
}
