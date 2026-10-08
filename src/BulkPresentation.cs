using System;
namespace Quartermaster;

internal struct StorageTint
{
    internal float R,G,B;
    internal StorageTint(float r,float g,float b){R=r;G=g;B=b;}
}
internal static class BulkPresentation
{
    internal static int Level(int count,int max)=>count<=0?0:count<=Math.Max(1,max)*.25f?1:count<=Math.Max(1,max)*.66f?2:3;
    internal static string Model(string category,string id,int level,string furniture=null)=>
        (category=="fish"&&id=="FishRaw"?"fish_cuts":category=="grain"&&id.EndsWith("Flour",StringComparison.Ordinal)?"flour":category=="produce"&&id.StartsWith("Mushroom",StringComparison.Ordinal)?"mushrooms":category=="produce"&&(id.EndsWith("berry",StringComparison.OrdinalIgnoreCase)||id.EndsWith("berries",StringComparison.OrdinalIgnoreCase))?"berries":category=="masonry"&&id=="BlackMarble"?"marble":category=="masonry"&&id=="Grausten"?"grausten":category=="ores"&&id.EndsWith("Scrap",StringComparison.Ordinal)?"scrap":category=="hides"&&furniture=="hide_rail"?"hanging_hides":category=="textiles"&&id.EndsWith("Thread",StringComparison.Ordinal)?"thread":category)+(category=="masonry"&&furniture=="masonry_crib"?"_crib":"")+"_load_"+level;
    internal static float ContactHeight(string furniture,int level)=>furniture=="meat_rail"||furniture=="fish_rafter"?.51f:furniture=="grain_bin"||furniture=="flour_stand"?.13f:furniture=="hide_rail"?.70f:furniture=="masonry_crib"?(level>=3?.53f:level==2?.27f:.14f):.15f;
    // Restrained species/material palettes, multiplied over the original coarse atlas.
    // Unknown/modded materials keep the family-neutral finish; overrides still control routing.
    internal static StorageTint Tint(string id,string category)
    {
        switch(id)
        {
            case "BarleyFlour":return new StorageTint(1.11f,1.09f,1.06f);
            case "Blueberries":return new StorageTint(.59f,.67f,1.03f);
            case "Cloudberry":return new StorageTint(1.08f,.88f,.45f);
            case "Carrot":return new StorageTint(1.12f,.73f,.48f);
            case "Turnip":return new StorageTint(.97f,.81f,.97f);
            case "Onion":return new StorageTint(1.09f,1.09f,.83f);
            case "TrollHide":return new StorageTint(.57f,.73f,1.02f);
            case "WolfPelt":return new StorageTint(1.02f,1.10f,1.18f);
            case "LoxPelt":return new StorageTint(.72f,.68f,.59f);
            case "ScaleHide":return new StorageTint(.63f,.76f,.68f);
            case "AskHide":return new StorageTint(.65f,.54f,.47f);
            case "JuteRed":return new StorageTint(.75f,.43f,.35f);
            case "JuteBlue":return new StorageTint(.48f,.65f,.92f);
            case "CharredBone":return new StorageTint(.25f,.24f,.22f);
            case "CelestialFeather":return new StorageTint(.75f,.82f,.90f);
            case "FineWood":return new StorageTint(1.28f,1.20f,1.01f);
            case "RoundLog":return new StorageTint(.90f,.83f,.72f);
            case "ElderBark":return new StorageTint(.62f,.67f,.64f);
            case "YggdrasilWood":return new StorageTint(.98f,1.05f,.88f);
            case "Blackwood":return new StorageTint(.57f,.54f,.49f);
            case "Copper":case "CopperOre":case "CopperScrap":return new StorageTint(1.12f,.77f,.53f);
            case "Bronze":return new StorageTint(.96f,.85f,.55f);
            case "Tin":case "TinOre":return new StorageTint(.99f,1.05f,1.09f);
            case "Silver":case "SilverOre":return new StorageTint(1.15f,1.23f,1.26f);
            case "BlackMetal":case "BlackMetalScrap":return new StorageTint(.31f,.43f,.39f);
            case "Flametal":case "FlametalOre":case "FlametalNew":case "FlametalOreNew":return new StorageTint(.96f,.59f,.31f);
            case "Coal":return new StorageTint(.19f,.21f,.23f);
            case "IronScrap":return new StorageTint(.54f,.42f,.32f);
            case "Iron":case "IronOre":return new StorageTint(.65f,.72f,.76f);
        }
        return category=="ingots"?new StorageTint(.75f,.81f,.83f):category=="ores"?new StorageTint(.66f,.69f,.65f):category=="coal"?new StorageTint(.19f,.21f,.23f):new StorageTint(1,1,1);
    }
}
internal static class FurnitureMotion
{
    internal static bool IsDisplay(FurnitureHandling style)=>style==FurnitureHandling.Trophy||style==FurnitureHandling.Gem||style==FurnitureHandling.Treasure||style==FurnitureHandling.Mead;
    internal static float DisplayLift(FurnitureHandling style,float time)
    {
        if(!IsDisplay(style))return 0;
        float t=(time-Contact(style)+.25f)/1.1f;if(t<=0||t>=1)return 0;
        return (float)Math.Sin(t*Math.PI)*(style==FurnitureHandling.Trophy?.045f:.018f);
    }
    internal static bool IsRack(FurnitureHandling style)=>style==FurnitureHandling.Ammunition||style==FurnitureHandling.Weapon||style==FurnitureHandling.Shield;
    internal static float RackLift(FurnitureHandling style,float time)
    {
        if(!IsRack(style))return 0;
        float t=(time-Contact(style)+.25f)/1.15f;if(t<=0||t>=1)return 0;
        return (float)Math.Sin(t*Math.PI)*(style==FurnitureHandling.Ammunition?.035f:.065f);
    }
    internal static bool Carries(FurnitureHandling style)=>IsDisplay(style)||style==FurnitureHandling.Wardrobe||IsRack(style)||style==FurnitureHandling.Pantry||style==FurnitureHandling.Hanging||style==FurnitureHandling.Masonry||style==FurnitureHandling.Lumber||style==FurnitureHandling.Ingot||style==FurnitureHandling.Hide||style==FurnitureHandling.Textile||style==FurnitureHandling.Feather;
    internal static float HangingSway(float time)
    {float t=time-Contact(FurnitureHandling.Hanging)-.65f;return t<=0||time>=Duration(FurnitureHandling.Hanging)?0:(float)(Math.Sin(t*9)*Math.Exp(-t*4))*3f;}
    internal static float Cover(float time)
    {
        float open=Math.Max(0,Math.Min(1,(time-.2f)/.65f)),close=Math.Max(0,Math.Min(1,(time-3.25f)/.65f));
        return 68*open*open*(3-2*open)*(1-close*close*(3-2*close));
    }
    internal static float Press(FurnitureHandling style,float time)
    {
        if(style!=FurnitureHandling.Hide&&style!=FurnitureHandling.Textile)return 0;
        float t=(time-Contact(style)-.4f)/.8f;if(t<0||t>1)return 0;
        return (float)Math.Sin(t*Math.PI)*.075f;
    }
    internal static float Contact(FurnitureHandling style)=>style==FurnitureHandling.Treasure?2.0f:style==FurnitureHandling.Trophy?1.8f:style==FurnitureHandling.Gem?1.45f:style==FurnitureHandling.Wardrobe?2.0f:style==FurnitureHandling.Weapon?1.7f:style==FurnitureHandling.Shield?1.85f:style==FurnitureHandling.Ammunition?1.3f:style==FurnitureHandling.Pantry?1.45f:style==FurnitureHandling.Hanging?1.8f:style==FurnitureHandling.Grain?1.55f:style==FurnitureHandling.Masonry?1.85f:style==FurnitureHandling.Jar?2:style==FurnitureHandling.Hide||style==FurnitureHandling.Textile?1.65f:style==FurnitureHandling.Feather?1.5f:style==FurnitureHandling.Lumber?1.45f:style==FurnitureHandling.Ingot?1.70f:1.05f;
    internal static float Duration(FurnitureHandling style)=>style==FurnitureHandling.Treasure?4.6f:style==FurnitureHandling.Trophy?3.4f:style==FurnitureHandling.Gem?3.0f:style==FurnitureHandling.Wardrobe?4.6f:IsRack(style)?3.3f:style==FurnitureHandling.Pantry?3.0f:style==FurnitureHandling.Hanging?3.6f:style==FurnitureHandling.Grain?4.2f:style==FurnitureHandling.Masonry?3.5f:style==FurnitureHandling.Jar?5.2f:style==FurnitureHandling.Hide||style==FurnitureHandling.Textile?3.8f:style==FurnitureHandling.Feather?4.2f:style==FurnitureHandling.Ingot?3.3f:style==FurnitureHandling.Lumber?3.0f:2.4f;
    internal static float Lean(FurnitureHandling style,float time)
    {
        if(style==FurnitureHandling.Jar)return ApothecaryMotion.Sample(time).Lean;
        float contact=Contact(style),duration=Duration(style);
        float p=time<contact?time/contact:1-(time-contact)/(duration-contact);
        p=Math.Max(0,Math.Min(1,p));return p*p*(3-2*p);
    }
}
