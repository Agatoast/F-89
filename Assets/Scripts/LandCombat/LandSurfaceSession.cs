using System.Collections.Generic;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Preserves the outdoor ground map across bunker trips so exiting the bunker
    /// returns the player next to the entrance (not the landed plane).
    /// </summary>
    public static class LandSurfaceSession
    {
        private struct EnemyRecord
        {
            public Vector2 Position;
            public int Level;
        }

        private static bool hasSnapshot;
        private static bool pendingReturnAtBunker;
        private static Vector2 planePosition;
        private static Vector2 bunkerPosition;
        private static readonly List<EnemyRecord> livingEnemies = new();

        public static bool HasSnapshot => hasSnapshot;
        public static bool HasPendingReturnAtBunker => pendingReturnAtBunker;
        public static Vector2 BunkerPosition => bunkerPosition;
        public static Vector2 PlanePosition => planePosition;

        public static void CaptureFromWorld()
        {
            livingEnemies.Clear();
            hasSnapshot = false;

            var plane = Object.FindAnyObjectByType<LandLandedPlane>();
            planePosition = plane != null ? (Vector2)plane.WorldPosition : Vector2.zero;

            var bunker = Object.FindAnyObjectByType<LandBunkerEntrance>();
            if (bunker == null)
            {
                Debug.LogWarning("F-89 Land: No bunker to capture for surface return.");
                return;
            }

            bunkerPosition = bunker.WorldPosition;

            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                livingEnemies.Add(new EnemyRecord
                {
                    Position = enemy.transform.position,
                    Level = enemy.Level
                });
            }

            hasSnapshot = true;
        }

        public static void BeginReturnAtBunker()
        {
            if (!hasSnapshot)
            {
                Debug.LogWarning("F-89 Land: Return-at-bunker requested with no surface snapshot.");
            }

            pendingReturnAtBunker = true;
        }

        /// <summary>Creates a surface return point for Main Menu boss jumps that did not begin on the ground map.</summary>
        public static void PrepareDirectBossReturn()
        {
            livingEnemies.Clear();
            planePosition = Vector2.zero;
            bunkerPosition = LandOutpostSurfaceLayout.BunkerPositionFromPlane(planePosition);
            hasSnapshot = true;
        }

        public static bool TryConsumeReturnAtBunker(
            out Vector2 planePos,
            out Vector2 bunkerPos,
            out IReadOnlyList<(Vector2 position, int level)> enemies)
        {
            planePos = planePosition;
            bunkerPos = bunkerPosition;
            enemies = null;
            if (!pendingReturnAtBunker || !hasSnapshot)
            {
                pendingReturnAtBunker = false;
                return false;
            }

            pendingReturnAtBunker = false;
            var list = new List<(Vector2, int)>(livingEnemies.Count);
            for (var i = 0; i < livingEnemies.Count; i++)
            {
                list.Add((livingEnemies[i].Position, livingEnemies[i].Level));
            }

            enemies = list;
            return true;
        }

        public static void Clear()
        {
            hasSnapshot = false;
            pendingReturnAtBunker = false;
            planePosition = Vector2.zero;
            bunkerPosition = Vector2.zero;
            livingEnemies.Clear();
        }
    }
}
