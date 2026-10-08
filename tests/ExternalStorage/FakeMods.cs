// Mirrors the optional signatures verified against installed 1.3.3 / 0.8.1 assemblies.
namespace OdinsFoodBarrels {
 internal static class RestrictContainers {
  internal static Dictionary<string,HashSet<string>> Items=new(){{"$barrel_berries",new(){"Raspberry"}},{"$seed_bag",new(){"CarrotSeeds"}}};
  private static bool IsRestrictedContainer(string name,out HashSet<string> items)=>Items.TryGetValue(name,out items);
 }
}
namespace DynamicStoragePiles {
 internal static class DynamicStoragePiles {
  internal static Dictionary<string,string> Items=new(){{"dynamic_wood","Wood"},{"modded_ore_pile","ModOre"}};
  public static bool IsStackPiece(string name,out string item)=>Items.TryGetValue(name,out item);
 }
 internal static class RestrictContainers {
  internal static bool Restricted=true;
  private static bool IsRestrictedContainer(string name,out string item){item=null;return Restricted&&DynamicStoragePiles.Items.TryGetValue(name,out item);}
 }
}
