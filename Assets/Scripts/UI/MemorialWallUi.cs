using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Scrollable memorial listing KIA characters from the pause menu.
    /// Each entry uses 45 text lines: 1 blank top, 1 blank bottom, 3 blank middle, rest for info.
    /// </summary>
    public static class MemorialWallUi
    {
        public const int LinesPerEntry = 45;
        public const int BlankLinesTop = 1;
        public const int BlankLinesBottom = 1;
        public const int BlankLinesMiddle = 3;

        private const float PanelWidth = 760f;
        private const float PanelHeight = 620f;
        private const float LineHeight = 15f;
        private const float BackButtonWidth = 200f;
        private const float BackButtonHeight = 40f;
        private const float RemoveButtonWidth = 110f;
        private const float RemoveButtonHeight = 32f;

        private static Vector2 scrollPosition;
        private static string pendingRemoveSaveId;
        private static GUIStyle titleStyle;
        private static GUIStyle nameStyle;
        private static GUIStyle bodyStyle;
        private static GUIStyle statStyle;

        public static void Draw(System.Action onClose)
        {
            EnsureStyles();

            var panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - PanelHeight) * 0.5f,
                PanelWidth,
                PanelHeight);

            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(panelRect, 2f);

            GUI.Label(
                new Rect(panelRect.x + 16f, panelRect.y + 14f, panelRect.width - 32f, 34f),
                "MEMORIAL WALL",
                titleStyle);

            var listRect = new Rect(panelRect.x + 20f, panelRect.y + 56f, panelRect.width - 40f, panelRect.height - 120f);
            var kiaSaves = CollectKiaSaves();
            var entryHeight = LinesPerEntry * LineHeight;
            var contentHeight = Mathf.Max(listRect.height, kiaSaves.Count * entryHeight);

            scrollPosition = GUI.BeginScrollView(
                listRect,
                scrollPosition,
                new Rect(0f, 0f, listRect.width - 18f, contentHeight));

            if (kiaSaves.Count == 0)
            {
                GUI.Label(
                    new Rect(0f, LineHeight * 2f, listRect.width, LineHeight * 2f),
                    "No characters killed in action.",
                    bodyStyle);
            }
            else
            {
                for (var i = 0; i < kiaSaves.Count; i++)
                {
                    DrawEntry(new Rect(0f, i * entryHeight, listRect.width - 24f, entryHeight), kiaSaves[i]);
                }
            }

            GUI.EndScrollView();

            var backRect = new Rect(
                panelRect.x + (panelRect.width - BackButtonWidth) * 0.5f,
                panelRect.yMax - BackButtonHeight - 16f,
                BackButtonWidth,
                BackButtonHeight);
            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: 16))
            {
                pendingRemoveSaveId = null;
                onClose?.Invoke();
            }

            DrawRemoveConfirmationDialog();
        }

        private static List<CharacterSaveData> CollectKiaSaves()
        {
            var results = new List<CharacterSaveData>();
            foreach (var save in CharacterSaveRepository.Saves)
            {
                if (save != null && save.IsKilledInAction)
                {
                    results.Add(save);
                }
            }

            results.Sort((a, b) => string.CompareOrdinal(b.KilledInActionUtc, a.KilledInActionUtc));
            return results;
        }

        private static void DrawEntry(Rect entryRect, CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureUrKillArrays(save);

            var topSectionLines = (LinesPerEntry - BlankLinesTop - BlankLinesBottom - BlankLinesMiddle) / 2;
            var bottomSectionStartLine = BlankLinesTop + topSectionLines + BlankLinesMiddle;
            var y = entryRect.y + BlankLinesTop * LineHeight;

            GUI.color = new Color(0f, 0f, 0f, 0.08f);
            GUI.DrawTexture(entryRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var portraitSize = topSectionLines * LineHeight - LineHeight;
            var portraitRect = new Rect(entryRect.x + 8f, y, portraitSize * 0.72f, portraitSize);
            var portrait = CharacterPortraitService.GetPortraitTexture(save);
            PortraitDisplayUtility.DrawMetallicFramedPortrait(portraitRect, portrait, null);

            var textX = portraitRect.xMax + 12f;
            var textWidth = entryRect.xMax - textX - 8f;
            GUI.Label(new Rect(textX, y, textWidth, LineHeight * 2f), save.DisplayRankAndName, nameStyle);

            var dateLabel = FormatKiaDate(save.KilledInActionUtc);
            GUI.Label(
                new Rect(textX, y + LineHeight * 2f, textWidth, LineHeight),
                dateLabel,
                bodyStyle);

            var medalTexture = MilitaryMedalService.GetMedalTexture(save.HighestAward);
            if (medalTexture != null)
            {
                var medalSize = LineHeight * 4f;
                GUI.DrawTexture(
                    new Rect(textX, y + LineHeight * 3.2f, medalSize, medalSize),
                    medalTexture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            var ribbonRect = new Rect(
                entryRect.x + 8f,
                entryRect.y + bottomSectionStartLine * LineHeight,
                entryRect.width - 16f,
                topSectionLines * LineHeight * 0.42f);
            CharacterPageRibbonUi.DrawRibbons(ribbonRect, save.EarnedRibbonIds, save);

            var statsY = ribbonRect.yMax + LineHeight * 0.5f;
            GUI.Label(
                new Rect(entryRect.x + 8f, statsY, entryRect.width * 0.48f, LineHeight),
                $"Vehicles destroyed: {save.EnemyVehiclesKilled:N0}",
                statStyle);
            GUI.Label(
                new Rect(entryRect.x + entryRect.width * 0.5f, statsY, entryRect.width * 0.48f, LineHeight),
                $"Troops killed: {save.EnemyTroopsKilled:N0}",
                statStyle);

            statsY += LineHeight * 1.2f;
            GUI.Label(
                new Rect(entryRect.x + 8f, statsY, entryRect.width * 0.48f, LineHeight),
                $"Best mission score: {save.BestMissionScore:N0}",
                statStyle);
            GUI.Label(
                new Rect(entryRect.x + entryRect.width * 0.5f, statsY, entryRect.width * 0.48f, LineHeight),
                $"Total score: {save.TotalScore:N0}",
                statStyle);

            var folderHeight = topSectionLines * LineHeight * 0.38f;
            var folderWidth = (entryRect.width - 24f) * 0.5f;
            var vehicleFolderRect = new Rect(entryRect.x + 8f, statsY + LineHeight * 1.8f, folderWidth, folderHeight);
            var troopFolderRect = new Rect(vehicleFolderRect.xMax + 8f, vehicleFolderRect.y, folderWidth, folderHeight);
            DrawMiniKillFolder(vehicleFolderRect, save, troopsFolder: false);
            DrawMiniKillFolder(troopFolderRect, save, troopsFolder: true);

            var removeRect = new Rect(
                entryRect.xMax - RemoveButtonWidth - 8f,
                entryRect.yMax - RemoveButtonHeight - 8f,
                RemoveButtonWidth,
                RemoveButtonHeight);
            if (StartPageMenuStyles.DrawMenuButton(removeRect, "REMOVE", fontSize: 14))
            {
                pendingRemoveSaveId = save.Id;
            }
        }

        private static void DrawRemoveConfirmationDialog()
        {
            if (string.IsNullOrEmpty(pendingRemoveSaveId))
            {
                return;
            }

            CharacterSaveData pendingSave = null;
            foreach (var save in CharacterSaveRepository.Saves)
            {
                if (save != null && save.Id == pendingRemoveSaveId)
                {
                    pendingSave = save;
                    break;
                }
            }

            var result = DeleteCharacterDialog.Draw(
                true,
                pendingSave != null ? pendingSave.DisplayRankAndName : string.Empty);

            if (result == DeleteCharacterDialog.Result.Confirmed)
            {
                CharacterSaveRepository.DeleteSave(pendingRemoveSaveId);
                pendingRemoveSaveId = null;
            }
            else if (result == DeleteCharacterDialog.Result.Back)
            {
                pendingRemoveSaveId = null;
            }
        }

        private static void DrawMiniKillFolder(Rect folderRect, CharacterSaveData save, bool troopsFolder)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.35f);
            GUI.DrawTexture(folderRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            CharacterPageKillFolderUi.DrawFolder(folderRect, save, troopsFolder);
        }

        private static string FormatKiaDate(string killedInActionUtc)
        {
            if (string.IsNullOrEmpty(killedInActionUtc)
                || !System.DateTime.TryParse(killedInActionUtc, out var killed))
            {
                return "Killed in action";
            }

            return $"Killed in action — {killed.ToLocalTime():g}";
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(22, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            nameStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.75f, 0.12f, 0.08f));
            bodyStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.black);
            statStyle = HudStyleFactory.CreateLabel(13, FontStyle.Normal, TextAnchor.UpperLeft, Color.black);
        }
    }
}
