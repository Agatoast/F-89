using F89.Core;
using F89.Flight;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    [DefaultExecutionOrder(10000)]
    public class GamePauseController : MonoBehaviour
    {
        private enum PauseView
        {
            Root = 0,
            Settings = 1,
            MainMenuConfirm = 2
        }

        private const float DialogWidth = 420f;
        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 40f;
        private const float ButtonSpacing = 10f;

        public static bool IsPaused { get; private set; }

        private static PauseView currentView = PauseView.Root;
        private static SettingsMenuUi.View settingsView = SettingsMenuUi.View.Root;
        private static float timeScaleBeforePause = 1f;

        private GUIStyle titleStyle;
        private GUIStyle messageStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsPaused = false;
            currentView = PauseView.Root;
            settingsView = SettingsMenuUi.View.Root;
        }

        private void Awake()
        {
            var controllers = Object.FindObjectsByType<GamePauseController>(FindObjectsSortMode.None);
            if (controllers.Length > 1 && controllers[0] != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) || !CanOpenPauseMenu())
            {
                return;
            }

            if (IsPaused)
            {
                HandleEscapeWhilePaused();
            }
            else
            {
                ShowPauseMenu();
            }
        }

        private void LateUpdate()
        {
            if (!IsPaused || !ShouldFreezeGameplay())
            {
                return;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void OnGUI()
        {
            if (Event.current == null || !IsPaused)
            {
                return;
            }

            EnsureStyles();
            DrawOverlay();

            switch (currentView)
            {
                case PauseView.Settings:
                    DrawSettingsMenu();
                    break;
                case PauseView.MainMenuConfirm:
                    DrawMainMenuConfirm();
                    break;
                default:
                    DrawRootMenu();
                    break;
            }
        }

        private static bool CanOpenPauseMenu()
        {
            if (AntarcticaMapOverlay.IsOpen)
            {
                return false;
            }

            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName != GameScenes.LoadingScreen;
        }

        private static void HandleEscapeWhilePaused()
        {
            switch (currentView)
            {
                case PauseView.Settings:
                    if (GameKeyBindings.IsListening)
                    {
                        GameKeyBindings.CancelListening();
                        break;
                    }

                    if (settingsView == SettingsMenuUi.View.Keymap)
                    {
                        settingsView = SettingsMenuUi.View.Root;
                        break;
                    }

                    currentView = PauseView.Root;
                    settingsView = SettingsMenuUi.View.Root;
                    break;
                case PauseView.MainMenuConfirm:
                    currentView = PauseView.Root;
                    break;
                default:
                    ResumeGameplay();
                    break;
            }
        }
        private static void ShowPauseMenu()
        {
            IsPaused = true;
            currentView = PauseView.Root;
            CaptureTimeScaleBeforePause();
            if (ShouldFreezeGameplay())
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static void ResumeGameplay()
        {
            IsPaused = false;
            currentView = PauseView.Root;
            settingsView = SettingsMenuUi.View.Root;
            GameKeyBindings.CancelListening();
            AudioListener.pause = false;
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        }

        private static void CaptureTimeScaleBeforePause()
        {
            var autopilot = AutopilotController.Instance;
            if (autopilot != null && autopilot.IsFlying)
            {
                timeScaleBeforePause = autopilot.TimeWarpScale;
                return;
            }

            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        }

        private static bool ShouldFreezeGameplay()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName == GameScenes.FlightTest
                || sceneName == GameScenes.GroundAttack;
        }

        private void DrawOverlay()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawRootMenu()
        {
            var buttonCount = 4;
            var dialogHeight = GetDialogHeight(buttonCount, includeMessage: false);
            var dialogRect = GetDialogRect(dialogHeight);
            DrawDialogFrame(dialogRect, "Paused");

            var buttonX = dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f;
            var buttonY = dialogRect.y + 64f;

            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), GameSettings.SoundLabel))
            {
                GameSettings.ToggleSound();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "Main Menu"))
            {
                HandleMainMenuRequest();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "Settings"))
            {
                currentView = PauseView.Settings;
                settingsView = SettingsMenuUi.View.Root;
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "Exit Game"))
            {
                ExitGame();
            }
        }

        private void DrawSettingsMenu()
        {
            settingsView = SettingsMenuUi.Draw(settingsView, () =>
            {
                settingsView = SettingsMenuUi.View.Root;
                currentView = PauseView.Root;
            });
        }

        private void DrawMainMenuConfirm()
        {
            var dialogHeight = 220f;
            var dialogRect = GetDialogRect(dialogHeight);
            DrawDialogFrame(dialogRect, "Leave Mission?");

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 58f, dialogRect.width - 48f, 72f),
                "Returning to the Main Menu now will negatively impact your record for failing to finish the mission.",
                messageStyle);

            var choiceY = dialogRect.yMax - ButtonHeight - 24f;
            var yesRect = new Rect(dialogRect.x + dialogRect.width * 0.5f - ButtonWidth * 0.5f - 12f, choiceY, 120f, ButtonHeight);
            var noRect = new Rect(dialogRect.x + dialogRect.width * 0.5f + 12f, choiceY, 120f, ButtonHeight);

            if (DrawMenuButton(yesRect, "Yes"))
            {
                QuitToMainMenu(applyMissionPenalty: true);
            }

            if (DrawMenuButton(noRect, "No"))
            {
                currentView = PauseView.Root;
            }
        }

        private void HandleMainMenuRequest()
        {
            if (MissionProgressState.IsMissionInProgress())
            {
                currentView = PauseView.MainMenuConfirm;
                return;
            }

            QuitToMainMenu(applyMissionPenalty: false);
        }

        private static void QuitToMainMenu(bool applyMissionPenalty)
        {
            if (applyMissionPenalty)
            {
                var save = CharacterSessionState.ActiveSave;
                if (save != null)
                {
                    CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
                }
            }

            AutopilotController.Instance?.DisengageAutopilot("Returned to main menu.");
            Object.FindAnyObjectByType<AntarcticaMapOverlay>()?.CloseMap();

            IsPaused = false;
            currentView = PauseView.Root;
            AudioListener.pause = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }

        private static void ExitGame()
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void DrawDialogFrame(Rect dialogRect, string title)
        {
            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);

            GUI.Label(new Rect(dialogRect.x + 16f, dialogRect.y + 18f, dialogRect.width - 32f, 32f), title, titleStyle);
        }

        private static Rect GetDialogRect(float dialogHeight)
        {
            return new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                DialogWidth,
                dialogHeight);
        }

        private static float GetDialogHeight(int buttonCount, bool includeMessage)
        {
            var height = 64f + buttonCount * ButtonHeight + Mathf.Max(0, buttonCount - 1) * ButtonSpacing + 28f;
            if (includeMessage)
            {
                height += 48f;
            }

            return height;
        }

        private bool DrawMenuButton(Rect rect, string label)
        {
            HudGuiUtility.DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            messageStyle = HudStyleFactory.CreateLabel(15, FontStyle.Normal, TextAnchor.UpperCenter, Color.black, wordWrap: true);
            buttonStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
        }
    }
}
