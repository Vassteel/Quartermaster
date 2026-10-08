using System;
using System.Collections.Generic;
using System.Linq;

namespace Quartermaster;

internal static class InventoryTransfers
{
    // Optional hard furniture admission rule, shared by all Quartermaster transfer paths.
    internal static Func<Inventory,ItemDrop.ItemData,bool> Admission;
    private static bool Matches(ItemDrop.ItemData item, string name, int quality, bool worldLevel)
    {
        if (item?.m_shared == null || string.IsNullOrEmpty(name) || item.m_stack <= 0) return false;
        return string.Equals(name, item.m_shared.m_name, StringComparison.Ordinal)
            && (quality < 0 || quality == item.m_quality)
            && (!worldLevel || Game.m_worldLevel <= item.m_worldLevel);
    }

    internal static int CountType(Inventory inventory, string sharedName, int quality = -1, bool matchWorldLevel = false)
    {
        long quantity = 0;
        if (inventory != null)
            foreach (var item in inventory.GetAllItems())
                if (Matches(item, sharedName, quality, matchWorldLevel)) quantity += item.m_stack;
        return (int)Math.Min(quantity, int.MaxValue);
    }

    internal static int AvailableInContainer(Container container, string sharedName, int quality = -1, bool matchWorldLevel = false)
    {
        // Callers refresh and validate their source before counting.
        // No separate per-item reservation or physical sample reserve.
        return CountType(ContainerRegistry.SafeInventory(container), sharedName, quality, matchWorldLevel);
    }

