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
            if (LandCombatTestCheats.ShowDevMenuButtons)
            {
                DrawTempFightReichButton();
                DrawDevResetMapButton();
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

        /// <summary>Temporary land-combat jump; remove when land entry is fully wired.</summary>
        private static void DrawDevResetMapButton()
        {
            var width = UiFitCanvas.Px(220f);
            var height = UiFitCanvas.Px(44f);
            var x = UiFitCanvas.Rect.x + UiFitCanvas.Px(18f);
            var y = UiFitCanvas.Rect.y + UiFitCanvas.Px(18f);
            var resetRect = new Rect(x, y, width, height);
            if (!StartPageMenuStyles.DrawMenuButton(resetRect, "RESET MAP", fontSize: 22))
            {
                return;
            }

            EnsureActiveSaveForDevJump();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave, forceReload: true);
            CampaignWorldReset.ResetMapForFreshPlay();
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
