using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class StartPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "StartPage/main_menu";
        private const string SaveAntarcticaLogoResourcePath = "CharacterPage/save_antarctica_logo";

        private static readonly string[] ButtonLabels = { "PLAY", "STORY", "SETTINGS", "CREDITS" };

        private Texture2D backgroundTexture;
        private Texture2D saveAntarcticaLogoTexture;
        private static bool showDeleteAllSavesConfirm;

        private void Start()
        {
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);
            if (saveAntarcticaLogoTexture == null)
            {
                Debug.LogWarning("F-89: SAVE Antarctica logo missing from Resources/CharacterPage/save_antarctica_logo.");
            }
        }

        private void OnGUI()
        {
            StartPageMenuStyles.DrawFullscreenBackground(backgroundTexture);
            StartPageMenuStyles.DrawSaveAntarcticaLogo(saveAntarcticaLogoTexture);
            DrawButtons();
            if (!showDeleteAllSavesConfirm)
            {
                DrawDeleteAllSavesButton();
            }
            else
            {
                DrawDeleteAllSavesConfirm();
            }

            if (LandCombatTestCheats.ShowDevMenuButtons)
            {
                DrawTempFightReichButton();
            }
        }

        private static void DrawButtons()
        {
            for (var i = 0; i < ButtonLabels.Length; i++)
            {
                var rect = StartPageMenuStyles.GetMainMenuButtonRect(i, ButtonLabels.Length);
                if (!StartPageMenuStyles.DrawMenuButton(rect, ButtonLabels[i], fontSize: 50))
                {
                    continue;
                }

                HandleButton(ButtonLabels[i]);
            }
        }

        private static void DrawDeleteAllSavesButton()
        {
            var width = UiFitCanvas.Px(220f);
            var height = UiFitCanvas.Px(44f);
            var x = UiFitCanvas.Rect.x + UiFitCanvas.Px(18f);
            var y = UiFitCanvas.Rect.y + UiFitCanvas.Px(18f);
            var buttonRect = new Rect(x, y, width, height);
            if (!StartPageMenuStyles.DrawMenuButton(buttonRect, "DELETE ALL SAVES", fontSize: 22))
            {
                return;
            }

            showDeleteAllSavesConfirm = true;
        }

        private static void DrawDeleteAllSavesConfirm()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            const float dialogWidth = 520f;
            const float dialogHeight = 180f;
            var dialogRect = new Rect(
                (Screen.width - dialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                dialogWidth,
                dialogHeight);

            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);

            var messageStyle = HudStyleFactory.CreateLabel(
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                Color.black,
                wordWrap: true);
            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 28f, dialogRect.width - 48f, 70f),
                "Delete all characters and career progress? This cannot be undone.",
                messageStyle);

            const float choiceWidth = 120f;
            const float choiceHeight = 40f;
            var choiceY = dialogRect.yMax - choiceHeight - 24f;
            var yesRect = new Rect(dialogRect.x + dialogRect.width * 0.5f - choiceWidth - 12f, choiceY, choiceWidth, choiceHeight);
            var noRect = new Rect(dialogRect.x + dialogRect.width * 0.5f + 12f, choiceY, choiceWidth, choiceHeight);

            if (StartPageMenuStyles.DrawMenuButton(yesRect, "YES", fontSize: 16))
            {
                CharacterSaveRepository.ClearAllSaves();
                LandingMileFlagState.Clear();
                MissionScoreState.AbandonMissionWithoutScoring();
                showDeleteAllSavesConfirm = false;
            }

            if (StartPageMenuStyles.DrawMenuButton(noRect, "NO", fontSize: 16))
            {
                showDeleteAllSavesConfirm = false;
            }
        }

        /// <summary>Temporary land-combat jump; remove when land entry is fully wired.</summary>
        private static void DrawTempFightReichButton()
        {
            var width = UiFitCanvas.Px(220f);
            var height = UiFitCanvas.Px(44f);
            var gap = UiFitCanvas.Px(10f);
            var x = UiFitCanvas.Rect.xMax - width - UiFitCanvas.Px(18f);
            var y = UiFitCanvas.Rect.y + UiFitCanvas.Px(18f);

            var fightRect = new Rect(x, y, width, height);
            if (StartPageMenuStyles.DrawMenuButton(fightRect, "FIGHT REICH", fontSize: 22))
            {
                EnterLandCombatDirect();
            }

            for (var bossNumber = LandBossEncounter.FirstBossNumber;
                 bossNumber <= LandBossEncounter.LastBossNumber;
                 bossNumber++)
            {
                var bossRect = new Rect(x, y + (height + gap) * bossNumber, width, height);
                if (!StartPageMenuStyles.DrawMenuButton(bossRect, $"BOSS {bossNumber}", fontSize: 22))
                {
                    continue;
                }

                EnterLandCombatDirect(bossNumber);
            }
        }

        private static void EnterLandCombatDirect(int bossNumber = 0)
        {
            Time.timeScale = 1f;
            EnsureActiveSaveForDevJump();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave, forceReload: true);
            if (bossNumber != 0)
            {
                if (LandCombatTestCheats.ResetAllBossProgressOnDevEntry)
                {
                    CampaignWorldReset.ResetMapForFreshPlay();
                }

                LandBossAreaState.BeginArea(bossNumber);
            }
            else
            {
                LandBossAreaState.Clear();
            }

            var snapshot = new LandSortieSnapshot
            {
                IsValid = true,
                AircraftWorldPosition = Vector3.zero,
                AircraftWorldRotation = Quaternion.identity,
                FuelNormalized = 1f,
                ReturnSceneName = GameScenes.StartPage
            };

            LandMissionHandoffState.BeginEnterFromFlight(snapshot);
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }

        private static void EnsureActiveSaveForDevJump()
        {
            if (CharacterSessionState.ActiveSave != null)
            {
                return;
            }

            var lastId = CharacterSaveRepository.GetLastSelectedSaveId();
            var save = CharacterSaveRepository.FindById(lastId);
            if (save == null)
            {
                var saves = CharacterSaveRepository.Saves;
                if (saves != null && saves.Count > 0)
                {
                    save = saves[0];
                }
            }

            if (save == null)
            {
                save = CharacterSaveRepository.CreateSave("Dev Pilot");
            }

            CharacterSessionState.ActiveSave = save;
            CharacterSaveRepository.SetLastSelectedSaveId(save.Id);
        }

        private static void HandleButton(string label)
        {
            Time.timeScale = 1f;

            switch (label)
            {
                case "PLAY":
                    SceneManager.LoadScene(GameScenes.SelectionPage);
                    break;
                case "STORY":
                    OpenSubpage(StoryPageContent.Title, StoryPageContent.Body);
                    break;
                case "SETTINGS":
                    MenuNavigationState.Mode = MenuNavigationState.SubpageMode.Settings;
                    MenuNavigationState.SubpageTitle = "SETTINGS";
                    MenuNavigationState.SubpageMessage = string.Empty;
                    SceneManager.LoadScene(GameScenes.MenuSubpage);
                    break;
                case "CREDITS":
                    OpenSubpage("CREDITS", "Credits — coming soon.");
                    break;
            }
        }

        private static void OpenSubpage(string title, string message)
        {
            MenuNavigationState.Mode = MenuNavigationState.SubpageMode.Message;
            MenuNavigationState.SubpageTitle = title;
            MenuNavigationState.SubpageMessage = message;
            SceneManager.LoadScene(F89.Core.GameScenes.MenuSubpage);
        }
    }
}