    internal static string ItemId(ItemDrop.ItemData item) => item?.m_dropPrefab ? item.m_dropPrefab.name : item?.m_shared?.m_name ?? "";
    // Recipe/fuel references are prefab components: their ItemData.m_dropPrefab is
    // nonserialized and is initialized only when an ItemDrop instance runs Awake.
    internal static string PrefabId(ItemDrop prefab) => prefab ? prefab.gameObject.name : "";
    // Food/materials can carry different unused durability values after creation
    // or loading. Compare wear only for items whose definition actually uses it.
    internal static bool Stackable(ItemDrop.ItemData a, ItemDrop.ItemData b)
    {
        if (a?.m_shared == null || b?.m_shared == null || ItemId(a) != ItemId(b) || !a.IsSameType(b)
            || a.m_quality != b.m_quality || a.m_variant != b.m_variant || a.m_worldLevel != b.m_worldLevel
            || a.m_crafterID != b.m_crafterID || (a.m_crafterName ?? "") != (b.m_crafterName ?? "")
            || ((a.m_shared.m_useDurability || b.m_shared.m_useDurability) && a.m_durability != b.m_durability)
            || a.m_cheated != b.m_cheated) return false;
        var x = a.m_customData; var y = b.m_customData;
        if ((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
        if (x != null) foreach (var pair in x) if (y == null || !y.TryGetValue(pair.Key, out var v) || v != pair.Value) return false;
        return true;
    }
    internal static int CapacityFor(Inventory destination, ItemDrop.ItemData source, bool matchingOnly)
    {
        if (destination == null || source?.m_shared == null || (Admission!=null&&!Admission(destination,source))) return 0;
        bool match = false; long capacity = 0;
        foreach (var item in destination.GetAllItems())
        {
            match |= ItemId(item) == ItemId(source);
            if (Stackable(item, source)) capacity += Math.Max(0, item.m_shared.m_maxStackSize - item.m_stack);
        }
        if (matchingOnly && !match) return 0;
        capacity += Math.Max(0, destination.GetWidth() * destination.GetHeight() - destination.NrOfItems()) * (long)Math.Max(1, source.m_shared.m_maxStackSize);
        return (int)Math.Min(int.MaxValue, capacity);
    }
    // Mutate both inventories before notifying listeners. No AddItem partial-failure duplication,
    // and no mixing of equipment/custom metadata. Each new stack stays at the game's limit.
    internal static int Move(Inventory source, Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
    {
        if (source == null || source == destination || item == null || !source.ContainsItem(item)) return 0;
        int count = Math.Min(item.m_stack, Math.Min(Math.Max(0, requested), CapacityFor(destination, item, matchingOnly)));
        if (count <= 0) return 0;
        var copy = item.Clone();
        int moved = AddRaw(destination, copy, count);
        item.m_stack -= moved;
        if (item.m_stack == 0) source.GetAllItems().Remove(item);
        Notify(source); Notify(destination);
        return moved;
    }
    internal static int AddCopy(Inventory destination, ItemDrop.ItemData item, int requested, bool matchingOnly)
    {
        int count = Math.Min(Math.Max(0, requested), CapacityFor(destination, item, matchingOnly));
        if (count <= 0) return 0;
        int moved = AddRaw(destination, item, count); Notify(destination); return moved;
    }
    // Reserve a concrete slot/stack before consuming a world item. The native
    // RemoveOne handler persists the source without clamping oversized old piles.
    // Notify chest listeners only after both sides have their final quantities.
    internal static bool ReceiveOne(Inventory destination,ItemDrop.ItemData source,Func<bool> take)
    {
        if(destination==null||source?.m_shared==null||CapacityFor(destination,source,false)<1)return false;
        var items=destination.GetAllItems();
        foreach(var stack in items)
        {
            if(!Stackable(stack,source)||stack.m_stack>=stack.m_shared.m_maxStackSize)continue;
            if(!take())return false;stack.m_stack++;Notify(destination);return true;
        }
        int width=destination.GetWidth();var occupied=new HashSet<int>();
        foreach(var stack in items)occupied.Add(stack.m_gridPos.y*width+stack.m_gridPos.x);
        for(int slot=0;slot<width*destination.GetHeight();slot++)
        {
            if(occupied.Contains(slot))continue;
            var copy=source.Clone();copy.m_stack=1;copy.m_equipped=false;copy.m_gridPos=new Vector2i(slot%width,slot/width);
            if(!take())return false;items.Add(copy);Notify(destination);return true;
        }
        return false;
    }
    // Emit one legal stack at a time. A failed spawn leaves the source untouched.
    // This also handles limits lowered while an old inventory is unloaded.
    internal static int SplitExcess(Inventory inventory)
    {
        if (inventory == null) return 0;
        int moved = 0;
        foreach (var item in inventory.GetAllItems().ToArray())
        {
            if (item?.m_shared == null) continue;
            int excess = item.m_stack - Math.Max(1, item.m_shared.m_maxStackSize);
            if (excess <= 0) continue;
            // AddRaw skips this oversized source and fills compatible stacks/free
            // cells. Keep metadata, then notify only once both sides are balanced.
            int stored = AddRaw(inventory, item, excess);
            item.m_stack -= stored;
            moved += stored;
        }
        if (moved > 0) Notify(inventory);
        return moved;
    }
    internal static int EjectExcess(Inventory inventory,int budget,Func<ItemDrop.ItemData,bool> spawn)
    {
        if(inventory==null||budget<=0)return 0;
        int drops=0;
        foreach(var item in inventory.GetAllItems().ToArray())
        {
            if(item?.m_shared==null)continue;
            int limit=Math.Max(1,item.m_shared.m_maxStackSize);
            while(item.m_stack>limit&&drops<budget)
            {
                int amount=Math.Min(limit,item.m_stack-limit);
                var copy=item.Clone();copy.m_stack=amount;copy.m_equipped=false;
                if(!spawn(copy))return drops;
                item.m_stack-=amount;drops++;Notify(inventory);
            }
            if(drops>=budget)break;
        }
        return drops;
    }
    private static int AddRaw(Inventory inventory, ItemDrop.ItemData item, int count)
    {
        int left = count;
        var items = inventory.GetAllItems();
        foreach (var stack in items)
        {
            if (!Stackable(stack, item)) continue;
            int n = Math.Min(left, Math.Max(0, stack.m_shared.m_maxStackSize - stack.m_stack));
            stack.m_stack += n; left -= n;
            if (left == 0) return count;
        }
        var occupied = new HashSet<int>(); int width = inventory.GetWidth();
        foreach (var stack in items) occupied.Add(stack.m_gridPos.y * width + stack.m_gridPos.x);
        for (int slot = 0; slot < width * inventory.GetHeight() && left > 0; slot++)
        {
            if (occupied.Contains(slot)) continue;
            var stack = item.Clone(); stack.m_stack = Math.Min(left, Math.Max(1, item.m_shared.m_maxStackSize));
            stack.m_gridPos = new Vector2i(slot % width, slot / width); stack.m_equipped = false;
            items.Add(stack); left -= stack.m_stack;
        }
        return count - left;
    }
    internal static void Notify(Inventory inventory)
    {
        // Recalculate weight and invoke game's persistence callbacks.
        try { HarmonyLib.AccessTools.Method(typeof(Inventory), "Changed").Invoke(inventory, new object[] { false, false }); }
        catch (Exception e) { Plugin.Log?.LogError("Inventory changed callback failed: " + e); }
    }
    internal static void StackWithin(Inventory inventory)
    {
        var items = inventory.GetAllItems(); bool changed = false;
        for (int i = 0; i < items.Count; i++)
            for (int j = items.Count - 1; j > i; j--)
            {
                if (!Stackable(items[i], items[j])) continue;
                int n = Math.Min(items[j].m_stack, Math.Max(0, items[i].m_shared.m_maxStackSize - items[i].m_stack));
                if (n <= 0) continue;
                items[i].m_stack += n; items[j].m_stack -= n; changed = true;
                if (items[j].m_stack == 0) items.RemoveAt(j);
            }
        if (changed) Notify(inventory);
    }

    internal static int Remove(Inventory inventory, string sharedName, int amount, int quality, bool matchWorldLevel)
    {
        if (amount <= 0 || inventory == null) return 0;
        var candidates = inventory.GetAllItems()
            .Where(item => Matches(item, sharedName, quality, matchWorldLevel))
            .OrderBy(item => item.m_stack).ToArray();
        int removed = 0;
        foreach (var stack in candidates)
        {
            if (removed == amount) break;
            // Native removal persists each change; callbacks may alter later candidates.
            if (!inventory.ContainsItem(stack) || !Matches(stack, sharedName, quality, matchWorldLevel)) continue;
            int portion = Math.Min(amount - removed, stack.m_stack);
            if (inventory.RemoveItem(stack, portion)) removed += portion;
        }
        return removed;
    }

    internal static bool HasType(Inventory inventory, ItemDrop.ItemData item) =>
        item?.m_shared != null && inventory != null
        && inventory.GetAllItems().Any(stored => stored?.m_shared != null && stored.IsSameType(item));
}
