using UnityEngine;

namespace F89.UI
{
    public static class SelectionPageLayout
    {
        private const float ListPanelLeft = 0f;
        private const float ListPanelTop = 0.11f;
        private const float ListPanelWidth = 0.419f;
        private const float ListPanelHeight = 0.707f;
        private const float SharedFrameTopNorm = 0.11f;
        private const float ColumnButtonOffsetRightDesignPx = 30f;
        private const float ColumnButtonOffsetUpDesignPx = 180f;

        private const float SelectButtonCenterX = 0.535f;
        private const float SelectButtonCenterY = 0.793f;
        private const float SelectButtonOffsetRightDesignPx = 143f;
        private const float SelectButtonOffsetDownDesignPx = 150f;

        private const float DossierNameLeftInsetDesignPx = 10f;
        private const float DossierNameOffsetRightDesignPx = 110f;
        private const float DossierNameOffsetDownDesignPx = 100f;
        private const float DossierNameHeightDesignPx = 56f;

        private const float DossierPictureSizeDesignPx = 455f;
        private const float DossierPictureInsetDesignPx = 15f;

        private const float DossierAwardBoxLeft = 0.381f;
        private const float DossierAwardBoxTop = 0.191f;
        private const float DossierAwardBoxWidth = 0.072f;
        private const float DossierAwardBoxHeight = 0.198f;
        private const float DossierAwardMedalInsetDesignPx = 4f;
        private const float DossierAwardMedalOffsetRightDesignPx = 499f;
        private const float DossierAwardMedalOffsetDownDesignPx = 62f;

        // Layout preview: 1-11 forces that medal; 12 shows fruit salad ribbon; 0 uses earned ribbons.
        public const int DossierMedalLayoutPreviewPrecedence = 0;

        // Layout preview: >0 shows that value on all four dossier stat lines; 0 uses save data.
        public const int DossierStatLayoutPreviewValue = 0;

        private const float DossierFruitSaladRibbonOffsetRightDesignPx = 499f;
        private const float DossierFruitSaladRibbonOffsetDownDesignPx = 153f;

        private const float DossierVehicleKillsOnesXNorm = 0.5869f;
        private const float DossierVehicleKillsValueYNorm = 0.5927f;
        private const float DossierTroopKillsOnesXNorm = 0.8174f;
        private const float DossierTroopKillsValueYNorm = 0.5936f;
        private const float DossierBestScoreOnesXNorm = 0.7764f;
        private const float DossierBestScoreValueYNorm = 0.7022f;
        private const float DossierTotalScoreOnesXNorm = 0.7764f;
        private const float DossierTotalScoreValueYNorm = 0.7940f;
        private const float DossierKillStatValueWidthDesignPx = 120f;
        private const float DossierScoreStatValueWidthDesignPx = 180f;
        private const float DossierStatValueHeightDesignPx = 44f;
        private const float DossierStatValueOffsetDownDesignPx = 40f;

        public const int VisiblePlaqueCount = 8;
        private const float PlaqueScrollbarWidthDesignPx = 18f;
        private const float PlaqueTextOffsetFromPanelLeftDesignPx = 150f;
        private const float PlaqueRowSpacingDesignPx = 6f;
        private const float CharacterListOffsetDownDesignPx = 25f;

        public static float PlaqueScrollbarWidthPx => UiFitCanvas.Px(PlaqueScrollbarWidthDesignPx);

        public static float GetPlaqueRowHeightPx() =>
            SelectionPageStyles.GetPlaqueHeight() + UiFitCanvas.Px(PlaqueRowSpacingDesignPx);

        public static float GetCharacterListViewHeightPx()
        {
            var scrollRect = GetCharacterListScrollRect();
            var visibleHeight = VisiblePlaqueCount * GetPlaqueRowHeightPx();
            return Mathf.Min(scrollRect.height, visibleHeight);
        }

        public static Rect GetCharacterListScrollRect()
        {
            var panel = GetCharacterListPanelRect();
            var top = panel.y + UiFitCanvas.Px(CharacterListOffsetDownDesignPx);
            var bottom = panel.yMax - UiFitCanvas.Px(ColumnButtonOffsetUpDesignPx);
            return new Rect(panel.x, top, panel.width, Mathf.Max(0f, bottom - top));
        }

        public static Rect GetCharacterListRowSlotRect(int index, int visibleCount)
        {
            var scrollRect = GetCharacterListScrollRect();
            var slotHeight = scrollRect.height / Mathf.Max(visibleCount, 1);
            return new Rect(
                scrollRect.x,
                scrollRect.y + index * slotHeight,
                scrollRect.width,
                slotHeight);
        }

