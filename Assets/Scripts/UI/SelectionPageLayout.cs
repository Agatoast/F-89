using UnityEngine;

namespace F89.UI
{
    public static class SelectionPageLayout
    {
        private const float ListPanelLeft = 0f;
        private const float ListPanelTop = 0.111f;
        private const float ListPanelWidth = 0.419f;
        private const float ListPanelHeight = 0.707f;
        private const float ColumnButtonWidthScale = 0.92f;
        private const float ColumnButtonOffsetRightPx = 30f;
        private const float ColumnButtonOffsetUpPx = 180f;

        private const float SelectButtonCenterX = 0.535f;
        private const float SelectButtonCenterY = 0.793f;
        private const float SelectButtonOffsetRightPx = 55f;
        private const float SelectButtonOffsetDownPx = 150f;

        private const float DossierNameLeftInsetPx = 10f;
        private const float DossierNameOffsetRightPx = 110f;
        private const float DossierNameOffsetDownPx = 100f;
        private const float DossierNameHeightPx = 40f;

        private const float DossierPictureLeft = 0.752f;
        private const float DossierPictureTop = 0.179f;
        private const float DossierPictureSizePx = 325f;
        private const float DossierPhotoOffsetLeftPx = 9f;
        private const float DossierPhotoOffsetUpPx = 12f;

        private const float DossierAwardBoxLeft = 0.381f;
        private const float DossierAwardBoxTop = 0.191f;
        private const float DossierAwardBoxWidth = 0.072f;
        private const float DossierAwardBoxHeight = 0.198f;
        private const float DossierAwardMedalInsetPx = 4f;
        private const float DossierAwardMedalOffsetRightPx = 434f;
        private const float DossierAwardMedalOffsetDownPx = 153f;

        private const float DossierVehicleKillsOnesXNorm = 0.5869f;
        private const float DossierVehicleKillsValueYNorm = 0.5927f;
        private const float DossierTroopKillsOnesXNorm = 0.8174f;
        private const float DossierTroopKillsValueYNorm = 0.5936f;
        private const float DossierBestScoreOnesXNorm = 0.7764f;
        private const float DossierBestScoreValueYNorm = 0.7022f;
        private const float DossierTotalScoreOnesXNorm = 0.7764f;
        private const float DossierTotalScoreValueYNorm = 0.7940f;
        private const float DossierKillStatValueWidthPx = 120f;
        private const float DossierScoreStatValueWidthPx = 180f;
        private const float DossierStatValueHeightPx = 44f;
        private const float DossierStatValueOffsetDownPx = 40f;

        public const int VisiblePlaqueCount = 8;
        public const float PlaqueScrollbarWidthPx = 18f;
        public const float PlaqueTextOffsetFromPanelLeftPx = 150f;
        private const float PlaqueRowSpacingPx = 6f;

        public static float GetPlaqueRowHeightPx() =>
            SelectionPageStyles.GetPlaqueHeight() + PlaqueRowSpacingPx;

        public static float GetCharacterListViewHeightPx()
        {
            var scrollRect = GetCharacterListScrollRect();
            var visibleHeight = VisiblePlaqueCount * GetPlaqueRowHeightPx();
            return Mathf.Min(scrollRect.height, visibleHeight);
        }

