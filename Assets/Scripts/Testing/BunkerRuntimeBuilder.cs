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
        private bool pendingBossSpawn;

        private void Start()
        {
            LandGroundCrosshair.Apply();
            LandBossEncounter.ClearIntro();
            if (LandBunkerHandoffState.ConsumeBossFightPending())
            {
                pendingBossSpawn = true;
                LandBossEncounter.BeginIntroCountdown();
                Debug.Log("F-89 Bunker: Boss fight intro — 5s countdown.");
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
            TryScheduleDownedExit();
            TryFinishBossIntro();
        }

        private void TryFinishBossIntro()
        {
            if (!pendingBossSpawn || !LandBossEncounter.TickIntroFinished())
            {
                return;
            }

            pendingBossSpawn = false;
            SpawnBoss();
        }

        private static void SpawnBoss()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null)
            {
                Debug.LogWarning("F-89 Bunker: Cannot spawn boss — no player.");
                return;
            }

            var spawnPos = (Vector2)player.transform.position + Vector2.up * 4.5f;
            var go = new GameObject("Boss1");
            var enemy = go.AddComponent<LandGroundEnemy>();
            enemy.Initialize(spawnPos, 2, player.transform);
            go.name = "Boss1";
            Debug.Log("F-89 Bunker: Boss 1 engaged.");
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

            LandCombatHud.DrawEnemyHpBars();

            var style = HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            GUI.Label(new Rect(12f, 12f, 480f, 40f), "BUNKER  |  Blue pad returns to surface", style);

            var exitRect = new Rect(12f, Screen.height - 48f, 180f, 36f);
            if (StartPageMenuStyles.DrawMenuButton(exitRect, "SURFACE", fontSize: 14))
            {
                Time.timeScale = 1f;
                LandBunkerHandoffState.Clear();
                LandSurfaceSession.BeginReturnAtBunker();
                UnityEngine.SceneManagement.SceneManager.LoadScene(F89.Core.GameScenes.GroundAttack);
            }

            var settingsRect = new Rect(Screen.width - 230f, Screen.height - 48f, 100f, 36f);
            if (StartPageMenuStyles.DrawMenuButton(settingsRect, "SETTINGS", fontSize: 14))
            {
                GamePauseController.OpenSettingsMenu();
            }
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
