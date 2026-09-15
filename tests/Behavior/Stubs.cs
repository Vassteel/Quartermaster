using System.Reflection;
// Minimal host interfaces for executing the shipped transfer/policy code without starting Unity.
// Binary API linkage is checked separately against the actual installed game.
public sealed class Prefab { public string name; public static implicit operator bool(Prefab p) => p != null; }
public struct Vector2i { public int x, y; public Vector2i(int x, int y) { this.x=x; this.y=y; } }
public static class Game { public static int m_worldLevel; }
public sealed class Container : UnityEngine.MonoBehaviour { public Inventory Inventory; public bool Accessible = true; public Quartermaster.ChestSettings Settings = new(); }
public class ItemDrop
{
    public Prefab gameObject;
    public ItemData m_itemData;
    public static implicit operator bool(ItemDrop item) => item != null;
    public class SharedData { public string m_name; public int m_maxStackSize = 50; }
    public class ItemData
    {
        public Prefab m_dropPrefab;
        public SharedData m_shared;
        public int m_stack, m_quality=1, m_variant, m_worldLevel;
        public long m_crafterID;
        public string m_crafterName="";
        public float m_durability=100;
        public bool m_cheated, m_equipped;
        public Vector2i m_gridPos;
        public Dictionary<string,string> m_customData = new();
        public bool IsSameType(ItemData i) => m_shared.m_name == i.m_shared.m_name && m_quality == i.m_quality && m_worldLevel == i.m_worldLevel;
        public ItemData Clone() { var i=(ItemData)MemberwiseClone(); i.m_customData=new(m_customData); return i; }
    }
}
public sealed class Inventory
{
    readonly List<ItemDrop.ItemData> items = new(); readonly int width, height;
    public int Notifications; public Action OnChanged;
    public Inventory(int w, int h) { width=w; height=h; }
    public List<ItemDrop.ItemData> GetAllItems() => items;
    public int GetWidth()=>width; public int GetHeight()=>height; public int NrOfItems()=>items.Count;
    public bool ContainsItem(ItemDrop.ItemData i)=>items.Contains(i);
    public bool RemoveItem(ItemDrop.ItemData i, int amount) { if (!items.Contains(i)||amount<0||amount>i.m_stack)return false; i.m_stack-=amount;if(i.m_stack==0)items.Remove(i);Changed();return true; }
    private void Changed(bool success=false, bool cheatedStateChanged=false) { Notifications++; OnChanged?.Invoke(); }
}
namespace HarmonyLib { public static class AccessTools { public static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); } }
namespace Quartermaster
{
    internal static class ContainerRegistry
    {
        internal static readonly List<Container> All = new();
        internal static Inventory SafeInventory(Container c)=>c.Inventory;
        internal static int ReserveFor(Container c,string name)=>0;
        internal static bool Accessible(Container c)=>c.Accessible;
        internal static ChestSettings GetSettings(Container c)=>c.Settings;
    }
    internal sealed class TestLog { public void LogError(string s)=>throw new Exception(s); public void LogWarning(string s)=>throw new Exception(s); }
    internal sealed class TestSetting<T> { internal T Value; internal TestSetting(T value) { Value=value; } }
    internal static class Plugin
    {
        internal static TestLog Log=new();
        internal static TestSetting<bool> Enabled=new(true), ExtendStationCoverage=new(true);
        internal static TestSetting<float> Range=new(100f);
    }
}
