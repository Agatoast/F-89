using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class MilitaryAwardTooltipUi
    {
        private const float OffsetXPx = 14f;
        private const float OffsetYPx = 18f;
        private const float PaddingPx = 10f;
        private const float MaxWidthPx = 300f;
        private const float ScreenMarginPx = 8f;

        private static string hoveredAwardId;
        private static GUIStyle titleStyle;
        private static Texture2D backgroundTexture;

        public static void BeginFrame()
        {
            hoveredAwardId = null;
        }

        public static void RegisterHover(Rect rect, string awardId)
        {
            if (string.IsNullOrWhiteSpace(awardId) || !IsHovered(rect))
            {
                return;
            }

            hoveredAwardId = awardId;
        }

        public static void Draw()
        {
            if (string.IsNullOrWhiteSpace(hoveredAwardId))
            {
                return;
            }

            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!MilitaryRibbonCatalog.TryGetDefinition(hoveredAwardId, out var definition))
            {
                return;
            }

            EnsureStyles();
            DrawTooltip(definition.DisplayName);
        }

        private static void DrawTooltip(string title)
        {
            var titleContent = new GUIContent(title ?? string.Empty);
            var contentWidth = MaxWidthPx - PaddingPx * 2f;
            var titleSize = titleStyle.CalcSize(titleContent);
            titleSize.x = Mathf.Min(titleSize.x, contentWidth);

            var width = titleSize.x + PaddingPx * 2f;
            var height = titleSize.y + PaddingPx * 2f;
            var mouse = Event.current.mousePosition;
            var x = mouse.x + OffsetXPx;
            var y = mouse.y + OffsetYPx;

            if (x + width > Screen.width - ScreenMarginPx)
            {
                x = mouse.x - width - OffsetXPx;
            }

            if (y + height > Screen.height - ScreenMarginPx)
            {
                y = mouse.y - height - OffsetYPx;
            }

            x = Mathf.Clamp(x, ScreenMarginPx, Screen.width - width - ScreenMarginPx);
            y = Mathf.Clamp(y, ScreenMarginPx, Screen.height - height - ScreenMarginPx);

            var rect = new Rect(x, y, width, height);
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(rect, backgroundTexture);
            GUI.color = previousColor;

            var titleRect = new Rect(rect.x + PaddingPx, rect.y + PaddingPx, contentWidth, titleSize.y);
            GUI.Label(titleRect, titleContent, titleStyle);
        }

        private static bool IsHovered(Rect rect)
        {
            if (Event.current == null)
            {
                return false;
            }

            return rect.Contains(Event.current.mousePosition);
        }

        private static void EnsureStyles()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = Texture2D.whiteTexture;
            }

            if (titleStyle == null)
            {
                titleStyle = HudStyleFactory.CreateLabel(
                    14,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    Color.white,
                    wordWrap: true);
            }
        }
    }
}
