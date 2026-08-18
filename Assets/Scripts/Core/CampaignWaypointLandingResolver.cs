using UnityEngine;

namespace F89.Core
{
    /// <summary>Resolves landing on a waypoint secondary pad (same 1 MI grid cell as destroyed tank wreck).</summary>
    public static class CampaignWaypointLandingResolver
    {
        public static bool IsLandingInSecondaryPadGrid(
            Vector3 landingWorldPosition,
            string siteCode,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (!CampaignWaypointSecondaryState.CanLandForSecondary(siteCode)
                || worldMap == null
                || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            if (!CampaignWaypointSecondaryState.TryGetPadWorldPosition(siteCode, out var padPosition))
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    landingWorldPosition,
                    ticSizeWorldUnits,
                    out var landingCell))
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    padPosition,
                    ticSizeWorldUnits,
                    out var padCell))
            {
                return false;
            }

            return landingCell == padCell;
        }

        public static bool TryResolveSecondaryPadLanding(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string siteCode)
        {
            siteCode = string.Empty;
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    landingWorldPosition,
                    ticSizeWorldUnits,
                    out var landingCell))
            {
                return false;
            }

            CampaignWaypointLayoutState.EnsureLoaded();
            var waypoints = CampaignWaypointLayoutState.Waypoints;
            for (var i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                if (waypoint == null || string.IsNullOrWhiteSpace(waypoint.SiteCode))
                {
                    continue;
                }

                if (!CampaignWaypointSecondaryState.CanLandForSecondary(waypoint.SiteCode))
                {
                    continue;
                }

                if (!CampaignWaypointSecondaryState.TryGetPadWorldPosition(waypoint.SiteCode, out var padPosition))
                {
                    continue;
                }

                if (!worldMap.TryWorldPositionToGridCell(
                        padPosition,
                        ticSizeWorldUnits,
                        out var padCell)
                    || padCell != landingCell)
                {
                    continue;
                }

                siteCode = waypoint.SiteCode;
                return true;
            }

            return false;
        }
    }
}
