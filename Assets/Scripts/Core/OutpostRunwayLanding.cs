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
            if (baseSite == null
                || baseSite.SiteKind != BaseSiteKind.Land
                || !baseSite.IsActive
                || baseSite.IsDestroyed)
            {
                return false;
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
