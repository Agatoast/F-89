using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterNameBarUi
    {
        public static void Draw(Rect rect, CharacterSaveData save, GUIStyle labelStyle)
        {
            var label = save != null ? save.DisplayRankAndName : "NO CHARACTER";
            if (save == null)
            {
                GUI.Label(rect, label, labelStyle);
                return;
            }

            var texture = PilotRankInsigniaService.GetInsigniaTexture(save.Rank);
            if (texture == null)
            {
                GUI.Label(rect, label, labelStyle);
                return;
            }

            var maxHeight = rect.height * 0.92f;
            var maxWidth = rect.width * 0.44f;
            var insigniaBounds = new Rect(
                CharacterPageLayout.GetRibbonArrayLeftX(),
                rect.y + (rect.height - maxHeight) * 0.5f,
                maxWidth,
                maxHeight);
            var insigniaRect = GetLeftAlignedInsigniaRect(insigniaBounds, texture);
            var prevColor = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(insigniaRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, alphaBlend: false);
            GUI.color = prevColor;
            UiTextureFit.DrawTextureExact(insigniaRect, texture, alphaBlend: false);

            var gap = rect.height * 0.08f;
            var textRect = new Rect(
                insigniaRect.xMax + gap,
                rect.y,
                rect.xMax - insigniaRect.xMax - gap,
                rect.height);
            GUI.Label(textRect, label, labelStyle);
        }

        private static Rect GetLeftAlignedInsigniaRect(Rect bounds, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return bounds;
            }

            var aspect = texture.height / (float)texture.width;
            var height = bounds.height;
            var width = height / aspect;
            if (width > bounds.width)
            {
                width = bounds.width;
                height = width * aspect;
            }

            var y = bounds.y + (bounds.height - height) * 0.5f;
            return new Rect(bounds.x, y, width, height);
        }
    }
}
