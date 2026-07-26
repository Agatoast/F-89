using F89.Core;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public class CharacterPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "CharacterPage/character_page_bg";
        private const string PaperdollResourcePath = "CharacterPage/paperdoll";
        private const string TopSecretFolderResourcePath = "CharacterPage/top_secret_folder";

        private const string SaveAntarcticaLogoResourcePath = "CharacterPage/save_antarctica_logo";

        private Texture2D backgroundTexture;
        private Texture2D paperdollTexture;
        private Texture2D topSecretFolderTexture;
        private Texture2D saveAntarcticaLogoTexture;

        private void Start()
        {
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            paperdollTexture = Resources.Load<Texture2D>(PaperdollResourcePath);
            topSecretFolderTexture = Resources.Load<Texture2D>(TopSecretFolderResourcePath);
            saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);

            if (backgroundTexture == null)
            {
                Debug.LogWarning("F-89: Character page background missing from Resources/CharacterPage/character_page_bg.");
            }

            if (topSecretFolderTexture == null)
            {
                Debug.LogWarning("F-89: Top secret folder graphic missing from Resources/CharacterPage/top_secret_folder.");
            }

            if (saveAntarcticaLogoTexture == null)
            {
                Debug.LogWarning("F-89: SAVE Antarctica logo missing from Resources/CharacterPage/save_antarctica_logo.");
            }
        }

        private void OnGUI()
        {
            MilitaryAwardTooltipUi.BeginFrame();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            DrawFootlockerHeader();
            CharacterPageGearUi.DrawEquipmentSlots();
            CharacterPageGearUi.DrawInventory(CharacterPageLayout.GetInventoryGridRect());
            DrawPaperdoll();
            DrawLeftColumn(save);
            CharacterPageGearUi.DrawFootlocker(CharacterPageLayout.GetFootlockerGridRect());
            DrawPortrait(save);
            DrawScoreRows(save);
            DrawKillFolderLabels();
            DrawSaveAntarcticaLogo();
            MilitaryAwardTooltipUi.Draw();
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
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetTotalScoreLabelRect(),
                "Total Mission Score:",
                save?.TotalScore ?? 0,
                CharacterPageLayout.MissionScoreLabelColumnWidthPx,
                onesColumnX);
            MissionScoreDisplayUi.DrawCharacterPageRow(
                CharacterPageLayout.GetBestScoreLabelRect(),
                "Best Mission Score:",
                save?.BestMissionScore ?? 0,
                CharacterPageLayout.MissionScoreLabelColumnWidthPx,
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

        private static void DrawPortrait(CharacterSaveData save)
        {
            if (save != null)
            {
                save = CharacterSaveRepository.FindById(save.Id) ?? save;
            }

            var frameRect = CharacterPageLayout.GetPortraitRect();
            var inset = Mathf.Min(frameRect.width, frameRect.height) * 0.11f;
            var portraitRect = new Rect(
                frameRect.x + inset,
                frameRect.y + inset,
                frameRect.width - inset * 2f,
                frameRect.height - inset * 2f);

            var portraitTexture = CharacterPortraitService.GetPortraitTexture(save);
            GUI.color = Color.white;
            PortraitDisplayUtility.DrawPortraitFrame(portraitRect, portraitTexture, null, null);
            GUI.color = Color.white;
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
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (backgroundTexture == null)
            {
                return;
            }

            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                backgroundTexture,
                ScaleMode.StretchToFill,
                true);
        }
    }
}
