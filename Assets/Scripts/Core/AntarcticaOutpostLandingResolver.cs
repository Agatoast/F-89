using UnityEngine;

namespace F89.Core
{
    /// <summary>Resolves whether an aircraft landed at or near an active land outpost.</summary>
    public static class AntarcticaOutpostLandingResolver
    {
        public const float NearestOutpostMaxMiles = 1.5f;

        public static bool TryResolveOutpost(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string outpostName)
        {
            if (TryResolveOutpostGridCell(landingWorldPosition, worldMap, ticSizeWorldUnits, out outpostName))
            {
                return true;
            }

            return TryResolveNearestOutpost(
                landingWorldPosition,
                worldMap,
                ticSizeWorldUnits,
                NearestOutpostMaxMiles,
                out outpostName);
        }

        public static bool TryResolveNearestOutpost(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            float maxMiles,
            out string outpostName)
        {
            outpostName = string.Empty;
            if (worldMap == null || maxMiles <= 0f)
            {
                return false;
            }

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            AntarcticaBase nearest = null;
            var nearestDistance = float.MaxValue;

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

                var distanceMiles = F89.Flight.CombatThreatRange.DistanceMiles(
                    landingWorldPosition,
                    candidate.transform.position,
                    worldMap,
                    ticSizeWorldUnits);
                if (distanceMiles > maxMiles || distanceMiles >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distanceMiles;
                nearest = candidate;
            }

            if (nearest == null)
            {
                return false;
            }

            outpostName = nearest.BaseName;
            return true;
        }

        private static bool TryResolveOutpostGridCell(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string outpostName)
        {
            outpostName = string.Empty;
            if (worldMap == null)
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

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate == null
                    || candidate.SiteKind != BaseSiteKind.Land
                    || !candidate.IsActive)
                {
                    continue;
                }

                if (!worldMap.TryWorldPositionToGridCell(
                        candidate.transform.position,
                        ticSizeWorldUnits,
                        out var outpostCell)
                    || outpostCell != landingCell)
                {
                    continue;
                }

                outpostName = candidate.BaseName;
                return true;
            }

            return false;
        }
    }
}
