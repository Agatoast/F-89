using F89.LandCombat;

namespace F89.Core
{
    /// <summary>Tracks a parked aircraft on an outpost runway between landing and takeoff.</summary>
    public static class OutpostRunwayDeckState
    {
        public const int DefaultRunwayGuardCount = 5;

        public static string ParkedOutpostName { get; private set; } = string.Empty;
        public static bool IsFriendlyOutpost { get; private set; }
        public static bool SurfaceGuardsCleared { get; private set; }
        public static bool PendingDeckRefuelOnRestore { get; private set; }
        public static bool PendingDeckRearmOnRestore { get; private set; }
        public static bool PendingDeckTakeoffOnRestore { get; private set; }

        public static bool IsParkedAtRunway => !string.IsNullOrEmpty(ParkedOutpostName);

        public static void BeginParked(string outpostName, bool friendly)
        {
            ParkedOutpostName = outpostName ?? string.Empty;
            IsFriendlyOutpost = friendly;
            SurfaceGuardsCleared = OutpostGroundGuardState.RefreshSurfaceClearance(
                ParkedOutpostName,
                ResolveRunwayGuardCount(ParkedOutpostName));
        }

        public static void RefreshSurfaceGuardsCleared(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                SurfaceGuardsCleared = false;
                return;
            }

            var guardCount = ResolveRunwayGuardCount(outpostName);
            SurfaceGuardsCleared = OutpostGroundGuardState.RefreshSurfaceClearance(outpostName, guardCount);
        }

        public static void NotifyGuardDestroyed(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            if (!string.IsNullOrEmpty(ParkedOutpostName)
                && !string.Equals(ParkedOutpostName, outpostName, System.StringComparison.Ordinal))
            {
                return;
            }

            RefreshSurfaceGuardsCleared(outpostName);
        }

        public static void RequestDeckRefuelOnRestore()
        {
            PendingDeckRefuelOnRestore = true;
        }

        public static void RequestDeckRearmOnRestore()
        {
            PendingDeckRearmOnRestore = true;
        }

        public static void RequestDeckTakeoffOnRestore()
        {
            PendingDeckTakeoffOnRestore = true;
        }

        public static bool ConsumePendingDeckRefuelOnRestore()
        {
            if (!PendingDeckRefuelOnRestore)
            {
                return false;
            }

            PendingDeckRefuelOnRestore = false;
            return true;
        }

        public static bool ConsumePendingDeckRearmOnRestore()
        {
            if (!PendingDeckRearmOnRestore)
            {
                return false;
            }

            PendingDeckRearmOnRestore = false;
            return true;
        }

        public static bool ConsumePendingDeckTakeoffOnRestore()
        {
            if (!PendingDeckTakeoffOnRestore)
            {
                return false;
            }

            PendingDeckTakeoffOnRestore = false;
            return true;
        }

        public static int ResolveRunwayGuardCount(string outpostName)
        {
            var save = CharacterSessionState.ActiveSave;
            if (LandBossMissionAssignment.TryGetBossForOutpost(save, outpostName, out var bossNumber)
                && bossNumber > 0
                && LandBossAreaCatalog.TryGet(bossNumber, out var area))
            {
                return area.GuardCount;
            }

            return DefaultRunwayGuardCount;
        }

        public static void Clear()
        {
            ParkedOutpostName = string.Empty;
            IsFriendlyOutpost = false;
            SurfaceGuardsCleared = false;
            PendingDeckRefuelOnRestore = false;
            PendingDeckRearmOnRestore = false;
            PendingDeckTakeoffOnRestore = false;
        }
    }
}
