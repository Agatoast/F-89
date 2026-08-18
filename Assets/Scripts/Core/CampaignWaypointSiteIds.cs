namespace F89.Core
{
    public static class CampaignWaypointSiteIds
    {
        public static bool IsWaypointSiteCode(string siteCode) =>
            !string.IsNullOrWhiteSpace(siteCode)
            && siteCode.Trim().StartsWith("WP-", System.StringComparison.OrdinalIgnoreCase);
    }
}
