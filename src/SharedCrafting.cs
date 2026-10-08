using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Need = Quartermaster.CraftMaterialPull.Need;

namespace Quartermaster;

[HarmonyPatch]
internal static class SharedCrafting
{
    private static readonly MethodInfo Craft=AccessTools.Method(typeof(InventoryGui),"DoCrafting"), Place=AccessTools.Method(typeof(Player),"TryPlacePiece");
    private static readonly MethodInfo RecipeRequirements=AccessTools.Method(typeof(Player),"HaveRequirements",new[]{typeof(Recipe),typeof(bool),typeof(int),typeof(int)});
    private static readonly MethodInfo PieceRequirements=AccessTools.Method(typeof(Player),"HaveRequirements",new[]{typeof(Piece),typeof(Player.RequirementMode)});
    private static readonly FieldInfo RecipeField=AccessTools.Field(typeof(InventoryGui),"m_craftRecipe"),UpgradeField=AccessTools.Field(typeof(InventoryGui),"m_craftUpgradeItem"),MultiField=AccessTools.Field(typeof(InventoryGui),"m_multiCrafting"),AmountField=AccessTools.Field(typeof(InventoryGui),"m_multiCraftAmount");
    private static readonly FieldInfo NoPlacementCost=AccessTools.Field(typeof(Player),"m_noPlacementCost");
    private static readonly FieldInfo GhostField=AccessTools.Field(typeof(Player),"m_placementGhost");
    private static bool Enabled=>Plugin.Enabled.Value&&Plugin.CraftFromContainers.Value;
    private static bool LocalOnly(Func<bool> check)
    {
        bool before=CraftingPatches.LocalCountsOnly;CraftingPatches.LocalCountsOnly=true;
        try{return check();}finally{CraftingPatches.LocalCountsOnly=before;}
    }
    private static Need[] Needs(Player player, Piece.Requirement[] requirements, int level, int multiplier, bool building, Recipe recipe = null)
    {
        if (recipe && recipe.m_requireOnlyOneIngredient)
        {
            // Ask vanilla which alternative/quality it will consume, including multi-craft.
            var args = new object[] { player.GetInventory(), recipe, level, 0, 0, multiplier };
            var item = (ItemDrop.ItemData)AccessTools.Method(typeof(Player), "GetFirstRequiredItem").Invoke(player,args);
            return item == null ? null : new[] { new Need { Name=item.m_shared.m_name, Quality=item.m_quality, Count=(int)args[3] } };
        }
        var station = player.GetCurrentCraftingStation();
        bool upgrader = station && station.m_upgrader;
        var result = new List<Need>();
        foreach (var r in requirements.Where(r => r?.m_resItem && (building || r.m_upgraderResource == upgrader)))
        {
            int count = checked((building ? r.m_amount : r.GetAmount(level)) * multiplier);
            if (count <= 0) continue;
            string name = r.m_resItem.m_itemData.m_shared.m_name;
            int quality = -1;
            if (!building)
            {
                // Vanilla requires enough of a single quality, even for ordinary recipes.
                var candidates = Enumerable.Range(1,Math.Max(1,r.m_resItem.m_itemData.m_shared.m_maxQuality))
                    .Where(q => CraftingPatches.CountItemsIncludingWarehouse(player.GetInventory(),name,q,true) >= count)
                    .OrderByDescending(q => InventoryTransfers.CountType(player.GetInventory(),name,q,true)).ToArray();
                if (candidates.Length == 0) return null;
                quality = candidates[0];
            }
            var existing = result.FirstOrDefault(n => n.Name == name && n.Quality == quality);
            if (existing != null) existing.Count = checked(existing.Count + count);
            else result.Add(new Need { Name=name, Count=count, Quality=quality });
        }
        return result.ToArray();
    }
    private static int Missing(Player player, Need n) => Math.Max(0,n.Count-InventoryTransfers.CountType(player.GetInventory(),n.Name,n.Quality,true));
    private static Container[] Sources(Player player, Need[] needs)
    {
        if (needs == null) return null;
        var stores = WarehouseService.CraftStores(player,true)
            .OrderBy(c => (c.transform.position-player.transform.position).sqrMagnitude)
            .ThenBy(c => ContainerRegistry.GetView(c).GetZDO().m_uid.ToString(),StringComparer.Ordinal).ToArray();
        var selected = CraftSupplyPlan.Select(needs.Select(n => Missing(player,n)).ToArray(),
            stores.Select(c => needs.Select(n => InventoryTransfers.AvailableInContainer(c,n.Name,n.Quality,true)).ToArray()).ToArray());
        return selected?.Select(i => stores[i]).ToArray();
    }
    private static bool SourcesValid(Player player, Container[] sources) => sources.All(c => c && CraftStorageAccess.CanSupply(c)
        && ContainerRegistry.GetSettings(c).CraftingSupply && Vector3.Distance(c.transform.position,player.transform.position)<=Plugin.CraftRange.Value);
    private static string lastMessage;
    private static float lastMessageAt;
    private static void Message(string text)
    {
        if(text=="Saved"||!Player.m_localPlayer)return;
        if(text==lastMessage&&Time.unscaledTime-lastMessageAt<2)return;
        lastMessage=text;lastMessageAt=Time.unscaledTime;
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,text);
    }
    [HarmonyPatch(typeof(InventoryGui),"DoCrafting"),HarmonyPrefix,HarmonyPriority(Priority.First)]
    private static bool ReserveCraft(InventoryGui __instance,Player player,out bool __state)
    {
        __state=CraftingPatches.LocalCountsOnly;
        if(!Enabled||CraftingPatches.LocalCountsOnly||player!=Player.m_localPlayer||player.NoCostCheat()||ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))return true;
        var recipe=(Recipe)RecipeField.GetValue(__instance);if(!recipe)return true;
        var upgrade=(ItemDrop.ItemData)UpgradeField.GetValue(__instance);
        int quality=upgrade==null?1:upgrade.m_quality+1;
        bool multi=(bool)MultiField.GetValue(__instance);int amount=multi?(int)AmountField.GetValue(__instance):1;
        bool HasRequirements()=>(bool)RecipeRequirements.Invoke(player,new object[]{recipe,false,quality,amount});
        if(LocalOnly(HasRequirements)){CraftingPatches.LocalCountsOnly=true;return true;}
        if(!HasRequirements()){Message("Required materials are unavailable or busy; try again or carry them");return false;}
        var station=player.GetCurrentCraftingStation();
        var needs=Needs(player,recipe.m_resources,quality,amount,false,recipe);
        var sources=Sources(player,needs);
        if(sources==null){Message("Materials are busy or spread across more than four supply chests; try again or carry them");return false;}
        bool Valid()=>__instance&&InventoryGui.IsVisible()&&player&&player==Player.m_localPlayer&&!player.IsDead()&&!player.IsTeleporting()&&
            (Recipe)RecipeField.GetValue(__instance)==recipe&&ReferenceEquals(UpgradeField.GetValue(__instance),upgrade)&&
            (upgrade==null||upgrade.m_quality+1==quality)&&player.GetCurrentCraftingStation()==station&&
            (bool)MultiField.GetValue(__instance)==multi&&(!multi||(int)AmountField.GetValue(__instance)==amount)&&
            SourcesValid(player,sources);
        bool pulled=false;
        if(CraftStorageAccess.Run("crafting materials",sources,Valid,
            ()=>pulled=CraftMaterialPull.TryPull(player.GetInventory(),needs,sources,Message),Message,afterPull:()=>
            {
                if(!pulled)return;
                LocalOnly(()=>{if(HasRequirements())Craft.Invoke(__instance,new object[]{player});return true;});
            }) && pulled)Message("Crafting materials collected");
        return false;
    }
    [HarmonyPatch(typeof(InventoryGui),"DoCrafting"),HarmonyFinalizer]
    private static Exception CraftScope(Exception __exception,bool __state)
    {CraftingPatches.LocalCountsOnly=__state;return __exception;}
    [HarmonyPatch(typeof(Player),"UpdatePlacement"),HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> GuardPlacement(IEnumerable<CodeInstruction> instructions)
    {
        var guarded=AccessTools.Method(typeof(SharedCrafting),nameof(TrySharedPlacement));
        foreach(var original in instructions)
        {
            var copy=new CodeInstruction(original);
            if(copy.Calls(Place)){copy.opcode=OpCodes.Call;copy.operand=guarded;}
            yield return copy;
        }
    }
    private static bool TrySharedPlacement(Player player,Piece piece)
    {
        bool PlaceNow()=>(bool)Place.Invoke(player,new object[]{piece});
        bool Requirements()=>(bool)PieceRequirements.Invoke(player,new object[]{piece,Player.RequirementMode.CanBuild});
        if(!Enabled||CraftingPatches.LocalCountsOnly||player!=Player.m_localPlayer||(bool)NoPlacementCost.GetValue(player)||ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()) )return PlaceNow();
        if(PlantEasilyCompatibility.TryPrepare(player,piece,(GameObject)GhostField.GetValue(player),out bool funded))
        {
            if(!funded)return false;
            CraftingPatches.LocalCountsOnly=true;
            return PlaceNow();
        }
        if(LocalOnly(Requirements)){CraftingPatches.LocalCountsOnly=true;return PlaceNow();}
        string name="build "+piece.name;
        var needs=Needs(player,piece.m_resources,0,1,true);
        var sources=Sources(player,needs);
        if(sources==null){Message("Materials are busy or spread across more than four supply chests; try again or carry them");return false;}
        var ghost=(GameObject)GhostField.GetValue(player);if(!ghost)return false;
        Vector3 position=ghost.transform.position;Quaternion rotation=ghost.transform.rotation;
        bool Valid()=>player&&player==Player.m_localPlayer&&!player.IsDead()&&!player.IsTeleporting()&&
            player.GetSelectedPiece()==piece&&ghost&&(GameObject)GhostField.GetValue(player)==ghost&&
            Vector3.Distance(ghost.transform.position,position)<.1f&&Quaternion.Angle(ghost.transform.rotation,rotation)<1&&
            SourcesValid(player,sources);
        bool pulled=false;
        if(!CraftStorageAccess.Run(name,sources,Valid,()=>pulled=CraftMaterialPull.TryPull(player.GetInventory(),needs,sources,Message),Message)
            || !pulled || !LocalOnly(Requirements))return false;
        // UpdatePlacement consumes payment after TryPlacePiece returns; its finalizer
        // restores this flag after that payment, not immediately after placement.
        CraftingPatches.LocalCountsOnly=true;
        return PlaceNow();
    }
    internal static int AffordablePlants(Player player,Piece piece,int maximum)
    {
        var unit=Needs(player,piece.m_resources,0,1,true);
        int amount=maximum;
        foreach(var n in unit)
            amount=Math.Min(amount,CraftingPatches.CountItemsIncludingWarehouse(player.GetInventory(),n.Name,n.Quality,true)/n.Count);
        return amount;
    }
    internal static bool FundPlants(Player player,Piece piece,int amount)
    {
        if(amount<1)return false;
        var needs=Needs(player,piece.m_resources,0,amount,true);
        if(needs==null)return false;
        if(needs.All(n=>Missing(player,n)==0))return true;
        var sources=Sources(player,needs);
        if(sources==null){Message("Not enough accessible planting materials");return false;}
        bool pulled=false;
        bool valid=CraftStorageAccess.Run("planting materials",sources,()=>SourcesValid(player,sources),
            ()=>pulled=CraftMaterialPull.TryPull(player.GetInventory(),needs,sources,Message),Message);
        return valid&&pulled&&needs.All(n=>Missing(player,n)==0);
    }
    // Once funded, keep the entire native placement/payment call local-only.
    // No ownership transfer or chest lock is needed for this path.
    [HarmonyPatch(typeof(Player),"UpdatePlacement"),HarmonyPrefix]
    private static void PlacementScope(out bool __state) { __state=CraftingPatches.LocalCountsOnly; }
    [HarmonyPatch(typeof(Player),"UpdatePlacement"),HarmonyFinalizer]
    private static Exception ReleasePlacement(Exception __exception,bool __state)
    {CraftingPatches.LocalCountsOnly=__state;return __exception;}
}
