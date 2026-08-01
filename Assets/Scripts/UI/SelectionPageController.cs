using System.Collections.Generic;
using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class SelectionPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "SelectionPage/selection_page";
        private const string PortraitFrameResourcePath = "CharacterPage/portrait_frame";
        private const string DefaultCharacterRank = "2nd LT";

        private readonly List<CharacterSaveData> visibleSaves = new List<CharacterSaveData>();

        private Texture2D backgroundTexture;
        private Texture2D portraitFrameTexture;
        private Vector2 plaqueScrollPosition;
        private string selectedSaveId;
        private bool hasInitializedSelection;
        private bool showNewCharacterDialog;
        private bool showDeleteCharacterDialog;
        private bool showSelectPortraitDialog;
        private bool pendingPortraitBrowse;
        private string pendingCharacterName = string.Empty;
        private bool focusNameFieldPending;
        private bool pendingScrollToSelected;

        private void Start()
        {
            CharacterPortraitService.ClearPresetCache();
            MilitaryMedalService.ClearCache();
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            portraitFrameTexture = Resources.Load<Texture2D>(PortraitFrameResourcePath);

            if (backgroundTexture == null)
            {
                Debug.LogWarning("F-89: Selection page background missing from Resources/SelectionPage/selection_page.");
            }

            if (portraitFrameTexture == null)
            {
                Debug.LogWarning("F-89: Portrait frame missing from Resources/CharacterPage/portrait_frame.");
            }
        }

        private void OnEnable()
        {
            hasInitializedSelection = false;
            plaqueScrollPosition = Vector2.zero;
            pendingScrollToSelected = false;
            RefreshVisibleSaves();
        }

        private void Update()
        {
            if (!pendingPortraitBrowse)
            {
                return;
            }

            pendingPortraitBrowse = false;
            if (!PortraitFilePicker.TryPickPortraitFile(out var sourcePath))
            {
                return;
            }

            var save = FindSelectedSave();
            if (save != null)
            {
                CharacterPortraitService.TryImportCustomPortrait(save, sourcePath);
            }
        }

        private void OnGUI()
        {
            MilitaryAwardTooltipUi.BeginFrame();
            DrawBackground();
            SelectionPageStyles.DrawSectionHeaders();
            SelectionPageStyles.DrawCharacterListBackdrop();
            SelectionPageStyles.DrawDossierPanelChrome();
            SelectionPageStyles.DrawDossierStaticLabels();

            if (showNewCharacterDialog)
            {
                DrawNewCharacterDialog();
                return;
            }

            if (showDeleteCharacterDialog)
            {
                DrawDeleteCharacterDialog();
                return;
            }

            if (showSelectPortraitDialog)
            {
                DrawSelectPortraitDialog();
                return;
            }

            DrawCharacterList();
            DrawCharacterDossier();
            DrawNewCharacterButton();
            DrawDeleteButton();
            DrawSelectButton();
            MilitaryAwardTooltipUi.Draw();
        }

        private void DrawBackground()
        {
            UiFitCanvas.DrawLetterboxedBackground(backgroundTexture);
        }

        private void DrawCharacterList()
        {
            if (visibleSaves.Count == 0)
            {
                return;
            }

            if (pendingScrollToSelected)
            {
                EnsureSelectedPlaqueVisible();
                pendingScrollToSelected = false;
            }

            var needsScroll = visibleSaves.Count > SelectionPageLayout.VisiblePlaqueCount;
            var viewRect = SelectionPageLayout.GetCharacterListViewRect(needsScroll);
            var rowHeight = SelectionPageLayout.GetPlaqueRowHeightPx();
            var contentHeight = visibleSaves.Count * rowHeight;
            var maxScroll = Mathf.Max(0f, contentHeight - viewRect.height);

            if (needsScroll)
            {
                var scrollbarRect = SelectionPageLayout.GetCharacterListScrollbarRect();
                var previousScrollbar = GUI.skin.verticalScrollbar;
                var previousThumb = GUI.skin.verticalScrollbarThumb;
                GUI.skin.verticalScrollbar = SelectionPageStyles.GetPlaqueVerticalScrollbar();
                GUI.skin.verticalScrollbarThumb = SelectionPageStyles.GetPlaqueVerticalScrollbarThumb();
                plaqueScrollPosition.y = GUI.VerticalScrollbar(
                    scrollbarRect,
                    plaqueScrollPosition.y,
                    viewRect.height,
                    0f,
                    contentHeight);
                GUI.skin.verticalScrollbar = previousScrollbar;
                GUI.skin.verticalScrollbarThumb = previousThumb;
            }
            else
            {
                plaqueScrollPosition.y = 0f;
            }

            plaqueScrollPosition.y = Mathf.Clamp(plaqueScrollPosition.y, 0f, maxScroll);
            HandlePlaqueScrollWheel(viewRect, maxScroll);

            GUI.BeginGroup(viewRect);
            for (var i = 0; i < visibleSaves.Count; i++)
            {
                var slotY = i * rowHeight - plaqueScrollPosition.y;
                if (slotY + rowHeight < 0f || slotY > viewRect.height)
                {
                    continue;
                }

                var slotRect = new Rect(0f, slotY, viewRect.width, rowHeight);
                DrawCharacterListRow(
                    visibleSaves[i],
                    SelectionPageLayout.GetCompactPlaqueRectLocal(slotRect),
                    needsScroll);
            }

            GUI.EndGroup();
        }

        private void HandlePlaqueScrollWheel(Rect viewRect, float maxScroll)
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.ScrollWheel || !viewRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            plaqueScrollPosition.y = Mathf.Clamp(
                plaqueScrollPosition.y + currentEvent.delta.y * SelectionPageLayout.GetPlaqueRowHeightPx(),
                0f,
                maxScroll);
            currentEvent.Use();
        }

        private void DrawCharacterListRow(CharacterSaveData save, Rect rowRect, bool includeScrollbar)
        {
            var isSelected = save.Id == selectedSaveId;
            var labelRect = SelectionPageLayout.GetPlaqueLabelRect(rowRect, includeScrollbar);

            SelectionPageStyles.DrawPlaqueSelection(rowRect, labelRect, save.DisplayRankAndName, isSelected);
            SelectionPageStyles.DrawPlaqueLabel(labelRect, save.DisplayRankAndName);

            if (SelectionPageStyles.DrawInvisibleButton(rowRect))
            {
                SelectSave(save.Id);
            }
        }

        private void DrawCharacterDossier()
        {
            if (portraitFrameTexture == null)
            {
                portraitFrameTexture = Resources.Load<Texture2D>(PortraitFrameResourcePath);
            }

            var save = FindSelectedSave();
            var pictureRect = SelectionPageLayout.GetDossierPictureRect();
            var portraitTexture = save != null ? CharacterPortraitService.GetPortraitTexture(save) : null;
            SelectionPageStyles.DrawDossierPortrait(pictureRect, portraitTexture, portraitFrameTexture);
            if (save != null && !save.IsKilledInAction && !save.IsCourtMartialed && SelectionPageStyles.DrawInvisibleButton(pictureRect))
            {
                OpenSelectPortraitDialog();
            }

            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureUrKillArrays(save);
            DrawDossierHighestAward(save);
            SelectionPageStyles.DrawDossierName(
                SelectionPageLayout.GetDossierNameRect(),
                save.DisplayRankAndName);

            var preview = SelectionPageLayout.DossierStatLayoutPreviewValue;
            SelectionPageStyles.DrawDossierStats(
                preview > 0 ? preview : save.EnemyVehiclesKilled,
                preview > 0 ? preview : save.EnemyTroopsKilled,
                preview > 0 ? preview : save.BestMissionScore,
                preview > 0 ? preview : save.TotalScore);
        }

        private void DrawSelectPortraitDialog()
        {
            var result = SelectPortraitDialog.Draw(showSelectPortraitDialog, out var selectedPortraitId);
            if (result == SelectPortraitDialog.Result.Selected)
            {
                var save = FindSelectedSave();
                if (save != null && !string.IsNullOrEmpty(selectedPortraitId))
                {
                    CharacterPortraitService.SetPortrait(save, selectedPortraitId);
                }

                showSelectPortraitDialog = false;
            }
            else if (result == SelectPortraitDialog.Result.Browse)
            {
                showSelectPortraitDialog = false;
                pendingPortraitBrowse = true;
            }
            else if (result == SelectPortraitDialog.Result.Cancel)
            {
                showSelectPortraitDialog = false;
            }
        }

        private void OpenSelectPortraitDialog()
        {
            if (FindSelectedSave() == null)
            {
                return;
            }

            showSelectPortraitDialog = true;
        }

        private void DrawNewCharacterButton()
        {
            var rect = SelectionPageLayout.GetNewCharacterButtonRect();
            if (StartPageMenuStyles.DrawMenuButton(rect, "NEW CHARACTER"))
            {
                OpenNewCharacterDialog();
            }
        }

        private void DrawDeleteButton()
        {
            if (FindSelectedSave() == null)
            {
                return;
            }

            var rect = SelectionPageLayout.GetDeleteButtonRect();
            if (StartPageMenuStyles.DrawMenuButton(rect, "DELETE"))
            {
                OpenDeleteCharacterDialog();
            }
        }

        private void DrawSelectButton()
        {
            var save = FindSelectedSave();
            if (save == null)
            {
                return;
            }

            var rect = SelectionPageLayout.GetSelectButtonRect();
            var label = save.IsKilledInAction || save.IsCourtMartialed ? "VIEW CHARACTER" : "SELECT CHARACTER";
            if (StartPageMenuStyles.DrawMenuButton(rect, label, panelAlpha: 1f))
            {
                OpenSelectedCharacterPage();
            }
        }

        private void OpenSelectedCharacterPage()
        {
            var save = FindSelectedSave();
            if (save == null)
            {
                return;
            }

            CharacterSessionState.ActiveSave = save;
            CharacterGearSession.Bind(save, forceReload: true);
            CharacterSaveRepository.SetLastSelectedSaveId(save.Id);
            if (!save.IsKilledInAction && !save.IsCourtMartialed)
            {
                CharacterSaveRepository.TouchLastPlayed(save);
            }

            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        private void DrawNewCharacterDialog()
        {
            var result = NewCharacterDialog.Draw(
                true,
                ref pendingCharacterName,
                ref focusNameFieldPending);

            if (result == NewCharacterDialog.Result.Accepted)
            {
                AcceptNewCharacter();
            }
            else if (result == NewCharacterDialog.Result.Back)
            {
                showNewCharacterDialog = false;
                pendingCharacterName = string.Empty;
            }
        }

        private void OpenNewCharacterDialog()
        {
            showNewCharacterDialog = true;
            pendingCharacterName = string.Empty;
            focusNameFieldPending = true;
        }

        private void AcceptNewCharacter()
        {
            if (string.IsNullOrWhiteSpace(pendingCharacterName))
            {
                return;
            }

            var save = CharacterSaveRepository.CreateSave(pendingCharacterName, DefaultCharacterRank);
            RefreshVisibleSavesListOnly();
            SelectSave(save.Id);
            pendingScrollToSelected = visibleSaves.Count > SelectionPageLayout.VisiblePlaqueCount;
            pendingCharacterName = string.Empty;
            showNewCharacterDialog = false;
        }

        private void DrawDeleteCharacterDialog()
        {
            var selectedSave = FindSelectedSave();
            var result = DeleteCharacterDialog.Draw(
                true,
                selectedSave != null ? selectedSave.DisplayRankAndName : string.Empty);

            if (result == DeleteCharacterDialog.Result.Confirmed)
            {
                ConfirmDeleteSelectedCharacter();
            }
            else if (result == DeleteCharacterDialog.Result.Back)
            {
                showDeleteCharacterDialog = false;
            }
        }

        private void OpenDeleteCharacterDialog()
        {
            if (FindSelectedSave() == null)
            {
                return;
            }

            showDeleteCharacterDialog = true;
        }

        private void ConfirmDeleteSelectedCharacter()
        {
            if (string.IsNullOrEmpty(selectedSaveId))
            {
                showDeleteCharacterDialog = false;
                return;
            }

            CharacterSaveRepository.DeleteSave(selectedSaveId);
            showDeleteCharacterDialog = false;
            RefreshVisibleSaves();

            if (visibleSaves.Count > 0)
            {
                SelectSave(visibleSaves[0].Id);
            }
            else
            {
                selectedSaveId = null;
            }
        }

        private void SelectSave(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            selectedSaveId = saveId;
            CharacterSaveRepository.SetLastSelectedSaveId(saveId);
            EnsureSelectedPlaqueVisible();
        }

        private void EnsureSelectedPlaqueVisible()
        {
            if (visibleSaves.Count <= SelectionPageLayout.VisiblePlaqueCount)
            {
                return;
            }

            var index = visibleSaves.FindIndex(save => save != null && save.Id == selectedSaveId);
            if (index < 0)
            {
                return;
            }

            var rowHeight = SelectionPageLayout.GetPlaqueRowHeightPx();
            var viewRect = SelectionPageLayout.GetCharacterListViewRect(true);
            var rowTop = index * rowHeight;
            var rowBottom = rowTop + rowHeight;

            if (rowTop < plaqueScrollPosition.y)
            {
                plaqueScrollPosition.y = rowTop;
            }
            else if (rowBottom > plaqueScrollPosition.y + viewRect.height)
            {
                plaqueScrollPosition.y = rowBottom - viewRect.height;
            }

            var contentHeight = visibleSaves.Count * rowHeight;
            var maxScroll = Mathf.Max(0f, contentHeight - viewRect.height);
            plaqueScrollPosition.y = Mathf.Clamp(plaqueScrollPosition.y, 0f, maxScroll);
        }

        private void RefreshVisibleSaves()
        {
            visibleSaves.Clear();
            foreach (var save in CharacterSaveRepository.Saves)
            {
                if (save != null)
                {
                    visibleSaves.Add(save);
                }
            }

            EnsureSelectedSave();
        }

        private void RefreshVisibleSavesListOnly()
        {
            visibleSaves.Clear();
            foreach (var save in CharacterSaveRepository.Saves)
            {
                if (save != null)
                {
                    visibleSaves.Add(save);
                }
            }
        }

        private void EnsureSelectedSave()
        {
            if (!hasInitializedSelection)
            {
                hasInitializedSelection = true;

                var lastSelectedId = CharacterSaveRepository.GetLastSelectedSaveId();
                if (!string.IsNullOrEmpty(lastSelectedId) && CharacterSaveRepository.FindById(lastSelectedId) != null)
                {
                    selectedSaveId = lastSelectedId;
                    pendingScrollToSelected = true;
                    return;
                }
            }

            if (FindSelectedSave() != null)
            {
                return;
            }

            selectedSaveId = visibleSaves.Count > 0 ? visibleSaves[0].Id : null;
        }

        private CharacterSaveData FindSelectedSave()
        {
            return CharacterSaveRepository.FindById(selectedSaveId);
        }

        private static void DrawDossierHighestAward(CharacterSaveData save)
        {
            var preview = SelectionPageLayout.DossierMedalLayoutPreviewPrecedence;
            if (preview == 12)
            {
                DrawDossierFruitSaladRibbon();
                return;
            }

            if (preview > 0 && preview <= 11)
            {
                DrawDossierMedalByPrecedence(preview);
                return;
            }

            var medalId = MilitaryMedalCatalog.GetHighestMedalIdFromEarnedRibbons(save?.EarnedRibbonIds);
            if (!string.IsNullOrEmpty(medalId) && medalId != MilitaryMedalIds.None)
            {
                DrawDossierMedal(medalId);
                return;
            }

            if (save != null && HasEarnedFruitSaladRibbon(save))
            {
                DrawDossierFruitSaladRibbon();
            }
        }

        private static void DrawDossierMedalByPrecedence(int precedence)
        {
            if (!MilitaryMedalCatalog.TryGetDefinitionByPrecedence(precedence, out var definition))
            {
                return;
            }

            DrawDossierMedal(definition.Id);
        }

        private static void DrawDossierMedal(string medalId)
        {
            var medalTexture = MilitaryMedalService.GetMedalTexture(medalId);
            if (medalTexture == null)
            {
                return;
            }

            var medalRect = SelectionPageLayout.GetDossierHighestAwardMedalRect(medalTexture);
            SelectionPageStyles.DrawDossierHighestAwardMedal(medalRect, medalTexture);
            MilitaryAwardTooltipUi.RegisterHover(medalRect, medalId);
        }

        private static void DrawDossierFruitSaladRibbon()
        {
            var ribbonTexture = MilitaryRibbonService.GetRibbonTexture(MilitaryRibbonIds.FruitSalad);
            var ribbonRect = SelectionPageLayout.GetDossierFruitSaladRibbonRect(ribbonTexture);
            SelectionPageStyles.DrawDossierHighestAwardMedal(ribbonRect, ribbonTexture);
            MilitaryAwardTooltipUi.RegisterHover(ribbonRect, MilitaryRibbonIds.FruitSalad);
        }

        private static bool HasEarnedFruitSaladRibbon(CharacterSaveData save)
        {
            if (save?.EarnedRibbonIds == null)
            {
                return false;
            }

            foreach (var ribbonId in save.EarnedRibbonIds)
            {
                if (ribbonId == MilitaryRibbonIds.FruitSalad)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
