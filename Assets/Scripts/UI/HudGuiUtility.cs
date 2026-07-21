using UnityEngine;

namespace F89.UI
{
    public static class HudGuiUtility
    {
        public static bool IsRepaintEvent =>
            Event.current != null && Event.current.type == EventType.Repaint;

        public static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness, Texture2D texture)
        {
            if (!IsRepaintEvent)
            {
                return;
            }

            var delta = end - start;
            var length = delta.magnitude;
            if (length < 0.5f)
            {
                return;
            }

            var lineTexture = texture != null ? texture : Texture2D.whiteTexture;
            GUI.color = color;

            if (Mathf.Abs(delta.x) < 0.01f)
            {
                var yMin = Mathf.Min(start.y, end.y);
                GUI.DrawTexture(new Rect(start.x - thickness * 0.5f, yMin, thickness, length), lineTexture);
            }
            else if (Mathf.Abs(delta.y) < 0.01f)
            {
                var xMin = Mathf.Min(start.x, end.x);
                GUI.DrawTexture(new Rect(xMin, start.y - thickness * 0.5f, length, thickness), lineTexture);
            }
            else
            {
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                var rect = new Rect(start.x, start.y - thickness * 0.5f, length, thickness);
                GUIUtility.RotateAroundPivot(angle, start);
                GUI.DrawTexture(rect, lineTexture);
                GUIUtility.RotateAroundPivot(-angle, start);
            }

            GUI.color = Color.white;
        }
    }
}
