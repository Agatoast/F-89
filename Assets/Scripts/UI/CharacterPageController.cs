using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class CharacterPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "CharacterPage/character_page_bg";
        private const string PaperdollResourcePath = "CharacterPage/paperdoll";
        private const string TopSecretFolderResourcePath = "CharacterPage/top_secret_folder";
        private const string PortraitFrameResourcePath = "CharacterPage/portrait_frame";
        private const string SaveAntarcticaLogoResourcePath = "CharacterPage/save_antarctica_logo";

        private Texture2D backgroundTexture;
        private Texture2D paperdollTexture;
        private Texture2D topSecretFolderTexture;
        private Texture2D portraitFrameTexture;
        private Texture2D saveAntarcticaLogoTexture;

        private void Start()
        {
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            paperdollTexture = Resources.Load<Texture2D>(PaperdollResourcePath);
            topSecretFolderTexture = Resources.Load<Texture2D>(TopSecretFolderResourcePath);
            portraitFrameTexture = Resources.Load<Texture2D>(PortraitFrameResourcePath);
            saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);

            if (backgroundTexture == null)
            {
                Debug.LogWarning("F-89: Character page background missing from Resources/CharacterPage/character_page_bg.");
            }

            if (topSecretFolderTexture == null)
            {
                Debug.LogWarning("F-89: Top secret folder graphic missing from Resources/CharacterPage/top_secret_folder.");
            }

            if (portraitFrameTexture == null)
            {
                Debug.LogWarning("F-89: Portrait frame missing from Resources/CharacterPage/portrait_frame.");
            }

            if (saveAntarcticaLogoTexture == null)
            {
                Debug.LogWarning("F-89: SAVE Antarctica logo missing from Resources/CharacterPage/save_antarctica_logo.");
            }

            var save = CharacterSessionState.ActiveSave;
            LandResearchTestSetup.ApplyCleanupOnceForSession(save);
            CharacterGearSession.Bind(save, forceReload: true);
        }

        private void OnGUI()
        {
            MilitaryAwardTooltipUi.BeginFrame();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            DrawFootlockerHeader();
            DrawResearchAndDevelopment();
            CharacterPageGearUi.HandleGearDragAndDrop(
                index => CharacterPageLayout.GetEquipmentSlotRect(index, 4),
                CharacterPageLayout.GetInventoryGridRect(),
                CharacterPageLayout.GetFootlockerGridRect(),
                footlockerTopAlign: false,
                getBasicRect: null,
                allowResearchDrop: true);
            CharacterPageGearUi.DrawEquipmentSlots();
            CharacterPageGearUi.DrawInventory(CharacterPageLayout.GetInventoryGridRect());
            DrawPaperdoll();
            DrawLeftColumn(save);
            CharacterPageGearUi.DrawFootlocker(CharacterPageLayout.GetFootlockerGridRect());
            DrawPortrait(save);
            DrawScoreRows(save);
            DrawKillFolderLabels();
            DrawSaveAntarcticaLogo();
            DrawCharacterLoadoutButton();
            DrawMissionBriefButton();
            CharacterPageGearUi.DrawDragOverlay();
            DrawResearchConfirmDialog();
            DrawResearchTechTooLowDialog();
            DrawResearchWrongCategoryDialog();
            MilitaryAwardTooltipUi.Draw();
        }

        private static void DrawResearchConfirmDialog()
        {
            if (!CharacterPageGearUi.IsResearchConfirmPending)
            {
                return;
            }

            var result = ResearchDestroyConfirmDialog.Draw(true);
            if (result == ResearchDestroyConfirmDialog.Result.Confirmed)
            {
                CharacterPageGearUi.ConfirmResearchDestroy();
            }
            else if (result == ResearchDestroyConfirmDialog.Result.Cancelled)
            {
                CharacterPageGearUi.CancelResearchConfirm();
            }
        }

        private static void DrawResearchTechTooLowDialog()
        {
            if (!CharacterPageGearUi.IsResearchTechTooLowPending)
            {
                return;
            }

            if (ResearchTechTooLowDialog.Draw(true) == ResearchTechTooLowDialog.Result.Acknowledged)
            {
                CharacterPageGearUi.AcknowledgeResearchTechTooLow();
            }
        }

        private static void DrawResearchWrongCategoryDialog()
        {
            if (!CharacterPageGearUi.IsResearchWrongCategoryPending)
            {
                return;
            }

            if (ResearchWrongCategoryDialog.Draw(true) == ResearchWrongCategoryDialog.Result.Acknowledged)
            {
                CharacterPageGearUi.AcknowledgeResearchWrongCategory();
            }
        }

        private static void DrawCharacterLoadoutButton()
        {
            var rect = CharacterPageLayout.GetCharacterLoadoutButtonRect();
            if (!StartPageMenuStyles.DrawMenuButton(rect, "CHARACTER LOADOUT"))
            {
                return;
            }

            CharacterGearSession.PersistActive();
            CharacterLoadoutNavState.MarkEnteredFromCharacterPage();
            SceneManager.LoadScene(GameScenes.CharacterLoadout);
        }

        private static void DrawMissionBriefButton()
        {
            var rect = CharacterPageLayout.GetMissionBriefButtonRect();
            if (!StartPageMenuStyles.DrawMenuButton(rect, "MISSION BRIEF"))
            {
                return;
            }

            CharacterGearSession.PersistActive();
            MissionBriefingState.PrepareNextMission(CharacterSessionState.ActiveSave);
            SceneManager.LoadScene(GameScenes.MissionBriefing);
        }

        private static void DrawKillFolderLabels()
        {
            DrawKillFolderLabel(
                CharacterPageLayout.GetVehicleKillsLabelRect(),
                "Enemy Vehicles Destroyed");
            DrawKillFolderLabel(
                CharacterPageLayout.GetTroopKillsLabelRect(),
                "Enemy Troops Killed");
        }

        private static void DrawFootlockerHeader()
        {
            GUI.Label(CharacterPageLayout.GetFootlockerTitleRect(), "FOOTLOCKER", CharacterPageStyles.FootlockerTitleStyle);
        }

        private static void DrawResearchAndDevelopment()
        {
            GUI.Label(
                CharacterPageLayout.GetResearchAndDevelopmentTitleRect(),
                "R&D",
                CharacterPageStyles.FootlockerTitleStyle);

            var box = CharacterPageLayout.GetResearchAndDevelopmentRect();
            const float border = 2f;
            GUI.color = new Color(0.12f, 0.2f, 0.3f, 0.88f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.78f, 0.95f, 0.95f);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.yMax - border, box.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y, border, box.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.xMax - border, box.y, border, box.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(
                CharacterPageLayout.GetResearchAndDevelopmentHintRect(),
                "Drop items here to research next Tech Level.",
                CharacterPageStyles.ResearchDropHintStyle);
            CharacterPageGearUi.DrawResearchAndDevelopmentSlots();
            GUI.Label(
                CharacterPageLayout.GetResearchAndDevelopmentFooterRect(),
                "Same or higher tech level UR items are destroyed and add 0.5% to R&D chance to create a new item after next mission. Maximum 30%",
                CharacterPageStyles.ResearchFooterStyle);
        }

        private void DrawLeftColumn(CharacterSaveData save)
        {
            DrawNameBar(save);
            DrawRibbonsPanel(save);
            DrawKillFolder(CharacterPageLayout.GetVehicleKillsRect());
            DrawKillFolder(CharacterPageLayout.GetTroopKillsRect());
        }

        private static void DrawKillFolderLabel(Rect rect, string label)
        {
            GUI.Label(rect, label, CharacterPageStyles.KillFolderLabelStyle);
        }

        private static void DrawNameBar(CharacterSaveData save)
        {
            var rect = CharacterPageLayout.GetNameBarRect();
            var label = save != null ? save.DisplayRankAndName : "NO CHARACTER";
            GUI.Label(rect, label, CharacterPageStyles.NameBarStyle);
        }

        private static void DrawRibbonsPanel(CharacterSaveData save)
        {
            var rect = CharacterPageLayout.GetRibbonsRect();
            var earnedRibbonIds = save?.EarnedRibbonIds;
            if (earnedRibbonIds == null || earnedRibbonIds.Length == 0)
            {
                return;
            }

            CharacterPageRibbonUi.DrawRibbons(rect, earnedRibbonIds);
        }

        private static void DrawScoreRows(CharacterSaveData save)
        {
            var onesColumnX = CharacterPageLayout.GetMissionScoreOnesColumnX();
            var labelWidth = CharacterPageLayout.MissionScoreLabelColumnWidthPx;
            var preview = SelectionPageLayout.DossierStatLayoutPreviewValue;
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetScoreRowRect(0),
                "Number of Enemy Vehicles Destroyed",
                preview > 0 ? preview : save?.EnemyVehiclesKilled ?? 0,
                labelWidth,
                onesColumnX);
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetScoreRowRect(1),
                "Number of Enemy Troops Killed",
                preview > 0 ? preview : save?.EnemyTroopsKilled ?? 0,
                labelWidth,
                onesColumnX);
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetScoreRowRect(2),
                "Best Mission Score",
                preview > 0 ? preview : save?.BestMissionScore ?? 0,
                labelWidth,
                onesColumnX);
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetScoreRowRect(3),
                "Total Score",
                preview > 0 ? preview : save?.TotalScore ?? 0,
                labelWidth,
                onesColumnX);
        }

        private void DrawKillFolder(Rect rect)
        {
            if (topSecretFolderTexture == null)
            {
                return;
            }

            GUI.DrawTexture(rect, topSecretFolderTexture, ScaleMode.ScaleToFit, true);
            DrawKillFolderSlotGrid(rect);
        }

        private static GUIStyle killFolderPlaceholderStyle;

        private static void DrawKillFolderSlotGrid(Rect folderRect)
        {
            EnsureKillFolderPlaceholderStyle();

            for (var i = 0; i < CharacterPageLayout.KillFolderSlotCount; i++)
            {
                var slotRect = CharacterPageLayout.GetKillFolderSlotRect(folderRect, i);
                GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
                GUI.DrawTexture(slotRect, Texture2D.whiteTexture);

                DrawKillFolderSlotBorder(slotRect, 1f, new Color(0.95f, 0.95f, 0.95f, 0.95f));

                GUI.color = Color.white;
                GUI.Label(slotRect, (i + 1).ToString(), killFolderPlaceholderStyle);
            }

            GUI.color = Color.white;
        }

        private static void EnsureKillFolderPlaceholderStyle()
        {
            if (killFolderPlaceholderStyle != null)
            {
                return;
            }

            killFolderPlaceholderStyle = HudStyleFactory.CreateLabel(
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
        }

        private static void DrawKillFolderSlotBorder(Rect rect, float thickness, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
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
                CharacterPageLayout.GetPortraitRect(),
                CharacterPortraitService.GetPortraitTexture(save),
                portraitFrameTexture);
        }

        private void DrawSaveAntarcticaLogo()
        {
            if (saveAntarcticaLogoTexture == null)
            {
                saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);
                if (saveAntarcticaLogoTexture == null)
                {
                    return;
                }
            }

            var rect = CharacterPageLayout.GetSaveAntarcticaLogoRect(saveAntarcticaLogoTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, saveAntarcticaLogoTexture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        private void DrawPaperdoll()
        {
            if (paperdollTexture == null)
            {
                return;
            }

            var rect = CharacterPageLayout.GetPaperdollRect();
            GUI.DrawTexture(rect, paperdollTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawPageBackground()
        {
            UiFitCanvas.DrawLetterboxedBackground(backgroundTexture);
        }
    }
}
