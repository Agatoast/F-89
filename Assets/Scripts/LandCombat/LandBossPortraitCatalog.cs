using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Maps boss numbers to briefing and Character Page portrait assets under Resources/LandCombat/.</summary>
    public static class LandBossPortraitCatalog
    {
        /// <summary>Boss 10 uses the Boss20 portrait asset.</summary>
        public static string GetPortraitResourcePath(int bossNumber)
        {
            bossNumber = Mathf.Clamp(
                bossNumber,
                LandBossEncounter.FirstBossNumber,
                LandBossEncounter.LastBossNumber);
            return bossNumber == 10 ? "LandCombat/Boss20" : $"LandCombat/Boss{bossNumber}";
        }

        public static Texture2D LoadPortrait(int bossNumber)
        {
            var resourcePath = GetPortraitResourcePath(bossNumber);
            var portrait = Resources.Load<Texture2D>(resourcePath);
            if (portrait != null)
            {
                return portrait;
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            return sprite != null ? sprite.texture : null;
        }
    }
}
