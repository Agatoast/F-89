using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Shared OnGUI item-tile overlays: category silhouette, name bottom, level upper-right.
    /// Black silhouettes tint white when the rarity tile is too dark for black ink.
    /// </summary>
    public static class LandItemTileOverlay
    {
        private const string OverlayRoot = "LandCombat/Overlays/";
        private const string HelmetOverlayPath = OverlayRoot + "helmet";
        private const string VestOverlayPath = OverlayRoot + "vest";
        private const string WeaponOverlayPath = OverlayRoot + "weapon";
        private const string BootsOverlayPath = OverlayRoot + "boots";
        private const string GrenadeOverlayPath = OverlayRoot + "grenade";
        private const string BandageOverlayPath = OverlayRoot + "bandage";

        private static Texture2D helmetOverlay;
        private static Texture2D vestOverlay;
        private static Texture2D weaponOverlay;
        private static Texture2D bootsOverlay;
        private static Texture2D grenadeOverlay;
        private static Texture2D bandageOverlay;
        private static bool overlaysLoaded;

        public static void Draw(
            Rect cell,
            LandGearInstance item,
            LandItemCatalog catalog,
            int baseNameFontSize,
            Color labelColor,
            bool scaleFonts = false,
            bool paintRarityFill = false)
        {
            if (!LandLoadoutSlots.IsValidItem(item) || catalog == null)
            {
                return;
            }

            EnsureOverlaysLoaded();

            if (LandConsumableIds.IsConsumable(item))
            {
                DrawConsumableTile(cell, item, catalog, baseNameFontSize, labelColor, scaleFonts, paintRarityFill);
                return;
            }

            if (!TryResolveCategory(item, catalog, out var slot))
            {
                DrawGeneric(cell, item, catalog, baseNameFontSize, labelColor, scaleFonts);
                return;
            }

            if (paintRarityFill)
            {
                GUI.color = LandItemRarityColors.GetTile(item.Rarity);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var overlayColor = LandItemRarityColors.GetOverlayColor(item.Rarity);
            DrawCategoryTile(cell, item, catalog, slot, baseNameFontSize, overlayColor, scaleFonts);
        }

        public static Texture2D GetGrenadeIcon()
        {
            EnsureOverlaysLoaded();
            return grenadeOverlay;
        }

        public static Texture2D GetBandageIcon()
        {
            EnsureOverlaysLoaded();
            return bandageOverlay;
        }

        public static bool IsWeapon(LandGearInstance item, LandItemCatalog catalog) =>
            TryResolveCategory(item, catalog, out var slot) && slot == LandEquipmentSlot.Weapon;

        public static bool HasCategoryOverlay(LandGearInstance item, LandItemCatalog catalog) =>
            TryResolveCategory(item, catalog, out _);

        private static void DrawCategoryTile(
            Rect cell,
            LandGearInstance item,
            LandItemCatalog catalog,
            LandEquipmentSlot slot,
            int baseNameFontSize,
            Color overlayColor,
            bool scaleFonts)
        {
            var nameFontSize = Scale(baseNameFontSize, scaleFonts);
            var levelFontSize = Scale(Mathf.Max(8, baseNameFontSize), scaleFonts);
            var level = LandTechLevelRules.GetTechLevel(item);

            var overlay = GetOverlayTexture(slot);
            if (overlay != null)
            {
                var pad = Mathf.Max(2f, cell.width * 0.08f);
                var iconRect = new Rect(
                    cell.x + pad,
                    cell.y + pad,
                    cell.width - pad * 2f,
                    cell.height - pad * 2f);
                var previous = GUI.color;
                GUI.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
                GUI.DrawTexture(iconRect, overlay, ScaleMode.ScaleToFit, alphaBlend: true);
                GUI.color = previous;
            }
            else
            {
                var categoryFontSize = Scale(Mathf.Max(8, baseNameFontSize + 1), scaleFonts);
                var categoryStyle = HudStyleFactory.CreateLabel(
                    categoryFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    overlayColor,
                    wordWrap: false);
                GUI.Label(cell, GetFallbackCategoryLabel(slot), categoryStyle);
            }

            // Level sits in the free corner on the rarity tile — match overlay ink (black on L1).
            // Vest name sits on the silhouette, so invert that ink only.
            var levelColor = overlayColor;
            var nameColor = slot == LandEquipmentSlot.Core
                ? InvertInk(overlayColor)
                : overlayColor;

            var levelStyle = HudStyleFactory.CreateLabel(
                levelFontSize,
                FontStyle.Bold,
                TextAnchor.UpperRight,
                levelColor,
                wordWrap: false);
            var nameAnchor = slot == LandEquipmentSlot.Core ? TextAnchor.MiddleCenter : TextAnchor.LowerCenter;
            var nameStyle = HudStyleFactory.CreateLabel(
                nameFontSize,
                FontStyle.Bold,
                nameAnchor,
                nameColor,
                wordWrap: true);

            var inset = 2f;
            var levelRect = new Rect(cell.x + inset, cell.y + 1f, cell.width - inset * 2f - 1f, levelFontSize + 4f);
            var nameRect = slot == LandEquipmentSlot.Core
                ? new Rect(
                    cell.x + inset,
                    cell.y + cell.height * 0.22f,
                    cell.width - inset * 2f,
                    cell.height * 0.34f)
                : new Rect(
                    cell.x + inset,
                    cell.y + cell.height * 0.62f,
                    cell.width - inset * 2f,
                    cell.height * 0.35f);

            if (slot == LandEquipmentSlot.Boots)
            {
                if (LandItemCategoryRules.TryResolve(item, catalog, out var category)
                    && category == LandItemCategory.UltimateReich)
                {
                    var urStyle = HudStyleFactory.CreateLabel(
                        nameFontSize,
                        FontStyle.Bold,
                        TextAnchor.LowerCenter,
                        new Color(0.92f, 0.12f, 0.12f),
                        wordWrap: false);
                    GUI.Label(nameRect, "UR", urStyle);
                }
            }
            else
            {
                GUI.Label(nameRect, catalog.GetDisplayName(item), nameStyle);
            }

            // Draw level last so vest center-name never covers the corner badge.
            GUI.Label(levelRect, level.ToString(), levelStyle);
        }

        private static Color InvertInk(Color overlayColor) =>
            overlayColor.r + overlayColor.g + overlayColor.b > 1.5f ? Color.black : Color.white;

        private static void DrawGeneric(
            Rect cell,
            LandGearInstance item,
            LandItemCatalog catalog,
            int baseNameFontSize,
            Color labelColor,
            bool scaleFonts)
        {
            var nameFontSize = Scale(baseNameFontSize, scaleFonts);
            var statFontSize = Scale(Mathf.Max(7, baseNameFontSize - 2), scaleFonts);
            var nameStyle = HudStyleFactory.CreateLabel(
                nameFontSize,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                labelColor,
                wordWrap: true);
            var statStyle = HudStyleFactory.CreateLabel(
                statFontSize,
                FontStyle.Normal,
                TextAnchor.LowerCenter,
                labelColor,
                wordWrap: true);

            var nameRect = new Rect(cell.x, cell.y + 2f, cell.width, cell.height * 0.55f);
            var statRect = new Rect(cell.x, cell.y + cell.height * 0.45f, cell.width, cell.height * 0.5f);

            GUI.Label(nameRect, catalog.GetDisplayName(item), nameStyle);
            if (catalog.TryGetWeaponSummary(item, out var summary)
                || catalog.TryGetGearSummary(item, out summary))
            {
                GUI.Label(statRect, summary, statStyle);
            }
        }

        private static void DrawConsumableTile(
            Rect cell,
            LandGearInstance item,
            LandItemCatalog catalog,
            int baseNameFontSize,
            Color labelColor,
            bool scaleFonts,
            bool paintRarityFill)
        {
            if (paintRarityFill)
            {
                GUI.color = LandItemRarityColors.GetTile(item.Rarity);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var icon = LandConsumableIds.IsGrenade(item) ? grenadeOverlay : bandageOverlay;
            if (icon != null)
            {
                var pad = Mathf.Max(2f, cell.width * 0.08f);
                var iconRect = new Rect(
                    cell.x + pad,
                    cell.y + pad,
                    cell.width - pad * 2f,
                    cell.height - pad * 2f - 14f);
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
            }

            var nameFontSize = Scale(baseNameFontSize, scaleFonts);
            var nameStyle = HudStyleFactory.CreateLabel(
                nameFontSize,
                FontStyle.Bold,
                TextAnchor.LowerCenter,
                labelColor,
                wordWrap: true);
            GUI.Label(
                new Rect(cell.x + 2f, cell.y + cell.height * 0.68f, cell.width - 4f, cell.height * 0.3f),
                catalog.GetDisplayName(item),
                nameStyle);
        }

        private static bool TryResolveCategory(
            LandGearInstance item,
            LandItemCatalog catalog,
            out LandEquipmentSlot slot)
        {
            return LandLoadoutEquipService.TryResolveItemSlot(item, catalog, out slot)
                   && (slot == LandEquipmentSlot.Weapon
                       || slot == LandEquipmentSlot.Helmet
                       || slot == LandEquipmentSlot.Core
                       || slot == LandEquipmentSlot.Boots);
        }

        private static Texture2D GetOverlayTexture(LandEquipmentSlot slot) =>
            slot switch
            {
                LandEquipmentSlot.Weapon => weaponOverlay,
                LandEquipmentSlot.Helmet => helmetOverlay,
                LandEquipmentSlot.Core => vestOverlay,
                LandEquipmentSlot.Boots => bootsOverlay,
                _ => null
            };

        private static string GetFallbackCategoryLabel(LandEquipmentSlot slot) =>
            slot switch
            {
                LandEquipmentSlot.Weapon => "weapon",
                LandEquipmentSlot.Helmet => "helmet",
                LandEquipmentSlot.Core => "vest",
                LandEquipmentSlot.Boots => "boots",
                _ => string.Empty
            };

        private static void EnsureOverlaysLoaded()
        {
            if (overlaysLoaded)
            {
                return;
            }

            // Source art is white-with-alpha so GUI.color can tint black or white.
            helmetOverlay = Resources.Load<Texture2D>(HelmetOverlayPath);
            vestOverlay = Resources.Load<Texture2D>(VestOverlayPath);
            weaponOverlay = Resources.Load<Texture2D>(WeaponOverlayPath);
            bootsOverlay = Resources.Load<Texture2D>(BootsOverlayPath);
            grenadeOverlay = Resources.Load<Texture2D>(GrenadeOverlayPath);
            bandageOverlay = Resources.Load<Texture2D>(BandageOverlayPath);
            overlaysLoaded = true;
        }

        private static int Scale(int fontSize, bool scaleFonts) =>
            scaleFonts ? CharacterPageStyles.ScaleSlotFont(fontSize) : fontSize;
    }
}
