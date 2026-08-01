namespace F89.UI
{
    /// <summary>
    /// Pending end-mission score summary shown before R&amp;D results.
    /// Court-martial copy will expand when the full end-mission report is defined.
    /// </summary>
    public static class MissionEndReportState
    {
        public static bool HasPendingReport { get; private set; }
        public static int MissionScore { get; private set; }
        public static bool IsNewBestMissionScore { get; private set; }
        public static bool RequiresCourtMartial { get; private set; }

        public static void Begin(int missionScore, bool newBestMissionScore, bool requiresCourtMartial)
        {
            MissionScore = missionScore;
            IsNewBestMissionScore = newBestMissionScore;
            RequiresCourtMartial = requiresCourtMartial;
            HasPendingReport = true;
        }

        public static void Clear()
        {
            HasPendingReport = false;
            MissionScore = 0;
            IsNewBestMissionScore = false;
            RequiresCourtMartial = false;
        }
    }
}
