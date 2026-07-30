using F89.LandCombat;

namespace F89.Core
{
    /// <summary>Whether an outpost is ready for ground dismount (bunker pad) and surface clearance checks.</summary>
    public static class OutpostSurfaceAccess
    {
        public static bool HasBunkerGroundAccess(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var save = CharacterSessionState.ActiveSave;
            return OutpostSurfaceBunkerPad.IsRevealedAtOutpostName(outpostName)
                || AntarcticaOutpostState.IsTargetDestroyed(outpostName, OutpostPrimaryObjective.BunkerBuildingLabel)
                || LandBossMissionAssignment.IsBunkerRevealedAtOutpost(save, outpostName);
        }

        public static bool ShouldSpawnGroundGuard(string outpostName, int bossNumber, int guardIndex)
        {
            if (guardIndex < 0)
            {
                return false;
            }

            if (bossNumber > 0 && LandBossEncounter.IsGuardCleared(bossNumber))
            {
                return false;
            }

            return !OutpostGroundGuardState.IsGuardDestroyed(outpostName, guardIndex);
        }
    }
}
