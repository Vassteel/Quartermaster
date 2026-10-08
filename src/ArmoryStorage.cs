namespace Quartermaster;

internal static class ArmoryStorage
{
    internal static readonly ApothecaryDefinition[] Pieces={
        new ApothecaryDefinition{Prefab="Quartermaster_ArrowStand",Name="Arrow Stand",Model="arrow_stand",Columns=3,Rows=1,Width=1.20f,Height=1.10f,Depth=.52f,Wood=12,FineWood=2,Category="arrows",Handling=FurnitureHandling.Ammunition,DisplayWidth=.24f,DisplayHeight=.76f,DisplayDepth=.22f},
        new ApothecaryDefinition{Prefab="Quartermaster_BoltTray",Name="Wall Bolt Rack",Model="bolt_wall",Columns=3,Rows=1,Width=1.24f,Height=.74f,Depth=.34f,Wood=10,FineWood=2,Category="bolts",Handling=FurnitureHandling.Ammunition,Mount=CabinetMount.Wall,DisplayWidth=.24f,DisplayHeight=.48f,DisplayDepth=.20f},
        new ApothecaryDefinition{Prefab="Quartermaster_WeaponRack",Name="Weapon Rack",Model="weapon_rack",Columns=3,Rows=1,Width=1.62f,Height=1.50f,Depth=.66f,Wood=18,FineWood=3,Category="weapons",Handling=FurnitureHandling.Weapon,DisplayWidth=.42f,DisplayHeight=1.16f,DisplayDepth=.24f},
        new ApothecaryDefinition{Prefab="Quartermaster_WeaponRackWide",Name="Wide Weapon Rack",Model="weapon_rack_wide",Columns=5,Rows=1,Width=2.60f,Height=1.50f,Depth=.66f,Wood=28,FineWood=5,Category="weapons",Handling=FurnitureHandling.Weapon,DisplayWidth=.42f,DisplayHeight=1.16f,DisplayDepth=.24f},
        new ApothecaryDefinition{Prefab="Quartermaster_LongWeaponRack",Name="Long Weapon Rack",Model="longweapon_rack",Columns=3,Rows=1,Width=1.88f,Height=2.25f,Depth=.76f,Wood=24,FineWood=4,Category="longweapons",Handling=FurnitureHandling.Weapon,DisplayWidth=.48f,DisplayHeight=1.88f,DisplayDepth=.28f},
        new ApothecaryDefinition{Prefab="Quartermaster_BowRack",Name="Wall Bow Rack",Model="bow_wall",Columns=1,Rows=2,Width=1.64f,Height=1.80f,Depth=.35f,Wood=14,FineWood=3,Category="bows",Handling=FurnitureHandling.Weapon,Mount=CabinetMount.Wall,DisplayWidth=1.32f,DisplayHeight=.66f,DisplayDepth=.22f,HorizontalDisplay=true},
        new ApothecaryDefinition{Prefab="Quartermaster_CrossbowRack",Name="Wall Crossbow Rack",Model="crossbow_wall",Columns=1,Rows=2,Width=1.68f,Height=1.86f,Depth=.44f,Wood=16,FineWood=4,Category="crossbows",Handling=FurnitureHandling.Weapon,Mount=CabinetMount.Wall,DisplayWidth=1.30f,DisplayHeight=.68f,DisplayDepth=.28f,HorizontalDisplay=true},
        new ApothecaryDefinition{Prefab="Quartermaster_ShieldWall",Name="Wall Shield Rack",Model="shield_wall",Columns=2,Rows=1,Width=2.04f,Height=1.25f,Depth=.34f,Wood=14,FineWood=2,Category="shields",Handling=FurnitureHandling.Shield,Mount=CabinetMount.Wall,DisplayWidth=.85f,DisplayHeight=.94f,DisplayDepth=.22f},
        new ApothecaryDefinition{Prefab="Quartermaster_ShieldRack",Name="Shield Stand",Model="shield_stand",Columns=3,Rows=1,Width=2.90f,Height=1.40f,Depth=.70f,Wood=24,FineWood=3,Category="shields",Handling=FurnitureHandling.Shield,DisplayWidth=.81f,DisplayHeight=1.04f,DisplayDepth=.24f}
    };
}
