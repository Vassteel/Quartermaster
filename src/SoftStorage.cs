namespace Quartermaster;

internal static class SoftStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_HideRail",Name="Hanging Hide Rail",Model="hide_rail",Columns=3,Rows=1,Width=1.85f,Height=1.60f,Depth=.65f,Wood=18,FineWood=0,Category="hides",Handling=FurnitureHandling.Hide},
        new ApothecaryDefinition{Prefab="Quartermaster_HideShelf",Name="Hide Roll Shelf",Model="hide_shelf",Columns=3,Rows=2,Width=1.75f,Height=1.10f,Depth=.70f,Wood=20,FineWood=0,Category="hides",Handling=FurnitureHandling.Hide},
        new ApothecaryDefinition{Prefab="Quartermaster_TextileShelf",Name="Textile Shelf",Model="textile_shelf",Columns=3,Rows=2,Width=1.65f,Height=1.12f,Depth=.62f,Wood=18,FineWood=3,Category="textiles",Handling=FurnitureHandling.Textile},
        new ApothecaryDefinition{Prefab="Quartermaster_TextileWall",Name="Wall Textile Shelf",Model="textile_wall",Columns=3,Rows=1,Width=1.65f,Height=.63f,Depth=.50f,Wood=12,FineWood=2,Category="textiles",Handling=FurnitureHandling.Textile,Mount=CabinetMount.Wall},
        new ApothecaryDefinition{Prefab="Quartermaster_FeatherCoffer",Name="Feather Coffer",Model="feather_coffer",Columns=3,Rows=1,Width=1.50f,Height=.74f,Depth=.60f,Wood=14,FineWood=2,Category="feathers",Handling=FurnitureHandling.Feather,LidHeight=.69f,LidBack=-.335f},
        new ApothecaryDefinition{Prefab="Quartermaster_BoneCrate",Name="Bone Crate",Model="bone_crate",Columns=3,Rows=2,Width=1.42f,Height=.52f,Depth=.90f,Wood=14,FineWood=0,Category="bones",Handling=FurnitureHandling.Bone}
    };
}
