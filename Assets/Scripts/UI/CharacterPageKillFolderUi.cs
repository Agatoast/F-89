using F89.Core;
using F89.Enemies;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageKillFolderUi
    {
        private static GUIStyle slotNumberStyle;
        private static GUIStyle unitAbbrevStyle;
        private static GUIStyle killCountStyle;

        public static void DrawFolder(Rect folderRect, CharacterSaveData save, bool troopsFolder)
        {
            EnsureStyles();
            var catalog = VehicleUnitCatalog.LoadOrDefault();

            for (var i = 0; i < CharacterPageLayout.KillFolderSlotCount; i++)
            {
                var slotRect = CharacterPageLayout.GetKillFolderSlotRect(folderRect, i);
                var level = i + 1;
                var kills = troopsFolder
                    ? UrKillCredit.GetTroopKillsAtLevel(save, level)
                    : UrKillCredit.GetVehicleKillsAtLevel(save, level);
                var unit = troopsFolder
                    ? catalog.GetUrTroopByLevel(level)
                    : catalog.GetUrVehicleByLevel(level);

                GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
                GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
                DrawSlotBorder(slotRect, 1f, new Color(0.95f, 0.95f, 0.95f, 0.95f));

                if (kills > 0 && unit != null)
                {
                    DrawUnitIcon(slotRect, unit);
                    DrawKillCountBadge(slotRect, kills);
                }
                else
                {
                    GUI.color = Color.white;
                    GUI.Label(slotRect, level.ToString(), slotNumberStyle);
                }
            }

            GUI.color = Color.white;
        }

        private static void DrawUnitIcon(Rect slotRect, VehicleUnitDefinition unit)
        {
            var inset = InsetRect(slotRect, UiFitCanvas.Px(4f));
            GUI.color = VehicleUnitVisual.GetUnitColor(unit);
            GUI.DrawTexture(inset, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(inset, unit.abbreviation, unitAbbrevStyle);
        }

        private static void DrawKillCountBadge(Rect slotRect, int kills)
        {
            var label = kills.ToString();
            var badgeWidth = Mathf.Max(UiFitCanvas.Px(18f), killCountStyle.CalcSize(new GUIContent(label)).x + UiFitCanvas.Px(6f));
            var badgeHeight = UiFitCanvas.Px(16f);
            var badgeRect = new Rect(
                slotRect.xMax - badgeWidth - UiFitCanvas.Px(2f),
                slotRect.yMax - badgeHeight - UiFitCanvas.Px(2f),
                badgeWidth,
                badgeHeight);

            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(badgeRect, label, killCountStyle);
        }

        private static Rect InsetRect(Rect rect, float inset)
        {
            return new Rect(
                rect.x + inset,
                rect.y + inset,
                Mathf.Max(0f, rect.width - (inset * 2f)),
                Mathf.Max(0f, rect.height - (inset * 2f)));
        }

        private static void DrawSlotBorder(Rect rect, float thickness, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void EnsureStyles()
        {
            if (slotNumberStyle != null)
            {
                return;
            }

            slotNumberStyle = HudStyleFactory.CreateLabel(
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            unitAbbrevStyle = HudStyleFactory.CreateLabel(
                14,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            killCountStyle = HudStyleFactory.CreateLabel(
                12,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
        }
    }
}
