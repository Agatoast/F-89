using F89.Core;

namespace F89.LandCombat
{
    /// <summary>Active waypoint secondary landing — surface infantry only, no bunker.</summary>
    public static class WaypointLandingState
    {
        public static bool IsActive { get; private set; }
        public static string SiteCode { get; private set; } = string.Empty;

        public static void BeginFromSortie(LandSortieSnapshot snapshot)
        {
            SiteCode = snapshot.WaypointSiteCode ?? string.Empty;
            IsActive = !string.IsNullOrWhiteSpace(SiteCode);
        }

        public static void Clear()
        {
            IsActive = false;
            SiteCode = string.Empty;
        }
    }
}
