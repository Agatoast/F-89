using System.Collections.Generic;
using F89.Core;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageGearUi
    {
        private const int EquipmentItemNameFontSize = 8;
        private const int GridItemNameFontSize = 9;
        private const float DragClickThreshold = 4f;
        private const float InventoryCellInset = 4f;

        private static readonly Color FootlockerSlotColor = new Color(0.18f, 0.52f, 0.24f, 0.92f);
        private static readonly Color GearSlotFill = new Color(0.12f, 0.2f, 0.3f, 0.88f);
        private static readonly Color GearSlotBorder = new Color(0.55f, 0.78f, 0.95f, 0.95f);

        private enum DragSourceKind
        {
            None,
            BasicTray,
            Equipment,
            Inventory,
            Vault
        }

        private struct PendingResearchOffer
        {
            public DragSourceKind Source;
            public int Index;
            public int ResearchSlotIndex;
        }

        private static LandEquipmentSlot? selectedEquipmentSlot;
        private static readonly HashSet<int> selectedInventoryIndices = new HashSet<int>();
        private static readonly HashSet<int> selectedVaultIndices = new HashSet<int>();
        private static bool ctrlGridSelectionActive;

        private static bool isDragging;
        private static DragSourceKind dragSource;
        private static LandGearInstance dragItem;
        private static LandEquipmentSlot dragEquipmentSlot;
        private static int dragIndex;
        private static readonly List<int> dragIndices = new List<int>(8);
        private static float dragCellSize;
        private static Vector2 dragStartGui;
        private static bool dragMoved;

        private static bool pendingResearchConfirm;
        private static bool pendingResearchTechTooLow;
        private static bool pendingResearchWrongCategory;
        private static bool pendingBasicLoadoutSlotOccupied;
        private static readonly List<PendingResearchOffer> pendingResearchOffers = new List<PendingResearchOffer>(8);

        private static bool pendingDeleteConfirm;
        private static DragSourceKind pendingDeleteSource;
        private static int pendingDeleteIndex = -1;

        private static readonly (LandEquipmentSlot slot, string label)[] EquipmentSlots =
        {
            (LandEquipmentSlot.Helmet, "Helmet"),
            (LandEquipmentSlot.Core, "Vest"),
            (LandEquipmentSlot.Weapon, "Weapon"),
            (LandEquipmentSlot.Boots, "Boots")
        };

        private static readonly (LandEquipmentSlot slot, string fallbackDefinitionId)[] BasicTraySlots =
        {
            (LandEquipmentSlot.Helmet, LandUsGearCatalog.BasicHelmetId),
            (LandEquipmentSlot.Core, LandUsGearCatalog.BasicVestId),
            (LandEquipmentSlot.Weapon, LandUsWeaponCatalog.BasicLoadoutDefinitionId),
            (LandEquipmentSlot.Boots, LandUsGearCatalog.BasicBootsId)
        };

        public static void DrawFootlocker(Rect gridRect, bool topAlign = false)
        {
            var vault = CharacterGearSession.ActiveVault;
            var slotCount = vault != null
                ? LandVaultStorageService.GetVaultSlotCount(vault)
                : LandGameConstants.VaultBaseSlotCount;

            if (vault != null)
            {
                LandVaultStorageService.EnsureVaultSize(vault);
            }

            GetFootlockerCellLayout(gridRect, slotCount, topAlign, out var originX, out var originY, out var cellSize, out var gap);
            var columns = LandGameConstants.VaultGridColumns;

            for (var i = 0; i < slotCount; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var cell = new Rect(
                    originX + col * (cellSize + gap),
                    originY + row * (cellSize + gap),
                    cellSize,
                    cellSize);
                DrawFootlockerCell(cell, i, vault);
            }
        }

        public static float GetFootlockerCellsLeft(Rect gridRect, bool topAlign = false)
        {
            var vault = CharacterGearSession.ActiveVault;
            var slotCount = vault != null
                ? LandVaultStorageService.GetVaultSlotCount(vault)
                : LandGameConstants.VaultBaseSlotCount;
            GetFootlockerCellLayout(gridRect, slotCount, topAlign, out var originX, out _, out _, out _);
            return originX;
        }

        public static void GetFootlockerCellsBounds(
            Rect gridRect,
            bool topAlign,
            out float cellsRight,
            out float cellsBottom)
        {
            var vault = CharacterGearSession.ActiveVault;
            var slotCount = vault != null
                ? LandVaultStorageService.GetVaultSlotCount(vault)
                : LandGameConstants.VaultBaseSlotCount;
            GetFootlockerCellLayout(gridRect, slotCount, topAlign, out var originX, out var originY, out var cellSize, out var gap);
            var columns = LandGameConstants.VaultGridColumns;
            var rows = Mathf.CeilToInt(slotCount / (float)columns);
            var gridWidth = cellSize * columns + gap * (columns - 1);
            var gridHeight = cellSize * rows + gap * (rows - 1);
            cellsRight = originX + gridWidth;
            cellsBottom = originY + gridHeight;
        }

        private static void GetFootlockerCellLayout(
            Rect gridRect,
            int slotCount,
            bool topAlign,
            out float originX,
            out float originY,
            out float cellSize,
            out float gap)
        {
            var columns = LandGameConstants.VaultGridColumns;
            var rows = Mathf.CeilToInt(slotCount / (float)columns);
            gap = 6f;
            cellSize = Mathf.Max(
                1f,
                Mathf.Min(
                    (gridRect.width - gap * (columns - 1)) / columns,
                    (gridRect.height - gap * (rows - 1)) / rows));
            var gridWidth = cellSize * columns + gap * (columns - 1);
            var gridHeight = cellSize * rows + gap * (rows - 1);
            originX = gridRect.xMax - gridWidth;
            originY = topAlign
                ? gridRect.y
                : gridRect.y + (gridRect.height - gridHeight) * 0.5f;
        }

        public static bool TryGetFootlockerCellRect(
            Rect gridRect,
            int index,
            bool topAlign,
            out Rect cell)
        {
            cell = default;
            var vault = CharacterGearSession.ActiveVault;
            var slotCount = vault != null
                ? LandVaultStorageService.GetVaultSlotCount(vault)
                : LandGameConstants.VaultBaseSlotCount;
            if (index < 0 || index >= slotCount)
            {
                return false;
            }

            GetFootlockerCellLayout(gridRect, slotCount, topAlign, out var originX, out var originY, out var cellSize, out var gap);
            var columns = LandGameConstants.VaultGridColumns;
            var col = index % columns;
            var row = index / columns;
            cell = new Rect(
                originX + col * (cellSize + gap),
                originY + row * (cellSize + gap),
                cellSize,
                cellSize);
            return true;
        }

        public static bool TryGetInventoryCellRect(Rect gridRect, int index, out Rect cell)
        {
            cell = default;
            var columns = LandGameConstants.InventoryGridColumns;
            var rows = LandGameConstants.InventoryGridRows;
            if (index < 0 || index >= LandGameConstants.PackSlotCount)
            {
                return false;
            }

            var col = index % columns;
            var row = index / columns;
            if (row >= rows)
            {
                return false;
            }

            var cellWidth = gridRect.width / columns;
            var cellHeight = gridRect.height / rows;
            cell = new Rect(
                gridRect.x + col * cellWidth + InventoryCellInset,
                gridRect.y + row * cellHeight + InventoryCellInset,
                cellWidth - InventoryCellInset * 2f,
                cellHeight - InventoryCellInset * 2f);
            return true;
        }

        public static void DrawEquipmentSlots()
        {
            DrawEquipmentSlots(null);
        }

        public static void DrawEquipmentSlots(System.Func<int, Rect> getSlotRect)
        {
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var entry = EquipmentSlots[i];
                var cell = getSlotRect != null
                    ? getSlotRect(i)
                    : CharacterPageLayout.GetEquipmentSlotRect(i, EquipmentSlots.Length);
                DrawEquipmentCell(cell, entry.slot);
            }
        }

        public static void DrawBasicLoadoutBoxes(System.Func<int, Rect> getSlotRect)
        {
            for (var i = 0; i < BasicTraySlots.Length; i++)
            {
                var cell = getSlotRect != null
                    ? getSlotRect(i)
                    : CharacterPageLayout.GetEquipmentSlotRect(i, BasicTraySlots.Length);
                var hide = isDragging && dragMoved && dragSource == DragSourceKind.BasicTray && dragIndex == i;
                if (hide)
                {
                    DrawTechTile(cell, null, selected: false);
                }
                else
                {
                    var item = GetBasicTrayItem(i);
                    DrawTechTile(cell, item, selected: false);
                    LandItemTooltipUi.RegisterHover(cell, item);
                }
            }
        }

        public static void DrawResearchAndDevelopmentSlots()
        {
            var labels = new[] { "Helmet", "Vest", "Weapon", "Boots" };
            var save = CharacterGearSession.ActiveSave;
            LandResearchService.EnsureSlotTechLevels(save);
            var prefixStyle = CharacterPageStyles.ResearchSlotLabelStyle;
            var valueStyle = CharacterPageStyles.ResearchSlotTlValueStyle;
            const string tlPrefix = "TL: ";

            for (var i = 0; i < CharacterPageLayout.ResearchAndDevelopmentSlotCount; i++)
            {
                var cell = CharacterPageLayout.GetResearchAndDevelopmentSlotRect(i);
                DrawFilledBox(cell, GearSlotFill, GearSlotBorder, 1.5f);
                var topPad = UiFitCanvas.Px(3f);
                var labelRect = new Rect(cell.x, cell.y + topPad, cell.width, cell.height - topPad);
                GUI.Label(labelRect, labels[i], prefixStyle);

                var levelText = LandResearchService.GetSlotTechLevel(save, i).ToString();
                var prefixSize = prefixStyle.CalcSize(new GUIContent(tlPrefix));
                var valueSize = valueStyle.CalcSize(new GUIContent(levelText));
                var lineHeight = Mathf.Max(prefixSize.y, valueSize.y);
                var totalWidth = prefixSize.x + valueSize.x;
                var lineY = cell.y + topPad + prefixStyle.CalcSize(new GUIContent(labels[i])).y + UiFitCanvas.Px(2f);
                var lineX = cell.x + (cell.width - totalWidth) * 0.5f;
                GUI.Label(new Rect(lineX, lineY, prefixSize.x, lineHeight), tlPrefix, prefixStyle);
                GUI.Label(new Rect(lineX + prefixSize.x, lineY, valueSize.x, lineHeight), levelText, valueStyle);

                var chance = LandResearchService.GetChancePercent(save, i);
                var chanceText = FormatResearchChancePercent(chance);
                var chanceRect = new Rect(
                    cell.x + UiFitCanvas.Px(2f),
                    cell.y + UiFitCanvas.Px(15f),
                    cell.width,
                    cell.height);
                GUI.Label(chanceRect, chanceText, CharacterPageStyles.ResearchSlotChanceStyle);
            }
        }

        private static string FormatResearchChancePercent(float chancePercent)
        {
            return Mathf.Approximately(chancePercent % 1f, 0f)
                ? $"{chancePercent:0}%"
                : $"{chancePercent:0.0}%";
        }

        public static void HandleGearDragAndDrop(
            System.Func<int, Rect> getEquipmentRect,
            Rect inventoryGridRect,
            Rect footlockerGridRect,
            bool footlockerTopAlign,
            System.Func<int, Rect> getBasicRect = null,
            bool allowResearchDrop = false)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type == EventType.Used)
            {
                return;
            }

            if (pendingResearchConfirm
                || pendingResearchTechTooLow
                || pendingResearchWrongCategory
                || pendingBasicLoadoutSlotOccupied
                || pendingDeleteConfirm)
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                if (isDragging)
                {
                    ResetDragState();
                    currentEvent.Use();
                    return;
                }

                if (TryPromptDeleteAtMousePosition(
                        inventoryGridRect,
                        footlockerGridRect,
                        footlockerTopAlign,
                        currentEvent.mousePosition))
                {
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (getBasicRect != null && TryBeginBasicTrayDrag(getBasicRect, currentEvent.mousePosition))
                {
                    currentEvent.Use();
                    return;
                }

                if (TryBeginEquipmentDrag(getEquipmentRect, currentEvent.mousePosition)
                    || TryBeginInventoryDrag(inventoryGridRect, currentEvent.mousePosition)
                    || TryBeginVaultDrag(footlockerGridRect, footlockerTopAlign, currentEvent.mousePosition))
                {
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDrag && isDragging)
            {
                if (Vector2.Distance(currentEvent.mousePosition, dragStartGui) >= DragClickThreshold)
                {
                    dragMoved = true;
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && isDragging)
            {
                if (dragMoved)
                {
                    TryDropDraggedItem(
                        getEquipmentRect,
                        inventoryGridRect,
                        footlockerGridRect,
                        footlockerTopAlign,
                        currentEvent.mousePosition,
                        allowResearchDrop);
                }
                else
                {
                    HandleClickWithoutDrag();
                }

                ResetDragState();
                currentEvent.Use();
            }
        }

        public static bool IsDraggingGear => isDragging;
        public static bool IsResearchConfirmPending => pendingResearchConfirm;
        public static bool IsDeleteConfirmPending => pendingDeleteConfirm;
        public static bool IsResearchTechTooLowPending => pendingResearchTechTooLow;
        public static bool IsResearchWrongCategoryPending => pendingResearchWrongCategory;
        public static bool IsBasicLoadoutSlotOccupiedPending => pendingBasicLoadoutSlotOccupied;

        public static void CancelResearchConfirm()
        {
            pendingResearchConfirm = false;
            pendingResearchTechTooLow = false;
            pendingResearchWrongCategory = false;
            pendingBasicLoadoutSlotOccupied = false;
            pendingResearchOffers.Clear();
        }

        public static void AcknowledgeResearchTechTooLow()
        {
            CancelResearchConfirm();
        }

        public static void AcknowledgeResearchWrongCategory()
        {
            CancelResearchConfirm();
        }

        public static void AcknowledgeBasicLoadoutSlotOccupied()
        {
            pendingBasicLoadoutSlotOccupied = false;
        }

        public static bool ConfirmDeleteItem()
        {
            if (!pendingDeleteConfirm)
            {
                return false;
            }

            if (!TryDeletePendingItem())
            {
                CancelDeleteConfirm();
                return false;
            }

            CancelDeleteConfirm();
            CharacterGearSession.PersistActive();
            return true;
        }

        public static void CancelDeleteConfirm()
        {
            pendingDeleteConfirm = false;
            pendingDeleteSource = DragSourceKind.None;
            pendingDeleteIndex = -1;
        }

        public static bool ConfirmResearchDestroy()
        {
            if (!pendingResearchConfirm)
            {
                return false;
            }

            if (!TryDestroyPendingResearchItems())
            {
                CancelResearchConfirm();
                return false;
            }

            var save = CharacterGearSession.ActiveSave;
            for (var i = 0; i < pendingResearchOffers.Count; i++)
            {
                LandResearchService.AddResearchProgress(save, pendingResearchOffers[i].ResearchSlotIndex);
            }

            CharacterGearSession.PersistActive();
            ClearGridSelection();
            CancelResearchConfirm();
            return true;
        }

        public static int PendingResearchOfferCount => pendingResearchOffers.Count;

        public static void DrawDragOverlay()
        {
            if (!isDragging || !LandLoadoutSlots.IsValidItem(dragItem) || !dragMoved)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent != null
                && currentEvent.type != EventType.Repaint
                && currentEvent.type != EventType.MouseDrag)
            {
                return;
            }

            var mouse = currentEvent != null ? currentEvent.mousePosition : Event.current.mousePosition;
            var size = Mathf.Max(24f, dragCellSize);
            var cell = new Rect(mouse.x - size * 0.5f, mouse.y - size * 0.5f, size, size);
            DrawTechTile(cell, dragItem, selected: true);
            if (dragIndices.Count > 1)
            {
                var badgeStyle = HudStyleFactory.CreateLabel(
                    12,
                    FontStyle.Bold,
                    TextAnchor.LowerRight,
                    Color.white,
                    wordWrap: false);
                GUI.Label(cell, dragIndices.Count.ToString(), badgeStyle);
            }
        }

        public static void ResetDragState()
        {
            isDragging = false;
            dragSource = DragSourceKind.None;
            dragItem = null;
            dragIndex = -1;
            dragIndices.Clear();
            dragCellSize = 0f;
            dragMoved = false;
        }

        public static LandEquipmentSlot GetEquipmentSlotAt(int index) =>
            EquipmentSlots[Mathf.Clamp(index, 0, EquipmentSlots.Length - 1)].slot;

        public static string GetEquipmentSlotLabel(int index) =>
            EquipmentSlots[Mathf.Clamp(index, 0, EquipmentSlots.Length - 1)].label;

        /// <summary>
        /// True when one or more paperdoll equipment spots are empty.
        /// <paramref name="missingTypesCsv"/> lists display names separated by ", ".
        /// </summary>
        public static bool TryGetMissingEquipmentTypes(out string missingTypesCsv)
        {
            missingTypesCsv = string.Empty;
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout == null)
            {
                missingTypesCsv = string.Join(", ", GetAllEquipmentTypeLabels());
                return true;
            }

            var missing = new System.Collections.Generic.List<string>(EquipmentSlots.Length);
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var slot = EquipmentSlots[i].slot;
                var equipped = LandLoadoutSlots.GetEquipped(loadout, slot);
                if (!LandLoadoutSlots.IsValidItem(equipped))
                {
                    missing.Add(EquipmentSlots[i].label);
                }
            }

            if (missing.Count == 0)
            {
                return false;
            }

            missingTypesCsv = string.Join(", ", missing);
            return true;
        }

        private static string[] GetAllEquipmentTypeLabels()
        {
            var labels = new string[EquipmentSlots.Length];
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                labels[i] = EquipmentSlots[i].label;
            }

            return labels;
        }

        public static void DrawInventory(Rect gridRect)
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout == null)
            {
                return;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);

            for (var i = 0; i < LandGameConstants.PackSlotCount; i++)
            {
                if (!TryGetInventoryCellRect(gridRect, i, out var cell))
                {
                    break;
                }

                DrawInventoryCell(cell, i, i < accessible);
            }
        }

        private static LandGearInstance GetBasicTrayItem(int index)
        {
            index = Mathf.Clamp(index, 0, BasicTraySlots.Length - 1);
            var entry = BasicTraySlots[index];
            var definitionId = entry.fallbackDefinitionId;
            var save = CharacterGearSession.ActiveSave;
            var unlockedId = LandResearchBreakthroughService.GetBasicLoadoutDefinitionId(save, entry.slot);
            if (!string.IsNullOrEmpty(unlockedId))
            {
                definitionId = unlockedId;
            }

            var rarity = LandItemRarity.White;
            var catalog = CharacterGearSession.Catalog;
            if (catalog != null && catalog.TryGetWeapon(definitionId, out var weapon))
            {
                rarity = weapon.Rarity;
            }
            else if (catalog != null && catalog.TryGetGear(definitionId, out var gear))
            {
                rarity = gear.Rarity;
            }

            return new LandGearInstance
            {
                DefinitionId = definitionId,
                Rarity = rarity
            };
        }

        private static void BeginDrag(
            DragSourceKind source,
            LandGearInstance item,
            float cellSize,
            Vector2 mousePosition,
            LandEquipmentSlot equipmentSlot = LandEquipmentSlot.Helmet,
            int index = -1)
        {
            isDragging = true;
            dragSource = source;
            dragItem = LandLoadoutEquipService.CloneItem(item);
            dragEquipmentSlot = equipmentSlot;
            dragIndex = index;
            dragCellSize = cellSize;
            dragStartGui = mousePosition;
            dragMoved = false;
            selectedEquipmentSlot = null;

            dragIndices.Clear();
            dragIndices.Add(index);
            if (source == DragSourceKind.Inventory
                && selectedInventoryIndices.Contains(index)
                && selectedInventoryIndices.Count > 1)
            {
                dragIndices.Clear();
                dragIndices.AddRange(selectedInventoryIndices);
            }
            else if (source == DragSourceKind.Vault
                     && selectedVaultIndices.Contains(index)
                     && selectedVaultIndices.Count > 1)
            {
                dragIndices.Clear();
                dragIndices.AddRange(selectedVaultIndices);
            }
        }

        private static bool IsControlHeld()
        {
            var currentEvent = Event.current;
            return currentEvent != null && currentEvent.control;
        }

        private static void ClearGridSelection()
        {
            selectedInventoryIndices.Clear();
            selectedVaultIndices.Clear();
            ctrlGridSelectionActive = false;
        }

        private static bool IsCtrlGridSelected(bool inventory, int index) =>
            ctrlGridSelectionActive
            && (inventory ? selectedInventoryIndices : selectedVaultIndices).Contains(index);

        private static Color InvertRgb(Color color) =>
            new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);

        private static bool TryGetGridItem(DragSourceKind source, int index, out LandGearInstance item)
        {
            item = null;
            if (source == DragSourceKind.Inventory)
            {
                var loadout = CharacterGearSession.ActiveLoadout;
                if (loadout == null
                    || index < 0
                    || index >= loadout.Inventory.Count)
                {
                    return false;
                }

                item = loadout.Inventory[index];
                return LandLoadoutSlots.IsValidItem(item);
            }

            if (source == DragSourceKind.Vault)
            {
                var vault = CharacterGearSession.ActiveVault;
                if (vault == null || !LandVaultStorageService.IsVaultIndexValid(vault, index))
                {
                    return false;
                }

                item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[index]);
                return LandLoadoutSlots.IsValidItem(item);
            }

            return false;
        }

        private static bool TryBeginBasicTrayDrag(System.Func<int, Rect> getBasicRect, Vector2 mousePosition)
        {
            for (var i = 0; i < BasicTraySlots.Length; i++)
            {
                var cell = getBasicRect(i);
                if (!cell.Contains(mousePosition))
                {
                    continue;
                }

                BeginDrag(DragSourceKind.BasicTray, GetBasicTrayItem(i), cell.width, mousePosition, index: i);
                return true;
            }

            return false;
        }

        private static bool TryBeginEquipmentDrag(System.Func<int, Rect> getEquipmentRect, Vector2 mousePosition)
        {
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var cell = getEquipmentRect != null
                    ? getEquipmentRect(i)
                    : CharacterPageLayout.GetEquipmentSlotRect(i, EquipmentSlots.Length);
                if (!cell.Contains(mousePosition))
                {
                    continue;
                }

                var slot = EquipmentSlots[i].slot;
                var item = LandLoadoutSlots.GetEquipped(CharacterGearSession.ActiveLoadout, slot);
                if (!LandLoadoutSlots.IsValidItem(item))
                {
                    return false;
                }

                BeginDrag(DragSourceKind.Equipment, item, cell.width, mousePosition, slot, i);
                return true;
            }

            return false;
        }

        private static bool TryBeginInventoryDrag(Rect inventoryGridRect, Vector2 mousePosition)
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout == null)
            {
                return false;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);
            for (var i = 0; i < accessible && i < LandGameConstants.PackSlotCount; i++)
            {
                if (!TryGetInventoryCellRect(inventoryGridRect, i, out var cell) || !cell.Contains(mousePosition))
                {
                    continue;
                }

                var item = loadout.Inventory[i];
                if (!LandLoadoutSlots.IsValidItem(item))
                {
                    return false;
                }

                BeginDrag(DragSourceKind.Inventory, item, cell.width, mousePosition, index: i);
                return true;
            }

            return false;
        }

        private static bool TryBeginVaultDrag(Rect footlockerGridRect, bool topAlign, Vector2 mousePosition)
        {
            var vault = CharacterGearSession.ActiveVault;
            if (vault == null)
            {
                return false;
            }

            LandVaultStorageService.EnsureVaultSize(vault);
            var slotCount = LandVaultStorageService.GetVaultSlotCount(vault);
            for (var i = 0; i < slotCount; i++)
            {
                if (!TryGetFootlockerCellRect(footlockerGridRect, i, topAlign, out var cell)
                    || !cell.Contains(mousePosition))
                {
                    continue;
                }

                var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[i]);
                if (!LandLoadoutSlots.IsValidItem(item))
                {
                    return false;
                }

                BeginDrag(DragSourceKind.Vault, item, cell.width, mousePosition, index: i);
                return true;
            }

            return false;
        }

        private static void HandleClickWithoutDrag()
        {
            switch (dragSource)
            {
                case DragSourceKind.Equipment:
                    HandleEquipmentClick(dragEquipmentSlot);
                    break;
                case DragSourceKind.Inventory:
                    HandleInventoryClick(dragIndex);
                    break;
                case DragSourceKind.Vault:
                    HandleVaultClick(dragIndex);
                    break;
            }
        }

        private static bool TryDropDraggedItem(
            System.Func<int, Rect> getEquipmentRect,
            Rect inventoryGridRect,
            Rect footlockerGridRect,
            bool footlockerTopAlign,
            Vector2 mousePosition,
            bool allowResearchDrop)
        {
            if (!LandLoadoutSlots.IsValidItem(dragItem))
            {
                return false;
            }

            if (allowResearchDrop && TryDropOnResearch(mousePosition))
            {
                return true;
            }

            if (TryDropOnEquipment(getEquipmentRect, mousePosition)
                || TryDropOnInventory(inventoryGridRect, mousePosition)
                || TryDropOnVault(footlockerGridRect, footlockerTopAlign, mousePosition))
            {
                // Dialog-only outcomes (e.g. Basic Loadout into occupied slot) must not persist.
                if (!pendingBasicLoadoutSlotOccupied
                    && !pendingResearchConfirm
                    && !pendingResearchTechTooLow
                    && !pendingResearchWrongCategory
                    && !pendingDeleteConfirm)
                {
                    CharacterGearSession.PersistActive();
                }

                return true;
            }

            return false;
        }

        private static bool TryDropOnResearch(Vector2 mousePosition)
        {
            if (dragSource == DragSourceKind.BasicTray || dragSource == DragSourceKind.None)
            {
                return false;
            }

            if (!CharacterPageLayout.GetResearchAndDevelopmentRect().Contains(mousePosition))
            {
                return false;
            }

            var offers = new List<PendingResearchOffer>(dragIndices.Count);
            if (dragSource == DragSourceKind.Equipment)
            {
                if (LandLoadoutSlots.IsValidItem(dragItem)
                    && TryBuildResearchOffer(
                        dragSource,
                        (int)dragEquipmentSlot,
                        dragItem,
                        out var equipmentOffer))
                {
                    offers.Add(equipmentOffer);
                }
            }
            else
            {
                for (var i = 0; i < dragIndices.Count; i++)
                {
                    var index = dragIndices[i];
                    if (!TryGetGridItem(dragSource, index, out var item))
                    {
                        continue;
                    }

                    if (!TryBuildResearchOffer(dragSource, index, item, out var offer))
                    {
                        if (pendingResearchWrongCategory || pendingResearchTechTooLow)
                        {
                            return true;
                        }

                        continue;
                    }

                    offers.Add(offer);
                }
            }

            if (offers.Count == 0)
            {
                return false;
            }

            pendingResearchOffers.Clear();
            pendingResearchOffers.AddRange(offers);
            pendingResearchConfirm = true;
            return true;
        }

        private static bool TryBuildResearchOffer(
            DragSourceKind source,
            int index,
            LandGearInstance item,
            out PendingResearchOffer offer)
        {
            offer = default;
            if (!LandLoadoutEquipService.TryResolveItemSlot(
                    item,
                    CharacterGearSession.Catalog,
                    out var itemSlot))
            {
                return false;
            }

            if (!CharacterPageLayout.TryGetResearchAndDevelopmentSlotIndex(itemSlot, out var researchSlotIndex))
            {
                return false;
            }

            if (!LandResearchService.IsValidResearchCandidate(
                    CharacterGearSession.ActiveSave,
                    researchSlotIndex,
                    item,
                    CharacterGearSession.Catalog,
                    out var wrongCategory,
                    out var techTooLow))
            {
                if (wrongCategory)
                {
                    pendingResearchWrongCategory = true;
                }
                else if (techTooLow)
                {
                    pendingResearchTechTooLow = true;
                }

                return false;
            }

            offer = new PendingResearchOffer
            {
                Source = source,
                Index = index,
                ResearchSlotIndex = researchSlotIndex
            };
            return true;
        }

        private static bool TryPromptDeleteAtMousePosition(
            Rect inventoryGridRect,
            Rect footlockerGridRect,
            bool footlockerTopAlign,
            Vector2 mousePosition)
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout != null)
            {
                LandInventoryRules.EnsureInventoryCapacity(loadout);
                var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);
                for (var i = 0; i < accessible && i < LandGameConstants.PackSlotCount; i++)
                {
                    if (!TryGetInventoryCellRect(inventoryGridRect, i, out var cell) || !cell.Contains(mousePosition))
                    {
                        continue;
                    }

                    if (!LandLoadoutSlots.IsValidItem(loadout.Inventory[i]))
                    {
                        return false;
                    }

                    pendingDeleteConfirm = true;
                    pendingDeleteSource = DragSourceKind.Inventory;
                    pendingDeleteIndex = i;
                    return true;
                }
            }

            var vault = CharacterGearSession.ActiveVault;
            if (vault != null)
            {
                LandVaultStorageService.EnsureVaultSize(vault);
                var slotCount = LandVaultStorageService.GetVaultSlotCount(vault);
                for (var i = 0; i < slotCount; i++)
                {
                    if (!TryGetFootlockerCellRect(footlockerGridRect, i, footlockerTopAlign, out var cell)
                        || !cell.Contains(mousePosition))
                    {
                        continue;
                    }

                    var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[i]);
                    if (!LandLoadoutSlots.IsValidItem(item))
                    {
                        return false;
                    }

                    pendingDeleteConfirm = true;
                    pendingDeleteSource = DragSourceKind.Vault;
                    pendingDeleteIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryDeletePendingItem()
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            var vault = CharacterGearSession.ActiveVault;
            switch (pendingDeleteSource)
            {
                case DragSourceKind.Inventory:
                    if (loadout == null
                        || pendingDeleteIndex < 0
                        || pendingDeleteIndex >= loadout.Inventory.Count
                        || !LandLoadoutSlots.IsValidItem(loadout.Inventory[pendingDeleteIndex]))
                    {
                        return false;
                    }

                    loadout.Inventory[pendingDeleteIndex] = null;
                    return true;

                case DragSourceKind.Vault:
                    if (vault == null
                        || !LandVaultStorageService.IsVaultIndexValid(vault, pendingDeleteIndex)
                        || !LandLoadoutSlots.IsValidItem(
                            LandGearSaveMapper.ToRuntimeInstance(vault.Items[pendingDeleteIndex])))
                    {
                        return false;
                    }

                    vault.Items[pendingDeleteIndex] = null;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryDestroyPendingResearchItems()
        {
            if (pendingResearchOffers.Count == 0)
            {
                return false;
            }

            var loadout = CharacterGearSession.ActiveLoadout;
            var vault = CharacterGearSession.ActiveVault;
            var destroyedAny = false;
            for (var i = 0; i < pendingResearchOffers.Count; i++)
            {
                var offer = pendingResearchOffers[i];
                switch (offer.Source)
                {
                    case DragSourceKind.Equipment:
                        if (LandLoadoutSlots.IsValidItem(
                                LandLoadoutSlots.GetEquipped(loadout, (LandEquipmentSlot)offer.Index)))
                        {
                            LandLoadoutSlots.SetEquipped(loadout, (LandEquipmentSlot)offer.Index, null);
                            destroyedAny = true;
                        }

                        break;

                    case DragSourceKind.Inventory:
                        if (loadout != null
                            && offer.Index >= 0
                            && offer.Index < loadout.Inventory.Count
                            && LandLoadoutSlots.IsValidItem(loadout.Inventory[offer.Index]))
                        {
                            loadout.Inventory[offer.Index] = null;
                            destroyedAny = true;
                        }

                        break;

                    case DragSourceKind.Vault:
                        if (vault != null
                            && LandVaultStorageService.IsVaultIndexValid(vault, offer.Index)
                            && LandLoadoutSlots.IsValidItem(
                                LandGearSaveMapper.ToRuntimeInstance(vault.Items[offer.Index])))
                        {
                            vault.Items[offer.Index] = null;
                            destroyedAny = true;
                        }

                        break;
                }
            }

            return destroyedAny;
        }

        private static bool TryDropOnEquipment(System.Func<int, Rect> getEquipmentRect, Vector2 mousePosition)
        {
            if (!IsPointerOverEquipmentBoxes(getEquipmentRect, mousePosition))
            {
                return false;
            }

            var loadout = CharacterGearSession.ActiveLoadout;
            var catalog = CharacterGearSession.Catalog;
            var vault = CharacterGearSession.ActiveVault;
            if (loadout == null || catalog == null)
            {
                return false;
            }

            // Always route to the item's correct equipment box, regardless of which box was hovered.
            if (!LandLoadoutEquipService.TryResolveItemSlot(dragItem, catalog, out var correctSlot))
            {
                return false;
            }

            switch (dragSource)
            {
                case DragSourceKind.BasicTray:
                {
                    var occupied = LandLoadoutSlots.IsValidItem(LandLoadoutSlots.GetEquipped(loadout, correctSlot));
                    if (occupied)
                    {
                        pendingBasicLoadoutSlotOccupied = true;
                        return true;
                    }

                    return LandLoadoutEquipService.TryEquipItemCopy(
                               loadout, dragItem, correctSlot, catalog, vault)
                           == LandEquipResult.Success;
                }

                case DragSourceKind.Equipment:
                    if (dragEquipmentSlot == correctSlot)
                    {
                        return false;
                    }

                    return LandLoadoutEquipService.TrySwapPaperdollSlots(
                               loadout, dragEquipmentSlot, correctSlot, catalog)
                           == LandEquipResult.Success;

                case DragSourceKind.Inventory:
                    // Previous equipped item returns to the inventory cell this item came from.
                    return LandLoadoutEquipService.TryEquipFromInventory(
                               loadout, new LandInventoryAddress(dragIndex), correctSlot, catalog)
                           == LandEquipResult.Success;

                case DragSourceKind.Vault:
                    // Previous equipped item returns to the vault cell this item came from.
                    return LandVaultStorageService.TryMoveOrSwapEquipmentWithVault(
                               loadout, correctSlot, vault, dragIndex, catalog)
                           == LandEquipResult.Success;
            }

            return false;
        }

        private static bool IsPointerOverEquipmentBoxes(System.Func<int, Rect> getEquipmentRect, Vector2 mousePosition)
        {
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var cell = getEquipmentRect != null
                    ? getEquipmentRect(i)
                    : CharacterPageLayout.GetEquipmentSlotRect(i, EquipmentSlots.Length);
                if (cell.Contains(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryDropOnInventory(Rect inventoryGridRect, Vector2 mousePosition)
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout == null)
            {
                return false;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);
            for (var i = 0; i < accessible && i < LandGameConstants.PackSlotCount; i++)
            {
                if (!TryGetInventoryCellRect(inventoryGridRect, i, out var cell) || !cell.Contains(mousePosition))
                {
                    continue;
                }

                var address = new LandInventoryAddress(i);
                switch (dragSource)
                {
                    case DragSourceKind.BasicTray:
                        return LandLoadoutEquipService.TryPlaceItemCopyInInventory(loadout, dragItem, address)
                               == LandEquipResult.Success;

                    case DragSourceKind.Equipment:
                        return LandLoadoutEquipService.TryMoveOrSwapEquipmentWithInventory(
                                   loadout, dragEquipmentSlot, address, CharacterGearSession.Catalog)
                               == LandEquipResult.Success;

                    case DragSourceKind.Inventory:
                        if (dragIndex == i)
                        {
                            return false;
                        }

                        return LandLoadoutEquipService.TrySwapInventorySlots(
                                   loadout, new LandInventoryAddress(dragIndex), address)
                               == LandEquipResult.Success;

                    case DragSourceKind.Vault:
                        return LandVaultStorageService.TrySwapInventoryWithVault(
                                   loadout, i, CharacterGearSession.ActiveVault, dragIndex)
                               == LandEquipResult.Success;
                }

                return false;
            }

            return false;
        }

        private static bool TryDropOnVault(Rect footlockerGridRect, bool topAlign, Vector2 mousePosition)
        {
            var vault = CharacterGearSession.ActiveVault;
            var loadout = CharacterGearSession.ActiveLoadout;
            if (vault == null || loadout == null)
            {
                return false;
            }

            LandVaultStorageService.EnsureVaultSize(vault);
            var slotCount = LandVaultStorageService.GetVaultSlotCount(vault);
            for (var i = 0; i < slotCount; i++)
            {
                if (!TryGetFootlockerCellRect(footlockerGridRect, i, topAlign, out var cell)
                    || !cell.Contains(mousePosition))
                {
                    continue;
                }

                switch (dragSource)
                {
                    case DragSourceKind.BasicTray:
                        return LandVaultStorageService.TryPlaceItemCopy(vault, i, dragItem)
                               == LandEquipResult.Success;

                    case DragSourceKind.Equipment:
                        return LandVaultStorageService.TryMoveOrSwapEquipmentWithVault(
                                   loadout, dragEquipmentSlot, vault, i, CharacterGearSession.Catalog)
                               == LandEquipResult.Success;

                    case DragSourceKind.Inventory:
                        return LandVaultStorageService.TrySwapInventoryWithVault(loadout, dragIndex, vault, i)
                               == LandEquipResult.Success;

                    case DragSourceKind.Vault:
                        if (dragIndex == i)
                        {
                            return false;
                        }

                        return LandVaultStorageService.TrySwapVaultSlots(vault, dragIndex, i)
                               == LandEquipResult.Success;
                }

                return false;
            }

            return false;
        }

        private static bool IsDragSourceHidden(DragSourceKind source, LandEquipmentSlot slot = default, int index = -1)
        {
            if (!isDragging || !dragMoved || dragSource != source)
            {
                return false;
            }

            return source switch
            {
                DragSourceKind.Equipment => dragEquipmentSlot == slot,
                DragSourceKind.Inventory => dragIndices.Contains(index),
                DragSourceKind.Vault => dragIndices.Contains(index),
                _ => false
            };
        }

        private static void DrawFootlockerCell(Rect cell, int index, CharacterVaultSaveData vault)
        {
            var ctrlSelected = IsCtrlGridSelected(inventory: false, index);
            var fill = ctrlSelected ? InvertRgb(FootlockerSlotColor) : FootlockerSlotColor;
            var border = ctrlSelected ? InvertRgb(GearSlotBorder) : GearSlotBorder;
            var selected = ctrlSelected || selectedVaultIndices.Contains(index);
            DrawFilledBox(cell, fill, border, selected ? 3f : 1.5f);
            if (vault != null && !IsDragSourceHidden(DragSourceKind.Vault, index: index))
            {
                var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[index]);
                if (LandLoadoutSlots.IsValidItem(item))
                {
                    DrawItemLabel(cell, item, GridItemNameFontSize, invertColors: ctrlSelected);
                    LandItemTooltipUi.RegisterHover(cell, item);
                }
            }

            if (vault == null || isDragging)
            {
                return;
            }

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                HandleVaultClick(index);
            }
        }

        private static void DrawEquipmentCell(Rect cell, LandEquipmentSlot slot)
        {
            var item = IsDragSourceHidden(DragSourceKind.Equipment, slot)
                ? null
                : LandLoadoutSlots.GetEquipped(CharacterGearSession.ActiveLoadout, slot);
            DrawTechTile(cell, item, selectedEquipmentSlot == slot);
            if (LandLoadoutSlots.IsValidItem(item))
            {
                LandItemTooltipUi.RegisterHover(cell, item);
            }

            if (isDragging)
            {
                return;
            }

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                HandleEquipmentClick(slot);
            }
        }

        private static void DrawTechTile(Rect cell, LandGearInstance item, bool selected)
        {
            if (LandLoadoutSlots.IsValidItem(item))
            {
                var fill = LandItemRarityColors.GetTile(item);
                var border = Color.Lerp(fill, Color.black, 0.28f);
                border.a = 0.95f;
                DrawFilledBox(cell, fill, border, selected ? 3f : 1.5f);
                if (LandItemTileOverlay.HasCategoryOverlay(item, CharacterGearSession.Catalog))
                {
                    LandItemTileOverlay.Draw(
                        cell,
                        item,
                        CharacterGearSession.Catalog,
                        FontSizeForTile(cell, 0.2f, 8, 14),
                        LandItemRarityColors.GetOverlayColor(item.Rarity),
                        scaleFonts: false);
                }
                else
                {
                    DrawTechLevelBadge(cell, item);
                }

                return;
            }

            DrawFilledBox(cell, GearSlotFill, GearSlotBorder, selected ? 3f : 1.5f);
        }

        private static void DrawTechLevelBadge(Rect cell, LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return;
            }

            var level = LandTechLevelRules.GetTechLevel(item);
            var ink = level <= 0
                ? Color.black
                : LandItemRarityColors.GetTechLevelNumberColor(item.Rarity);
            var fontSize = FontSizeForTile(cell, 0.22f, 8, 14);
            var style = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.UpperRight,
                ink,
                wordWrap: false);
            var badge = new Rect(cell.x, cell.y + 1f, cell.width - 3f, fontSize + 4f);
            GUI.Label(badge, level.ToString(), style);
        }

        private static void DrawInventoryCell(Rect cell, int index, bool enabled)
        {
            var ctrlSelected = enabled && IsCtrlGridSelected(inventory: true, index);
            var fill = enabled ? GearSlotFill : new Color(0.08f, 0.12f, 0.18f, 0.55f);
            if (ctrlSelected)
            {
                fill = InvertRgb(fill);
            }

            var border = ctrlSelected ? InvertRgb(GearSlotBorder) : GearSlotBorder;
            var selected = ctrlSelected || selectedInventoryIndices.Contains(index);
            DrawFilledBox(cell, fill, border, selected ? 3f : 1.5f);
            if (!enabled)
            {
                return;
            }

            if (!IsDragSourceHidden(DragSourceKind.Inventory, index: index))
            {
                var item = CharacterGearSession.ActiveLoadout.Inventory[index];
                if (LandLoadoutSlots.IsValidItem(item))
                {
                    DrawItemLabel(cell, item, GridItemNameFontSize, invertColors: ctrlSelected);
                    LandItemTooltipUi.RegisterHover(cell, item);
                }
            }

            if (isDragging)
            {
                return;
            }

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                HandleInventoryClick(index);
            }
        }

        private static void DrawItemLabel(
            Rect cell,
            LandGearInstance item,
            int baseNameFontSize,
            string nameOverride = null,
            bool invertColors = false)
        {
            var fontSize = FontSizeForTile(cell, 0.18f, 7, 14);
            if (nameOverride != null && !LandItemTileOverlay.HasCategoryOverlay(item, CharacterGearSession.Catalog))
            {
                var nameStyle = HudStyleFactory.CreateLabel(
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    invertColors ? InvertRgb(Color.white) : Color.white,
                    wordWrap: true);
                GUI.Label(cell, nameOverride, nameStyle);
                return;
            }

            LandItemTileOverlay.Draw(
                cell,
                item,
                CharacterGearSession.Catalog,
                fontSize,
                invertColors ? InvertRgb(Color.white) : Color.white,
                scaleFonts: false,
                paintRarityFill: true,
                invertColors: invertColors);
        }

        private static int FontSizeForTile(Rect cell, float heightRatio, int minSize, int maxSize) =>
            Mathf.Clamp(Mathf.RoundToInt(cell.height * heightRatio), minSize, maxSize);

        private static void HandleInventoryClick(int index)
        {
            if (IsControlHeld())
            {
                selectedEquipmentSlot = null;
                ctrlGridSelectionActive = true;
                if (selectedInventoryIndices.Contains(index))
                {
                    selectedInventoryIndices.Remove(index);
                }
                else
                {
                    selectedInventoryIndices.Add(index);
                    selectedVaultIndices.Clear();
                }

                if (selectedInventoryIndices.Count == 0 && selectedVaultIndices.Count == 0)
                {
                    ctrlGridSelectionActive = false;
                }

                return;
            }

            if (selectedVaultIndices.Count == 1)
            {
                LandVaultStorageService.TrySwapInventoryWithVault(
                    CharacterGearSession.ActiveLoadout,
                    index,
                    CharacterGearSession.ActiveVault,
                    GetSingleSelectedVaultIndex());
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedEquipmentSlot.HasValue)
            {
                LandLoadoutEquipService.TryEquipFromInventory(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(index),
                    selectedEquipmentSlot.Value,
                    CharacterGearSession.Catalog);
                selectedEquipmentSlot = null;
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedInventoryIndices.Count == 1 && !selectedInventoryIndices.Contains(index))
            {
                var fromIndex = GetSingleSelectedInventoryIndex();
                LandLoadoutEquipService.TrySwapInventorySlots(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(fromIndex),
                    new LandInventoryAddress(index));
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedInventoryIndices.Count == 1 && selectedInventoryIndices.Contains(index))
            {
                ClearGridSelection();
                return;
            }

            ClearGridSelection();
            selectedInventoryIndices.Add(index);
        }

        private static int GetSingleSelectedInventoryIndex()
        {
            foreach (var index in selectedInventoryIndices)
            {
                return index;
            }

            return -1;
        }

        private static int GetSingleSelectedVaultIndex()
        {
            foreach (var index in selectedVaultIndices)
            {
                return index;
            }

            return -1;
        }

        private static void HandleEquipmentClick(LandEquipmentSlot slot)
        {
            if (selectedInventoryIndices.Count == 1)
            {
                LandLoadoutEquipService.TryEquipFromInventory(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(GetSingleSelectedInventoryIndex()),
                    slot,
                    CharacterGearSession.Catalog);
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            selectedEquipmentSlot = selectedEquipmentSlot == slot ? null : slot;
        }

        private static void HandleVaultClick(int index)
        {
            if (IsControlHeld())
            {
                selectedEquipmentSlot = null;
                ctrlGridSelectionActive = true;
                if (selectedVaultIndices.Contains(index))
                {
                    selectedVaultIndices.Remove(index);
                }
                else
                {
                    selectedVaultIndices.Add(index);
                    selectedInventoryIndices.Clear();
                }

                if (selectedInventoryIndices.Count == 0 && selectedVaultIndices.Count == 0)
                {
                    ctrlGridSelectionActive = false;
                }

                return;
            }

            if (selectedInventoryIndices.Count == 1)
            {
                LandVaultStorageService.TrySwapInventoryWithVault(
                    CharacterGearSession.ActiveLoadout,
                    GetSingleSelectedInventoryIndex(),
                    CharacterGearSession.ActiveVault,
                    index);
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedVaultIndices.Count == 1 && !selectedVaultIndices.Contains(index))
            {
                LandVaultStorageService.TrySwapVaultSlots(
                    CharacterGearSession.ActiveVault,
                    GetSingleSelectedVaultIndex(),
                    index);
                ClearGridSelection();
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedVaultIndices.Count == 1 && selectedVaultIndices.Contains(index))
            {
                ClearGridSelection();
                return;
            }

            ClearGridSelection();
            selectedVaultIndices.Add(index);
        }

        private static void DrawFilledBox(Rect rect, Color fill, Color border, float borderThickness)
        {
            GUI.color = fill;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = border;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - borderThickness, rect.width, borderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, borderThickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - borderThickness, rect.y, borderThickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
