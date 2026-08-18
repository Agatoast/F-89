using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Detects when the aircraft is over an outpost runway and eligible to land.</summary>
    public static class OutpostRunwayLanding
    {
        public const float LandingRangeTacs = 3f;

        public static bool TryFindNearestRunway(
            AircraftController aircraft,
            out AntarcticaBase baseSite,
            out Transform runwayTransform,
            out float distanceWorld)
        {
            baseSite = null;
            runwayTransform = null;
            distanceWorld = float.MaxValue;

            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return false;
            }

            var ticSize = aircraft.Profile.ticSizeWorldUnits;
            var landingRange = TacScale.TacsToWorld(LandingRangeTacs, ticSize);
            var position = aircraft.transform.position;
            position.y = 0f;

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate == null
                    || candidate.SiteKind != BaseSiteKind.Land
                    || !candidate.IsActive
                    || candidate.IsDestroyed)
                {
                    continue;
                }

                var runway = FindRunwayTransform(candidate.transform);
                if (runway == null)
                {
                    continue;
                }

                var runwayPos = runway.position;
                runwayPos.y = 0f;
                var distance = Vector3.Distance(position, runwayPos);
                if (distance > landingRange || distance >= distanceWorld)
                {
                    continue;
                }

                baseSite = candidate;
                runwayTransform = runway;
                distanceWorld = distance;
            }

            return baseSite != null;
        }

        public static bool IsNearRunway(AircraftController aircraft) =>
            TryFindNearestRunway(aircraft, out _, out _, out _);

        public static bool TryGetRunwaySpawn(
            string outpostName,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var trimmedName = outpostName.Trim();
            var baseSite = FindBaseByName(trimmedName);
            var friendlyOccupied = AntarcticaOutpostState.IsFriendlyOccupied(trimmedName);
            // Friendly occupied runways remain launchable even if surface structures were destroyed.
            if (baseSite == null
                || baseSite.SiteKind != BaseSiteKind.Land
                || !baseSite.IsActive
                || (baseSite.IsDestroyed && !friendlyOccupied))
            {
                return TryGetLayoutMilesSpawn(trimmedName, out worldPosition, out worldRotation);
            }

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            OutpostBuildingClusterSpawner.EnsureCluster(baseSite, worldUnitsPerMile, ticSize);

            var runway = FindRunwayTransform(baseSite.transform);
            if (runway != null)
            {
                worldPosition = runway.position;
                worldPosition.y = 0f;
                var forward = runway.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector3.forward;
                }

                worldRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                return true;
            }

            if (TryGetLockedBaseMiles(baseSite, trimmedName, out var lockedMiles)
                && worldMap != null)
            {
                worldPosition = CampaignMapCoordinates.MilesToWorld(lockedMiles, worldMap, ticSize);
                worldPosition.y = 0f;
                worldRotation = Quaternion.identity;
                Debug.LogWarning(
                    $"F-89: Runway missing at '{trimmedName}'; using locked layout miles "
                    + $"{CampaignMapCoordinates.FormatMilesLabel(lockedMiles)}.");
                return true;
            }

            worldPosition = baseSite.transform.position;
            worldPosition.y = 0f;
            var baseForward = baseSite.transform.forward;
            baseForward.y = 0f;
            if (baseForward.sqrMagnitude < 0.0001f)
            {
                baseForward = Vector3.forward;
            }

            worldRotation = Quaternion.LookRotation(baseForward.normalized, Vector3.up);
            Debug.LogWarning(
                $"F-89: Runway missing at '{trimmedName}'; using base center for spawn.");
            return true;
        }

        /// <summary>
        /// Layout-mile spawn when the scene base is missing or unusable.
        /// Prefer this over carrier fallback for parked friendly outpost launches.
        /// </summary>
        public static bool TryGetLayoutMilesSpawn(
            string outpostName,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;
            if (string.IsNullOrWhiteSpace(outpostName)
                || !TryGetLockedBaseMiles(null, outpostName.Trim(), out var lockedMiles))
            {
                return false;
            }

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            var hasCatalogGrid = CampaignMapLayoutState.TryGetSite(outpostName.Trim(), out var site)
                && site != null
                && site.GridCellX > 0
                && site.GridCellZ > 0;
            if (!hasCatalogGrid
                && AntarcticaLandMask.IsVisibleOceanMiles(lockedMiles, mapSizeMiles))
            {
                Debug.LogWarning(
                    $"F-89: Layout miles for '{outpostName.Trim()}' sit in visible ocean — skipping layout spawn.");
                return false;
            }

            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null || lockedMiles.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            worldPosition = CampaignMapCoordinates.MilesToWorld(lockedMiles, worldMap, ticSize);
            worldPosition.y = 0f;
            worldRotation = Quaternion.identity;
            Debug.LogWarning(
                $"F-89: Using layout miles for outpost launch '{outpostName.Trim()}' at "
                + $"{CampaignMapCoordinates.FormatMilesLabel(lockedMiles)}.");
            return true;
        }

        private static bool TryGetLockedBaseMiles(
            AntarcticaBase baseSite,
            string outpostName,
            out Vector2 miles)
        {
            miles = Vector2.zero;
            if (baseSite != null && CampaignMapCoordinates.TryGetLockedBaseMiles(baseSite, out miles))
            {
                return miles.sqrMagnitude > 0.01f;
            }

            if (!CampaignMapLayoutState.TryGetSite(outpostName, out var site) || site == null)
            {
                return false;
            }

            miles = CampaignMapLayoutState.GetLockedMiles(site);
            return miles.sqrMagnitude > 0.01f;
        }

        private static AntarcticaBase FindBaseByName(string outpostName)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate != null && MatchesOutpostName(candidate, outpostName))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool MatchesOutpostName(AntarcticaBase baseSite, string outpostName)
        {
            return string.Equals(baseSite.BaseName, outpostName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseSite.SiteCode, outpostName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static Transform FindRunwayTransform(Transform baseRoot)
        {
            if (baseRoot == null)
            {
                return null;
            }

            var direct = baseRoot.Find(OutpostRunwayVisual.RunwayObjectName);
            if (direct != null)
            {
                return direct;
            }

            var children = baseRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name == OutpostRunwayVisual.RunwayObjectName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static float ResolveWorldUnitsPerMile(WorldMapConfig worldMap, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null)
            {
                return 20f * ticSize;
            }

            return worldMap.GridSpacingTics * ticSize / worldMap.milesPerGrid;
        }
    }
}
