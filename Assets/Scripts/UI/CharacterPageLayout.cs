using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageLayout
    {
        public static Rect GetNameBarRect()
        {
            const float marginLeftDesignPx = 10f;
            const float yNorm = 0.014f;
            const float widthNorm = 0.19f;
            const float heightNorm = 0.05f;
            return new Rect(
                UiFitCanvas.Rect.x + UiFitCanvas.Px(marginLeftDesignPx),
                UiFitCanvas.NormY(yNorm),
                UiFitCanvas.Rect.width * widthNorm,
                UiFitCanvas.Rect.height * heightNorm);
        }

        public static Rect GetRibbonsRect()
        {
            const float shiftDownDesignPx = 20f;
            var rect = NormalizedRect(0.016f, 0.065f, 0.334f, 0.278f);
            return new Rect(rect.x, rect.y + UiFitCanvas.Px(shiftDownDesignPx), rect.width, rect.height);
        }

        public static Rect GetRibbonSlotRect(Rect panelRect, int row, int column)
        {
            const int maxColumns = 3;
            const int maxRows = 4;
            const float sizeScale = 0.855f * 0.8f;

            var slotWidth = panelRect.width / maxColumns;
            var slotHeight = panelRect.height / maxRows;
            var ribbonWidth = slotWidth * sizeScale;
            var ribbonHeight = slotHeight * sizeScale;
            var x = panelRect.x + column * ribbonWidth;
            var y = panelRect.y + row * ribbonHeight;
            return new Rect(x, y, ribbonWidth, ribbonHeight);
        }

        private const float MissionScoreRowGapDesignPx = 22f;
        private const float MissionScoreShiftDownDesignPx = 20f;
        private const float MissionScoreLabelColumnWidthDesignPx = 520f;
        private const float MissionScoreOnesOffsetFromLabelDesignPx = 80f;
        private const int EquipmentSlotCount = 4;
        public const int MissionScoreRowCount = 4;

        public static float MissionScoreLabelColumnWidthPx =>
            UiFitCanvas.Px(MissionScoreLabelColumnWidthDesignPx);

        public static float MissionScoreValueShiftRightPx =>
            UiFitCanvas.Px(MissionScoreOnesOffsetFromLabelDesignPx);

        public static float GetMissionScoreOnesColumnX()
        {
            var row = GetScoreRowRect(0);
            return row.x;
        }

        public static Rect GetScoreRowRect(int index)
        {
            var portrait = GetPortraitRect();
            var equipmentLeft = GetEquipmentSlotRect(0, EquipmentSlotCount).x;
            var x = portrait.xMax;
            var width = Mathf.Max(1f, equipmentLeft - x);
            var height = UiFitCanvas.Rect.height * 0.065f;
            var gap = UiFitCanvas.Px(MissionScoreRowGapDesignPx);
            var y = portrait.y
                + UiFitCanvas.Px(MissionScoreShiftDownDesignPx)
                + index * (height + gap);
            return new Rect(x, y, width, height);
        }

        public static Rect GetTotalScoreLabelRect() => GetScoreRowRect(3);

        public static Rect GetBestScoreLabelRect() => GetScoreRowRect(2);

        private const float KillFolderHeightNorm = 0.4318272f;
        private const float KillFolderLeftNorm = 0.016f;
        private const float KillFolderGapDesignPx = 10f;
        private const float KillFolderShiftLeftDesignPx = 42f;
        private const float KillFolderRightShiftLeftDesignPx = 25f;
        private const float KillFolderShiftDownDesignPx = 45f;

        public const int KillFolderSlotColumns = 5;
        public const int KillFolderSlotRows = 2;
        public const int KillFolderSlotCount = KillFolderSlotColumns * KillFolderSlotRows;
        private const float KillFolderSlotSizeDesignPx = 72f;
        private const float KillFolderSlotGapDesignPx = 0f;
        private const float KillFolderSlotGridTopDesignPx = 245f;

        public static Rect GetKillFolderSlotRect(Rect folderRect, int index)
        {
            var col = index % KillFolderSlotColumns;
            var row = index / KillFolderSlotColumns;
            var slotSize = UiFitCanvas.Px(KillFolderSlotSizeDesignPx);
            var slotGap = UiFitCanvas.Px(KillFolderSlotGapDesignPx);
            var gridWidth = KillFolderSlotColumns * slotSize
                + (KillFolderSlotColumns - 1) * slotGap;
            var gridLeft = folderRect.x + (folderRect.width - gridWidth) * 0.5f;
            var gridTop = folderRect.y + UiFitCanvas.Px(KillFolderSlotGridTopDesignPx);
            return new Rect(
                gridLeft + col * (slotSize + slotGap),
                gridTop + row * (slotSize + slotGap),
                slotSize,
                slotSize);
        }

        public static Rect GetVehicleKillsRect() => GetKillFolderRect(0);

        public static Rect GetTroopKillsRect() => GetKillFolderRect(1);

        public static Rect GetVehicleKillsLabelRect()
        {
            const float labelOffsetDownDesignPx = 150f;
            const float labelOffsetRightDesignPx = 1f;
            var folder = GetVehicleKillsRect();
            var labelHeight = UiFitCanvas.Rect.height * 0.05f;
            return new Rect(
                folder.x + UiFitCanvas.Px(labelOffsetRightDesignPx),
                folder.y - labelHeight - UiFitCanvas.Px(4f) + UiFitCanvas.Px(labelOffsetDownDesignPx),
                folder.width,
                labelHeight);
        }

        public static Rect GetTroopKillsLabelRect()
        {
            const float gapFromVehicleLabelDesignPx = 15f;
            const float shiftLeftDesignPx = 30f;
            var vehicleLabel = GetVehicleKillsLabelRect();
            var folder = GetTroopKillsRect();
            return new Rect(
                vehicleLabel.xMax
                    + UiFitCanvas.Px(gapFromVehicleLabelDesignPx)
                    - UiFitCanvas.Px(shiftLeftDesignPx),
                vehicleLabel.y,
                folder.width,
                vehicleLabel.height);
        }

        private static Rect GetKillFolderRect(int index)
        {
            var height = UiFitCanvas.Rect.height * KillFolderHeightNorm;
            var width = height;
            var portrait = GetPortraitRect();
            var bottomY = portrait.y + UiFitCanvas.Px(KillFolderShiftDownDesignPx);
            var leftX = UiFitCanvas.NormX(KillFolderLeftNorm) - UiFitCanvas.Px(KillFolderShiftLeftDesignPx);
            var x = index == 0
                ? leftX
                : leftX + width + UiFitCanvas.Px(KillFolderGapDesignPx)
                    - UiFitCanvas.Px(KillFolderRightShiftLeftDesignPx);
            return new Rect(x, bottomY - height, width, height);
        }

        public static Rect GetFootlockerTitleRect()
        {
            var grid = GetFootlockerGridRect();
            // Boxes are right-packed inside the grid rect — pin title to the first cell, not the container.
            var left = CharacterPageGearUi.GetFootlockerCellsLeft(grid);
            var width = UiFitCanvas.Rect.width * 0.36f;
            return new Rect(
                left,
                UiFitCanvas.NormY(0.018f),
                width,
                UiFitCanvas.Rect.height * 0.06f);
        }

        public static Rect GetFootlockerGridRect()
        {
            const float marginRightDesignPx = 10f;
            const float yNorm = 0.069f;
            const float widthNorm = 0.675f;
            const float heightNorm = 0.27972f;
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = UiFitCanvas.Rect.height * heightNorm;
            var x = UiFitCanvas.Rect.xMax - UiFitCanvas.Px(marginRightDesignPx) - width;
            var y = UiFitCanvas.NormY(yNorm);
            return new Rect(x, y, width, height);
        }

        public const int ResearchAndDevelopmentSlotCount = 4;

        public static Rect GetResearchAndDevelopmentRect()
        {
            const float heightDesignPx = 200f;
            const float gapFromLoadoutDesignPx = 10f;
            var footlocker = GetFootlockerGridRect();
            var footlockerLeft = CharacterPageGearUi.GetFootlockerCellsLeft(footlocker);
            CharacterPageGearUi.GetFootlockerCellsBounds(
                footlocker,
                topAlign: false,
                out var cellsRight,
                out _);
            var width = (cellsRight - footlockerLeft) * 0.5f;
            var height = UiFitCanvas.Px(heightDesignPx);
            var loadout = GetCharacterLoadoutButtonRect();
            var x = loadout.x - UiFitCanvas.Px(gapFromLoadoutDesignPx) - width;
            return new Rect(x, loadout.y, width, height);
        }

        public static Rect GetResearchAndDevelopmentTitleRect()
        {
            const float gapFromBoxLeftDesignPx = 10f;
            const float titleWidthDesignPx = 72f;
            const float titleHeightDesignPx = 44f;
            var box = GetResearchAndDevelopmentRect();
            var width = UiFitCanvas.Px(titleWidthDesignPx);
            var height = UiFitCanvas.Px(titleHeightDesignPx);
            return new Rect(
                box.x - UiFitCanvas.Px(gapFromBoxLeftDesignPx) - width,
                box.y,
                width,
                height);
        }

        private static Rect GetSaveAntarcticaPanelRect()
        {
            const float heightDesignPx = 200f;
            const float gapFromFootlockerDesignPx = 16f;
            var footlocker = GetFootlockerGridRect();
            var footlockerLeft = CharacterPageGearUi.GetFootlockerCellsLeft(footlocker);
            var width = GetInventoryGridRect().width;
            var height = UiFitCanvas.Px(heightDesignPx);
            return new Rect(
                footlockerLeft - UiFitCanvas.Px(gapFromFootlockerDesignPx) - width,
                footlocker.y,
                width,
                height);
        }

        public static Rect GetResearchAndDevelopmentHintRect()
        {
            const float hintHeightDesignPx = 36f;
            const float padDesignPx = 12f;
            var panel = GetResearchAndDevelopmentRect();
            var pad = UiFitCanvas.Px(padDesignPx);
            return new Rect(
                panel.x + pad,
                panel.y + pad,
                panel.width - pad * 2f,
                UiFitCanvas.Px(hintHeightDesignPx));
        }

        public static LandEquipmentSlot GetResearchAndDevelopmentEquipmentSlot(int index)
        {
            return index switch
            {
                0 => LandEquipmentSlot.Helmet,
                1 => LandEquipmentSlot.Core,
                2 => LandEquipmentSlot.Weapon,
                3 => LandEquipmentSlot.Boots,
                _ => LandEquipmentSlot.Helmet
            };
        }

        public static bool TryGetResearchAndDevelopmentSlotIndex(LandEquipmentSlot itemSlot, out int researchSlotIndex)
        {
            for (var i = 0; i < ResearchAndDevelopmentSlotCount; i++)
            {
                if (LandGearEquipRules.CanEquip(itemSlot, GetResearchAndDevelopmentEquipmentSlot(i)))
                {
                    researchSlotIndex = i;
                    return true;
                }
            }

            researchSlotIndex = -1;
            return false;
        }

        public static Rect GetResearchAndDevelopmentFooterRect()
        {
            const float padDesignPx = 12f;
            const float footerHeightDesignPx = 48f;
            var panel = GetResearchAndDevelopmentRect();
            var pad = UiFitCanvas.Px(padDesignPx);
            var height = UiFitCanvas.Px(footerHeightDesignPx);
            return new Rect(
                panel.x + pad,
                panel.yMax - pad - height,
                panel.width - pad * 2f,
                height);
        }

        /// <summary>Four R&amp;D drop slots in one row inside the panel.</summary>
        public static Rect GetResearchAndDevelopmentSlotRect(int index)
        {
            index = Mathf.Clamp(index, 0, ResearchAndDevelopmentSlotCount - 1);
            var panel = GetResearchAndDevelopmentRect();
            var hint = GetResearchAndDevelopmentHintRect();
            var footer = GetResearchAndDevelopmentFooterRect();
            const float padDesignPx = 12f;
            const float slotGapDesignPx = 4f;
            var pad = UiFitCanvas.Px(padDesignPx);
            var gap = UiFitCanvas.Px(slotGapDesignPx);
            var rowTop = hint.yMax + UiFitCanvas.Px(4f);
            var rowBottom = footer.y - UiFitCanvas.Px(4f);
            var rowHeight = Mathf.Max(1f, rowBottom - rowTop);
            var rowWidth = panel.width - pad * 2f;
            var cellWidth = (rowWidth - gap * (ResearchAndDevelopmentSlotCount - 1))
                / ResearchAndDevelopmentSlotCount;
            var x = panel.x + pad + index * (cellWidth + gap);
            return new Rect(x, rowTop, cellWidth, Mathf.Min(cellWidth, rowHeight));
        }

        public static Rect GetPortraitRect()
        {
            const float marginLeftNorm = 0.016f;
            const float marginBottomDesignPx = 10f;
            const float shiftLeftDesignPx = 26f;
            const float shiftUpDesignPx = 16f;
            const float widthNorm = 0.19f;
            const float heightNorm = 0.338f;
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = UiFitCanvas.Rect.height * heightNorm;
            var x = UiFitCanvas.NormX(marginLeftNorm) - UiFitCanvas.Px(shiftLeftDesignPx);
            var y = UiFitCanvas.Rect.yMax
                - UiFitCanvas.Px(marginBottomDesignPx)
                - height
                - UiFitCanvas.Px(shiftUpDesignPx);
            return new Rect(x, y, width, height);
        }

        private const float BottomRightClusterShiftLeftDesignPx = 100f;

        public static Rect GetSaveAntarcticaLogoRect(Texture2D logoTexture)
        {
            const float padDesignPx = 10f;
            var panel = GetSaveAntarcticaPanelRect();
            var pad = UiFitCanvas.Px(padDesignPx);
            var inner = new Rect(
                panel.x + pad,
                panel.y + pad,
                panel.width - pad * 2f,
                panel.height - pad * 2f);
            var aspect = logoTexture != null && logoTexture.width > 0
                ? (float)logoTexture.height / logoTexture.width
                : 0.34f;
            var fitWidth = inner.width;
            var fitHeight = fitWidth * aspect;
            if (fitHeight > inner.height)
            {
                fitHeight = inner.height;
                fitWidth = fitHeight / aspect;
            }

            var x = inner.x + (inner.width - fitWidth) * 0.5f;
            var y = inner.y + (inner.height - fitHeight) * 0.5f;
            return new Rect(x, y, fitWidth, fitHeight);
        }

        public static Rect GetPaperdollRect()
        {
            const float gapDesignPx = 10f;
            const float marginRightDesignPx = 10f;
            const float marginBottomDesignPx = 10f;
            const float widthNorm = 0.104f;
            const float heightNorm = 0.417f;
            const float inventoryWidthNorm = 0.198f;
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = UiFitCanvas.Rect.height * heightNorm;
            var inventoryWidth = UiFitCanvas.Rect.width * inventoryWidthNorm;
            var inventoryX = UiFitCanvas.Rect.xMax
                - UiFitCanvas.Px(marginRightDesignPx)
                - inventoryWidth
                - UiFitCanvas.Px(BottomRightClusterShiftLeftDesignPx);
            var x = inventoryX - UiFitCanvas.Px(gapDesignPx) - width;
            var y = UiFitCanvas.Rect.yMax - UiFitCanvas.Px(marginBottomDesignPx) - height;
            return new Rect(x, y, width, height);
        }

        public static Rect GetInventoryGridRect()
        {
            const float marginRightDesignPx = 10f;
            const float marginBottomDesignPx = 10f;
            const float shiftUpDesignPx = 100f;
            const float widthNorm = 0.198f;
            const float heightNorm = 0.259f;
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = UiFitCanvas.Rect.height * heightNorm;
            var x = UiFitCanvas.Rect.xMax
                - UiFitCanvas.Px(marginRightDesignPx)
                - width
                - UiFitCanvas.Px(BottomRightClusterShiftLeftDesignPx);
            var y = UiFitCanvas.Rect.yMax
                - UiFitCanvas.Px(marginBottomDesignPx)
                - height
                - UiFitCanvas.Px(shiftUpDesignPx);
            return new Rect(x, y, width, height);
        }

        public static Rect GetMissionBriefButtonRect()
        {
            const float marginRightDesignPx = 18f;
            const float marginBottomDesignPx = 18f;
            StartPageMenuStyles.GetMenuButtonSize(out var width, out var height);
            // Slightly narrower than main-menu buttons so it fits the CP cluster.
            width *= 0.72f;
            var x = UiFitCanvas.Rect.xMax - UiFitCanvas.Px(marginRightDesignPx) - width;
            var y = UiFitCanvas.Rect.yMax - UiFitCanvas.Px(marginBottomDesignPx) - height;
            return new Rect(x, y, width, height);
        }

        public static Rect GetCharacterLoadoutButtonRect()
        {
            const float gapBelowFootlockerDesignPx = 10f;
            var footlocker = GetFootlockerGridRect();
            CharacterPageGearUi.GetFootlockerCellsBounds(
                footlocker,
                topAlign: false,
                out var cellsRight,
                out var cellsBottom);
            StartPageMenuStyles.GetMenuButtonSize(out var width, out var height);
            // Match Mission Brief button sizing.
            width *= 0.72f;
            return new Rect(
                cellsRight - width,
                cellsBottom + UiFitCanvas.Px(gapBelowFootlockerDesignPx),
                width,
                height);
        }

        public static Rect GetEquipmentSlotRect(int index, int slotCount)
        {
            var paperdoll = GetPaperdollRect();
            const float gapDesignPx = 8f;
            var slotAreaTop = paperdoll.y;
            var slotAreaHeight = paperdoll.height;
            var gap = slotAreaHeight * 0.025f;
            var cellSize = (slotAreaHeight - gap * (slotCount - 1)) / slotCount;
            var x = paperdoll.x - UiFitCanvas.Px(gapDesignPx) - cellSize;
            var y = slotAreaTop + index * (cellSize + gap);
            return new Rect(x, y, cellSize, cellSize);
        }

        private static Rect NormalizedRect(float xNorm, float yNorm, float wNorm, float hNorm) =>
            UiFitCanvas.NormRect(xNorm, yNorm, wNorm, hNorm);
    }
}
