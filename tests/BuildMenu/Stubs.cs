namespace HarmonyLib {
[System.AttributeUsage(System.AttributeTargets.Class)] public class HarmonyPatch:System.Attribute { public HarmonyPatch(params object[] args){} }
[System.AttributeUsage(System.AttributeTargets.Method)] public class HarmonyPriority:System.Attribute { public HarmonyPriority(int value){} }
public enum MethodType { Constructor }
public static class Priority { public const int First=800,Last=0; }
}
public class ByUsagePieceList {public void GetTagDisplayName(){} public void UpdateAvailableTags(){} public void GetAvailablePiecesWithTag(){} }
public class Piece { public enum UsageTagFlags {None=0,Building=1,Furniture=2} public string gameObject; public bool m_repairPiece,m_removePiece; public static implicit operator bool(Piece value)=>value!=null; }
public class PieceTable { public HashSet<Piece> m_availablePieces=new(); }
public static class Utils {public static string GetPrefabName(string name)=>name.Replace("(Clone)","");}
namespace Quartermaster {internal static class ChestDefaults { internal const string DepositPrefab="quartermaster_deposit_chest"; } internal static class OwlLedger {internal const string PrefabName="piece_quartermaster_ledger";} }
