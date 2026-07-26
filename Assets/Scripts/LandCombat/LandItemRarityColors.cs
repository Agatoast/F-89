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

        public static Color GetLabelColor(LandItemRarity rarity)
        {
            return rarity is LandItemRarity.White
                or LandItemRarity.Green
                or LandItemRarity.Yellow
                or LandItemRarity.Orange
                or LandItemRarity.Red
                or LandItemRarity.Gold
                ? Color.black
                : Color.white;
        }

        /// <summary>Tech-level number ink (MTAU: black on White/Green tiles, yellow otherwise).</summary>
        public static Color GetTechLevelNumberColor(LandItemRarity rarity)
        {
            if (rarity is LandItemRarity.White or LandItemRarity.Green)
            {
                return Color.black;
            }

            return new Color(1f, 0.92f, 0.08f, 1f);
        }
    }
}
