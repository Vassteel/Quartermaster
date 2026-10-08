using System.Collections.Generic;
namespace Quartermaster;

// A bounded native item display per inventory cell; real storage remains a native Container.
internal static class DisplayStorage
{
    // Shelved by user request. Keep source and artwork for a later update.
    internal static readonly bool Enabled=false;
    internal static void Assign(ObjectDB db,Dictionary<string,string> categories)
    {
        if(!Enabled)return;
        categories["Coins"]="valuables";
        foreach(var id in new[]{"Ruby","Amber","AmberPearl","Crystal","GemstoneRed","GemstoneGreen","GemstoneBlue"})categories[id]="gems";
        foreach(var prefab in db.m_items)
        {
            var drop=prefab?prefab.GetComponent<ItemDrop>():null;if(!drop)continue;
            var shared=drop.m_itemData.m_shared;
            if(shared.m_itemType==ItemDrop.ItemData.ItemType.Trophy)categories[prefab.name]="trophies";
            else if(shared.m_itemType==ItemDrop.ItemData.ItemType.Material&&shared.m_value>0&&!categories.ContainsKey(prefab.name))categories[prefab.name]="valuables";
        }
    }
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_TrophyShelf",Name="Wall Trophy Shelf",Model="trophy_shelf",Columns=2,Rows=1,Width=1.70f,Height=.92f,Depth=.58f,Wood=10,FineWood=2,Mount=CabinetMount.Wall,Category="trophies",Handling=FurnitureHandling.Trophy,DisplayWidth=.66f,DisplayHeight=.66f,DisplayDepth=.39f},
        new ApothecaryDefinition{Prefab="Quartermaster_TrophyCabinet",Name="Trophy Cabinet",Model="trophy_cabinet",Columns=2,Rows=2,Width=1.80f,Height=1.96f,Depth=.70f,Wood=22,FineWood=4,Category="trophies",Handling=FurnitureHandling.Trophy,DisplayWidth=.72f,DisplayHeight=.72f,DisplayDepth=.47f},
        new ApothecaryDefinition{Prefab="Quartermaster_TreasureCoffer",Name="Treasure Coffer",Model="treasure_coffer",Columns=2,Rows=2,Width=1.10f,Height=.66f,Depth=.76f,Wood=10,FineWood=3,Bronze=2,Category="valuables",Handling=FurnitureHandling.Treasure,DisplayWidth=.40f,DisplayHeight=.24f,DisplayDepth=.25f,LidHeight=.61f,LidBack=-.38f,PlayerLid=true},
        new ApothecaryDefinition{Prefab="Quartermaster_GemTray",Name="Gem Sorting Tray",Model="gem_tray",Columns=3,Rows=1,Width=.96f,Height=.24f,Depth=.42f,Wood=4,FineWood=1,Category="gems",Handling=FurnitureHandling.Gem,DisplayWidth=.24f,DisplayHeight=.13f,DisplayDepth=.25f},
        new ApothecaryDefinition{Prefab="Quartermaster_GemShelf",Name="Wall Gem Shelf",Model="gem_shelf",Columns=3,Rows=2,Width=1.04f,Height=.91f,Depth=.36f,Wood=7,FineWood=2,Mount=CabinetMount.Wall,Category="gems",Handling=FurnitureHandling.Gem,DisplayWidth=.25f,DisplayHeight=.25f,DisplayDepth=.22f}
    };
}
