using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    /// <summary>Shared DR label/value box drawn on paperdolls (ground HUD, Character Page, Character Loadout).</summary>
    public static class LandPaperdollDrUi
    {
        public static void Draw(Rect labelRect, Rect valueRect)
        {
            var backing = Rect.MinMaxRect(labelRect.xMin, labelRect.yMin, labelRect.xMax, valueRect.yMax);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(backing, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var labelStyle = HudStyleFactory.CreateLabel(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var valueStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            GUI.Label(labelRect, "DR:", labelStyle);
            GUI.Label(valueRect, GetEquippedGearDamageResistance().ToString(), valueStyle);
        }

        /// <summary>Places the DR box on the paperdoll torso (used when equipment slots sit beside the doll).</summary>
        public static void DrawOnPaperdoll(Rect paperdoll, float offsetX = 0f, float offsetY = 0f)
        {
            GetPaperdollTorsoDrRects(paperdoll, out var labelRect, out var valueRect);
            if (offsetX != 0f || offsetY != 0f)
            {
                labelRect.x += offsetX;
                labelRect.y += offsetY;
                valueRect.x += offsetX;
                valueRect.y += offsetY;
            }

            Draw(labelRect, valueRect);
        }

        public static void GetPaperdollTorsoDrRects(Rect paperdoll, out Rect labelRect, out Rect valueRect)
        {
            const float labelHeight = 14f;
            const float valueHeight = 18f;
            var width = Mathf.Max(paperdoll.width * 0.28f, 64f);
            var x = paperdoll.x + (paperdoll.width - width) * 0.5f;
            var y = paperdoll.y + paperdoll.height * 0.42f;
            labelRect = new Rect(x, y, width, labelHeight);
            valueRect = new Rect(x, y + labelHeight, width, valueHeight);
        }

        public static int GetEquippedGearDamageResistance()
        {
            var loadout = CharacterGearSession.ActiveLoadout;
            var catalog = CharacterGearSession.Catalog;
            if (loadout == null || catalog == null)
            {
                return 0;
            }

            var attributes = Object.FindAnyObjectByType<LandPlayerAttributes>();
            if (attributes != null)
            {
                attributes.ApplyEquippedGear(loadout, catalog);
                return attributes.GearDamageResistance;
            }

            var total = 0;
            AddGearDamageResistance(loadout.Helmet, catalog, ref total);
            AddGearDamageResistance(loadout.Core, catalog, ref total);
            AddGearDamageResistance(loadout.Boots, catalog, ref total);
            return total;
        }

        private static void AddGearDamageResistance(LandGearInstance item, LandItemCatalog catalog, ref int total)
        {
            if (!catalog.TryGetGearCombatStats(item, out _, out var damageResistance, out _))
            {
                return;
            }

            total += damageResistance;
        }
    }
}
