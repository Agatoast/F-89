using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Loot bag drawn at the cursor tip (hover) or pinned click position.</summary>
    public static class LandLootBagUi
    {
        private const float CursorOffsetX = 14f;
        private const float CursorOffsetY = 14f;

        private static Rect lastPanelRect;

        public static bool IsPointerOverPanel()
        {
            if (!LandLootBagSession.IsOpen || lastPanelRect.width <= 0f)
            {
                return false;
            }

            var mouse = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return lastPanelRect.Contains(mouse);
        }

        public static void Draw()
        {
            lastPanelRect = default;
            if (!LandLootBagSession.IsOpen)
            {
                return;
            }

            var items = LandLootBagSession.GetOpenItems();
            if (items == null)
            {
                return;
            }

            const float slot = 72f;
            const float pad = 8f;
            const float gap = 6f;
            var panelW = pad * 2f + slot * 2f + gap;
            var panelH = pad * 2f + slot * 2f + gap + 28f;

            Vector2 anchor;
            if (LandLootBagSession.IsPinned)
            {
                anchor = LandLootBagSession.PinnedGuiPosition;
            }
            else if (Event.current != null)
            {
                anchor = Event.current.mousePosition;
            }
            else
            {
                anchor = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            }

            var panel = new Rect(
                anchor.x + CursorOffsetX,
                anchor.y + CursorOffsetY,
                panelW,
                panelH);
            panel.x = Mathf.Clamp(panel.x, 4f, Screen.width - panelW - 4f);
            panel.y = Mathf.Clamp(panel.y, 4f, Screen.height - panelH - 4f);
            lastPanelRect = panel;

            GUI.color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.75f, 0.7f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var titleText = LandLootBagSession.IsPinned ? "LOOT" : "LOOT (click to open)";
            GUI.Label(new Rect(panel.x, panel.y + 4f, panel.width, 22f), titleText, title);

            var catalog = CharacterGearSession.Catalog;
            for (var i = 0; i < LandEnemyLootRules.LootBagSlotCount; i++)
            {
                var col = i % 2;
                var row = i / 2;
                var cell = new Rect(
                    panel.x + pad + col * (slot + gap),
                    panel.y + 28f + pad + row * (slot + gap),
                    slot,
                    slot);

                GUI.color = new Color(0.18f, 0.2f, 0.24f, 0.95f);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = Color.white;

                var item = i < items.Count ? items[i] : null;
                if (!LandLoadoutSlots.IsValidItem(item))
                {
                    continue;
                }

                LandItemTileOverlay.Draw(
                    cell,
                    item,
                    catalog,
                    11,
                    Color.white,
                    scaleFonts: false,
                    paintRarityFill: true);

                // Only take items once the bag is pinned open.
                if (LandLootBagSession.IsPinned
                    && GUI.Button(cell, GUIContent.none, GUIStyle.none))
                {
                    LandLootBagSession.TryTakeItem(i);
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                LandLootBagSession.Close();
                Event.current.Use();
            }
        }

        public static void HandleCloseHotkey()
        {
            if (LandLootBagSession.IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                LandLootBagSession.Close();
            }
        }
    }
}
