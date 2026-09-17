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
            prefab.GetComponent<Container>().m_name="Quartermaster Deposit Chest";
            // Custom coffer visuals are suspended. Keep the established prefab ID
            // so placed chests and saved contents load using the native model/icon.
            // Native storage, recipe and networking; the owl decoration
            // is attached by ContainerRegistry after the placed chest loads.
            if(!PieceManager.Instance.AddPiece(new CustomPiece(prefab,false,new PieceConfig {
                Name="Quartermaster Deposit Chest",
                Description="The owl sorts deposited items into configured storage chests.",
                PieceTable="Hammer",Category="Quartermaster"
            })))throw new InvalidOperationException("Cannot register the Quartermaster Deposit Chest.");
            Shutdown();
            Plugin.Log.LogInfo("Registered Quartermaster build category and Deposit Chest.");
        }
        catch(Exception error){Plugin.Log.LogError("Quartermaster build-piece registration failed: "+error);}
    }
}
