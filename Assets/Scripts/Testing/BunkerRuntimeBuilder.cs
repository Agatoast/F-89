using F89.Core;
using F89.LandCombat;
using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class BunkerRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<BunkerScreenController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            LandBunkerSceneBuilder.BuildIfNeeded();
            var root = new GameObject("BunkerScreen");
            root.AddComponent<BunkerScreenController>();
        }
    }
}

namespace F89.UI
{
    public sealed class BunkerScreenController : MonoBehaviour
    {
        private const float DownedExitDelaySeconds = 2.5f;
        private bool exitingAfterDowned;
        private float downedExitAt;
        private int pendingBossNumber;

        private void Start()
        {
            LandGroundCrosshair.Apply();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            LandBossEncounter.ClearIntro();
            pendingBossNumber = LandBunkerHandoffState.ConsumeBossFightPending();
            if (pendingBossNumber != 0)
            {
                LandBossEncounter.BeginBossFight(pendingBossNumber);
                LandBossEncounter.BeginIntroCountdown();
                Debug.Log($"F-89 Bunker: Boss {pendingBossNumber} intro — 5s countdown.");
            }
        }

        private void OnDestroy()
        {
            LandGroundCrosshair.Clear();
            LandBossEncounter.ClearIntro();
        }

        private void OnDisable()
        {
            LandGroundCrosshair.Clear();
        }

        private void Update()
        {
            LandCombatHud.HandleHotkeys();
            TryScheduleDownedExit();
            TryFinishBossIntro();
        }

        private void TryFinishBossIntro()
        {
            if (pendingBossNumber == 0 || !LandBossEncounter.TickIntroFinished())
            {
                return;
            }

            var bossNumber = pendingBossNumber;
            pendingBossNumber = 0;
            SpawnBoss(bossNumber);
        }

        private static void SpawnBoss(int bossNumber)
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null)
            {
                Debug.LogWarning("F-89 Bunker: Cannot spawn boss — no player.");
                return;
            }

            var bossCount = LandBossEncounter.GetEnemyCount(bossNumber);
            for (var enemyIndex = 0; enemyIndex < bossCount; enemyIndex++)
            {
                var savedHitPoints = LandBossEncounter.GetSavedHitPoints(bossNumber, enemyIndex);
                if (savedHitPoints == 0f)
                {
                    continue;
                }

                var lateralOffset = bossCount == 1 ? 0f : (enemyIndex == 0 ? -1.25f : 1.25f);
                var spawnPos = (Vector2)player.transform.position + Vector2.up * 4.5f + Vector2.right * lateralOffset;
                var objectName = LandBossEncounter.GetObjectName(bossNumber, enemyIndex);
                var go = new GameObject(objectName);
                var enemy = go.AddComponent<LandGroundEnemy>();
                enemy.Initialize(
                    spawnPos,
                    LandBossEncounter.GetEnemyLevel(bossNumber),
                    player.transform,
                    savedHitPoints);
                go.name = objectName;
            }

            var enemyLabel = bossCount == 1 ? "enemy" : "enemies";
            Debug.Log($"F-89 Bunker: Boss {bossNumber} engaged with {bossCount} UR level {LandBossEncounter.GetEnemyLevel(bossNumber)} {enemyLabel}.");
        }

        private void TryScheduleDownedExit()
        {
            if (exitingAfterDowned)
            {
                if (Time.unscaledTime >= downedExitAt)
                {
                    exitingAfterDowned = false;
                    LandGroundMissionExit.Leave();
                }

                return;
            }

            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            if (health == null || !health.IsUnconscious)
            {
                return;
            }

            exitingAfterDowned = true;
            downedExitAt = Time.unscaledTime + DownedExitDelaySeconds;
            Debug.Log($"F-89 Land: Downed in bunker ({health.DownedOutcome}) — leaving in {DownedExitDelaySeconds:0.0}s.");
        }

        private void OnGUI()
        {
            if (LandBossEncounter.IsIntroActive)
            {
                DrawBossCountdown();
                return;
            }

            LandCombatHud.Draw(null);
            LandLootBagUi.Draw();

            var style = HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var bunkerCode = LandBossAreaState.HasActiveArea ? LandBossAreaState.BunkerCode : "BUNKER";
            GUI.Label(new Rect(12f, 12f, 480f, 40f), $"{bunkerCode}  |  Blue pad returns to surface", style);
        }

        private static void DrawBossCountdown()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var secondsLeft = Mathf.CeilToInt(LandBossEncounter.RemainingSeconds);
            var numberStyle = HudStyleFactory.CreateLabel(96, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var labelStyle = HudStyleFactory.CreateLabel(22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.55f));
            GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 40f), "BOSS FIGHT", labelStyle);
            GUI.Label(new Rect(0f, Screen.height * 0.36f, Screen.width, 120f), secondsLeft.ToString(), numberStyle);
        }
    }
}
