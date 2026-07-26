using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    public static class LandCharacterGearPanelUi
    {
        private enum PanelTab
        {
            Loadout = 0,
            Bank = 1
        }

        private static PanelTab activeTab = PanelTab.Loadout;
        private static LandEquipmentSlot? selectedPaperdollSlot;
        private static int? selectedInventoryIndex;
        private static int? selectedVaultIndex;

        public static void Draw(Rect rect)
        {
            DrawTabs(rect);
            var contentRect = new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, rect.height - 50f);
            if (activeTab == PanelTab.Bank)
            {
                DrawBankPanel(contentRect);
            }
            else
            {
                DrawLoadoutPanel(contentRect);
            }
        }

        private static void DrawTabs(Rect rect)
        {
            var tabWidth = 120f;
            var tabHeight = 28f;
            var loadoutRect = new Rect(rect.x + 12f, rect.y + 8f, tabWidth, tabHeight);
            var bankRect = new Rect(loadoutRect.xMax + 8f, rect.y + 8f, tabWidth, tabHeight);

            if (GUI.Button(loadoutRect, activeTab == PanelTab.Loadout ? "[ Loadout ]" : "Loadout"))
            {
                activeTab = PanelTab.Loadout;
            }

            if (GUI.Button(bankRect, activeTab == PanelTab.Bank ? "[ Bank ]" : "Bank"))
            {
                activeTab = PanelTab.Bank;
            }
        }

        private static void DrawLoadoutPanel(Rect rect)
        {
            var inventoryRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.28f);
            var figureRect = new Rect(
                rect.x + rect.width * 0.22f,
                rect.y + rect.height * 0.3f,
                rect.width * 0.56f,
                rect.height * 0.68f);

            DrawInventory(inventoryRect);
            DrawPaperdoll(figureRect);
        }

        private static void DrawBankPanel(Rect rect)
        {
            var vault = CharacterGearSession.ActiveVault;
            if (vault == null)
            {
                GUI.Label(rect, "No character loaded.", HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black));
                return;
            }

            LandVaultStorageService.EnsureVaultSize(vault);
            DrawWireBox(rect, 2f);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width, 20f), "Gear Vault", HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.UpperLeft, Color.black));

            var gridRect = new Rect(rect.x + 8f, rect.y + 28f, rect.width - 16f, rect.height - 36f);
            var columns = LandGameConstants.VaultGridColumns;
            var slotCount = LandVaultStorageService.GetVaultSlotCount(vault);
            var rows = Mathf.CeilToInt(slotCount / (float)columns);
            var cellWidth = gridRect.width / columns;
            var cellHeight = gridRect.height / Mathf.Max(1, rows);

            for (var i = 0; i < slotCount; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var cell = new Rect(
                    gridRect.x + col * cellWidth + 2f,
                    gridRect.y + row * cellHeight + 2f,
                    cellWidth - 4f,
                    cellHeight - 4f);
                DrawVaultCell(cell, i, vault);
            }
        }

        private static void DrawInventory(Rect rect)
        {
            DrawWireBox(rect, 2f);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, 120f, 18f), "Inventory", HudStyleFactory.CreateLabel(13, FontStyle.Bold, TextAnchor.UpperLeft, Color.black));

            var inner = new Rect(rect.x + 8f, rect.y + 24f, rect.width - 16f, rect.height - 30f);
            var columns = LandGameConstants.InventoryGridColumns;
            var rows = LandGameConstants.InventoryGridRows;
            var cellWidth = inner.width / columns;
            var cellHeight = inner.height / rows;
            var loadout = CharacterGearSession.ActiveLoadout;
            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);

            for (var i = 0; i < LandGameConstants.MainSlotCount; i++)
            {
                var col = i % columns;
                var row = i / columns;
                if (row >= rows)
                {
                    break;
                }

                var cell = new Rect(
                    inner.x + col * cellWidth + 2f,
                    inner.y + row * cellHeight + 2f,
                    cellWidth - 4f,
                    cellHeight - 4f);
                var enabled = i < accessible;
                DrawInventoryCell(cell, i, enabled);
            }
        }

        private static void DrawPaperdoll(Rect rect)
        {
            DrawWireBox(rect, 2f);
            DrawSilhouette(rect);

            var slot = Mathf.Min(rect.width, rect.height) * 0.11f;
            var positions = new (LandEquipmentSlot slot, Vector2 anchor)[]
            {
                (LandEquipmentSlot.Helmet, new Vector2(0.5f, 0.08f)),
                (LandEquipmentSlot.Utility1, new Vector2(0.28f, 0.16f)),
                (LandEquipmentSlot.Utility2, new Vector2(0.72f, 0.16f)),
                (LandEquipmentSlot.Core, new Vector2(0.5f, 0.34f)),
                (LandEquipmentSlot.Shield, new Vector2(0.18f, 0.44f)),
                (LandEquipmentSlot.Weapon, new Vector2(0.82f, 0.44f)),
                (LandEquipmentSlot.Module1, new Vector2(0.32f, 0.62f)),
                (LandEquipmentSlot.Module2, new Vector2(0.68f, 0.62f)),
                (LandEquipmentSlot.Boots, new Vector2(0.5f, 0.82f)),
                (LandEquipmentSlot.DuffleBag, new Vector2(0.5f, 0.92f))
            };

            foreach (var entry in positions)
            {
                var center = new Vector2(rect.x + rect.width * entry.anchor.x, rect.y + rect.height * entry.anchor.y);
                var slotRect = new Rect(center.x - slot * 0.5f, center.y - slot * 0.5f, slot, slot);
                DrawPaperdollCell(slotRect, entry.slot);
            }
        }

        private static void DrawInventoryCell(Rect cell, int index, bool enabled)
        {
            GUI.color = enabled ? Color.black : new Color(0.6f, 0.6f, 0.6f);
            DrawWireBox(cell, 1.5f);
            GUI.color = Color.white;
            if (!enabled)
            {
                return;
            }

            var item = CharacterGearSession.ActiveLoadout.Inventory[index];
            if (LandLoadoutSlots.IsValidItem(item))
            {
                DrawItemLabel(cell, item, 10);
            }

            if (!GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            HandleInventoryClick(index);
        }

        private static void DrawPaperdollCell(Rect cell, LandEquipmentSlot slot)
        {
            DrawWireBox(cell, 1.5f);
            var item = LandLoadoutSlots.GetEquipped(CharacterGearSession.ActiveLoadout, slot);
            if (LandLoadoutSlots.IsValidItem(item))
            {
                DrawItemLabel(cell, item, 9);
            }
            else
            {
                GUI.Label(cell, slot.ToString(), HudStyleFactory.CreateLabel(8, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black, wordWrap: true));
            }

            if (!GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            HandlePaperdollClick(slot);
        }

        private static void DrawVaultCell(Rect cell, int index, CharacterVaultSaveData vault)
        {
            DrawWireBox(cell, 1.5f);
            var item = LandGearSaveMapper.ToRuntimeInstance(vault.Items[index]);
            if (LandLoadoutSlots.IsValidItem(item))
            {
                DrawItemLabel(cell, item, 9);
            }

            if (!GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            HandleVaultClick(index);
        }

        private static void DrawItemLabel(Rect cell, LandGearInstance item, int nameFontSize)
        {
            var catalog = CharacterGearSession.Catalog;
            var nameStyle = HudStyleFactory.CreateLabel(nameFontSize, FontStyle.Bold, TextAnchor.UpperCenter, Color.black, wordWrap: true);
            var statStyle = HudStyleFactory.CreateLabel(Mathf.Max(7, nameFontSize - 2), FontStyle.Normal, TextAnchor.LowerCenter, Color.black, wordWrap: true);
            var nameRect = new Rect(cell.x, cell.y + 2f, cell.width, cell.height * 0.55f);
            var statRect = new Rect(cell.x, cell.y + cell.height * 0.45f, cell.width, cell.height * 0.5f);

            GUI.Label(nameRect, catalog.GetDisplayName(item), nameStyle);
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

            if (selectedPaperdollSlot.HasValue)
            {
                LandLoadoutEquipService.TryEquipFromInventory(
                    CharacterGearSession.ActiveLoadout,
                    new LandInventoryAddress(index),
                    selectedPaperdollSlot.Value,
                    CharacterGearSession.Catalog);
                selectedPaperdollSlot = null;
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

        private static void HandlePaperdollClick(LandEquipmentSlot slot)
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

            selectedPaperdollSlot = selectedPaperdollSlot == slot ? null : slot;
        }

        private static void HandleVaultClick(int index)
        {
            if (activeTab != PanelTab.Bank && selectedInventoryIndex.HasValue)
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

        private static void DrawSilhouette(Rect rect)
        {
            var centerX = rect.x + rect.width * 0.5f;
            var headY = rect.y + rect.height * 0.14f;
            var hipY = rect.y + rect.height * 0.58f;
            var footY = rect.y + rect.height * 0.88f;
            var shoulderY = rect.y + rect.height * 0.24f;
            var handY = rect.y + rect.height * 0.48f;
            var shoulderSpan = rect.width * 0.22f;
            var hipSpan = rect.width * 0.12f;

            HudGuiUtility.DrawScreenLine(new Vector2(centerX, headY + 16f), new Vector2(centerX, hipY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - shoulderSpan, shoulderY), new Vector2(centerX + shoulderSpan, shoulderY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - shoulderSpan, shoulderY), new Vector2(centerX - shoulderSpan - 8f, handY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX + shoulderSpan, shoulderY), new Vector2(centerX + shoulderSpan + 8f, handY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - hipSpan, hipY), new Vector2(centerX - hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX + hipSpan, hipY), new Vector2(centerX + hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - hipSpan, footY), new Vector2(centerX + hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);
        }

        private static void DrawWireBox(Rect rect, float thickness)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
