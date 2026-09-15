using Quartermaster;
using System.Text.Json;

internal static class ItemTagTests
{
    internal static void Run(Action<bool,string> assert)
    {
        ItemDrop.ItemData Tagged()=>new() {
            m_dropPrefab=new Prefab{name="Wood"},m_shared=new ItemDrop.SharedData{m_name="Wood"},
            m_cheated=true,m_stack=37,m_quality=2,m_variant=3,m_worldLevel=4,m_crafterID=123,
            m_crafterName="Builder",m_durability=47,m_equipped=true,m_gridPos=new Vector2i(3,2),
            m_customData=new(){{"custom","preserved"}}
        };
        void Reset()
        {
            ItemTagCleanup.Clear(); ContainerRegistry.All.Clear();
            Player.m_localPlayer=new Player();Plugin.Enabled.Value=true;Plugin.ClearCheatItemTagsOnLoad.Value=true;
        }
        Reset();
        var inventory=Player.m_localPlayer.GetInventory();var item=Tagged();inventory.GetAllItems().Add(item);
        var expected=item.Clone();expected.m_cheated=false;
        var options=new JsonSerializerOptions{IncludeFields=true};
        ItemTagCleanup.Tick();assert(item.m_cheated,"player scan waits for completed spawn/load");
        ItemTagCleanup.PlayerLoaded(new Player());ItemTagCleanup.Tick();assert(item.m_cheated,"remote player load cannot trigger local scan");
        ItemTagCleanup.PlayerLoaded(Player.m_localPlayer);
        Plugin.ClearCheatItemTagsOnLoad.Value=false;ItemTagCleanup.Tick();assert(item.m_cheated,"config off does not clear tags");
        Plugin.ClearCheatItemTagsOnLoad.Value=true;Plugin.Enabled.Value=false;ItemTagCleanup.Tick();assert(item.m_cheated,"disabled plugin does not clear tags");
        Plugin.Enabled.Value=true;
        bool callbackClean=false;inventory.OnChanged=()=>callbackClean=inventory.GetAllItems().All(i=>!i.m_cheated);
        ItemTagCleanup.Tick();
        assert(!item.m_cheated&&callbackClean&&inventory.Notifications==1,"player flags cleared before one persistence notification");
        assert(JsonSerializer.Serialize(item,options)==JsonSerializer.Serialize(expected,options),"scan changes only the cheat flag, preserving all item metadata");
        var addedLater=Tagged();inventory.GetAllItems().Add(addedLater);ItemTagCleanup.Tick();
        assert(addedLater.m_cheated&&inventory.Notifications==1,"startup scan does not continuously rewrite new items");
        var storage=new Container{Inventory=new Inventory(4,4)};ContainerRegistry.All.Add(storage);
        ItemTagCleanup.ContainerLoaded(storage,false);ItemTagCleanup.Tick();
        var storedItem=Tagged();storage.Inventory.GetAllItems().Add(storedItem);
        ItemTagCleanup.ContainerLoaded(storage,true);
        storage.Accessible=false;ItemTagCleanup.Tick();assert(storedItem.m_cheated,"inaccessible storage remains untouched");
        storage.Accessible=true;storage.Owned=false;ItemTagCleanup.Tick();assert(storedItem.m_cheated,"another client's storage ownership is respected");
        storage.Owned=true;storage.InUse=true;ItemTagCleanup.Tick();assert(storedItem.m_cheated,"busy storage waits before scan");
        storage.InUse=false;ItemTagCleanup.Tick();assert(!storedItem.m_cheated&&storage.Inventory.Notifications==1,"deferred saved contents clear once ownership and access allow");
        storedItem.m_cheated=true;ItemTagCleanup.ContainerLoaded(storage,true);ItemTagCleanup.Tick();
        assert(storedItem.m_cheated&&storage.Inventory.Notifications==1,"later container updates do not repeat completed startup scan");
        var unrelated=new Container{Inventory=new Inventory(1,1)};var other=Tagged();unrelated.Inventory.GetAllItems().Add(other);
        ItemTagCleanup.ContainerLoaded(unrelated,true);ItemTagCleanup.Tick();assert(other.m_cheated,"unregistered containers are excluded");
        var laterChest=new Container{Inventory=new Inventory(1,1)};ContainerRegistry.All.Add(laterChest);var later=Tagged();laterChest.Inventory.GetAllItems().Add(later);
        ItemTagCleanup.ContainerLoaded(laterChest,true);ItemTagCleanup.Tick();assert(!later.m_cheated,"chests loaded later receive their first scan");
        var clean=new Inventory(1,1);assert(ItemTagCleanup.ScanOnce(clean)==0&&clean.Notifications==0,"clean inventory avoids redundant save");
        assert(ItemTagCleanup.ScanOnce(null)==0,"null inventory ignored");
        ItemTagCleanup.Clear();ItemTagCleanup.PlayerLoaded(Player.m_localPlayer);ItemTagCleanup.Tick();
        assert(!addedLater.m_cheated,"new world entry permits a fresh inventory scan");
        Reset();
    }
}
