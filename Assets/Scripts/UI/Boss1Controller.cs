using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>Boss 1 dialogue page. Continue returns to the bunker for the fight intro.</summary>
    public sealed class Boss1Controller : MonoBehaviour
    {
        private const string BackgroundResourcePath = "LandCombat/Boss1";

        private Texture2D backgroundTexture;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            if (backgroundTexture == null)
            {
                Debug.LogWarning($"F-89: Boss1 background missing from Resources/{BackgroundResourcePath}.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoToMainMenu();
            }
        }

        private void OnGUI()
        {
            StartPageMenuStyles.DrawFullscreenBackground(backgroundTexture);

            var buttonWidth = UiFitCanvas.Px(280f);
            var buttonHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - UiFitCanvas.Px(40f),
                buttonWidth,
                buttonHeight);

            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 28))
            {
                ContinueToBunkerFight();
            }
        }

        private static void ContinueToBunkerFight()
        {
            Time.timeScale = 1f;
            EnsureActiveSaveForDevJump();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave, forceReload: true);

            // From surface bunker pad: layout already queued. From Main Menu BOSS 1: pick a layout.
            if (LandBunkerHandoffState.HasPendingLayout)
            {
                LandBunkerHandoffState.EnsureBossFightPending();
            }
            else
            {
                LandBunkerHandoffState.BeginBossFightEnter(layoutIndex: 1);
            }

            SceneManager.LoadScene(GameScenes.Bunker);
        }

        private static void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
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
    }
}
