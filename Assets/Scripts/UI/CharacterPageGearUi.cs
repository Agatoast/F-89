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

        private static LandEquipmentSlot? selectedEquipmentSlot;
        private static int? selectedInventoryIndex;
        private static int? selectedVaultIndex;

        private static bool isDragging;
        private static DragSourceKind dragSource;
        private static LandGearInstance dragItem;
        private static LandEquipmentSlot dragEquipmentSlot;
        private static int dragIndex;
        private static float dragCellSize;
        private static Vector2 dragStartGui;
        private static bool dragMoved;

        private static bool pendingResearchConfirm;
        private static bool pendingResearchTechTooLow;
        private static bool pendingResearchWrongCategory;
        private static DragSourceKind pendingResearchSource;
        private static LandEquipmentSlot pendingResearchEquipmentSlot;
        private static int pendingResearchIndex;
        private static int pendingResearchSlotIndex = -1;

        private static readonly (LandEquipmentSlot slot, string label)[] EquipmentSlots =
        {
            (LandEquipmentSlot.Helmet, "Helmet"),
            (LandEquipmentSlot.Core, "Vest"),
            (LandEquipmentSlot.Weapon, "Weapon"),
            (LandEquipmentSlot.Boots, "Boots")
        };

        private static readonly (LandEquipmentSlot slot, string definitionId)[] BasicTrayItems =
        {
            (LandEquipmentSlot.Helmet, "TestHelmet"),
            (LandEquipmentSlot.Core, "ScrapCore"),
            (LandEquipmentSlot.Weapon, "Blaster"),
            (LandEquipmentSlot.Boots, "Boots")
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
            for (var i = 0; i < BasicTrayItems.Length; i++)
            {
                var cell = getSlotRect != null
                    ? getSlotRect(i)
                    : CharacterPageLayout.GetEquipmentSlotRect(i, BasicTrayItems.Length);
                var hide = isDragging && dragMoved && dragSource == DragSourceKind.BasicTray && dragIndex == i;
                if (hide)
                {
                    DrawTechTile(cell, null, selected: false);
                }
                else
                {
                    DrawTechTile(cell, GetBasicTrayItem(i), selected: false);
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

            if (pendingResearchConfirm || pendingResearchTechTooLow || pendingResearchWrongCategory)
            {
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

        public static bool IsResearchConfirmPending => pendingResearchConfirm;
        public static bool IsResearchTechTooLowPending => pendingResearchTechTooLow;
        public static bool IsResearchWrongCategoryPending => pendingResearchWrongCategory;

        public static void CancelResearchConfirm()
        {
            pendingResearchConfirm = false;
            pendingResearchTechTooLow = false;
            pendingResearchWrongCategory = false;
            pendingResearchSource = DragSourceKind.None;
            pendingResearchIndex = -1;
            pendingResearchSlotIndex = -1;
        }

        public static void AcknowledgeResearchTechTooLow()
        {
            CancelResearchConfirm();
        }

        public static void AcknowledgeResearchWrongCategory()
        {
            CancelResearchConfirm();
        }

        public static bool ConfirmResearchDestroy()
        {
            if (!pendingResearchConfirm)
            {
                return false;
            }

            var researchSlotIndex = pendingResearchSlotIndex;
            if (!TryDestroyPendingResearchItem())
            {
                CancelResearchConfirm();
                return false;
            }

            LandResearchService.AddResearchProgress(CharacterGearSession.ActiveSave, researchSlotIndex);
            CharacterGearSession.PersistActive();
            CancelResearchConfirm();
            return true;
        }

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
        }

        public static void ResetDragState()
        {
            isDragging = false;
            dragSource = DragSourceKind.None;
            dragItem = null;
            dragIndex = -1;
            dragCellSize = 0f;
            dragMoved = false;
        }

        public static LandEquipmentSlot GetEquipmentSlotAt(int index) =>
            EquipmentSlots[Mathf.Clamp(index, 0, EquipmentSlots.Length - 1)].slot;

        public static string GetEquipmentSlotLabel(int index) =>
            EquipmentSlots[Mathf.Clamp(index, 0, EquipmentSlots.Length - 1)].label;

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
            index = Mathf.Clamp(index, 0, BasicTrayItems.Length - 1);
            var entry = BasicTrayItems[index];
            return new LandGearInstance
            {
                DefinitionId = entry.definitionId,
                Rarity = LandItemRarity.White
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
            selectedInventoryIndex = null;
            selectedVaultIndex = null;
        }

        private static bool TryBeginBasicTrayDrag(System.Func<int, Rect> getBasicRect, Vector2 mousePosition)
        {
            for (var i = 0; i < BasicTrayItems.Length; i++)
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
                CharacterGearSession.PersistActive();
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

            if (!LandLoadoutEquipService.TryResolveItemSlot(
                    dragItem,
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
                    dragItem,
                    CharacterGearSession.Catalog,
                    out var wrongCategory,
                    out var techTooLow))
            {
                if (wrongCategory)
                {
                    pendingResearchWrongCategory = true;
                    return true;
                }

                if (techTooLow)
                {
                    pendingResearchTechTooLow = true;
                    return true;
                }

                return false;
            }

            pendingResearchConfirm = true;
            pendingResearchSource = dragSource;
            pendingResearchEquipmentSlot = dragEquipmentSlot;
            pendingResearchIndex = dragIndex;
            pendingResearchSlotIndex = researchSlotIndex;
            return true;
        }

        private static bool TryDestroyPendingResearchItem()
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            var vault = CharacterGearSession.ActiveVault;
            switch (pendingResearchSource)
            {
                case DragSourceKind.Equipment:
                    if (!LandLoadoutSlots.IsValidItem(LandLoadoutSlots.GetEquipped(loadout, pendingResearchEquipmentSlot)))
                    {
                        return false;
                    }

                    LandLoadoutSlots.SetEquipped(loadout, pendingResearchEquipmentSlot, null);
                    return true;

                case DragSourceKind.Inventory:
                    if (loadout == null
                        || pendingResearchIndex < 0
                        || pendingResearchIndex >= loadout.Inventory.Count
                        || !LandLoadoutSlots.IsValidItem(loadout.Inventory[pendingResearchIndex]))
                    {
                        return false;
                    }

                    loadout.Inventory[pendingResearchIndex] = null;
                    return true;

                case DragSourceKind.Vault:
                    if (vault == null
                        || !LandVaultStorageService.IsVaultIndexValid(vault, pendingResearchIndex)
                        || !LandLoadoutSlots.IsValidItem(
                            LandGearSaveMapper.ToRuntimeInstance(vault.Items[pendingResearchIndex])))
                    {
                        return false;
                    }

                    vault.Items[pendingResearchIndex] = null;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryDropOnEquipment(System.Func<int, Rect> getEquipmentRect, Vector2 mousePosition)
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

                var targetSlot = EquipmentSlots[i].slot;
                var loadout = CharacterGearSession.ActiveLoadout;
                var catalog = CharacterGearSession.Catalog;
                var vault = CharacterGearSession.ActiveVault;

                switch (dragSource)
                {
                    case DragSourceKind.BasicTray:
                        return LandLoadoutEquipService.TryEquipItemCopy(
                                   loadout, dragItem, targetSlot, catalog, vault)
                               == LandEquipResult.Success;

                    case DragSourceKind.Equipment:
                        if (dragEquipmentSlot == targetSlot)
                        {
                            return false;
                        }

                        return LandLoadoutEquipService.TrySwapPaperdollSlots(
                                   loadout, dragEquipmentSlot, targetSlot, catalog)
                               == LandEquipResult.Success;

                    case DragSourceKind.Inventory:
                        return LandLoadoutEquipService.TryEquipFromInventory(
                                   loadout, new LandInventoryAddress(dragIndex), targetSlot, catalog)
                               == LandEquipResult.Success;

                    case DragSourceKind.Vault:
                        return LandVaultStorageService.TryMoveOrSwapEquipmentWithVault(
                                   loadout, targetSlot, vault, dragIndex, catalog)
                               == LandEquipResult.Success;
                }

                return false;
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
                DragSourceKind.Inventory => dragIndex == index,
                DragSourceKind.Vault => dragIndex == index,
                _ => false
            };
        }

        private static void DrawFootlockerCell(Rect cell, int index, CharacterVaultSaveData vault)
        {
            DrawFilledBox(cell, FootlockerSlotColor, GearSlotBorder, selectedVaultIndex == index ? 3f : 1.5f);
            if (vault != null && !IsDragSourceHidden(DragSourceKind.Vault, index: index))
            {
                var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[index]);
                if (LandLoadoutSlots.IsValidItem(item))
                {
                    DrawItemLabel(cell, item, GridItemNameFontSize);
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
                var fill = LandItemRarityColors.GetTile(item.Rarity);
                var border = Color.Lerp(fill, Color.black, 0.28f);
                border.a = 0.95f;
                DrawFilledBox(cell, fill, border, selected ? 3f : 1.5f);
                DrawTechLevelBadge(cell, item);
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
            var fontSize = CharacterPageStyles.ScaleSlotFont(10);
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
            var fill = enabled ? GearSlotFill : new Color(0.08f, 0.12f, 0.18f, 0.55f);
            DrawFilledBox(cell, fill, GearSlotBorder, selectedInventoryIndex == index ? 3f : 1.5f);
            if (!enabled)
            {
                return;
            }

            if (!IsDragSourceHidden(DragSourceKind.Inventory, index: index))
            {
                var item = CharacterGearSession.ActiveLoadout.Inventory[index];
                if (LandLoadoutSlots.IsValidItem(item))
                {
                    DrawItemLabel(cell, item, GridItemNameFontSize);
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

        private static void DrawItemLabel(Rect cell, LandGearInstance item, int baseNameFontSize, string nameOverride = null)
        {
            var catalog = CharacterGearSession.Catalog;
            var nameFontSize = CharacterPageStyles.ScaleSlotFont(baseNameFontSize);
            var statFontSize = CharacterPageStyles.ScaleSlotFont(Mathf.Max(7, baseNameFontSize - 2));
            var nameStyle = HudStyleFactory.CreateLabel(nameFontSize, FontStyle.Bold, TextAnchor.UpperCenter, Color.white, wordWrap: true);
            var statStyle = HudStyleFactory.CreateLabel(
                statFontSize,
                FontStyle.Normal,
                TextAnchor.LowerCenter,
                new Color(0.88f, 0.92f, 1f),
                wordWrap: true);
            var nameRect = new Rect(cell.x, cell.y + 2f, cell.width, cell.height * 0.55f);
            var statRect = new Rect(cell.x, cell.y + cell.height * 0.45f, cell.width, cell.height * 0.5f);

            GUI.Label(nameRect, nameOverride ?? catalog.GetDisplayName(item), nameStyle);
            if (catalog.TryGetWeaponSummary(item, out var summary))
            {
                GUI.Label(statRect, summary, statStyle);
            }
        }

        private static void HandleInventoryClick(int index)
        {
            if (selectedVaultIndex.HasValue)
            {
                LandVaultStorageService.TrySwapInventoryWithVault(
                    CharacterGearSession.ActiveLoadout,
                    index,
                    CharacterGearSession.ActiveVault,
                    selectedVaultIndex.Value);
                selectedVaultIndex = null;
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
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedInventoryIndex.HasValue && selectedInventoryIndex.Value != index)
            {
                LandLoadoutEquipService.TrySwapInventorySlots(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(selectedInventoryIndex.Value),
                    new LandInventoryAddress(index));
                selectedInventoryIndex = null;
                CharacterGearSession.PersistActive();
                return;
            }

            selectedInventoryIndex = selectedInventoryIndex == index ? null : index;
        }

        private static void HandleEquipmentClick(LandEquipmentSlot slot)
        {
            if (selectedInventoryIndex.HasValue)
            {
                LandLoadoutEquipService.TryEquipFromInventory(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(selectedInventoryIndex.Value),
                    slot,
                    CharacterGearSession.Catalog);
                selectedInventoryIndex = null;
                CharacterGearSession.PersistActive();
                return;
            }

            selectedEquipmentSlot = selectedEquipmentSlot == slot ? null : slot;
        }

        private static void HandleVaultClick(int index)
        {
            if (selectedInventoryIndex.HasValue)
            {
                LandVaultStorageService.TrySwapInventoryWithVault(
                    CharacterGearSession.ActiveLoadout,
                    selectedInventoryIndex.Value,
                    CharacterGearSession.ActiveVault,
                    index);
                selectedInventoryIndex = null;
                CharacterGearSession.PersistActive();
                return;
            }

            if (selectedVaultIndex.HasValue && selectedVaultIndex.Value != index)
            {
                LandVaultStorageService.TrySwapVaultSlots(CharacterGearSession.ActiveVault, selectedVaultIndex.Value, index);
                selectedVaultIndex = null;
                CharacterGearSession.PersistActive();
                return;
            }

            selectedVaultIndex = selectedVaultIndex == index ? null : index;
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
