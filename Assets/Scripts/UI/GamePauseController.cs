using F89.Core;
using F89.Flight;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class GamePauseController : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsPaused = false;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (IsPaused)
            {
                ResumeGameplay();
            }
            else
            {
                ShowPausePrompt();
            }
        }

        private void OnGUI()
        {
            if (Event.current == null || !IsPaused)
            {
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawOverlay();
            }

            DrawDialog();
        }

        private static void ShowPausePrompt()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        private static void ResumeGameplay()
        {
            IsPaused = false;
            var autopilot = AutopilotController.Instance;
            Time.timeScale = autopilot != null && autopilot.IsFlying
                ? autopilot.TimeWarpScale
                : 1f;
        }

        private void QuitToMainMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }

        private void DrawOverlay()
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawDialog()
        {
            const float dialogWidth = 360f;
            const float dialogHeight = 170f;
            var dialogRect = new Rect(
                (Screen.width - dialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                dialogWidth,
                dialogHeight);

            GUI.Box(dialogRect, GUIContent.none);

            var titleStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            GUI.Label(new Rect(dialogRect.x + 16f, dialogRect.y + 20f, dialogRect.width - 32f, 30f), "Paused", titleStyle);

            var messageStyle = HudStyleFactory.CreateLabel(15, FontStyle.Normal, TextAnchor.UpperCenter, Color.white, wordWrap: true);
            GUI.Label(
                new Rect(dialogRect.x + 16f, dialogRect.y + 58f, dialogRect.width - 32f, 40f),
                "Quit to Main Menu?",
                messageStyle);

            var buttonWidth = 120f;
            var buttonHeight = 34f;
            var buttonY = dialogRect.y + dialogRect.height - buttonHeight - 20f;
            var yesX = dialogRect.x + dialogRect.width * 0.5f - buttonWidth - 8f;
            var noX = dialogRect.x + dialogRect.width * 0.5f + 8f;

            if (GUI.Button(new Rect(yesX, buttonY, buttonWidth, buttonHeight), "Yes"))
            {
                QuitToMainMenu();
            }

            if (GUI.Button(new Rect(noX, buttonY, buttonWidth, buttonHeight), "No"))
            {
                ResumeGameplay();
            }
        }
    }
}
