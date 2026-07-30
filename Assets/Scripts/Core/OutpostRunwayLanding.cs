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

                var runway = candidate.transform.Find(OutpostRunwayVisual.RunwayObjectName);
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

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null
                    || baseSite.SiteKind != BaseSiteKind.Land
                    || !string.Equals(baseSite.BaseName, outpostName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                var runway = baseSite.transform.Find(OutpostRunwayVisual.RunwayObjectName);
                if (runway == null)
                {
                    return false;
                }

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

            return false;
        }
    }
}
