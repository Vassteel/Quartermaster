namespace Quartermaster;

internal static class ChestDefaults
{
    internal const string DepositPrefab="quartermaster_deposit_chest";
    // Used only when no saved settings exist. Player edits always take precedence.
    internal static ChestSettings Create(string prefab) => new ChestSettings { Deposit=prefab==DepositPrefab };
}