        public static Rect GetCharacterListScrollRect()
        {
            var panel = GetCharacterListPanelRect();
            var bottom = panel.yMax - ColumnButtonOffsetUpPx;
            return new Rect(panel.x, panel.y, panel.width, Mathf.Max(0f, bottom - panel.y));
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
            var labelX = panelLeft + PlaqueTextOffsetFromPanelLeftPx - viewLeft;
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

            return new Rect(
                scrollRect.x + PlaqueScrollbarWidthPx,
                scrollRect.y,
                scrollRect.width - PlaqueScrollbarWidthPx,
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
            return ScaleRect(ListPanelLeft, ListPanelTop, ListPanelWidth, ListPanelHeight);
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

        public static Rect GetNewCharacterButtonRect(Texture2D buttonTexture)
        {
            var panel = GetCharacterListPanelRect();
            GetStandardButtonSize(buttonTexture, panel.width, out var width, out var height);
            var x = panel.x + (panel.width - width) * 0.5f + ColumnButtonOffsetRightPx;
            var y = panel.yMax - ColumnButtonOffsetUpPx;
            return new Rect(x, y, width, height);
        }

        public static Rect GetDeleteButtonRect(Texture2D buttonTexture)
        {
            var newCharacterRect = GetNewCharacterButtonRect(buttonTexture);
            GetStandardButtonSize(buttonTexture, GetCharacterListPanelRect().width, out _, out var height);
            return new Rect(newCharacterRect.x, newCharacterRect.yMax, newCharacterRect.width, height);
        }

        public static Rect GetSelectButtonRect(Texture2D buttonTexture)
        {
            var panel = GetCharacterListPanelRect();
            GetStandardButtonSize(buttonTexture, panel.width, out var width, out var height);
            var centerX = Screen.width * SelectButtonCenterX + SelectButtonOffsetRightPx;
            var centerY = Screen.height * SelectButtonCenterY + SelectButtonOffsetDownPx;
            return new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }

        public static Rect GetDossierNameRect()
        {
            var panel = GetDossierPanelRect();
            return new Rect(
                panel.x + DossierNameLeftInsetPx + DossierNameOffsetRightPx,
                panel.y + DossierNameOffsetDownPx,
                panel.width * 0.42f,
                DossierNameHeightPx);
        }

        public static Rect GetDossierPanelRect()
        {
            return ScaleRect(0.43f, 0.08f, 0.54f, 0.62f);
        }

        public static Rect GetDossierPictureRect()
        {
            return new Rect(
                Screen.width * DossierPictureLeft - DossierPhotoOffsetLeftPx,
                Screen.height * DossierPictureTop - DossierPhotoOffsetUpPx,
                DossierPictureSizePx,
                DossierPictureSizePx);
        }

        public static Rect GetDossierHighestAwardBoxRect()
        {
            return ScaleRect(
                DossierAwardBoxLeft,
                DossierAwardBoxTop,
                DossierAwardBoxWidth,
                DossierAwardBoxHeight);
        }

        public static Rect GetDossierHighestAwardMedalRect(Texture2D medalTexture)
        {
            var boxRect = GetDossierHighestAwardBoxRect();
            var inset = DossierAwardMedalInsetPx;
            var maxWidth = Mathf.Max(0f, boxRect.width - inset * 2f);
            var maxHeight = Mathf.Max(0f, boxRect.height - inset * 2f);

            if (medalTexture == null || medalTexture.width <= 0 || medalTexture.height <= 0)
            {
                return new Rect(
                    boxRect.x + inset + DossierAwardMedalOffsetRightPx,
                    boxRect.y + inset + DossierAwardMedalOffsetDownPx,
                    maxWidth,
                    maxHeight);
            }

            var aspect = (float)medalTexture.height / medalTexture.width;
            var width = maxWidth;
            var height = width * aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height / aspect;
            }

            return new Rect(
                boxRect.x + inset + DossierAwardMedalOffsetRightPx,
                boxRect.y + inset + DossierAwardMedalOffsetDownPx,
                width,
                height);
        }

        public static Rect GetDossierVehicleKillsValueRect()
        {
            return GetDossierStatValueRect(
                DossierVehicleKillsOnesXNorm,
                DossierVehicleKillsValueYNorm,
                DossierKillStatValueWidthPx,
                45f,
                7f);
        }

        public static Rect GetDossierTroopKillsValueRect()
        {
            return GetDossierStatValueRect(
                DossierTroopKillsOnesXNorm,
                DossierTroopKillsValueYNorm,
                DossierKillStatValueWidthPx,
                38f,
                7f);
        }

        public static Rect GetDossierBestScoreValueRect()
        {
            return GetDossierStatValueRect(
                DossierBestScoreOnesXNorm,
                DossierBestScoreValueYNorm,
                DossierScoreStatValueWidthPx);
        }

        public static Rect GetDossierTotalScoreValueRect()
        {
            return GetDossierStatValueRect(
                DossierTotalScoreOnesXNorm,
                DossierTotalScoreValueYNorm,
                DossierScoreStatValueWidthPx,
                0f,
                -2f);
        }

        private static Rect GetDossierStatValueRect(
            float onesXNorm,
            float centerYNorm,
            float widthPx,
            float offsetRightPx = 0f,
            float offsetDownPx = 0f)
        {
            var onesX = Screen.width * onesXNorm + offsetRightPx;
            var centerY = Screen.height * centerYNorm + DossierStatValueOffsetDownPx + offsetDownPx;
            return new Rect(
                onesX - widthPx,
                centerY - DossierStatValueHeightPx * 0.5f,
                widthPx,
                DossierStatValueHeightPx);
        }

        private static void GetStandardButtonSize(Texture2D buttonTexture, float panelWidth, out float width, out float height)
        {
            height = GetButtonHeight(buttonTexture);
            width = GetButtonWidth(buttonTexture, height, panelWidth);
        }

        private static float GetButtonHeight(Texture2D buttonTexture)
        {
            var availableBelowPanel = Screen.height * (1f - (ListPanelTop + ListPanelHeight));
            var maxHeight = availableBelowPanel * 0.5f;
            if (buttonTexture == null || buttonTexture.width <= 0)
            {
                return maxHeight;
            }

            var aspectHeight = Screen.width * ListPanelWidth * ColumnButtonWidthScale *
                ((float)buttonTexture.height / buttonTexture.width);
            return Mathf.Min(aspectHeight, maxHeight);
        }

        private static float GetButtonWidth(Texture2D buttonTexture, float height, float panelWidth)
        {
            if (buttonTexture != null && buttonTexture.height > 0)
            {
                return height * ((float)buttonTexture.width / buttonTexture.height);
            }

            return panelWidth * ColumnButtonWidthScale;
        }

        private static Rect ScaleRect(float xNorm, float yNorm, float wNorm, float hNorm)
        {
            return new Rect(
                xNorm * Screen.width,
                yNorm * Screen.height,
                wNorm * Screen.width,
                hNorm * Screen.height);
        }
    }
}
