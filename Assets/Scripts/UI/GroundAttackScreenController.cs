using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class GroundAttackScreenController : MonoBehaviour
    {
        private void OnGUI()
        {
            DrawHudOverlay();
            DrawHealthBar();
            DrawReturnButton();
        }

        private static void DrawHealthBar()
        {
            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            if (health == null)
            {
                return;
            }

            const float barWidth = 220f;
            const float barHeight = 18f;
            const float margin = 16f;
            var barRect = new Rect(margin, Screen.height - barHeight - margin - 52f, barWidth, barHeight);
            var fillRect = new Rect(barRect.x + 1f, barRect.y + 1f, (barRect.width - 2f) * health.HealthNormalized, barRect.height - 2f);

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            GUI.color = health.IsAlive ? new Color(0.18f, 0.82f, 0.28f, 0.95f) : new Color(0.75f, 0.12f, 0.12f, 0.95f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var labelStyle = HudStyleFactory.CreateLabel(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            GUI.Label(barRect, $"{health.HealthPercent:0}%", labelStyle);
        }

        private static void DrawHudOverlay()
        {
            var hudStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            GUI.Label(
                new Rect(16f, 16f, 520f, 24f),
                $"Ground Ops  |  Kills: {LandGroundSceneController.SessionKills}  Score: {LandGroundSceneController.SessionScore}",
                hudStyle);

            var weaponLine = BuildEquippedWeaponLine();
            if (!string.IsNullOrEmpty(weaponLine))
            {
                GUI.Label(new Rect(16f, 40f, 520f, 24f), weaponLine, hudStyle);
                GUI.Label(new Rect(16f, 64f, 520f, 24f), "WASD move, mouse aim, click to fire.", hudStyle);
            }
            else
            {
                GUI.Label(new Rect(16f, 40f, 520f, 24f), "WASD move, mouse aim, click to fire.", hudStyle);
            }
        }

        private static string BuildEquippedWeaponLine()
        {
            var weaponItem = CharacterGearSession.ActiveLoadout?.Weapon;
            if (weaponItem == null || !LandLoadoutSlots.IsValidItem(weaponItem))
            {
                return string.Empty;
            }

            var catalog = CharacterGearSession.Catalog;
            if (!catalog.TryGetWeaponSummary(weaponItem, out var summary))
            {
                return catalog.GetDisplayName(weaponItem);
            }

            return $"{catalog.GetDisplayName(weaponItem)}  |  {summary}";
        }

        private static void DrawReturnButton()
        {
            const float buttonWidth = 220f;
            const float buttonHeight = 40f;
            var buttonX = (Screen.width - buttonWidth) * 0.5f;

            if (GUI.Button(new Rect(buttonX, Screen.height - buttonHeight - 24f, buttonWidth, buttonHeight), "Return to Flight"))
            {
                ReturnToFlight();
            }
        }

        private static void ReturnToFlight()
        {
            var result = new LandGroundSessionResult
            {
                CompletedVoluntarily = true,
                TroopsKilled = LandGroundSceneController.SessionKills,
                ScoreEarned = LandGroundSceneController.SessionScore
            };

            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.RecordGroundSession(save, result);
            }

            LandCombatModule.ExitToFlight(result);

            Time.timeScale = 1f;
            SceneManager.LoadScene(LandMissionHandoffState.PendingReturnSceneName);
        }
    }
}
