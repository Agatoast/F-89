namespace F89.Core
{
    /// <summary>
    /// Thin flight ↔ land handoff contract. Flight code should only call BeginEnterFromFlight
    /// and consume-return helpers here — never land module types directly.
    /// </summary>
    public static class LandMissionHandoffState
    {
        private static LandSortieSnapshot pendingEnterSnapshot;
        private static LandSortieSnapshot pendingReturnSnapshot;
        private static LandGroundSessionResult lastGroundResult;

        public static bool HasPendingEnter => pendingEnterSnapshot.IsValid;
        public static bool HasPendingReturn => pendingReturnSnapshot.IsValid;
        public static LandGroundSessionResult LastGroundResult => lastGroundResult;

        public static void BeginEnterFromFlight(LandSortieSnapshot snapshot)
        {
            pendingEnterSnapshot = snapshot;
            pendingEnterSnapshot.IsValid = true;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
        }

        public static bool TryConsumeEnterFromFlight(out LandSortieSnapshot snapshot)
        {
            snapshot = pendingEnterSnapshot;
            if (!pendingEnterSnapshot.IsValid)
            {
                return false;
            }

            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            return true;
        }

        public static void BeginReturnToFlight(LandSortieSnapshot snapshot, LandGroundSessionResult groundResult)
        {
            pendingReturnSnapshot = snapshot;
            pendingReturnSnapshot.IsValid = true;
            lastGroundResult = groundResult;
        }

        public static bool TryConsumeReturnToFlight(out LandSortieSnapshot snapshot, out LandGroundSessionResult groundResult)
        {
            snapshot = pendingReturnSnapshot;
            groundResult = lastGroundResult;
            if (!pendingReturnSnapshot.IsValid)
            {
                return false;
            }

            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            return true;
        }

        public static void Clear()
        {
            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
        }
    }
}
