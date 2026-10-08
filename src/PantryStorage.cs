namespace Quartermaster;

internal static class PantryStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_PantryShelf",Name="Pantry Shelf",Model="pantry_shelf",Columns=3,Rows=2,Width=1.70f,Height=1.28f,Depth=.64f,Wood=20,FineWood=2,Category="food",Handling=FurnitureHandling.Pantry},
        new ApothecaryDefinition{Prefab="Quartermaster_ProduceShelf",Name="Wall Produce Shelf",Model="produce_wall",Columns=3,Rows=1,Width=1.65f,Height=.67f,Depth=.56f,Wood=14,FineWood=0,Category="produce",Handling=FurnitureHandling.Pantry,Mount=CabinetMount.Wall},
        new ApothecaryDefinition{Prefab="Quartermaster_MeatRail",Name="Ceiling Meat Hook",Model="meat_rail",Columns=1,Rows=1,Width=.38f,Height=.92f,Depth=.32f,Wood=0,FineWood=0,Bronze=2,Station="forge",Category="meat",Handling=FurnitureHandling.Hanging,Mount=CabinetMount.Ceiling},
        new ApothecaryDefinition{Prefab="Quartermaster_FishRafter",Name="Ceiling Fish Hook",Model="fish_rafter",Columns=1,Rows=1,Width=.38f,Height=.92f,Depth=.32f,Wood=0,FineWood=0,Bronze=2,Station="forge",Category="fish",Handling=FurnitureHandling.Hanging,Mount=CabinetMount.Ceiling},
        new ApothecaryDefinition{Prefab="Quartermaster_GrainBin",Name="Grain Bin",Model="grain_bin",Columns=3,Rows=1,Width=1.65f,Height=.78f,Depth=.66f,Wood=20,FineWood=0,Category="grain",Handling=FurnitureHandling.Grain,LidHeight=.735f,LidBack=-.365f},
        new ApothecaryDefinition{Prefab="Quartermaster_FlourStand",Name="Flour Sack Stand",Model="flour_stand",Columns=3,Rows=1,Width=1.65f,Height=.66f,Depth=.66f,Wood=14,FineWood=0,Category="grain",Handling=FurnitureHandling.Grain}
    };
}
