using System.Collections.Generic;
using UnityEngine;

namespace F89.Core
{
    public static class AntarcticaBaseLandFactory
    {
        private const float MinLandBaseSeparationMiles = 85f;
        private const float MinCatalogSeparationMiles = 65f;
        private const int ScanStridePixels = 3;

        public static AntarcticaBaseCatalog.BaseDefinition[] GenerateLandBases(
            float mapSizeMiles,
            IReadOnlyList<Vector2> reservedPositions,
            int count = 50)
        {
            if (count <= 0)
            {
                return System.Array.Empty<AntarcticaBaseCatalog.BaseDefinition>();
            }

            var map = AntarcticaLandMask.GetReadableMap();
            if (map == null)
            {
                Debug.LogWarning("F-89: Antarctica map unavailable; no procedural land bases spawned.");
                return System.Array.Empty<AntarcticaBaseCatalog.BaseDefinition>();
            }

            var candidates = CollectInlandCandidates(map, mapSizeMiles);
            var selected = SelectSpreadPositions(candidates, reservedPositions, count);
            selected = ValidateSelectedPositions(selected, mapSizeMiles, candidates, reservedPositions, count);
            var definitions = new AntarcticaBaseCatalog.BaseDefinition[selected.Count];

            for (var i = 0; i < selected.Count; i++)
            {
                definitions[i] = new AntarcticaBaseCatalog.BaseDefinition
                {
                    baseName = $"Outpost {i + 1:00}",
                    positionMiles = selected[i],
                    control = BaseControl.Hostile,
                    startsActive = true
                };
            }

            if (selected.Count < count)
            {
                Debug.LogWarning(
                    $"F-89: Placed {selected.Count}/{count} land bases on solid ice (reduce spacing if needed).");
            }

            return definitions;
        }

        private static List<Vector2> CollectInlandCandidates(Texture2D map, float mapSizeMiles)
        {
            var candidates = new List<Vector2>(8192);

            for (var y = 0; y < map.height; y += ScanStridePixels)
            {
                for (var x = 0; x < map.width; x += ScanStridePixels)
                {
                    var miles = AntarcticaLandMask.PixelToMiles(new Vector2Int(x, y), map.width, map.height, mapSizeMiles);
                    if (!AntarcticaLandMask.IsValidBasePlacementMiles(miles, mapSizeMiles))
                    {
                        continue;
                    }

                    candidates.Add(miles);
                }
            }

            return candidates;
        }

        private static List<Vector2> ValidateSelectedPositions(
            List<Vector2> selected,
            float mapSizeMiles,
            List<Vector2> fallbackCandidates,
            IReadOnlyList<Vector2> reservedPositions,
            int targetCount)
        {
            var validated = new List<Vector2>(selected.Count);
            var usedFallback = new HashSet<int>();

            for (var i = 0; i < selected.Count; i++)
            {
                var position = selected[i];
                if (!AntarcticaLandMask.IsValidBasePlacementMiles(position, mapSizeMiles)
                    && !AntarcticaLandMask.TryFindNearestDisplayLand(
                        position,
                        mapSizeMiles,
                        AntarcticaLandMask.BasePlacementInsetMiles,
                        AntarcticaLandMask.BasePlacementSnapSearchMiles,
                        out position))
                {
                    continue;
                }

                validated.Add(position);
            }

            if (validated.Count >= targetCount)
            {
                return validated;
            }

            var working = new List<Vector2>(fallbackCandidates);
            for (var i = working.Count - 1; i >= 0; i--)
            {
                if (validated.Exists(point => Vector2.Distance(point, working[i]) < MinLandBaseSeparationMiles * 0.5f))
                {
                    working.RemoveAt(i);
                }
            }

            while (validated.Count < targetCount && working.Count > 0)
            {
                var bestIndex = -1;
                var bestScore = -1f;

                for (var i = 0; i < working.Count; i++)
                {
                    if (usedFallback.Contains(i))
                    {
                        continue;
                    }

                    var candidate = working[i];
                    if (!AntarcticaLandMask.IsValidBasePlacementMiles(candidate, mapSizeMiles))
                    {
                        continue;
                    }

                    var nearest = NearestDistance(candidate, validated, reservedPositions);
                    if (nearest < MinLandBaseSeparationMiles || nearest <= bestScore)
                    {
                        continue;
                    }

                    bestScore = nearest;
                    bestIndex = i;
                }

                if (bestIndex < 0)
                {
                    break;
                }

                usedFallback.Add(bestIndex);
                validated.Add(working[bestIndex]);
            }

            return validated;
        }

        private static List<Vector2> SelectSpreadPositions(
            List<Vector2> candidates,
            IReadOnlyList<Vector2> reservedPositions,
            int count)
        {
            var selected = new List<Vector2>(count);
            if (candidates.Count == 0)
            {
                return selected;
            }

            var working = new List<Vector2>(candidates);
            var startIndex = FindSeedIndex(working, reservedPositions);
            selected.Add(working[startIndex]);
            working.RemoveAt(startIndex);

            while (selected.Count < count && working.Count > 0)
            {
                var bestIndex = -1;
                var bestScore = -1f;

                for (var i = 0; i < working.Count; i++)
                {
                    var candidate = working[i];
                    var nearest = NearestDistance(candidate, selected, reservedPositions);
                    if (nearest < MinLandBaseSeparationMiles)
                    {
                        continue;
                    }

                    if (nearest > bestScore)
                    {
                        bestScore = nearest;
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    break;
                }

                selected.Add(working[bestIndex]);
                working.RemoveAt(bestIndex);
            }

            return selected;
        }

        private static int FindSeedIndex(List<Vector2> candidates, IReadOnlyList<Vector2> reservedPositions)
        {
            var bestIndex = 0;
            var bestScore = -1f;

            for (var i = 0; i < candidates.Count; i++)
            {
                var score = NearestDistance(candidates[i], null, reservedPositions);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static float NearestDistance(
            Vector2 point,
            List<Vector2> selected,
            IReadOnlyList<Vector2> reservedPositions)
        {
            var nearest = float.MaxValue;

            if (selected != null)
            {
                for (var i = 0; i < selected.Count; i++)
                {
                    nearest = Mathf.Min(nearest, Vector2.Distance(point, selected[i]));
                }
            }

            if (reservedPositions != null)
            {
                for (var i = 0; i < reservedPositions.Count; i++)
                {
                    nearest = Mathf.Min(nearest, Vector2.Distance(point, reservedPositions[i]));
                }
            }

            if (nearest == float.MaxValue)
            {
                return MinCatalogSeparationMiles;
            }

            return nearest;
        }
    }
}
