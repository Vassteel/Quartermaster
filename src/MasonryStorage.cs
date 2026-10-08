namespace Quartermaster;

// Heavy ground stores and suspended timber share the established container catalog.
internal static class MasonryStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_StonePallet",Name="Stone Pallet",Model="stone_pallet",Columns=3,Rows=1,Width=1.42f,Height=.42f,Depth=.72f,Wood=12,Category="masonry",Handling=FurnitureHandling.Masonry},
        new ApothecaryDefinition{Prefab="Quartermaster_StonePalletWide",Name="Wide Stone Pallet",Model="stone_pallet_wide",Columns=3,Rows=2,Width=1.95f,Height=.42f,Depth=1.16f,Wood=20,Category="masonry",Handling=FurnitureHandling.Masonry},
        new ApothecaryDefinition{Prefab="Quartermaster_MasonryCrib",Name="Masonry Crib",Model="masonry_crib",Columns=3,Rows=2,Width=1.72f,Height=.92f,Depth=1.10f,Wood=24,Category="masonry",Handling=FurnitureHandling.Masonry},
        new ApothecaryDefinition{Prefab="Quartermaster_LumberRafter",Name="Rafter Lumber Rack",Model="lumber_rafter",Columns=3,Rows=1,Width=2.00f,Height=.90f,Depth=.82f,Wood=20,FineWood=4,Category="lumber",Handling=FurnitureHandling.Lumber,Mount=CabinetMount.Ceiling}
    };
}
