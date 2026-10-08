namespace Quartermaster;

internal enum FurnitureHandling { Jar, Lumber, Ingot, Bin, Hide, Textile, Feather, Bone, Masonry, Pantry, Hanging, Grain, Ammunition, Weapon, Shield, Wardrobe, Trophy, Gem, Treasure, Mead }

// Definitions feed the same native-container registration and shared display as cabinets.
internal static class BulkStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_LumberRack",Name="Lumber Rack",Model="lumber_rack",Columns=2,Rows=2,Width=1.5f,Height=1.48f,Depth=.82f,Wood=18,FineWood=0,Category="lumber",Handling=FurnitureHandling.Lumber},
        new ApothecaryDefinition{Prefab="Quartermaster_LumberRackWide",Name="Wide Lumber Rack",Model="lumber_wide",Columns=3,Rows=2,Width=2.35f,Height=1.48f,Depth=.82f,Wood=26,FineWood=0,Category="lumber",Handling=FurnitureHandling.Lumber},
        new ApothecaryDefinition{Prefab="Quartermaster_IngotRack",Name="Ingot Rack",Model="ingot_rack",Columns=2,Rows=2,Width=1.18f,Height=1.12f,Depth=.65f,Wood=16,FineWood=4,Category="ingots",Handling=FurnitureHandling.Ingot},
        new ApothecaryDefinition{Prefab="Quartermaster_IngotRackWide",Name="Wide Ingot Rack",Model="ingot_wide",Columns=4,Rows=2,Width=2.18f,Height=1.12f,Depth=.65f,Wood=24,FineWood=6,Category="ingots",Handling=FurnitureHandling.Ingot},
        new ApothecaryDefinition{Prefab="Quartermaster_OreBin",Name="Ore & Scrap Bin",Model="ore_bin",Columns=3,Rows=2,Width=1.72f,Height=.82f,Depth=1.02f,Wood=20,FineWood=0,Category="ores",Handling=FurnitureHandling.Bin},
        new ApothecaryDefinition{Prefab="Quartermaster_CoalBin",Name="Coal Bin",Model="coal_bin",Columns=3,Rows=2,Width=1.62f,Height=1.20f,Depth=1.02f,Wood=22,FineWood=0,Category="coal",Handling=FurnitureHandling.Bin}
    };
}
