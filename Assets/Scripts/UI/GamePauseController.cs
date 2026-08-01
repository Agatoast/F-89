using F89.Core;
using F89.Flight;
using F89.LandCombat;
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
            MainMenuConfirm = 2,
            Memorial = 3
        }

        private const float DialogWidth = 420f;
        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 40f;
        private const float ButtonSpacing = 10f;
        private const int ButtonFontSize = 16;

        public static bool IsPaused { get; private set; }

        public static GamePauseController Instance { get; private set; }

        private static PauseView currentView = PauseView.Root;
        private static SettingsMenuUi.View settingsView = SettingsMenuUi.View.Root;
        private static float timeScaleBeforePause = 1f;
        private static int lastEscapePauseFrame = -1;

        private GUIStyle titleStyle;
        private GUIStyle messageStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsPaused = false;
            Instance = null;
            currentView = PauseView.Root;
            settingsView = SettingsMenuUi.View.Root;
            lastEscapePauseFrame = -1;
        }

        public static void ClearPauseOnSceneLoad()
        {
            IsPaused = false;
            currentView = PauseView.Root;
            settingsView = SettingsMenuUi.View.Root;
            lastEscapePauseFrame = -1;
            GameKeyBindings.ClearRebindState();
            AudioListener.pause = false;
        }

        public static void EnsureExists()
        {
            var controllers = Object.FindObjectsByType<GamePauseController>(FindObjectsSortMode.None);
            if (controllers.Length == 0)
            {
                var pauseObject = new GameObject("GamePauseController");
                pauseObject.AddComponent<GamePauseController>();
                return;
            }

            GamePauseController keeper = null;
            if (Instance != null)
            {
                for (var i = 0; i < controllers.Length; i++)
                {
                    if (controllers[i] == Instance)
                    {
                        keeper = Instance;
                        break;
                    }
                }
            }

            keeper ??= controllers[0];
            keeper.ClaimInstance();

            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller != null && controller != keeper)
                {
                    Object.Destroy(controller.gameObject);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ClaimInstance();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ClaimInstance()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Update stops when timeScale is 0, so unpause is handled in OnGUI.
            if (IsPaused || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            TryHandleEscapePause();
        }

        private void LateUpdate()
        {
            if (!IsPaused)
            {
                return;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void OnGUI()
        {
            if (Event.current != null
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape
                && TryHandleEscapePause())
            {
                Event.current.Use();
            }

            if (!IsPaused)
            {
                return;
            }

            if (Event.current == null)
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
                case PauseView.Memorial:
                    DrawMemorialWall();
                    break;
                default:
                    DrawRootMenu();
                    break;
            }
        }

        private static bool CanOpenPauseMenu()
        {
            if (GameKeyBindings.IsListening)
            {
                return false;
            }

            if (AircraftLandingController.IsCarrierApproachPromptVisible)
            {
                return false;
            }

            if (LandLootBagSession.IsOpen)
            {
                return false;
            }

            if (AircraftLoadoutController.BlocksPauseMenu)
            {
                return false;
            }

            return true;
        }

        private static bool TryHandleEscapePause()
        {
            if (!CanOpenPauseMenu())
            {
                return false;
            }

            var frame = Time.frameCount;
            if (lastEscapePauseFrame == frame)
            {
                return false;
            }

            lastEscapePauseFrame = frame;
            if (IsPaused)
            {
                HandleEscapeWhilePaused();
            }
            else
            {
                ShowPauseMenu();
            }

            return true;
        }

        private static void HandleEscapeWhilePaused()
        {
            switch (currentView)
            {
                case PauseView.Settings:
                    if (GameKeyBindings.HasPendingConflict)
                    {
                        GameKeyBindings.CancelPendingConflict();
                        break;
                    }

                    if (GameKeyBindings.IsListening)
                    {
                        GameKeyBindings.ClearRebindState();
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
                case PauseView.Memorial:
                    currentView = PauseView.Root;
                    break;
                default:
                    ResumeGameplay();
                    break;
            }
        }
        public static void OpenSettingsMenu()
        {
            if (!CanOpenPauseMenu())
            {
                return;
            }

            ShowPauseMenu();
            currentView = PauseView.Settings;
            settingsView = SettingsMenuUi.View.Root;
        }

        private static void ShowPauseMenu()
        {
            IsPaused = true;
            currentView = PauseView.Root;
            CaptureTimeScaleBeforePause();
            Time.timeScale = 0f;
            AudioListener.pause = true;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static void ResumeGameplay()
        {
            IsPaused = false;
            currentView = PauseView.Root;
            settingsView = SettingsMenuUi.View.Root;
            GameKeyBindings.ClearRebindState();
            AudioListener.pause = false;
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        }

        private static void CaptureTimeScaleBeforePause()
        {
            // Autopilot warp no longer changes Time.timeScale.
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        }

        private void DrawOverlay()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawRootMenu()
        {
            var buttonCount = 8;
            var dialogHeight = GetDialogHeight(buttonCount, includeMessage: false);
            var dialogRect = GetDialogRect(dialogHeight);
            DrawDialogFrame(dialogRect, "Paused");

            var buttonX = dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f;
            var buttonY = dialogRect.y + 64f;

            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "MAIN MENU", fontSize: ButtonFontSize))
            {
                HandleMainMenuRequest();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), GameSettings.SoundLabel.ToUpperInvariant(), fontSize: ButtonFontSize))
            {
                GameSettings.ToggleSound();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), GameSettings.MusicLabel.ToUpperInvariant(), fontSize: ButtonFontSize))
            {
                GameSettings.ToggleMusic();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), GameSettings.MissileSoundsLabel.ToUpperInvariant(), fontSize: ButtonFontSize))
            {
                GameSettings.ToggleMissileSounds();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            var displayLabel = GameSettings.FullscreenEnabled ? "WINDOW" : "FULLSCREEN";
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), displayLabel, fontSize: ButtonFontSize))
            {
                GameSettings.ToggleFullscreen();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "SETTINGS", fontSize: ButtonFontSize))
            {
                currentView = PauseView.Settings;
                settingsView = SettingsMenuUi.View.Root;
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "MEMORIAL WALL", fontSize: ButtonFontSize))
            {
                currentView = PauseView.Memorial;
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "EXIT GAME", fontSize: ButtonFontSize))
            {
                ExitGame();
            }
        }

        private void DrawMemorialWall()
        {
            MemorialWallUi.Draw(() => currentView = PauseView.Root);
        }

        private void DrawSettingsMenu()
        {
            settingsView = SettingsMenuUi.Draw(
                settingsView,
                onExitSettings: () =>
                {
                    settingsView = SettingsMenuUi.View.Root;
                    currentView = PauseView.Root;
                },
                onMainMenu: HandleMainMenuRequest,
                onExitGame: ExitGame);
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
            var yesRect = new Rect(dialogRect.x + dialogRect.width * 0.5f - 120f - 12f, choiceY, 120f, ButtonHeight);
            var noRect = new Rect(dialogRect.x + dialogRect.width * 0.5f + 12f, choiceY, 120f, ButtonHeight);

            if (StartPageMenuStyles.DrawMenuButton(yesRect, "YES", fontSize: ButtonFontSize))
            {
                QuitToMainMenu(applyMissionPenalty: true);
            }

            if (StartPageMenuStyles.DrawMenuButton(noRect, "NO", fontSize: ButtonFontSize))
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
                    CharacterSaveRepository.SyncVehicleKillCredit(save);
                    MissionScoreState.AbandonMissionWithoutScoring();
                    CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
                }
            }

            AutopilotController.Instance?.DisengageAutopilot("Returned to main menu.");
            Object.FindAnyObjectByType<AntarcticaMapOverlay>()?.CloseMap();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            messageStyle = HudStyleFactory.CreateLabel(15, FontStyle.Normal, TextAnchor.UpperCenter, Color.black, wordWrap: true);
        }
    }
}
