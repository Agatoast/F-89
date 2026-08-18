namespace F89.Core
{
    /// <summary>
    /// Mission number → SiteCode from Docs/MissionCatalog.md.
    /// Single source of truth for current-mission map markers and completion.
    /// </summary>
    public static class CampaignMissionSiteCatalog
    {
        private static readonly string[] SiteCodesByMission =
        {
            null, // 0 unused
            "OP-SOUTH",
            "WP-02",
            "WP-03",
            "WP-04",
            "OP-01",
            "WP-06",
            "WP-07",
            "OP-13",
            "WP-09",
            "OP-13-SE",
            "WP-11",
            "WP-12",
            "OP-43",
            "WP-14",
            "OP-21",
            "WP-16",
            "WP-17",
            "STN-ROTHERA",
            "WP-19",
            "WP-20",
            "STN-PALMER",
            "WP-22",
            "WP-23",
            "STN-MARAMBIO",
            "WP-25",
            "WP-26",
            "OP-18",
            "WP-28",
            "WP-29",
            "WP-30",
            "OP-05",
            "WP-32",
            "WP-33",
            "OP-33",
            "WP-35",
            "WP-36",
            "WP-37",
            "OP-27",
            "WP-39",
            "WP-40",
            "WP-41",
            "OP-41",
            "WP-43",
            "STN-CONCORDIA",
            "WP-45",
            "STN-NEUMAYER-III",
            "STN-HALLEY-VI",
            "WP-48",
            "OP-44",
            "WP-50",
            "WP-51",
            "WP-52",
            "STN-AMUNDSEN-SCOTT"
        };

        public static bool TryGetSiteCode(int missionNumber, out string siteCode)
        {
            siteCode = string.Empty;
            if (missionNumber <= 0 || missionNumber >= SiteCodesByMission.Length)
            {
                return false;
            }

            siteCode = SiteCodesByMission[missionNumber];
            return !string.IsNullOrWhiteSpace(siteCode);
        }

        public static bool TryGetCurrentSiteCode(CharacterSaveData save, out string siteCode)
        {
            return TryGetSiteCode(CampaignMissionProgress.GetCurrentMissionNumber(save), out siteCode);
        }

        public static bool IsWaypointSite(string siteCode) =>
            CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode);

        public static bool IsOutpostOrStationSite(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return false;
            }

            var code = siteCode.Trim();
            return code.StartsWith("OP-", System.StringComparison.OrdinalIgnoreCase)
                || code.StartsWith("STN-", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Missions that must launch from the carrier deck (brief: "from the carrier").</summary>
        public static bool RequiresCarrierLaunch(int missionNumber) => missionNumber == 2;

        public static bool CurrentMissionRequiresCarrierLaunch(CharacterSaveData save) =>
            save != null && RequiresCarrierLaunch(CampaignMissionProgress.GetCurrentMissionNumber(save));
    }
}