        public static Rect GetCharacterListRowSlotRectLocal(int index, float scrollWidth)
        {
            var rowHeight = GetPlaqueRowHeightPx();
            return new Rect(0f, index * rowHeight, scrollWidth, rowHeight);
        }

        public static Rect GetCompactPlaqueRect(Rect slotRect)
        {
            var height = SelectionPageStyles.GetPlaqueHeight();
            return new Rect(slotRect.x, slotRect.y, slotRect.width, height);
        }

        public static Rect GetCompactPlaqueRectLocal(Rect slotRect)
        {
            var height = SelectionPageStyles.GetPlaqueHeight();
            return new Rect(slotRect.x, slotRect.y, slotRect.width, height);
        }

        public static Rect GetPlaqueLabelRect(Rect plaqueRectLocal, bool includeScrollbar)
        {
            var panelLeft = GetCharacterListScrollRect().x;
            var viewLeft = GetCharacterListViewRect(includeScrollbar).x;
            var labelX = panelLeft + UiFitCanvas.Px(PlaqueTextOffsetFromPanelLeftDesignPx) - viewLeft;
            var width = Mathf.Max(0f, plaqueRectLocal.width - labelX);
            return new Rect(labelX, plaqueRectLocal.y, width, plaqueRectLocal.height);
        }

        public static Rect GetCharacterListViewRect(bool includeScrollbar)
        {
            var scrollRect = GetCharacterListScrollRect();
            var viewHeight = GetCharacterListViewHeightPx();
            if (!includeScrollbar)
            {
                return new Rect(scrollRect.x, scrollRect.y, scrollRect.width, viewHeight);
            }

            var scrollbarWidth = PlaqueScrollbarWidthPx;
            return new Rect(
                scrollRect.x + scrollbarWidth,
                scrollRect.y,
                scrollRect.width - scrollbarWidth,
                viewHeight);
        }

        public static Rect GetCharacterListScrollbarRect()
        {
            var scrollRect = GetCharacterListScrollRect();
            var viewHeight = GetCharacterListViewHeightPx();
            return new Rect(scrollRect.x, scrollRect.y, PlaqueScrollbarWidthPx, viewHeight);
        }

        public static Rect GetCharacterListPanelRect()
        {
            return UiFitCanvas.NormRect(ListPanelLeft, ListPanelTop, ListPanelWidth, ListPanelHeight);
        }

        public static Rect GetCharacterListRowRect(int index, int visibleCount)
        {
            return GetCompactPlaqueRect(GetCharacterListRowSlotRect(index, visibleCount));
        }

        public static Rect GetCharacterListContentRect(int saveCount)
        {
            var viewRect = GetCharacterListViewRect(saveCount > VisiblePlaqueCount);
            var rowHeight = GetPlaqueRowHeightPx();
            return new Rect(0f, 0f, viewRect.width, saveCount * rowHeight);
        }

        public static Rect GetCharacterListContentRowRect(int index)
        {
            var rowHeight = GetPlaqueRowHeightPx();
            var viewWidth = GetCharacterListViewRect(true).width;
            return new Rect(0f, index * rowHeight, viewWidth, rowHeight);
        }

        public static Rect GetNewCharacterButtonRect()
        {
            StartPageMenuStyles.GetMenuButtonSize(out var width, out var height);
            var frame = GetCharacterListBackdropRect();
            var x = frame.x + (frame.width - width) * 0.5f;
            var y = frame.yMax + UiFitCanvas.Px(12f);
            return new Rect(x, y, width, height);
        }

        public static Rect GetDeleteButtonRect()
        {
            var newCharacterRect = GetNewCharacterButtonRect();
            StartPageMenuStyles.GetMenuButtonSize(out _, out var height);
            return new Rect(
                newCharacterRect.x,
                newCharacterRect.yMax + UiFitCanvas.Px(8f),
                newCharacterRect.width,
                height);
        }

        public static Rect GetSelectButtonRect()
        {
            const float lowerDesignPx = 10f;

            StartPageMenuStyles.GetMenuButtonSize(out var width, out var height);
            GetDossierPanelHorizontal(out var panelX, out var panelWidth);
            var x = panelX + (panelWidth - width) * 0.5f;
            var centerY = UiFitCanvas.NormY(SelectButtonCenterY)
                + UiFitCanvas.Px(SelectButtonOffsetDownDesignPx + lowerDesignPx);
            return new Rect(x, centerY - height * 0.5f, width, height);
        }

