using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Screen layout for the ground-combat HUD (MTAU-style).</summary>
    public static class LandCombatHudLayout
    {
        private const float Margin = 12f;
        public const int EquipmentSlotCount = 4;

        public static Rect GetMinimapRect()
        {
            var size = Mathf.Clamp(Screen.height * 0.22f, 140f, 220f);
            return new Rect(Screen.width - size - Margin, Margin, size, size);
        }

        public static Rect GetHpBarRect()
        {
            const float width = 320f;
            const float height = 24f;
            return new Rect((Screen.width - width) * 0.5f, Margin, width, height);
        }

        public static Rect GetInventoryTitleRect()
        {
            var grid = GetInventoryGridRect();
            return new Rect(grid.x, grid.y - 22f, grid.width, 20f);
        }

        public static Rect GetInventoryGridRect()
        {
            var cell = 52f;
            var cols = LandGameConstants.InventoryGridColumns;
            var rows = LandGameConstants.InventoryGridRows;
            var gap = 4f;
            var width = cols * cell + (cols - 1) * gap;
            var height = rows * cell + (rows - 1) * gap;
            return new Rect(
                Screen.width - width - Margin,
                Screen.height - height - Margin - 48f,
                width,
                height);
        }

        /// <summary>Legacy full inventory panel (title + grid) for pointer hit tests.</summary>
        public static Rect GetInventoryRect()
        {
            var title = GetInventoryTitleRect();
            var grid = GetInventoryGridRect();
            return Rect.MinMaxRect(title.xMin, title.yMin, grid.xMax, grid.yMax);
        }

        public static Rect GetBandagesBoxRect()
        {
            var inventory = GetInventoryTitleRect();
            var width = (GetInventoryGridRect().width - 8f) * 0.5f;
            return new Rect(inventory.x, inventory.y - 64f - 8f, width, 64f);
        }

        public static Rect GetGrenadeBoxRect()
        {
            var bandages = GetBandagesBoxRect();
            return new Rect(bandages.xMax + 8f, bandages.y, bandages.width, bandages.height);
        }

        /// <summary>Item hover tooltip panel between minimap (above) and Bandages/Grenades (below).</summary>
        public static Rect GetItemTooltipRect() =>
            GetItemTooltipRect(52f * 2f);

        /// <summary>Square tip of the given size, centered in the gap between minimap and Bandages/Grenades.</summary>
        public static Rect GetItemTooltipRect(float tipSize)
        {
            tipSize = Mathf.Max(8f, tipSize);
            var minimap = GetMinimapRect();
            var bandages = GetBandagesBoxRect();
            var inventory = GetInventoryGridRect();
            var gapTop = minimap.yMax + 8f;
            var gapBottom = bandages.y - 8f;
            var gapHeight = Mathf.Max(tipSize, gapBottom - gapTop);
            var y = gapTop + (gapHeight - tipSize) * 0.5f;
            if (gapBottom > gapTop)
            {
                y = Mathf.Clamp(y, gapTop, gapBottom - tipSize);
            }

            var x = inventory.x + (inventory.width - tipSize) * 0.5f;
            x = Mathf.Clamp(x, Margin, Screen.width - tipSize - Margin);
            y = Mathf.Clamp(y, Margin, Screen.height - tipSize - Margin);
            return new Rect(x, y, tipSize, tipSize);
        }

        public static Rect GetPaperdollRect()
        {
            var height = Mathf.Clamp(Screen.height * 0.38f, 200f, 320f);
            var width = height * 0.55f;
            return new Rect(Margin, Screen.height - height - Margin - 48f, width, height);
        }

        /// <summary>Vertical equipment stack centered on the paperdoll, with per-slot nudges.</summary>
        public static Rect GetEquipmentSlotRect(int index)
        {
            index = Mathf.Clamp(index, 0, EquipmentSlotCount - 1);
            var paperdoll = GetPaperdollRect();
            var size = 48f;
            var gap = 6f;
            var totalHeight = EquipmentSlotCount * size + (EquipmentSlotCount - 1) * gap;
            var x = paperdoll.x + (paperdoll.width - size) * 0.5f;
            var startY = paperdoll.y + (paperdoll.height - totalHeight) * 0.5f;
            var cell = new Rect(x, startY + index * (size + gap), size, size);

            // Helmet, Vest, Weapon, Boots
            switch (index)
            {
                case 0: // Helmet — up 50, left 2
                    cell.y -= 50f;
                    cell.x -= 2f;
                    break;
                case 1: // Vest — up 40, left 2
                    cell.y -= 40f;
                    cell.x -= 2f;
                    break;
                case 2: // Weapon — left 50
                    cell.x -= 50f;
                    break;
                case 3: // Boots — down 50, left 2
                    cell.y += 50f;
                    cell.x -= 2f;
                    break;
            }

            return cell;
        }

        /// <summary>DR label directly below the vest slot on the paperdoll.</summary>
        public static Rect GetPaperdollDrLabelRect()
        {
            var vest = GetEquipmentSlotRect(1);
            const float lineHeight = 14f;
            const float gap = 4f;
            var width = Mathf.Max(vest.width + 16f, 64f);
            var x = vest.x + (vest.width - width) * 0.5f + 1f;
            return new Rect(x, vest.yMax + gap, width, lineHeight);
        }

        /// <summary>Total equipped DR value on the line below the DR label.</summary>
        public static Rect GetPaperdollDrValueRect()
        {
            var label = GetPaperdollDrLabelRect();
            return new Rect(label.x, label.yMax, label.width, 18f);
        }

        public static Rect GetSettingsButtonRect()
        {
            const float w = 110f;
            const float h = 36f;
            return new Rect(Screen.width - w - Margin, Screen.height - h - Margin, w, h);
        }

        public static Rect GetStatusLineRect()
        {
            return new Rect(Margin, Margin + 28f, Screen.width * 0.55f, 48f);
        }

        public static Rect GetReturnButtonRect()
        {
            const float w = 180f;
            const float h = 36f;
            return new Rect(Margin, Screen.height - h - Margin, w, h);
        }
    }
}
