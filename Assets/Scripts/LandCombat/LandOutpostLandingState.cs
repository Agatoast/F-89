using F89.Core;

namespace F89.LandCombat
{
    /// <summary>Active outpost landing. Guards use mission assignment; bunker access waits for launch.</summary>
    public static class LandOutpostLandingState
    {
        public static string ActiveOutpostName { get; private set; } = string.Empty;
        public static bool HasActiveOutpost => !string.IsNullOrEmpty(ActiveOutpostName);

        public static void BeginFromSortie(LandSortieSnapshot snapshot)
        {
            ActiveOutpostName = string.Empty;
            if (string.IsNullOrWhiteSpace(snapshot.OutpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            var hasBunkerAccess = snapshot.HasOutpostBunker
                || OutpostSurfaceAccess.HasBunkerGroundAccess(snapshot.OutpostName);
            LandBossMissionAssignment.TryGetBossForOutpost(save, snapshot.OutpostName, out var bossNumber);
            if (!hasBunkerAccess && bossNumber <= 0)
            {
                return;
            }

            if (LandCombatTestCheats.ResetAllBossProgressOnDevEntry)
            {
                LandBossEncounter.ResetAllBossesForTest();
            }

            ActiveOutpostName = snapshot.OutpostName;
            if (bossNumber > 0)
            {
                LandBossAreaState.BeginArea(bossNumber);
            }
        }

        public static void Clear() => ActiveOutpostName = string.Empty;
    }
}