        public static Rect GetDossierNameRect()
        {
            // Outer Character Dossier panel (brass frame on the right), not the white award box.
            var panel = GetDossierPanelRect();
            var picture = GetDossierPictureRect();
            const float leftInsetDesignPx = 15f;
            const float topInsetDesignPx = 25f;
            var x = panel.x + UiFitCanvas.Px(leftInsetDesignPx);
            var y = panel.y + UiFitCanvas.Px(topInsetDesignPx);
            var width = Mathf.Max(0f, picture.x - x - UiFitCanvas.Px(12f));
            return new Rect(x, y, width, UiFitCanvas.Px(DossierNameHeightDesignPx));
        }

        public static Rect GetCharacterSelectHeaderRect()
        {
            var frameTop = UiFitCanvas.NormY(SharedFrameTopNorm);
            var height = UiFitCanvas.Px(78f);
            return new Rect(
                UiFitCanvas.NormX(0.02f),
                frameTop - height - UiFitCanvas.Px(4f),
                UiFitCanvas.Rect.width * 0.38f,
                height);
        }

        public static Rect GetCharacterDossierHeaderRect()
        {
            var panel = GetDossierPanelRect();
            var frameTop = panel.y;
            var height = UiFitCanvas.Px(78f);
            return new Rect(
                panel.x,
                frameTop - height - UiFitCanvas.Px(4f),
                panel.width,
                height);
        }

        public static Rect GetCharacterListBackdropRect()
        {
            var panel = GetCharacterListPanelRect();
            var top = UiFitCanvas.NormY(SharedFrameTopNorm);
            var bottom = panel.yMax - UiFitCanvas.Px(ColumnButtonOffsetUpDesignPx) - UiFitCanvas.Px(8f);
            return new Rect(
                panel.x + UiFitCanvas.Px(8f),
                top,
                panel.width - UiFitCanvas.Px(16f),
                Mathf.Max(0f, bottom - top));
        }

        public static Rect GetHighestAwardLabelRect()
        {
            var name = GetDossierNameRect();
            GetHighestAwardBoxMetrics(out var boxX, out var boxWidth, out _);
            var labelWidth = Mathf.Max(boxWidth, UiFitCanvas.Px(320f));
            var labelX = boxX + (boxWidth - labelWidth) * 0.5f;
            return new Rect(
                labelX,
                name.yMax + UiFitCanvas.Px(10f),
                labelWidth,
                UiFitCanvas.Px(56f));
        }

        public static Rect GetHighestAwardSlotRect()
        {
            GetHighestAwardBoxMetrics(out var x, out var width, out var height);
            var label = GetHighestAwardLabelRect();
            return new Rect(x, label.yMax + UiFitCanvas.Px(20f), width, height);
        }

        private static void GetHighestAwardBoxMetrics(out float x, out float width, out float height)
        {
            // Was 144×240 (3:5); height 300 keeps that aspect → width 180.
            height = UiFitCanvas.Px(300f);
            width = UiFitCanvas.Px(180f);
            var panel = GetDossierPanelRect();
            var picture = GetDossierPictureRect();
            x = panel.x + (picture.x - panel.x - width) * 0.5f;
        }

        public static Rect GetVehicleKillsLabelRect() => GetDossierStatLabelRect(0);

        public static Rect GetTroopKillsLabelRect() => GetDossierStatLabelRect(1);

        public static Rect GetBestMissionScoreLabelRect() => GetDossierStatLabelRect(2);

        public static Rect GetTotalScoreLabelRect() => GetDossierStatLabelRect(3);

        public static Rect GetDossierVehicleKillsValueRect() => GetDossierStatValueRect(0);

        public static Rect GetDossierTroopKillsValueRect() => GetDossierStatValueRect(1);

        public static Rect GetDossierBestScoreValueRect() => GetDossierStatValueRect(2);

        public static Rect GetDossierTotalScoreValueRect() => GetDossierStatValueRect(3);

        public static Rect GetDossierStatLabelRect(int rowIndex)
        {
            var row = GetDossierStatRowRect(rowIndex);
            var panel = GetDossierPanelRect();
            if (rowIndex == 3)
            {
                return GetTotalScoreCenteredLabelRect(row, panel);
            }

            var labelWidth = SelectionPageStyles.GetDossierStatLabelColumnWidthPx();
            return new Rect(
                panel.x + UiFitCanvas.Px(10f),
                row.y,
                labelWidth,
                row.height);
        }

        public static Rect GetDossierStatValueRect(int rowIndex)
        {
            var label = GetDossierStatLabelRect(rowIndex);
            var valueWidth = UiFitCanvas.Px(DossierScoreStatValueWidthDesignPx);
            var gap = UiFitCanvas.Px(12f) + SelectionPageStyles.GetDossierStatValueFiveSpacesWidthPx();
            if (rowIndex == 3)
            {
                return new Rect(
                    label.xMax + gap,
                    label.y,
                    valueWidth,
                    label.height);
            }

            return new Rect(
                label.xMax + gap,
                label.y,
                valueWidth,
                label.height);
        }

