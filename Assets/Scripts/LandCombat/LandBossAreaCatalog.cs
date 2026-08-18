using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Named surface and bunker destinations for the boss-fight mission chain.</summary>
    public static class LandBossAreaCatalog
    {
        public readonly struct Definition
        {
            public Definition(int bossNumber, int guardCount, int guardLevel, float bearingDegrees)
            {
                BossNumber = bossNumber;
                SurfaceCode = $"BF{bossNumber}";
                BunkerCode = $"BF{bossNumber}B";
                GuardCount = guardCount;
                GuardLevel = guardLevel;
                BearingDegrees = bearingDegrees;
            }

            public int BossNumber { get; }
            public string SurfaceCode { get; }
            public string BunkerCode { get; }
            public int GuardCount { get; }
            public int GuardLevel { get; }
            public float BearingDegrees { get; }
        }

        // Fixed bearings make each BF surface a repeatable, distinct mission area.
        private static readonly Definition[] Areas =
        {
            new(1, 5, 1, 0f),
            new(2, 5, 1, 36f),
            new(3, 5, 2, 72f),
            new(4, 5, 2, 108f),
            new(5, 5, 3, 144f),
            new(6, 5, 3, 180f),
            new(7, 5, 4, 216f),
            new(8, 5, 4, 252f),
            new(9, 5, 5, 288f),
            new(10, 5, 5, 324f),
            new(11, 5, 5, 0f),
            new(12, 5, 5, 36f),
            new(13, 5, 5, 72f),
            new(14, 5, 5, 108f),
            new(15, 5, 5, 144f),
            new(16, 5, 5, 180f),
            new(17, 5, 5, 216f),
            new(18, 5, 5, 252f),
            new(19, 5, 5, 288f)
        };

        public static bool TryGet(int bossNumber, out Definition area)
        {
            if (bossNumber >= 1 && bossNumber <= Areas.Length)
            {
                area = Areas[bossNumber - 1];
                return true;
            }

            area = default;
            return false;
        }
    }
}
