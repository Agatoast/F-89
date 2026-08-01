using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    public static class AntarcticaLandMask
    {
        private const string MapResourcePath = "F89_AntarcticaMap";
        private const float DefaultMapWidthOverHeight = 1024f / 837f;
        /// <summary>
        /// Pixels at or below this luminance (black sea background) are treated as ocean.
        /// Land is defined by the continent silhouette, not interior terrain shading.
        /// </summary>
        private const float SeaLuminanceThreshold = 0.078f;

        private static Texture2D readableMap;

        /// <summary>
        /// Minimum distance from coast/shallow shelf required for base placement.
        /// </summary>
        public const float BasePlacementInsetMiles = 30f;

        public const float BasePlacementSnapSearchMiles = 250f;

        /// <summary>
        /// Native width÷height of the Antarctica map artwork (east-west ÷ north-south).
        /// </summary>
        public static float GetMapWidthOverHeight()
        {
            var map = GetReadableMap();
            if (map != null && map.height > 0)
            {
                return (float)map.width / map.height;
            }

            return DefaultMapWidthOverHeight;
        }

        /// <summary>
        /// North-south mile extent when east-west spans <paramref name="mapWidthMiles"/>.
        /// </summary>
        public static float GetMapHeightMiles(float mapWidthMiles)
        {
            return mapWidthMiles / GetMapWidthOverHeight();
        }

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

            var insetPixels = ResolveInsetPixelRadius(insetMiles, map, mapSizeMiles);
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
            return false;
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
            return GetDisplayLandBlendMiles(positionMiles, mapSizeMiles) < 0.5f;
        }

        /// <summary>
        /// Resolves a carrier seed toward flight-view ocean on the north-up tactical map (+Y north).
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
            var bestY = float.MinValue;
            var bestXDistance = float.MaxValue;
            const float stepMiles = 12f;
            var mapHeightMiles = GetMapHeightMiles(mapSizeMiles);
            var northLimit = Mathf.Min(seedMiles.y + maxNorthMiles, mapHeightMiles - stepMiles);

            for (var y = seedMiles.y; y <= northLimit; y += stepMiles)
            {
                for (var x = seedMiles.x - maxEastWestMiles; x <= seedMiles.x + maxEastWestMiles; x += stepMiles)
                {
                    var candidate = new Vector2(x, y);
                    if (!IsFlightOceanMiles(candidate, mapSizeMiles))
                    {
                        continue;
                    }

                    var xDistance = Mathf.Abs(candidate.x - seedMiles.x);
                    if (y > bestY + 0.01f
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

            for (var y = seedMiles.y - maxSouthMiles; y <= seedMiles.y + maxNorthMiles; y += stepMiles)
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
        /// Finds inland ice by stepping south (-mile Y) from a seed, fanning east/west.
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
                var y = startMiles.y - southRing * stepMiles;

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

        /// <summary>Mask UV in the same orientation as ProceduralFlightGround.shader WorldToMaskUv.</summary>
        public static Vector2 GetMaskUvForMiles(Vector2 positionMiles, float mapWidthMiles)
        {
            var mapHeightMiles = GetMapHeightMiles(mapWidthMiles);
            var u = positionMiles.x / mapWidthMiles;
            var uvY = positionMiles.y / mapHeightMiles;
            return new Vector2(u, uvY);
        }

        /// <summary>Matches ProceduralFlightGround.shader WorldToMaskUv for the processed land-mask texture.</summary>
        public static Vector2 WorldPositionToShaderMaskUv(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            var worldUnitsPerMile = worldMap != null ? worldMap.GetWorldUnitsPerMile(ticSizeWorldUnits) : 0f;
            if (worldMap == null || worldUnitsPerMile <= 0f)
            {
                return Vector2.zero;
            }

            var mapWidthWorld = worldMap.antarcticaSizeMiles * worldUnitsPerMile;
            var mapHeightWorld = GetMapHeightMiles(worldMap.antarcticaSizeMiles) * worldUnitsPerMile;
            var halfWidthWorld = mapWidthWorld * 0.5f;
            var halfHeightWorld = mapHeightWorld * 0.5f;
            var u = (worldPosition.x + halfWidthWorld) / mapWidthWorld;
            var mileV = (-worldPosition.z + halfHeightWorld) / mapHeightWorld;
            return new Vector2(u, 1f - mileV);
        }

        public static float GetLandBlendAtMaskUv(Vector2 maskUv)
        {
            var map = GetReadableMap();
            if (map == null)
            {
                return 1f;
            }

            if (maskUv.x < 0f || maskUv.x > 1f || maskUv.y < 0f || maskUv.y > 1f)
            {
                return 0f;
            }

            var x = maskUv.x * (map.width - 1);
            var y = maskUv.y * (map.height - 1);
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var x1 = Mathf.Min(x0 + 1, map.width - 1);
            var y1 = Mathf.Min(y0 + 1, map.height - 1);
            var tx = x - x0;
            var ty = y - y0;

            var c00 = GetDisplayLandBlendPixel(map, x0, y0);
            var c10 = GetDisplayLandBlendPixel(map, x1, y0);
            var c01 = GetDisplayLandBlendPixel(map, x0, y1);
            var c11 = GetDisplayLandBlendPixel(map, x1, y1);
            var c0 = Mathf.Lerp(c00, c10, tx);
            var c1 = Mathf.Lerp(c01, c11, ty);
            return Mathf.Lerp(c0, c1, ty);
        }

        public static float GetLandBlendAtWorld(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            var maskUv = WorldPositionToShaderMaskUv(worldPosition, worldMap, ticSizeWorldUnits);
            return GetLandBlendAtMaskUv(maskUv);
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

            var insetPixels = ResolveInsetPixelRadius(insetMiles, map, mapSizeMiles);
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

        public static Vector2 PixelToMiles(Vector2Int pixel, int width, int height, float mapWidthMiles)
        {
            var aspect = width > 0 && height > 0 ? (float)width / height : DefaultMapWidthOverHeight;
            var mapHeightMiles = mapWidthMiles / aspect;
            var u = pixel.x / (float)(width - 1);
            // Match ProceduralFlightGround.shader WorldToMaskUv (+Y north = increasing texture v).
            var v = pixel.y / (float)(height - 1);
            return new Vector2(u * mapWidthMiles, v * mapHeightMiles);
        }

        public static bool TryMilesToPixel(
            Vector2 positionMiles,
            int width,
            int height,
            float mapWidthMiles,
            out Vector2Int pixel)
        {
            pixel = default;
            var aspect = width > 0 && height > 0 ? (float)width / height : DefaultMapWidthOverHeight;
            var mapHeightMiles = mapWidthMiles / aspect;
            var u = positionMiles.x / mapWidthMiles;
            var v = positionMiles.y / mapHeightMiles;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return false;
            }

            pixel = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(u * (width - 1)), 0, width - 1),
                Mathf.Clamp(Mathf.RoundToInt(v * (height - 1)), 0, height - 1));
            return true;
        }

        /// <summary>
        /// Landing check aligned with flight-ground shader sampling; allows a small coastal sample radius.
        /// </summary>
        public static bool IsLandingLandWorld(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            const float sampleStepMiles = 0.4f;
            const float landThreshold = 0.45f;
            if (GetLandBlendAtWorld(worldPosition, worldMap, ticSizeWorldUnits) >= landThreshold)
            {
                return true;
            }

            var worldUnitsPerMile = worldMap.GetWorldUnitsPerMile(ticSizeWorldUnits);
            var sampleStepWorld = sampleStepMiles * worldUnitsPerMile;
            var offsets = new[]
            {
                new Vector3(sampleStepWorld, 0f, 0f),
                new Vector3(-sampleStepWorld, 0f, 0f),
                new Vector3(0f, 0f, sampleStepWorld),
                new Vector3(0f, 0f, -sampleStepWorld)
            };

            for (var i = 0; i < offsets.Length; i++)
            {
                if (GetLandBlendAtWorld(worldPosition + offsets[i], worldMap, ticSizeWorldUnits) >= landThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mile-space wrapper for <see cref="IsLandingLandWorld"/>.
        /// </summary>
        public static bool IsLandingLandMiles(Vector2 positionMiles, float mapSizeMiles)
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null || ticSize <= 0f)
            {
                return false;
            }

            var worldPosition = CampaignMapCoordinates.MilesToWorld(positionMiles, worldMap, ticSize);
            return IsLandingLandWorld(worldPosition, worldMap, ticSize);
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

        private static int MilesToPixels(float miles, int mapSpanPixels, float mapSpanMiles)
        {
            return Mathf.Max(1, Mathf.RoundToInt(miles / mapSpanMiles * mapSpanPixels));
        }

        private static int ResolveInsetPixelRadius(float insetMiles, Texture2D map, float mapWidthMiles)
        {
            var mapHeightMiles = GetMapHeightMiles(mapWidthMiles);
            var insetPixelsX = MilesToPixels(insetMiles, map.width, mapWidthMiles);
            var insetPixelsY = MilesToPixels(insetMiles, map.height, mapHeightMiles);
            return Mathf.Min(insetPixelsX, insetPixelsY);
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
            return GetDisplayLandBlendPixel(map, x, y);
        }

        private static float GetDisplayLandBlendPixel(Texture2D map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.width || y >= map.height)
            {
                return 0f;
            }

            return GetSilhouetteLandScore(map.GetPixel(x, y));
        }

        private static float GetLandBlendPixel(Texture2D map, int x, int y)
        {
            return GetDisplayLandBlendPixel(map, x, y);
        }

        private static float GetSilhouetteLandScore(Color color)
        {
            return color.a >= 0.5f ? 1f : 0f;
        }

        private static bool IsSourceLandPixel(Color color)
        {
            if (color.a <= 0.02f)
            {
                return false;
            }

            var luminance = (color.r + color.g + color.b) / 3f;
            return luminance > SeaLuminanceThreshold;
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

            var pixels = copy.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var color = (Color)pixels[i];
                if (IsSourceLandPixel(color))
                {
                    pixels[i] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255),
                        255);
                }
                else
                {
                    pixels[i] = new Color32(255, 255, 255, 0);
                }
            }

            copy.SetPixels32(pixels);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.filterMode = FilterMode.Bilinear;
            return copy;
        }
    }
}
