using UnityEngine;

namespace F89.Core
{
    public static class AntarcticaLandMask
    {
        private const string MapResourcePath = "F89_AntarcticaMap";
        private const float MinLandBrightness = 0.28f;
        private const float MaxOceanBlueDominance = 0.12f;

        private static Texture2D readableMap;

        private const float MinDisplayLandBrightness = 0.60f;
        private const float MaxDisplayOceanBlueDominance = 0.04f;
        private const float MinSolidIceBrightness = 0.66f;
        private const float MaxSolidIceBlueDominance = 0.04f;
        private const float MaxVisibleOceanBrightness = 0.60f;
        private const float MinVisibleOceanBlueDominance = 0.04f;

        /// <summary>
        /// Minimum distance from coast/shallow shelf required for base placement.
        /// </summary>
        public const float BasePlacementInsetMiles = 30f;

        public const float BasePlacementSnapSearchMiles = 250f;

        public static bool IsLandMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return GetLandBlendMiles(positionMiles, mapSizeMiles) >= 0.5f;
        }

        /// <summary>
        /// Stricter than <see cref="IsLandMiles"/> — matches solid white ice on the satellite map,
        /// excluding shallow shelf pixels that read as land to the spawn mask but render as blue water.
        /// </summary>
        public static bool IsDisplayLandMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return GetDisplayLandBlendMiles(positionMiles, mapSizeMiles) >= 0.5f;
        }

        public static bool IsDisplayInlandMiles(Vector2 positionMiles, float mapSizeMiles, float insetMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return true;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var center))
            {
                return false;
            }

            if (!IsDisplayLandPixel(map, center.x, center.y))
            {
                return false;
            }

            var insetPixels = MilesToPixels(insetMiles, map.width, mapSizeMiles);
            if (insetPixels < 1)
            {
                return true;
            }

            for (var y = -insetPixels; y <= insetPixels; y++)
            {
                for (var x = -insetPixels; x <= insetPixels; x++)
                {
                    if (x * x + y * y > insetPixels * insetPixels)
                    {
                        continue;
                    }

                    if (!IsDisplayLandPixel(map, center.x + x, center.y + y))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryFindNearestDisplayLand(
            Vector2 positionMiles,
            float mapSizeMiles,
            float insetMiles,
            float searchRadiusMiles,
            out Vector2 displayLandMiles)
        {
            displayLandMiles = positionMiles;
            if (IsDisplayInlandMiles(positionMiles, mapSizeMiles, insetMiles))
            {
                return true;
            }

            var map = GetReadableMap();
            if (map == null)
            {
                return false;
            }

            var bestDistance = float.MaxValue;
            var found = false;
            var searchStepMiles = Mathf.Max(8f, insetMiles * 0.5f);
            var searchSteps = Mathf.CeilToInt(searchRadiusMiles / searchStepMiles);

            for (var ring = 1; ring <= searchSteps; ring++)
            {
                var ringRadius = ring * searchStepMiles;
                var sampleCount = Mathf.Max(8, ring * 6);
                for (var i = 0; i < sampleCount; i++)
                {
                    var angle = i * Mathf.PI * 2f / sampleCount;
                    var candidate = positionMiles + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                    if (!TryMilesToPixel(candidate, map.width, map.height, mapSizeMiles, out _))
                    {
                        continue;
                    }

                    if (!IsDisplayInlandMiles(candidate, mapSizeMiles, insetMiles))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(positionMiles, candidate);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    displayLandMiles = candidate;
                    found = true;
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetLandBlendMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return 1f;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var pixel))
            {
                return 0f;
            }

            return GetLandBlendPixel(map, pixel.x, pixel.y);
        }

        public static bool TryFindNearestInland(
            Vector2 positionMiles,
            float mapSizeMiles,
            float insetMiles,
            float searchRadiusMiles,
            out Vector2 inlandMiles)
        {
            inlandMiles = positionMiles;
            if (IsInlandMiles(positionMiles, mapSizeMiles, insetMiles))
            {
                return true;
            }

            var map = GetReadableMap();
            if (map == null)
            {
                return false;
            }

            var bestDistance = float.MaxValue;
            var found = false;
            var searchStepMiles = Mathf.Max(8f, insetMiles * 0.5f);
            var searchSteps = Mathf.CeilToInt(searchRadiusMiles / searchStepMiles);

            for (var ring = 1; ring <= searchSteps; ring++)
            {
                var ringRadius = ring * searchStepMiles;
                var sampleCount = Mathf.Max(8, ring * 6);
                for (var i = 0; i < sampleCount; i++)
                {
                    var angle = i * Mathf.PI * 2f / sampleCount;
                    var candidate = positionMiles + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                    if (!TryMilesToPixel(candidate, map.width, map.height, mapSizeMiles, out _))
                    {
                        continue;
                    }

                    if (!IsInlandMiles(candidate, mapSizeMiles, insetMiles))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(positionMiles, candidate);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    inlandMiles = candidate;
                    found = true;
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsShallowShelfMiles(Vector2 positionMiles, float mapSizeMiles, float sampleRadiusMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return false;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var center))
            {
                return false;
            }

            var samplePixels = MilesToPixels(sampleRadiusMiles, map.width, mapSizeMiles);
            var highBlueCount = 0;
            var sampleCount = 0;

            for (var y = -samplePixels; y <= samplePixels; y++)
            {
                for (var x = -samplePixels; x <= samplePixels; x++)
                {
                    if (x * x + y * y > samplePixels * samplePixels)
                    {
                        continue;
                    }

                    var color = map.GetPixel(center.x + x, center.y + y);
                    var blueDominance = color.b - Mathf.Max(color.r, color.g);
                    if (blueDominance > 0.08f)
                    {
                        highBlueCount++;
                    }

                    sampleCount++;
                }
            }

            if (sampleCount == 0)
            {
                return false;
            }

            return highBlueCount / (float)sampleCount > 0.18f;
        }

        /// <summary>
        /// Matches solid white ice on the satellite texture — safe spawn target for map markers.
        /// </summary>
        public static bool IsSolidIceMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return GetSolidIceBlendMiles(positionMiles, mapSizeMiles) >= 0.5f;
        }

        /// <summary>
        /// Matches ProceduralFlightGround — open ocean when display-land blend is below 0.5.
        /// </summary>
        public static bool IsFlightOceanMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return GetDisplayLandBlendMiles(positionMiles, mapSizeMiles) < 0.5f;
        }

        /// <summary>
        /// True when the map pixel reads as the dark/blue ocean users see on the tactical map.
        /// </summary>
        public static bool IsVisibleOceanMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return false;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var pixel))
            {
                return true;
            }

            var color = map.GetPixel(pixel.x, pixel.y);
            var brightness = (color.r + color.g + color.b) / 3f;
            var blueDominance = color.b - Mathf.Max(color.r, color.g);
            return blueDominance > MinVisibleOceanBlueDominance || brightness < MaxVisibleOceanBrightness;
        }

        /// <summary>
        /// Resolves a carrier seed toward flight-view ocean on the north-up tactical map.
        /// Mile +Y renders toward the bottom of the map, so visual north is decreasing mile Y.
        /// </summary>
        public static bool TryResolveVisibleOceanNorthOf(
            Vector2 seedMiles,
            float mapSizeMiles,
            float maxNorthMiles,
            float maxEastWestMiles,
            out Vector2 oceanMiles)
        {
            oceanMiles = seedMiles;
            var found = false;
            var bestY = float.MaxValue;
            var bestXDistance = float.MaxValue;
            const float stepMiles = 12f;
            var mapHalfMiles = mapSizeMiles * 0.5f;
            var northLimit = Mathf.Max(seedMiles.y - maxNorthMiles, -mapHalfMiles + stepMiles);

            for (var y = seedMiles.y; y >= northLimit; y -= stepMiles)
            {
                for (var x = seedMiles.x - maxEastWestMiles; x <= seedMiles.x + maxEastWestMiles; x += stepMiles)
                {
                    var candidate = new Vector2(x, y);
                    if (!IsFlightOceanMiles(candidate, mapSizeMiles))
                    {
                        continue;
                    }

                    var xDistance = Mathf.Abs(candidate.x - seedMiles.x);
                    if (y < bestY - 0.01f
                        || (Mathf.Abs(y - bestY) <= 0.01f && xDistance < bestXDistance))
                    {
                        bestY = y;
                        bestXDistance = xDistance;
                        oceanMiles = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        public static bool TryFindNearestFlightOcean(
            Vector2 positionMiles,
            float mapSizeMiles,
            float searchRadiusMiles,
            out Vector2 oceanMiles)
        {
            oceanMiles = positionMiles;
            if (IsFlightOceanMiles(positionMiles, mapSizeMiles))
            {
                return true;
            }

            var bestDistance = float.MaxValue;
            var found = false;
            const float stepMiles = 12f;
            var searchSteps = Mathf.CeilToInt(searchRadiusMiles / stepMiles);

            for (var ring = 1; ring <= searchSteps; ring++)
            {
                var ringRadius = ring * stepMiles;
                var sampleCount = Mathf.Max(8, ring * 6);
                for (var i = 0; i < sampleCount; i++)
                {
                    var angle = i * Mathf.PI * 2f / sampleCount;
                    var candidate = positionMiles + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                    if (!IsFlightOceanMiles(candidate, mapSizeMiles))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(positionMiles, candidate);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    oceanMiles = candidate;
                    found = true;
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsValidBasePlacementMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return IsDisplayInlandMiles(positionMiles, mapSizeMiles, BasePlacementInsetMiles);
        }

        public static bool TryFindNearestSolidIce(
            Vector2 positionMiles,
            float mapSizeMiles,
            float searchRadiusMiles,
            out Vector2 solidIceMiles)
        {
            return TryFindNearestDisplayLand(
                positionMiles,
                mapSizeMiles,
                BasePlacementInsetMiles,
                searchRadiusMiles,
                out solidIceMiles);
        }

        /// <summary>
        /// Nearest white-ice pixel on the tactical map within a search box. Allows narrow coastal
        /// shelf pixels that fail the full inland inset check.
        /// </summary>
        public static bool TryFindNearestCoastalDisplayLand(
            Vector2 seedMiles,
            float mapSizeMiles,
            float maxSouthMiles,
            float maxNorthMiles,
            float maxEastMiles,
            float maxWestMiles,
            out Vector2 landMiles)
        {
            landMiles = seedMiles;
            var bestDistance = float.MaxValue;
            var found = false;
            const float stepMiles = 12f;
            var minX = seedMiles.x - maxWestMiles;
            var maxX = seedMiles.x + maxEastMiles;

            for (var y = seedMiles.y + maxNorthMiles; y >= seedMiles.y - maxSouthMiles; y -= stepMiles)
            {
                for (var x = minX; x <= maxX; x += stepMiles)
                {
                    var candidate = new Vector2(x, y);
                    if (!IsDisplayLandMiles(candidate, mapSizeMiles))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(seedMiles, candidate);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    landMiles = candidate;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Finds inland ice by stepping toward the bottom of the north-up map (+mile Y), fanning east/west.
        /// </summary>
        public static bool TryFindDisplayLandSouthOf(
            Vector2 startMiles,
            float mapSizeMiles,
            float insetMiles,
            float maxSouthMiles,
            float maxEastWestMiles,
            out Vector2 landMiles)
        {
            landMiles = startMiles;
            var stepMiles = Mathf.Max(8f, insetMiles * 0.4f);
            var southSteps = Mathf.CeilToInt(maxSouthMiles / stepMiles);
            var eastSteps = Mathf.CeilToInt(maxEastWestMiles / stepMiles);

            for (var southRing = 0; southRing <= southSteps; southRing++)
            {
                var y = startMiles.y + southRing * stepMiles;

                for (var eastRing = 0; eastRing <= eastSteps; eastRing++)
                {
                    if (eastRing == 0)
                    {
                        var center = new Vector2(startMiles.x, y);
                        if (IsDisplayInlandMiles(center, mapSizeMiles, insetMiles))
                        {
                            landMiles = center;
                            return true;
                        }

                        continue;
                    }

                    var eastOffset = eastRing * stepMiles;
                    var eastCandidate = new Vector2(startMiles.x + eastOffset, y);
                    if (IsDisplayInlandMiles(eastCandidate, mapSizeMiles, insetMiles))
                    {
                        landMiles = eastCandidate;
                        return true;
                    }

                    var westCandidate = new Vector2(startMiles.x - eastOffset, y);
                    if (IsDisplayInlandMiles(westCandidate, mapSizeMiles, insetMiles))
                    {
                        landMiles = westCandidate;
                        return true;
                    }
                }
            }

            return false;
        }

        public static float GetDisplayLandBlendMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            return GetDisplayLandBlendMilesInternal(positionMiles, mapSizeMiles);
        }

        public static Vector2 GetMaskUvForMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            var half = mapSizeMiles * 0.5f;
            var u = (positionMiles.x + half) / mapSizeMiles;
            var mileV = (positionMiles.y + half) / mapSizeMiles;
            return new Vector2(u, 1f - mileV);
        }

        public static bool IsInlandMiles(Vector2 positionMiles, float mapSizeMiles, float insetMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return true;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var center))
            {
                return false;
            }

            if (!IsLandPixel(map, center.x, center.y))
            {
                return false;
            }

            var insetPixels = MilesToPixels(insetMiles, map.width, mapSizeMiles);
            if (insetPixels < 1)
            {
                return true;
            }

            for (var y = -insetPixels; y <= insetPixels; y++)
            {
                for (var x = -insetPixels; x <= insetPixels; x++)
                {
                    if (x * x + y * y > insetPixels * insetPixels)
                    {
                        continue;
                    }

                    if (!IsLandPixel(map, center.x + x, center.y + y))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Vector2 PixelToMiles(Vector2Int pixel, int width, int height, float mapSizeMiles)
        {
            var half = mapSizeMiles * 0.5f;
            var u = pixel.x / (float)(width - 1);
            // Unity texture pixels use a bottom-left origin; mile +Y is map north (up).
            var v = 1f - pixel.y / (float)(height - 1);
            return new Vector2(u * mapSizeMiles - half, v * mapSizeMiles - half);
        }

        public static bool TryMilesToPixel(
            Vector2 positionMiles,
            int width,
            int height,
            float mapSizeMiles,
            out Vector2Int pixel)
        {
            pixel = default;
            var half = mapSizeMiles * 0.5f;
            var u = (positionMiles.x + half) / mapSizeMiles;
            var v = (positionMiles.y + half) / mapSizeMiles;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return false;
            }

            pixel = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(u * (width - 1)), 0, width - 1),
                Mathf.Clamp(Mathf.RoundToInt((1f - v) * (height - 1)), 0, height - 1));
            return true;
        }

        public static Texture2D GetReadableMap()
        {
            if (readableMap != null)
            {
                return readableMap;
            }

            var source = Resources.Load<Texture2D>(MapResourcePath);
            if (source == null)
            {
                return null;
            }

            readableMap = CreateReadableCopy(source);
            return readableMap;
        }

        private static int MilesToPixels(float miles, int mapWidthPixels, float mapSizeMiles)
        {
            return Mathf.Max(1, Mathf.RoundToInt(miles / mapSizeMiles * mapWidthPixels));
        }

        private static bool IsLandPixel(Texture2D map, int x, int y)
        {
            return GetLandBlendPixel(map, x, y) >= 0.5f;
        }

        private static bool IsDisplayLandPixel(Texture2D map, int x, int y)
        {
            return GetDisplayLandBlendPixel(map, x, y) >= 0.5f;
        }

        private static float GetDisplayLandBlendMilesInternal(Vector2 positionMiles, float mapSizeMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return 1f;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var pixel))
            {
                return 0f;
            }

            return GetDisplayLandBlendPixel(map, pixel.x, pixel.y);
        }

        private static float GetSolidIceBlendMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return 1f;
            }

            if (!TryMilesToPixel(positionMiles, map.width, map.height, mapSizeMiles, out var pixel))
            {
                return 0f;
            }

            return GetSolidIceBlendPixel(map, pixel.x, pixel.y);
        }

        private static float GetSolidIceBlendPixel(Texture2D map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.width || y >= map.height)
            {
                return 0f;
            }

            var color = map.GetPixel(x, y);
            var brightness = (color.r + color.g + color.b) / 3f;
            var blueDominance = color.b - Mathf.Max(color.r, color.g);
            if (brightness < MinSolidIceBrightness || blueDominance > MaxSolidIceBlueDominance)
            {
                return 0f;
            }

            return 1f;
        }

        private static float GetDisplayLandBlendPixel(Texture2D map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.width || y >= map.height)
            {
                return 0f;
            }

            var color = map.GetPixel(x, y);
            var brightness = (color.r + color.g + color.b) / 3f;
            var blueDominance = color.b - Mathf.Max(color.r, color.g);
            return Mathf.Clamp01(
                Mathf.InverseLerp(MinDisplayLandBrightness, 0.78f, brightness)
                * (1f - Mathf.SmoothStep(0f, MaxDisplayOceanBlueDominance, blueDominance)));
        }

        private static float GetLandBlendPixel(Texture2D map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.width || y >= map.height)
            {
                return 0f;
            }

            var color = map.GetPixel(x, y);
            var brightness = (color.r + color.g + color.b) / 3f;
            var blueDominance = color.b - Mathf.Max(color.r, color.g);
            var landScore = Mathf.Clamp01(
                Mathf.InverseLerp(0.38f, 0.72f, brightness)
                * (1f - Mathf.SmoothStep(0f, 0.10f, blueDominance)));
            return landScore;
        }

        private static bool IsLandColor(Color color)
        {
            var brightness = (color.r + color.g + color.b) / 3f;
            var blueDominance = color.b - Mathf.Max(color.r, color.g);
            return brightness >= MinLandBrightness && blueDominance <= MaxOceanBlueDominance;
        }

        private static Texture2D CreateReadableCopy(Texture2D source)
        {
            var renderTarget = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            Graphics.Blit(source, renderTarget);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTarget;

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            return copy;
        }
    }
}
