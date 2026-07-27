using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>MTAU rarity tile colors — White (tech 1) through Gold (tech 10).</summary>
    public static class LandItemRarityColors
    {
        public static Color GetTile(LandItemRarity rarity)
        {
            return rarity switch
            {
                LandItemRarity.White => new Color(0.92f, 0.94f, 0.98f),
                LandItemRarity.Green => new Color(0.35f, 0.92f, 0.45f),
                LandItemRarity.Blue => new Color(0.35f, 0.65f, 1f),
                LandItemRarity.Purple => new Color(0.72f, 0.45f, 0.98f),
                LandItemRarity.Yellow => new Color(0.98f, 0.88f, 0.25f),
                LandItemRarity.Orange => new Color(1f, 165f / 255f, 0f),
                LandItemRarity.Red => new Color(1f, 8f / 255f, 0f),
                LandItemRarity.Crimson => new Color(124f / 255f, 10f / 255f, 2f / 255f),
                LandItemRarity.Black => new Color(0f, 0f, 0f),
                LandItemRarity.Gold => new Color(1f, 215f / 255f, 0f),
                _ => new Color(0.78f, 0.82f, 0.88f)
            };
        }

        public static Color GetLabelColor(LandItemRarity rarity) => GetOverlayColor(rarity);

        /// <summary>
        /// Black silhouette on light rarity tiles; white when the tile is too dark for black.
        /// Level 4 always black; levels 8–9 (Crimson / Black) always white.
        /// </summary>
        public static Color GetOverlayColor(LandItemRarity rarity)
        {
            var level = LandTechLevelRules.GetTechLevel(rarity);
            if (level == 4)
            {
                return Color.black;
            }

            if (level is 8 or 9)
            {
                return Color.white;
            }

            var tile = GetTile(rarity);
            var luminance = (0.2126f * tile.r) + (0.7152f * tile.g) + (0.0722f * tile.b);
            return luminance < 0.55f ? Color.white : Color.black;
        }

        /// <summary>Corner level number: matches overlay, with level 4 forced black.</summary>
        public static Color GetTechLevelNumberColor(LandItemRarity rarity)
        {
            var level = LandTechLevelRules.GetTechLevel(rarity);
            if (level == 4)
            {
                return Color.black;
            }

            return GetOverlayColor(rarity);
        }
    }
}
