using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    /// <summary>Per-map bunker view plates. Map 1 uses the default sandbag frame.</summary>
    public static class BattlefieldBackgroundCatalog
    {
        public const string DefaultFrameResourcePath = "Bunker/sandbag_frame";

        private static readonly Rect DefaultOpeningViewport = new(0.075f, 0.106f, 0.845f, 0.783f);

        public const float DefaultSpawnLowerByPixels = 30f;

        private static readonly float[] SpawnLowerByPixelsByMap =
        {
            DefaultSpawnLowerByPixels,
            DefaultSpawnLowerByPixels + 30f,
            DefaultSpawnLowerByPixels + 40f
        };

        private static readonly string[] FrameResourcePathByMap =
        {
            DefaultFrameResourcePath,
            "Bunker/sandbag_frame_map02",
            "Bunker/sandbag_frame_map03"
        };

        public static string GetFrameResourcePath(int mapNumber)
        {
            if (mapNumber >= 1 && mapNumber <= FrameResourcePathByMap.Length)
            {
                return FrameResourcePathByMap[mapNumber - 1];
            }

            return DefaultFrameResourcePath;
        }

        public static Rect GetOpeningViewport(int mapNumber)
        {
            return DefaultOpeningViewport;
        }

        public static float GetSpawnLowerByPixels(int mapNumber)
        {
            mapNumber = Mathf.Max(1, mapNumber);
            var offset = mapNumber <= SpawnLowerByPixelsByMap.Length
                ? SpawnLowerByPixelsByMap[mapNumber - 1]
                : DefaultSpawnLowerByPixels;

            if (mapNumber >= 4)
            {
                offset += 40f;
            }

            return offset;
        }

        /// <summary>Map 1 baseline count; map 2 doubles it; each later map +10% rounded up.</summary>
        public static int ScaleSpawnCount(int mapOneCount, int mapNumber)
        {
            mapNumber = Mathf.Max(1, mapNumber);
            if (mapNumber == 1)
            {
                return mapOneCount;
            }

            var count = mapOneCount * 2;
            for (var map = 3; map <= mapNumber; map++)
            {
                count = Mathf.CeilToInt(count * 1.1f);
            }

            return count;
        }
    }
}
