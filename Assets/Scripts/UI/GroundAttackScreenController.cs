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
            DrawReturnButton();
        }

        private static void DrawHudOverlay()
        {
            var hudStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            GUI.Label(
                new Rect(16f, 16f, 520f, 24f),
                $"Ground Ops  |  Kills: {LandGroundSceneController.SessionKills}  Score: {LandGroundSceneController.SessionScore}",
                hudStyle);
            GUI.Label(new Rect(16f, 40f, 520f, 24f), "WASD move, mouse aim, click to fire.", hudStyle);
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

            var returnScene = GameScenes.FlightTest;
            if (LandMissionHandoffState.TryConsumeReturnToFlight(out var snapshot, out _))
            {
                returnScene = string.IsNullOrEmpty(snapshot.ReturnSceneName)
                    ? GameScenes.FlightTest
                    : snapshot.ReturnSceneName;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(returnScene);
        }
    }
}
