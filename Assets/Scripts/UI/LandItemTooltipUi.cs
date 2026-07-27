using System.Collections.Generic;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Hover tooltip for land gear/weapon tiles.
    /// Uniform square, 4× the hovered tile size, centered text.
    /// Ground HUD: fixed panel between minimap and Bandages/Grenades.
    /// Character Page / Character Loadout: above the cursor.
    /// </summary>
    public static class LandItemTooltipUi
    {
        public enum Placement
        {
            AboveCursor = 0,
            GroundHudPanel = 1
        }

        private const float CursorOffsetY = 10f;
        private const float Padding = 12f;
        private const float NameTypeGap = 2f;
        private const float AttrLineGap = 4f;
        private const float ScreenMargin = 8f;
        private const float TipSizeMultiplier = 2f;
        private const float FallbackTileSize = 52f;

        private static LandGearInstance hoveredItem;
        private static float hoveredTileSize = FallbackTileSize;
        private static GUIStyle nameStyle;
        private static GUIStyle bodyStyle;
        private static readonly List<string> attributeLines = new();

        public static void BeginFrame()
        {
            hoveredItem = null;
            hoveredTileSize = FallbackTileSize;
        }

        public static void RegisterHover(Rect cell, LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item) || Event.current == null)
            {
                return;
            }

            if (CharacterPageGearUi.IsDraggingGear)
            {
                return;
            }

            if (!cell.Contains(Event.current.mousePosition))
            {
                return;
            }

            hoveredItem = item;
            hoveredTileSize = Mathf.Max(1f, Mathf.Min(cell.width, cell.height));
        }

        public static float GetTipSize() =>
            Mathf.Max(1f, hoveredTileSize * TipSizeMultiplier);

        public static void Draw(Placement placement)
        {
            if (!LandLoadoutSlots.IsValidItem(hoveredItem))
            {
                return;
            }

            if (Event.current != null
                && Event.current.type != EventType.Repaint
                && Event.current.type != EventType.Layout)
            {
                return;
            }

            EnsureStyles();
            if (!TryBuildContent(hoveredItem, out var name, out var typeLine, attributeLines))
            {
                return;
            }

            var tipSize = GetTipSize();
            ScaleFontsForTip(tipSize);

            Rect tipRect;
            if (placement == Placement.GroundHudPanel)
            {
                tipRect = LandCombatHudLayout.GetItemTooltipRect(tipSize);
            }
            else
            {
                tipRect = GetAboveCursorRect(tipSize);
            }

            DrawSquareTip(tipRect, name, typeLine, attributeLines);
        }

        private static Rect GetAboveCursorRect(float tipSize)
        {
            var mouse = Event.current.mousePosition;
            var x = mouse.x - tipSize * 0.5f;
            var y = mouse.y - tipSize - CursorOffsetY;
            x = Mathf.Clamp(x, ScreenMargin, Screen.width - tipSize - ScreenMargin);
            y = Mathf.Clamp(y, ScreenMargin, Screen.height - tipSize - ScreenMargin);
            return new Rect(x, y, tipSize, tipSize);
        }

        private static bool TryBuildContent(
            LandGearInstance item,
            out string name,
            out string typeLine,
            List<string> attributes)
        {
            name = string.Empty;
            typeLine = string.Empty;
            attributes.Clear();
            var catalog = CharacterGearSession.Catalog;
            if (catalog == null || !LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            name = catalog.GetDisplayName(item);
            if (!LandLoadoutEquipService.TryResolveItemSlot(item, catalog, out var slot))
            {
                typeLine = "Item";
                return true;
            }

            typeLine = slot switch
            {
                LandEquipmentSlot.Helmet => "Helmet",
                LandEquipmentSlot.Core => "Vest",
                LandEquipmentSlot.Weapon => "Weapon",
                LandEquipmentSlot.Boots => "Boots",
                _ => "Item"
            };

            if (slot == LandEquipmentSlot.Weapon)
            {
                if (catalog.TryGetWeapon(item.DefinitionId, out var weapon))
                {
                    attributes.Add($"{LandItemStatFormatter.FormatStatLabel(LandItemStat.Damage)} {weapon.Damage:0}");
                    attributes.Add($"{LandItemStatFormatter.FormatStatLabel(LandItemStat.Range)} {weapon.Range:0}");
                }

                return true;
            }

            if (catalog.TryGetGearCombatStats(item, out _, out var dr, out var move))
            {
                // DR always above Move when both are shown.
                attributes.Add($"{LandItemStatFormatter.FormatStatLabel(LandItemStat.DamageResistance)} {dr}");
                if (slot == LandEquipmentSlot.Boots)
                {
                    attributes.Add($"{LandItemStatFormatter.FormatStatLabel(LandItemStat.Move)} {move}");
                }
            }

            return true;
        }

        private static void DrawSquareTip(Rect rect, string name, string typeLine, List<string> attributes)
        {
            if (rect.width < 8f || rect.height < 8f)
            {
                return;
            }

            var previous = GUI.color;
            GUI.color = new Color(0.06f, 0.09f, 0.12f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.78f, 0.95f, 0.95f);
            HudGuiUtility.DrawWireBox(rect, 1.5f);
            GUI.color = previous;

            var inner = new Rect(
                rect.x + Padding,
                rect.y + Padding,
                rect.width - Padding * 2f,
                rect.height - Padding * 2f);

            var nameHeight = Mathf.Max(nameStyle.lineHeight, nameStyle.fontSize + 4f);
            var typeHeight = Mathf.Max(bodyStyle.lineHeight, bodyStyle.fontSize + 4f);
            var attrLineHeight = Mathf.Max(bodyStyle.lineHeight, bodyStyle.fontSize + 4f);

            // Name, then type directly beneath.
            var headerY = inner.y;
            GUI.Label(new Rect(inner.x, headerY, inner.width, nameHeight), name, nameStyle);
            headerY += nameHeight + NameTypeGap;
            GUI.Label(new Rect(inner.x, headerY, inner.width, typeHeight), typeLine, bodyStyle);

            if (attributes == null || attributes.Count == 0)
            {
                return;
            }

            // Attributes stacked (DR on top), vertically centered in the space below the header.
            var attrCount = attributes.Count;
            var attrsBlockHeight = attrCount * attrLineHeight + (attrCount - 1) * AttrLineGap;
            var regionTop = headerY + typeHeight;
            var regionHeight = inner.yMax - regionTop;
            var attrsY = regionTop + Mathf.Max(0f, (regionHeight - attrsBlockHeight) * 0.5f);

            for (var i = 0; i < attrCount; i++)
            {
                GUI.Label(
                    new Rect(inner.x, attrsY + i * (attrLineHeight + AttrLineGap), inner.width, attrLineHeight),
                    attributes[i],
                    bodyStyle);
            }
        }

        private static void ScaleFontsForTip(float tipSize)
        {
            var nameSize = Mathf.Clamp(Mathf.RoundToInt(tipSize * 0.12f), 14, 36);
            var bodySize = Mathf.Clamp(Mathf.RoundToInt(tipSize * 0.09f), 12, 28);
            nameStyle.fontSize = nameSize;
            bodyStyle.fontSize = bodySize;
        }

        private static void EnsureStyles()
        {
            if (nameStyle != null)
            {
                return;
            }

            nameStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white,
                wordWrap: true);
            bodyStyle = HudStyleFactory.CreateLabel(
                14,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.86f, 0.9f, 0.94f),
                wordWrap: true);
        }
    }
}
