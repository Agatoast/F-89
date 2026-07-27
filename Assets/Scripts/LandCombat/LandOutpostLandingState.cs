using F89.Core;

namespace F89.LandCombat
{
    /// <summary>Active outpost landing with bunker access gated by assigned boss missions.</summary>
    public static class LandOutpostLandingState
    {
        public static string ActiveOutpostName { get; private set; } = string.Empty;
        public static bool HasActiveOutpost => !string.IsNullOrEmpty(ActiveOutpostName);

        public static void BeginFromSortie(LandSortieSnapshot snapshot)
        {
            ActiveOutpostName = string.Empty;
            if (!snapshot.HasOutpostBunker || string.IsNullOrWhiteSpace(snapshot.OutpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (!LandBossMissionAssignment.TryGetRevealedBossForOutpost(save, snapshot.OutpostName, out var bossNumber))
            {
                return;
            }

            if (LandCombatTestCheats.ResetAllBossProgressOnDevEntry)
            {
                LandBossEncounter.ResetAllBossesForTest();
            }

            ActiveOutpostName = snapshot.OutpostName;
            LandBossAreaState.BeginArea(bossNumber);
        }

        public static void Clear() => ActiveOutpostName = string.Empty;
    }
}
