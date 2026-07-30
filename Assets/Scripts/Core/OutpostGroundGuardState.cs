using F89.LandCombat;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Per-outpost persistence for surface bunker guards — killed guards never respawn.</summary>
    public static class OutpostGroundGuardState
    {
        public static string GetGuardLabel(int guardIndex) => $"Guard-{guardIndex}";

        public static bool IsGuardDestroyed(string outpostName, int guardIndex) =>
            AntarcticaOutpostState.IsTargetDestroyed(outpostName, GetGuardLabel(guardIndex));

        public static void MarkGuardDestroyed(string outpostName, int guardIndex)
        {
            if (string.IsNullOrWhiteSpace(outpostName) || guardIndex < 0)
            {
                return;
            }

            AntarcticaOutpostState.MarkTargetDestroyed(outpostName, GetGuardLabel(guardIndex));
        }

        public static bool AreAllGuardsDestroyed(string outpostName, int guardCount)
        {
            if (guardCount <= 0)
            {
                return true;
            }

            for (var i = 0; i < guardCount; i++)
            {
                if (!IsGuardDestroyed(outpostName, i))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True when every surface guard slot is cleared in save.</summary>
        public static bool AreSurfaceGuardsCleared(string outpostName, int guardCount)
        {
            if (guardCount <= 0)
            {
                return true;
            }

            return AreAllGuardsDestroyed(outpostName, guardCount);
        }

        /// <summary>Live ground check + save lookup for runway deck services.</summary>
        public static bool RefreshSurfaceClearance(string outpostName, int guardCount)
        {
            if (guardCount <= 0 || string.IsNullOrWhiteSpace(outpostName))
            {
                return true;
            }

            if (IsGroundCombatSceneActive() && !HasLivingSurfaceGuardsInScene())
            {
                PersistFullGuardClearance(outpostName, guardCount);
                return true;
            }

            return AreSurfaceGuardsCleared(outpostName, guardCount);
        }

        public static bool HasLivingSurfaceGuardsInScene()
        {
            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy != null
                    && enemy.IsAlive
                    && enemy.GetComponent<LandOutpostGuardMarker>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static void PersistFullGuardClearance(string outpostName, int guardCount)
        {
            if (string.IsNullOrWhiteSpace(outpostName) || guardCount <= 0)
            {
                return;
            }

            for (var i = 0; i < guardCount; i++)
            {
                MarkGuardDestroyed(outpostName, i);
            }

            if (LandBossMissionAssignment.TryGetBossForOutpost(
                    CharacterSessionState.ActiveSave,
                    outpostName,
                    out var bossNumber)
                && bossNumber > 0)
            {
                LandBossEncounter.MarkGuardsCleared(bossNumber);
            }
        }

        /// <summary>When leaving ground combat with no living guards, persist full clearance.</summary>
        public static void SyncClearanceFromGroundSession()
        {
            if (!OutpostRunwayDeckState.IsParkedAtRunway)
            {
                return;
            }

            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            if (HasLivingSurfaceGuardsInScene())
            {
                return;
            }

            PersistFullGuardClearance(
                outpostName,
                OutpostRunwayDeckState.ResolveRunwayGuardCount(outpostName));
        }

        private static bool IsGroundCombatSceneActive()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.GroundAttack;
        }

        public static void TryMarkBossGuardsCleared(string outpostName, int bossNumber, int guardCount)
        {
            if (bossNumber <= 0 || !AreAllGuardsDestroyed(outpostName, guardCount))
            {
                return;
            }

            LandBossEncounter.MarkGuardsCleared(bossNumber);
        }
    }
}
