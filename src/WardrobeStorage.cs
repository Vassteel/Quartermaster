namespace Quartermaster;
internal static class WardrobeStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_ArmorWardrobe",Name="Armor Wardrobe",Model="armor_wardrobe",Columns=2,Rows=2,Width=1.16f,Height=1.95f,Depth=.70f,Wood=22,FineWood=3,Bronze=1,Category="armor",Handling=FurnitureHandling.Wardrobe,DisplayWidth=.43f,DisplayHeight=.67f,DisplayDepth=.38f,Doors=1,DoorHingeX=.526f,DoorFront=.37f},
        new ApothecaryDefinition{Prefab="Quartermaster_ArmorWardrobeWide",Name="Wide Armor Wardrobe",Model="armor_wardrobe_wide",Columns=2,Rows=3,Width=1.80f,Height=2.10f,Depth=.70f,Wood=32,FineWood=5,Bronze=2,Category="armor",Handling=FurnitureHandling.Wardrobe,DisplayWidth=.74f,DisplayHeight=.47f,DisplayDepth=.38f,Doors=2,DoorHingeX=.846f,DoorFront=.37f}
    };
}
