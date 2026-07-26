using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterLoadoutLayout
    {
        private const int EquipmentSlotCount = 4;
        private const float InventoryCellInset = 4f;

        public static Rect GetNameBarRect()
        {
            const float marginLeftDesignPx = 10f;
            const float marginTopDesignPx = 10f;
            const float widthNorm = 0.22f;
            const float heightNorm = 0.05f;
            return new Rect(
                UiFitCanvas.Rect.x + UiFitCanvas.Px(marginLeftDesignPx),
                UiFitCanvas.Rect.y + UiFitCanvas.Px(marginTopDesignPx),
                UiFitCanvas.Rect.width * widthNorm,
                UiFitCanvas.Rect.height * heightNorm);
        }

        public static Rect GetPortraitRect()
        {
            const float marginLeftDesignPx = 10f;
            const float gapBelowNameDesignPx = 8f;
            const float widthNorm = 0.19f;
            const float heightNorm = 0.338f;
            var nameBar = GetNameBarRect();
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = UiFitCanvas.Rect.height * heightNorm;
            var x = UiFitCanvas.Rect.x + UiFitCanvas.Px(marginLeftDesignPx);
            var y = nameBar.yMax + UiFitCanvas.Px(gapBelowNameDesignPx);
            return new Rect(x, y, width, height);
        }

        public static Rect GetBasicLoadoutTitleRect()
        {
            const float gapBelowPortraitDesignPx = 25f;
            const float widthNorm = 0.36f;
            const float heightNorm = 0.06f;
            var portrait = GetPortraitRect();
            return new Rect(
                portrait.x,
                portrait.yMax + UiFitCanvas.Px(gapBelowPortraitDesignPx),
                UiFitCanvas.Rect.width * widthNorm,
                UiFitCanvas.Rect.height * heightNorm);
        }

        /// <summary>Loadout Boxes — copy of the four equipment slots under BASIC LOADOUT.</summary>
        public static Rect GetLoadoutBoxRect(int index)
        {
            index = Mathf.Clamp(index, 0, EquipmentSlotCount - 1);
            GetEquipmentColumnMetrics(out var cellSize, out var gap, out var topY);
            var title = GetBasicLoadoutTitleRect();
            var textWidth = CharacterPageStyles.FootlockerTitleStyle
                .CalcSize(new GUIContent("BASIC LOADOUT")).x;
            var textCenterX = title.x + textWidth * 0.5f;
            var x = textCenterX - cellSize * 0.5f;
            return new Rect(x, topY + index * (cellSize + gap), cellSize, cellSize);
        }

        public static Rect GetFootlockerTitleRect()
        {
            var grid = GetFootlockerGridRect();
            var left = CharacterPageGearUi.GetFootlockerCellsLeft(grid, topAlign: true);
            var width = UiFitCanvas.Rect.width * 0.36f;
            var height = UiFitCanvas.Rect.height * 0.06f;
            return new Rect(
                left,
                grid.y - height - UiFitCanvas.Px(4f),
                width,
                height);
        }

        public static Rect GetFootlockerGridRect()
        {
            const float cellScale = 1.21f;
            var baseRect = ShiftGearCluster(CharacterPageLayout.GetFootlockerGridRect());
            var width = baseRect.width * cellScale;
            var height = baseRect.height * cellScale;
            var portrait = GetPortraitRect();
            return new Rect(
                baseRect.center.x - width * 0.5f + UiFitCanvas.Px(50f),
                portrait.y,
                width,
                height);
        }

        public static Rect GetInventoryGridRect()
        {
            GetEquipmentCellSize(out var cellSize);
            var columns = LandGameConstants.InventoryGridColumns;
            var rows = LandGameConstants.InventoryGridRows;
            var cellPitch = cellSize + InventoryCellInset * 2f;
            var width = cellPitch * columns;
            var height = cellPitch * rows;
            var baseRect = ShiftBottomGearCluster(CharacterPageLayout.GetInventoryGridRect());
            return new Rect(
                baseRect.x + UiFitCanvas.Px(100f),
                baseRect.y,
                width,
                height);
        }

        public static Rect GetPaperdollRect()
        {
            const float sizeScale = 1.2f;
            var baseRect = ShiftBottomGearCluster(CharacterPageLayout.GetPaperdollRect());
            var width = baseRect.width * sizeScale;
            var height = baseRect.height * sizeScale;
            var bottomEquipment = GetEquipmentSlotRect(EquipmentSlotCount - 1);
            return new Rect(
                baseRect.center.x - width * 0.5f,
                bottomEquipment.yMax - height,
                width,
                height);
        }
        public static Rect GetEquipmentSlotRect(int index)
        {
            index = Mathf.Clamp(index, 0, EquipmentSlotCount - 1);
            GetEquipmentColumnMetrics(out var cellSize, out var gap, out var topY);
            var columnX = ShiftBottomGearCluster(
                    CharacterPageLayout.GetEquipmentSlotRect(0, EquipmentSlotCount)).x
                - UiFitCanvas.Px(100f);
            return new Rect(columnX, topY + index * (cellSize + gap), cellSize, cellSize);
        }

        public static float GetGearClusterShiftX()
        {
            var footlocker = CharacterPageLayout.GetFootlockerGridRect();
            var footlockerCellsLeft = CharacterPageGearUi.GetFootlockerCellsLeft(footlocker);
            var equipment = CharacterPageLayout.GetEquipmentSlotRect(0, EquipmentSlotCount);
            var paperdoll = CharacterPageLayout.GetPaperdollRect();
            var inventory = CharacterPageLayout.GetInventoryGridRect();

            var left = Mathf.Min(footlockerCellsLeft, equipment.x);
            var right = Mathf.Max(footlocker.xMax, Mathf.Max(paperdoll.xMax, inventory.xMax));
            var groupCenter = (left + right) * 0.5f;
            return UiFitCanvas.Rect.center.x - groupCenter;
        }

        private static void GetEquipmentCellSize(out float cellSize)
        {
            cellSize = CharacterPageLayout.GetEquipmentSlotRect(0, EquipmentSlotCount).width;
        }

        private static void GetEquipmentColumnMetrics(out float cellSize, out float gap, out float topY)
        {
            var slot0 = CharacterPageLayout.GetEquipmentSlotRect(0, EquipmentSlotCount);
            var slot1 = CharacterPageLayout.GetEquipmentSlotRect(1, EquipmentSlotCount);
            cellSize = slot0.width;
            gap = Mathf.Max(0f, slot1.y - slot0.yMax);
            topY = GetInventoryGridRect().y + InventoryCellInset;
        }

        private static Rect ShiftGearCluster(Rect rect)
        {
            var shiftX = GetGearClusterShiftX();
            return new Rect(rect.x + shiftX, rect.y, rect.width, rect.height);
        }

        private static Rect ShiftBottomGearCluster(Rect rect)
        {
            var shiftX = GetGearClusterShiftX();
            var shiftY = -UiFitCanvas.Rect.height * 0.25f + UiFitCanvas.Px(150f);
            return new Rect(rect.x + shiftX, rect.y + shiftY, rect.width, rect.height);
        }
    }
}
