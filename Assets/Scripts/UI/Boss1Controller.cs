using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>Boss dialogue page. Continue returns to the bunker for the selected fight intro.</summary>
    public sealed class Boss1Controller : MonoBehaviour
    {
        private Texture2D backgroundTexture;
        private int bossNumber;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
            bossNumber = GameScenes.GetBossNumber(SceneManager.GetActiveScene().name);
            if (bossNumber == 0)
            {
                bossNumber = LandBunkerHandoffState.PendingBossNumber;
            }

            bossNumber = Mathf.Clamp(bossNumber, LandBossEncounter.FirstBossNumber, LandBossEncounter.LastBossNumber);
            backgroundTexture = LandBossPortraitCatalog.LoadPortrait(bossNumber);
            if (backgroundTexture == null)
            {
                backgroundTexture = Resources.Load<Texture2D>("LandCombat/Boss1");
                Debug.LogWarning(
                    $"F-89: Boss {bossNumber} background missing from Resources/{LandBossPortraitCatalog.GetPortraitResourcePath(bossNumber)}; using Boss1 background.");
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
                ContinueToBunkerFight(bossNumber);
            }
        }

        private static void ContinueToBunkerFight(int bossNumber)
        {
            Time.timeScale = 1f;
            EnsureActiveSaveForDevJump();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave, forceReload: true);

            // From surface bunker pad: layout already queued. From Main Menu BOSS 1: pick a layout.
            if (LandBunkerHandoffState.HasPendingLayout)
            {
                LandBunkerHandoffState.EnsureBossFightPending(bossNumber);
            }
            else
            {
                // A Main Menu boss jump has no existing surface map to capture. Create one so
                // SURFACE returns next to an actual bunker entrance instead of the landed plane.
                if (!LandSurfaceSession.HasSnapshot)
                {
                    LandCombatConsumables.ResetForMission();
                    LandSurfaceSession.PrepareDirectBossReturn();
                }

                LandBunkerHandoffState.BeginBossFightEnter(layoutIndex: 1, bossNumber);
            }

            SceneManager.LoadScene(GameScenes.Bunker);
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
