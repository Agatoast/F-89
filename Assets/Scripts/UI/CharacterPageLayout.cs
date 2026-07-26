using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageLayout
    {
        public static Rect GetNameBarRect()
        {
            const float marginLeftPx = 10f;
            const float yNorm = 0.014f;
            const float widthNorm = 0.19f;
            const float heightNorm = 0.05f;
            return new Rect(
                marginLeftPx,
                Screen.height * yNorm,
                Screen.width * widthNorm,
                Screen.height * heightNorm);
        }

        public static Rect GetRibbonsRect() =>
            NormalizedRect(0.016f, 0.065f, 0.334f, 0.278f);

        public static Rect GetRibbonSlotRect(Rect panelRect, int row, int column)
        {
            const int maxColumns = 3;
            const int maxRows = 4;
            const float sizeScale = 0.855f;

            var slotWidth = panelRect.width / maxColumns;
            var slotHeight = panelRect.height / maxRows;
            var ribbonWidth = slotWidth * sizeScale;
            var ribbonHeight = slotHeight * sizeScale;
            var x = panelRect.x + column * ribbonWidth;
            var y = panelRect.y + row * ribbonHeight;
            return new Rect(x, y, ribbonWidth, ribbonHeight);
        }

        private const float MissionScoreGapFromPortraitPx = 30f;
        private const float MissionScoreRowGapPx = 70f;
        private const float MissionScoreShiftDownPx = 90f;
        public const float MissionScoreLabelColumnWidthPx = 420f;
        private const float MissionScoreOnesOffsetFromLabelPx = 140f;

        public static float GetMissionScoreOnesColumnX()
        {
            const float valueGapPx = 8f;
            var row = GetTotalScoreLabelRect();
            return row.x + MissionScoreLabelColumnWidthPx + valueGapPx + MissionScoreOnesOffsetFromLabelPx;
        }

        public static Rect GetTotalScoreLabelRect()
        {
            var portrait = GetPortraitRect();
            var height = Screen.height * 0.048f;
            var width = Screen.width * 0.33f;
            return new Rect(portrait.xMax + MissionScoreGapFromPortraitPx, portrait.y + MissionScoreShiftDownPx, width, height);
        }

        public static Rect GetBestScoreLabelRect()
        {
            var totalScore = GetTotalScoreLabelRect();
            return new Rect(totalScore.x, totalScore.yMax + MissionScoreRowGapPx, totalScore.width, totalScore.height);
        }

        private const float KillFolderHeightNorm = 0.4318272f;
        private const float KillFolderLeftNorm = 0.016f;
        private const float KillFolderGapPx = 10f;
        private const float KillFolderShiftLeftPx = 42f;
        private const float KillFolderRightShiftLeftPx = 25f;
        private const float KillFolderShiftDownPx = 45f;

        public const int KillFolderSlotColumns = 5;
        public const int KillFolderSlotRows = 2;
        public const int KillFolderSlotCount = KillFolderSlotColumns * KillFolderSlotRows;
        private const float KillFolderSlotSizePx = 72f;
        private const float KillFolderSlotGapPx = 0f;
        private const float KillFolderSlotGridTopPx = 245f;

        public static Rect GetKillFolderSlotRect(Rect folderRect, int index)
        {
            var col = index % KillFolderSlotColumns;
            var row = index / KillFolderSlotColumns;
            var gridWidth = KillFolderSlotColumns * KillFolderSlotSizePx
                + (KillFolderSlotColumns - 1) * KillFolderSlotGapPx;
            var gridLeft = folderRect.x + (folderRect.width - gridWidth) * 0.5f;
            var gridTop = folderRect.y + KillFolderSlotGridTopPx;
            return new Rect(
                gridLeft + col * (KillFolderSlotSizePx + KillFolderSlotGapPx),
                gridTop + row * (KillFolderSlotSizePx + KillFolderSlotGapPx),
                KillFolderSlotSizePx,
                KillFolderSlotSizePx);
        }

        public static Rect GetVehicleKillsRect() => GetKillFolderRect(0);

        public static Rect GetTroopKillsRect() => GetKillFolderRect(1);

        public static Rect GetVehicleKillsLabelRect()
        {
            const float labelOffsetDownPx = 150f;
            const float labelOffsetRightPx = 1f;
            var folder = GetVehicleKillsRect();
            var labelHeight = Screen.height * 0.05f;
            return new Rect(
                folder.x + labelOffsetRightPx,
                folder.y - labelHeight - 4f + labelOffsetDownPx,
                folder.width,
                labelHeight);
        }

        public static Rect GetTroopKillsLabelRect()
        {
            const float gapFromVehicleLabelPx = 15f;
            const float shiftLeftPx = 30f;
            var vehicleLabel = GetVehicleKillsLabelRect();
            var folder = GetTroopKillsRect();
            return new Rect(
                vehicleLabel.xMax + gapFromVehicleLabelPx - shiftLeftPx,
                vehicleLabel.y,
                folder.width,
                vehicleLabel.height);
        }

        private static Rect GetKillFolderRect(int index)
        {
            var height = Screen.height * KillFolderHeightNorm;
            var width = height;
            var portrait = GetPortraitRect();
            var bottomY = portrait.y + KillFolderShiftDownPx;
            var leftX = Screen.width * KillFolderLeftNorm - KillFolderShiftLeftPx;
            var x = index == 0
                ? leftX
                : leftX + width + KillFolderGapPx - KillFolderRightShiftLeftPx;
            return new Rect(x, bottomY - height, width, height);
        }

        public static Rect GetFootlockerTitleRect()
        {
            var width = Screen.width * 0.36f;
            return new Rect((Screen.width - width) * 0.5f, Screen.height * 0.018f, width, Screen.height * 0.05f);
        }

        public static Rect GetFootlockerGridRect()
        {
            const float marginRightPx = 10f;
            const float yNorm = 0.069f;
            const float widthNorm = 0.675f;
            const float heightNorm = 0.27972f;
            var width = Screen.width * widthNorm;
            var height = Screen.height * heightNorm;
            var x = Screen.width - marginRightPx - width;
            var y = Screen.height * yNorm;
            return new Rect(x, y, width, height);
        }

        public static Rect GetPortraitRect()
        {
            const float marginLeftNorm = 0.016f;
            const float marginBottomPx = 10f;
            const float shiftLeftPx = 26f;
            const float shiftUpPx = 16f;
            const float widthNorm = 0.19f;
            const float heightNorm = 0.338f;
            var width = Screen.width * widthNorm;
            var height = Screen.height * heightNorm;
            var x = Screen.width * marginLeftNorm - shiftLeftPx;
            var y = Screen.height - marginBottomPx - height - shiftUpPx;
            return new Rect(x, y, width, height);
        }

        private const float BottomRightClusterShiftLeftPx = 100f;

        public static Rect GetSaveAntarcticaLogoRect(Texture2D logoTexture)
        {
            const float gapAbovePaperdollPx = 6f;
            const float sizeScale = 2.667f;
            const float shiftRightPx = 130f;
            const int equipmentSlotCount = 4;
            var paperdoll = GetPaperdollRect();
            var equipmentSlot = GetEquipmentSlotRect(0, equipmentSlotCount);
            var clusterLeft = equipmentSlot.x;
            var clusterWidth = paperdoll.xMax - clusterLeft;
            var aspect = logoTexture != null && logoTexture.width > 0
                ? (float)logoTexture.height / logoTexture.width
                : 0.34f;
            var width = clusterWidth * sizeScale;
            var height = width * aspect;
            var x = clusterLeft + (clusterWidth - width) * 0.5f + shiftRightPx;
            var y = Mathf.Max(0f, paperdoll.y - gapAbovePaperdollPx - height);
            return new Rect(x, y, width, height);
        }

        public static Rect GetPaperdollRect()
        {
            const float gapPx = 10f;
            const float marginRightPx = 10f;
            const float marginBottomPx = 10f;
            const float widthNorm = 0.104f;
            const float heightNorm = 0.417f;
            const float inventoryWidthNorm = 0.198f;
            var width = Screen.width * widthNorm;
            var height = Screen.height * heightNorm;
            var inventoryWidth = Screen.width * inventoryWidthNorm;
            var inventoryX = Screen.width - marginRightPx - inventoryWidth - BottomRightClusterShiftLeftPx;
            var x = inventoryX - gapPx - width;
            var y = Screen.height - marginBottomPx - height;
            return new Rect(x, y, width, height);
        }

        public static Rect GetInventoryGridRect()
        {
            const float marginRightPx = 10f;
            const float marginBottomPx = 10f;
            const float shiftUpPx = 100f;
            const float widthNorm = 0.198f;
            const float heightNorm = 0.259f;
            var width = Screen.width * widthNorm;
            var height = Screen.height * heightNorm;
            var x = Screen.width - marginRightPx - width - BottomRightClusterShiftLeftPx;
            var y = Screen.height - marginBottomPx - height - shiftUpPx;
            return new Rect(x, y, width, height);
        }

        public static Rect GetEquipmentSlotRect(int index, int slotCount)
        {
            var paperdoll = GetPaperdollRect();
            const float gapPx = 8f;
            var slotAreaTop = paperdoll.y;
            var slotAreaHeight = paperdoll.height;
            var gap = slotAreaHeight * 0.025f;
            var cellSize = (slotAreaHeight - gap * (slotCount - 1)) / slotCount;
            var x = paperdoll.x - gapPx - cellSize;
            var y = slotAreaTop + index * (cellSize + gap);
            return new Rect(x, y, cellSize, cellSize);
        }

        private static Rect NormalizedRect(float xNorm, float yNorm, float wNorm, float hNorm) =>
            new Rect(
                Screen.width * xNorm,
                Screen.height * yNorm,
                Screen.width * wNorm,
                Screen.height * hNorm);
    }
}
