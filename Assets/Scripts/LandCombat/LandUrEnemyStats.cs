using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// UR soldier combat stats by level. Map placement / level mix is mission-driven later;
    /// early missions favor low levels, rising toward the main base.
    /// </summary>
    public static class LandUrEnemyStats
    {
        public const int MaxHitPoints = 100;
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        /// <summary>Total DR by soldier level (raw damage before HP loss = Damage - DR).</summary>
        private static readonly int[] DamageResistanceByLevel =
        {
            0,  // unused index 0
            8,  // 1
            10, // 2
            12, // 3
            15, // 4
            17, // 5
            19, // 6
            22, // 7
            24, // 8
            26, // 9
            29  // 10
        };

        /// <summary>Weapon Damage by soldier level (raw hit value before player DR).</summary>
        private static readonly int[] WeaponDamageByLevel =
        {
            0,  // unused index 0
            13, // 1
            15, // 2
            16, // 3
            20, // 4
            22, // 5
            24, // 6
            27, // 7
            29, // 8
            31, // 9
            34  // 10
        };

        /// <summary>Move rating by soldier level (same 3–15 scale as player Move).</summary>
        private static readonly int[] MoveByLevel =
        {
            0,  // unused index 0
            3,  // 1
            4,  // 2
            5,  // 3
            6,  // 4
            7,  // 5
            8,  // 6
            9,  // 7
            10, // 8
            11, // 9
            12  // 10
        };

        public static int ClampLevel(int level) =>
            Mathf.Clamp(level, MinLevel, MaxLevel);

        public static int GetDamageResistance(int level)
        {
            level = ClampLevel(level);
            return DamageResistanceByLevel[level];
        }

        public static int GetWeaponDamage(int level)
        {
            level = ClampLevel(level);
            return WeaponDamageByLevel[level];
        }

        public static int GetMove(int level)
        {
            level = ClampLevel(level);
            return MoveByLevel[level];
        }

        /// <summary>World units/sec from Move rating (same curve as player).</summary>
        public static float GetMoveSpeedWorldUnits(int level)
        {
            var move = GetMove(level);
            return LandGameConstants.MoveWorldUnitsPerSecondAtRating3
                   * (move / (float)LandGameConstants.MoveMin);
        }
    }
}
