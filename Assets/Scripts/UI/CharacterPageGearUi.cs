using F89.Core;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageGearUi
    {
        private const int EquipmentItemNameFontSize = 8;
        private const int GridItemNameFontSize = 9;

        private static readonly Color FootlockerSlotColor = new Color(0.18f, 0.52f, 0.24f, 0.92f);
        private static readonly Color GearSlotFill = new Color(0.12f, 0.2f, 0.3f, 0.88f);
        private static readonly Color GearSlotBorder = new Color(0.55f, 0.78f, 0.95f, 0.95f);

        private static LandEquipmentSlot? selectedEquipmentSlot;
        private static int? selectedInventoryIndex;
        private static int? selectedVaultIndex;

        private static readonly (LandEquipmentSlot slot, string label)[] EquipmentSlots =
        {
            (LandEquipmentSlot.Helmet, "Helmet"),
            (LandEquipmentSlot.Core, "Vest"),
            (LandEquipmentSlot.Weapon, "Weapon"),
            (LandEquipmentSlot.Boots, "Boots")
        };

        public static void DrawFootlocker(Rect gridRect)
        {
            var vault = CharacterGearSession.ActiveVault;
            var columns = LandGameConstants.VaultGridColumns;
            var slotCount = vault != null
                ? LandVaultStorageService.GetVaultSlotCount(vault)
                : LandGameConstants.VaultBaseSlotCount;

            if (vault != null)
            {
                LandVaultStorageService.EnsureVaultSize(vault);
            }

            var rows = Mathf.CeilToInt(slotCount / (float)columns);
            const float gap = 6f;

            var cellSize = Mathf.Max(
                1f,
                Mathf.Min(
                    (gridRect.width - gap * (columns - 1)) / columns,
                    (gridRect.height - gap * (rows - 1)) / rows));
            var gridWidth = cellSize * columns + gap * (columns - 1);
            var gridHeight = cellSize * rows + gap * (rows - 1);
            var originX = gridRect.xMax - gridWidth;
            var originY = gridRect.y + (gridRect.height - gridHeight) * 0.5f;

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

        public static void DrawEquipmentSlots()
        {
            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var entry = EquipmentSlots[i];
                DrawEquipmentCell(CharacterPageLayout.GetEquipmentSlotRect(i, EquipmentSlots.Length), entry.slot, entry.label);
            }
        }

        public static void DrawInventory(Rect gridRect)
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            if (loadout == null)
            {
                return;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var columns = LandGameConstants.InventoryGridColumns;
            var rows = LandGameConstants.InventoryGridRows;
            var cellWidth = gridRect.width / columns;
            var cellHeight = gridRect.height / rows;
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);
            const float inset = 4f;

            for (var i = 0; i < LandGameConstants.PackSlotCount; i++)
            {
                var col = i % columns;
                var row = i / columns;
                if (row >= rows)
                {
                    break;
                }

                var cell = new Rect(
                    gridRect.x + col * cellWidth + inset,
                    gridRect.y + row * cellHeight + inset,
                    cellWidth - inset * 2f,
                    cellHeight - inset * 2f);
                DrawInventoryCell(cell, i, i < accessible);
            }
        }

        private static void DrawFootlockerCell(Rect cell, int index, CharacterVaultSaveData vault)
        {
            DrawFilledBox(cell, FootlockerSlotColor, GearSlotBorder, selectedVaultIndex == index ? 3f : 1.5f);
            if (vault != null)
            {
                var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[index]);
                if (LandLoadoutSlots.IsValidItem(item))
                {
                    DrawItemLabel(cell, item, GridItemNameFontSize);
                }
            }

            if (vault == null)
            {
                return;
            }

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                HandleVaultClick(index);
            }
        }

        private static void DrawEquipmentCell(Rect cell, LandEquipmentSlot slot, string label)
        {
            DrawFilledBox(cell, GearSlotFill, GearSlotBorder, selectedEquipmentSlot == slot ? 3f : 1.5f);
            var item = LandLoadoutSlots.GetEquipped(CharacterGearSession.ActiveLoadout, slot);
            if (slot == LandEquipmentSlot.Weapon)
            {
                GUI.Label(cell, label, CharacterPageStyles.SmallSlotLabelStyle);
            }
            else if (LandLoadoutSlots.IsValidItem(item))
            {
                DrawItemLabel(cell, item, EquipmentItemNameFontSize);
            }
            else
            {
                GUI.Label(cell, label, CharacterPageStyles.SmallSlotLabelStyle);
            }

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                HandleEquipmentClick(slot);
            }
        }

        private static void DrawInventoryCell(Rect cell, int index, bool enabled)
        {
            var fill = enabled ? GearSlotFill : new Color(0.08f, 0.12f, 0.18f, 0.55f);
            DrawFilledBox(cell, fill, GearSlotBorder, selectedInventoryIndex == index ? 3f : 1.5f);
            if (!enabled)
            {
                return;
            }

            var item = CharacterGearSession.ActiveLoadout.Inventory[index];
            if (LandLoadoutSlots.IsValidItem(item))
            {
                DrawItemLabel(cell, item, GridItemNameFontSize);
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
