using UnityEngine;

namespace F89.Core
{
    public static class AntarcticaCarrierPlacement
    {
        private const float NneBearingDegrees = 22.5f;
        private const float DefaultNneDistanceMiles = 90f;
        private const float AdditionalEastMiles = 20f;
        private const float AdditionalNorthMiles = 180f;
        private const float MaxNneSearchMiles = 180f;
        private const float NneSearchStepMiles = 12f;
        private const float MaxNorthOceanSearchMiles = 150f;

        public static Vector2 ComputeOceanPositionNneOfLeftmostHostile(
            Transform basesRoot,
            float mapSizeMiles,
            float worldUnitsPerMile,
            float nneDistanceMiles = DefaultNneDistanceMiles)
        {
            if (!TryFindLeftmostHostileBaseMiles(basesRoot, worldUnitsPerMile, out var leftmostMiles))
            {
                Debug.LogWarning("F-89: No hostile bases found; using fallback carrier position.");
                return new Vector2(-620f, 420f);
            }

            var carrierMiles = FindOceanPointNne(leftmostMiles, mapSizeMiles, nneDistanceMiles);
            carrierMiles += new Vector2(AdditionalEastMiles, AdditionalNorthMiles);
            carrierMiles = EnsureOceanMiles(carrierMiles, mapSizeMiles);
            Debug.Log(
                $"F-89: Carrier placed NNE of leftmost hostile base ({leftmostMiles.x:0}, {leftmostMiles.y:0}) MI " +
                $"at ({carrierMiles.x:0}, {carrierMiles.y:0}) MI.");
            return carrierMiles;
        }

        private static bool TryFindLeftmostHostileBaseMiles(
            Transform basesRoot,
            float worldUnitsPerMile,
            out Vector2 leftmostMiles)
        {
            leftmostMiles = default;
            if (basesRoot == null || worldUnitsPerMile <= 0f)
            {
                return false;
            }

            var found = false;
            var minX = float.MaxValue;
            var bestY = float.MinValue;

            var bases = basesRoot.GetComponentsInChildren<AntarcticaBase>(true);
            foreach (var baseSite in bases)
            {
                if (baseSite == null
                    || !baseSite.IsActive
                    || baseSite.IsDestroyed
                    || baseSite.Control != BaseControl.Hostile
                    || baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    continue;
                }

                var miles = baseSite.PositionMiles;
                if (miles.x < minX - 0.5f)
                {
                    minX = miles.x;
                    bestY = miles.y;
                    leftmostMiles = miles;
                    found = true;
                    continue;
                }

                if (Mathf.Abs(miles.x - minX) <= 0.5f && miles.y > bestY)
                {
                    bestY = miles.y;
                    leftmostMiles = miles;
                    found = true;
                }
            }

            return found;
        }

        private static Vector2 FindOceanPointNne(
            Vector2 baseMiles,
            float mapSizeMiles,
            float startDistanceMiles)
        {
            var direction = BearingToOffset(NneBearingDegrees);
            var bestCandidate = baseMiles + direction * startDistanceMiles;

            for (var distance = startDistanceMiles;
                 distance <= MaxNneSearchMiles;
                 distance += NneSearchStepMiles)
            {
                var candidate = baseMiles + direction * distance;
                if (!AntarcticaLandMask.IsLandMiles(candidate, mapSizeMiles))
                {
                    return candidate;
                }

                bestCandidate = candidate;
            }

            foreach (var bearing in new[] { 30f, 45f, 15f, 0f })
            {
                var altDirection = BearingToOffset(bearing);
                for (var distance = startDistanceMiles;
                     distance <= MaxNneSearchMiles;
                     distance += NneSearchStepMiles)
                {
                    var candidate = baseMiles + altDirection * distance;
                    if (!AntarcticaLandMask.IsLandMiles(candidate, mapSizeMiles))
                    {
                        return candidate;
                    }
                }
            }

            return bestCandidate;
        }

        private static Vector2 EnsureOceanMiles(Vector2 miles, float mapSizeMiles)
        {
            if (!AntarcticaLandMask.IsLandMiles(miles, mapSizeMiles))
            {
                return miles;
            }

            for (var extraNorth = NneSearchStepMiles;
                 extraNorth <= MaxNorthOceanSearchMiles;
                 extraNorth += NneSearchStepMiles)
            {
                var candidate = miles + new Vector2(0f, extraNorth);
                if (!AntarcticaLandMask.IsLandMiles(candidate, mapSizeMiles))
                {
                    return candidate;
                }
            }

            for (var extraEast = NneSearchStepMiles;
                 extraEast <= MaxNorthOceanSearchMiles;
                 extraEast += NneSearchStepMiles)
            {
                var candidate = miles + new Vector2(extraEast, 0f);
                if (!AntarcticaLandMask.IsLandMiles(candidate, mapSizeMiles))
                {
                    return candidate;
                }
            }

            return miles;
        }

        private static Vector2 BearingToOffset(float bearingDegrees)
        {
            var radians = bearingDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
        }

        private static Vector2 WorldToMiles(Vector3 worldPosition, float worldUnitsPerMile)
        {
            return WorldMapConfig.WorldToMileOffset(worldPosition, worldUnitsPerMile);
        }
    }
}
