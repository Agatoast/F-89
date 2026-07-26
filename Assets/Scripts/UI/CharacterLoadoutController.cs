using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class CharacterLoadoutController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "CharacterLoadout/character_loadout_bg";
        private const string PaperdollResourcePath = "CharacterPage/paperdoll";
        private const string PortraitFrameResourcePath = "CharacterPage/portrait_frame";
        private const float Margin = 20f;
        private const float ButtonWidth = 190f;
        private const float ButtonHeight = 44f;

        private Texture2D backgroundTexture;
        private Texture2D paperdollTexture;
        private Texture2D portraitFrameTexture;
        private bool showBailOutConfirm;

        private void Start()
        {
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            paperdollTexture = Resources.Load<Texture2D>(PaperdollResourcePath);
            portraitFrameTexture = Resources.Load<Texture2D>(PortraitFrameResourcePath);
            CharacterPageGearUi.ResetDragState();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave, forceReload: true);

            if (backgroundTexture == null)
            {
                Debug.LogWarning(
                    "F-89: Character loadout background missing from Resources/CharacterLoadout/character_loadout_bg.");
            }

            if (paperdollTexture == null)
            {
                Debug.LogWarning("F-89: Paperdoll missing from Resources/CharacterPage/paperdoll.");
            }

            if (portraitFrameTexture == null)
            {
                Debug.LogWarning("F-89: Portrait frame missing from Resources/CharacterPage/portrait_frame.");
            }
        }

        private void OnGUI()
        {
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            DrawNameBar(save);
            DrawPortrait(save);
            GUI.Label(
                CharacterLoadoutLayout.GetBasicLoadoutTitleRect(),
                "BASIC LOADOUT",
                CharacterPageStyles.FootlockerTitleStyle);

            GUI.Label(
                CharacterLoadoutLayout.GetFootlockerTitleRect(),
                "FOOTLOCKER",
                CharacterPageStyles.FootlockerTitleStyle);
            CharacterPageGearUi.HandleGearDragAndDrop(
                CharacterLoadoutLayout.GetEquipmentSlotRect,
                CharacterLoadoutLayout.GetInventoryGridRect(),
                CharacterLoadoutLayout.GetFootlockerGridRect(),
                footlockerTopAlign: true,
                CharacterLoadoutLayout.GetLoadoutBoxRect);
            CharacterPageGearUi.DrawBasicLoadoutBoxes(CharacterLoadoutLayout.GetLoadoutBoxRect);
            CharacterPageGearUi.DrawEquipmentSlots(CharacterLoadoutLayout.GetEquipmentSlotRect);
            CharacterPageGearUi.DrawInventory(CharacterLoadoutLayout.GetInventoryGridRect());
            DrawPaperdoll();
            CharacterPageGearUi.DrawFootlocker(CharacterLoadoutLayout.GetFootlockerGridRect(), topAlign: true);
            CharacterPageGearUi.DrawDragOverlay();

            DrawActionButtons();
            if (showBailOutConfirm && CharacterLoadoutNavState.EnteredFromMissionBrief)
            {
                var dialogResult = BailOutConfirmDialog.Draw(true);
                if (dialogResult == BailOutConfirmDialog.Result.Confirmed)
                {
                    ConfirmBailOut();
                }
                else if (dialogResult == BailOutConfirmDialog.Result.Cancelled)
                {
                    showBailOutConfirm = false;
                }
            }
        }

        private static void DrawNameBar(CharacterSaveData save)
        {
            var rect = CharacterLoadoutLayout.GetNameBarRect();
            var label = save != null ? save.DisplayRankAndName : "NO CHARACTER";
            GUI.Label(rect, label, CharacterPageStyles.NameBarStyle);
        }

        private void DrawPortrait(CharacterSaveData save)
        {
            if (save != null)
            {
                save = CharacterSaveRepository.FindById(save.Id) ?? save;
            }

            if (portraitFrameTexture == null)
            {
                portraitFrameTexture = Resources.Load<Texture2D>(PortraitFrameResourcePath);
            }

            PortraitDisplayUtility.DrawMetallicFramedPortrait(
                CharacterLoadoutLayout.GetPortraitRect(),
                CharacterPortraitService.GetPortraitTexture(save),
                portraitFrameTexture);
        }

        private void DrawPaperdoll()
        {
            if (paperdollTexture == null)
            {
                paperdollTexture = Resources.Load<Texture2D>(PaperdollResourcePath);
                if (paperdollTexture == null)
                {
                    return;
                }
            }

            var rect = CharacterLoadoutLayout.GetPaperdollRect();
            GUI.DrawTexture(rect, paperdollTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawActionButtons()
        {
            const float gap = 16f;
            var totalWidth = ButtonWidth * 2f + gap;
            var startX = Screen.width - totalWidth - Margin;
            var y = Screen.height - ButtonHeight - Margin;

            var bailRect = new Rect(startX, y, ButtonWidth, ButtonHeight);
            var aircraftRect = new Rect(startX + ButtonWidth + gap, y, ButtonWidth, ButtonHeight);

            if (CharacterLoadoutNavState.EnteredFromMissionBrief)
            {
                if (StartPageMenuStyles.DrawMenuButton(bailRect, "BAIL OUT?", fontSize: 15))
                {
                    showBailOutConfirm = true;
                }
            }
            else if (StartPageMenuStyles.DrawMenuButton(bailRect, "CHARACTER PAGE", fontSize: 15))
            {
                ReturnToCharacterPage();
            }

            if (StartPageMenuStyles.DrawMenuButton(aircraftRect, "AIRCRAFT LOADOUT", fontSize: 15)
                && !showBailOutConfirm)
            {
                ProceedToAircraftLoadout();
            }
        }

        private static void ProceedToAircraftLoadout()
        {
            CharacterGearSession.PersistActive();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }

        private static void ReturnToCharacterPage()
        {
            CharacterGearSession.PersistActive();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        private static void ConfirmBailOut()
        {
            CharacterGearSession.PersistActive();
            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        private void DrawPageBackground()
        {
            if (backgroundTexture != null)
            {
                UiFitCanvas.DrawLetterboxedBackground(backgroundTexture);
                return;
            }

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