        private static Rect GetTotalScoreCenteredLabelRect(Rect row, Rect panel)
        {
            var labelWidth = SelectionPageStyles.GetTotalScoreLabelWidthPx();
            var gap = UiFitCanvas.Px(12f) + SelectionPageStyles.GetDossierStatValueFiveSpacesWidthPx();
            var preview = DossierStatLayoutPreviewValue;
            var valueText = MissionScoreDisplayUi.FormatScore(preview > 0 ? preview : 0);
            var valueWidth = MissionScoreDisplayUi.MeasureDossierValueTextWidth(valueText, 10);
            // Value draw uses MiddleLeft in a wider rect; center using actual text width.
            var lineWidth = labelWidth + gap + valueWidth;
            var x = panel.x + (panel.width - lineWidth) * 0.5f;
            return new Rect(x, row.y, labelWidth, row.height);
        }

        public static float GetDossierStatLabelColumnWidth()
        {
            return SelectionPageStyles.GetDossierStatLabelColumnWidthPx();
        }

        public static Rect GetDossierStatRowRect(int rowIndex)
        {
            var panel = GetDossierPanelRect();
            var picture = GetDossierPictureRect();
            var awardBottom = GetHighestAwardSlotRect().yMax;
            var baseRowHeight = UiFitCanvas.Px(40f);
            var rowHeight = rowIndex == 3 ? UiFitCanvas.Px(52f) : baseRowHeight;
            // Extra blank line (carriage return) after each of the top 3 rows.
            var rowGap = UiFitCanvas.Px(36f);
            var firstY = Mathf.Max(picture.yMax, awardBottom) + UiFitCanvas.Px(16f) + UiFitCanvas.Px(40f);
            var y = firstY + rowIndex * (baseRowHeight + rowGap);
            if (rowIndex == 3)
            {
                y += UiFitCanvas.Px(15f);
            }

            return new Rect(
                panel.x + UiFitCanvas.Px(10f),
                y,
                panel.width - UiFitCanvas.Px(20f),
                rowHeight);
        }

        public static Rect GetDossierPanelRect()
        {
            GetDossierPanelHorizontal(out var x, out var width);
            var top = UiFitCanvas.NormY(SharedFrameTopNorm);
            var bottom = GetSelectButtonRect().y - UiFitCanvas.Px(14f);
            return new Rect(x, top, width, Mathf.Max(0f, bottom - top));
        }

        private static void GetDossierPanelHorizontal(out float x, out float width)
        {
            x = UiFitCanvas.NormX(0.43f);
            width = UiFitCanvas.Rect.width * 0.54f;
        }

        public static Rect GetDossierPictureRect()
        {
            var panel = GetDossierPanelRect();
            var size = UiFitCanvas.Px(DossierPictureSizeDesignPx);
            var inset = UiFitCanvas.Px(DossierPictureInsetDesignPx);
            return new Rect(
                panel.xMax - inset - size,
                panel.y + inset,
                size,
                size);
        }

        public static Rect GetDossierHighestAwardBoxRect()
        {
            return UiFitCanvas.NormRect(
                DossierAwardBoxLeft,
                DossierAwardBoxTop,
                DossierAwardBoxWidth,
                DossierAwardBoxHeight);
        }

        public static Rect GetDossierHighestAwardMedalRect(Texture2D medalTexture)
        {
            return FitTextureInRect(GetHighestAwardSlotRect(), medalTexture, UiFitCanvas.Px(DossierAwardMedalInsetDesignPx));
        }

        public static Rect GetDossierFruitSaladRibbonRect(Texture2D ribbonTexture)
        {
            return FitTextureInRect(GetHighestAwardSlotRect(), ribbonTexture, UiFitCanvas.Px(DossierAwardMedalInsetDesignPx));
        }

        private static Rect FitTextureInRect(Rect boxRect, Texture2D texture, float inset)
        {
            var maxWidth = Mathf.Max(0f, boxRect.width - inset * 2f);
            var maxHeight = Mathf.Max(0f, boxRect.height - inset * 2f);
            var anchorX = boxRect.x + inset;
            var anchorY = boxRect.y + inset;

            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return new Rect(anchorX, anchorY, maxWidth, maxHeight);
            }

            var aspect = (float)texture.height / texture.width;
            var width = maxWidth;
            var height = width * aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height / aspect;
            }

            return new Rect(
                anchorX + (maxWidth - width) * 0.5f,
                anchorY + (maxHeight - height) * 0.5f,
                width,
                height);
        }
    }
}
