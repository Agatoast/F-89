namespace F89.LandCombat
{
    /// <summary>
    /// Compile-time documentation of the module boundary.
    /// Land combat code lives under Assets/Scripts/LandCombat/ and namespace F89.LandCombat.
    ///
    /// Flight code may only touch:
    ///   - F89.Core.LandMissionHandoffState
    ///   - F89.Core.LandSortieSnapshot
    ///   - F89.Core.LandGroundSessionResult
    ///   - F89.Core.GameScenes.GroundAttack
    ///
    /// Flight code must NOT reference types in F89.LandCombat.
    /// Inventory, bank, paperdoll, and MTAU ports belong here — not in F89.Flight or F89.Weapons.
    /// </summary>
    public static class LandCombatBoundary
    {
        public const string ModuleFolder = "Assets/Scripts/LandCombat";
    }
}
