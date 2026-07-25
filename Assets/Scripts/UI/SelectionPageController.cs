using System.Collections.Generic;
using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class SelectionPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "SelectionPage/selection_page";
        private const string NewCharacterButtonResourcePath = "SelectionPage/new_character_button";
        private const string DeleteButtonResourcePath = "SelectionPage/delete_button";
        private const string SelectButtonResourcePath = "SelectionPage/select_button";
        private const string DefaultCharacterRank = "2nd LT";

        private readonly List<CharacterSaveData> visibleSaves = new List<CharacterSaveData>();

        private Texture2D backgroundTexture;
        private Texture2D newCharacterButtonTexture;
        private Texture2D deleteButtonTexture;
        private Texture2D selectButtonTexture;
        private Vector2 plaqueScrollPosition;
        private string selectedSaveId;
        private bool hasInitializedSelection;
        private float newCharacterButtonPressUntil;
        private float deleteButtonPressUntil;
        private float selectButtonPressUntil;
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
            newCharacterButtonTexture = Resources.Load<Texture2D>(NewCharacterButtonResourcePath);
            deleteButtonTexture = Resources.Load<Texture2D>(DeleteButtonResourcePath);
            selectButtonTexture = Resources.Load<Texture2D>(SelectButtonResourcePath);

            if (backgroundTexture == null)
            {
                Debug.LogWarning("F-89: Selection page background missing from Resources/SelectionPage/selection_page.");
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
            DrawBackground();

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
        }

        private void DrawBackground()
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
            var save = FindSelectedSave();
            if (save == null)
            {
                return;
            }

            SelectionPageStyles.DrawDossierName(
                SelectionPageLayout.GetDossierNameRect(),
                save.DisplayRankAndName);
            var highestAwardMedalTexture = MilitaryMedalService.GetMedalTexture(save);
            SelectionPageStyles.DrawDossierHighestAwardMedal(
                SelectionPageLayout.GetDossierHighestAwardMedalRect(highestAwardMedalTexture),
                highestAwardMedalTexture);

            var pictureRect = SelectionPageLayout.GetDossierPictureRect();
            var portraitTexture = CharacterPortraitService.GetPortraitTexture(save);
            SelectionPageStyles.DrawDossierPortrait(pictureRect, portraitTexture);
            if (SelectionPageStyles.DrawInvisibleButton(pictureRect))
            {
                OpenSelectPortraitDialog();
            }

            SelectionPageStyles.DrawDossierStats(
                save.EnemyVehiclesKilled,
                save.EnemyTroopsKilled,
                save.BestMissionScore,
                save.TotalScore);
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
            if (newCharacterButtonTexture == null)
            {
                return;
            }

            var rect = SelectionPageLayout.GetNewCharacterButtonRect(newCharacterButtonTexture);
            if (SelectionPageStyles.DrawTexturedButton(rect, newCharacterButtonTexture, newCharacterButtonPressUntil))
            {
                newCharacterButtonPressUntil = Time.unscaledTime + SelectionPageStyles.PressDuration;
                OpenNewCharacterDialog();
            }
        }

        private void DrawDeleteButton()
        {
            if (deleteButtonTexture == null)
            {
                return;
            }

            var rect = SelectionPageLayout.GetDeleteButtonRect(deleteButtonTexture);
            if (SelectionPageStyles.DrawTexturedButton(rect, deleteButtonTexture, deleteButtonPressUntil))
            {
                deleteButtonPressUntil = Time.unscaledTime + SelectionPageStyles.PressDuration;
                OpenDeleteCharacterDialog();
            }
        }

        private void DrawSelectButton()
        {
            if (selectButtonTexture == null)
            {
                return;
            }

            var rect = SelectionPageLayout.GetSelectButtonRect(selectButtonTexture);
            if (SelectionPageStyles.DrawTexturedButton(rect, selectButtonTexture, selectButtonPressUntil))
            {
                selectButtonPressUntil = Time.unscaledTime + SelectionPageStyles.PressDuration;
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
            CharacterSaveRepository.SetLastSelectedSaveId(save.Id);
            CharacterSaveRepository.TouchLastPlayed(save);
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
    }
}
