using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Quartermaster;

internal static class BuildPieces
{
    internal static void Initialize() => PrefabManager.OnVanillaPrefabsAvailable += Register;
    internal static void Shutdown() => PrefabManager.OnVanillaPrefabsAvailable -= Register;

    private static void Register()
    {
        try
        {
            var prefab=PrefabManager.Instance.CreateClonedPrefab(ChestDefaults.DepositPrefab,"piece_chest_blackmetal");
            if(!prefab)throw new InvalidOperationException("Missing native Deposit Chest template: piece_chest_blackmetal");
            prefab.GetComponent<Container>().m_name="Quartermaster Deposit Chest";
            // Retain the network prefab and native destruction/drop behavior for
            // existing saves, but add no duplicate recipe to the Hammer table.
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab,false));
            if(PrefabManager.Instance.GetPrefab(ChestDefaults.DepositPrefab)!=prefab)
                throw new InvalidOperationException("Cannot register the saved Deposit Chest prefab.");
            var ledger=PrefabManager.Instance.CreateClonedPrefab(OwlLedger.PrefabName,"sign");
            if(!ledger)throw new InvalidOperationException("Missing native ledger template: sign");
            LedgerModel.Build(ledger);
            var sign=ledger.GetComponent<Sign>();
            if(sign){if(sign.m_textWidget)UnityEngine.Object.DestroyImmediate(sign.m_textWidget.gameObject);UnityEngine.Object.DestroyImmediate(sign);}
            ledger.AddComponent<OwlLedger>();
            if(!PieceManager.Instance.AddPiece(new CustomPiece(ledger,false,new PieceConfig {
                Name="Quartermaster Ledger Lectern", Icon=LedgerModel.Icon(), Description="Read base inventory, set production limits and change stack sizes.",
                PieceTable="Hammer",Category="Quartermaster",CraftingStation="piece_workbench",
                Requirements=new[]{new RequirementConfig("Wood",15,0,true),new RequirementConfig("FineWood",5,0,true),new RequirementConfig("DeerHide",2,0,true)}
            })))throw new InvalidOperationException("Cannot register the Quartermaster Ledger Lectern.");
            Shutdown();
            Plugin.Log.LogInfo("Registered Ledger Lectern in the Hammer menu; old Deposit Chest prefab retained for existing saves.");
        }
        catch(Exception error){Plugin.Log.LogError("Quartermaster build-piece registration failed: "+error);}
    }
}
