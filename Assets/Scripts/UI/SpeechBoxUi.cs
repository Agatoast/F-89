using System.Collections.Generic;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Small square speech boxes (~30px) with a green border and optional tail (boss-style callouts).
    /// </summary>
    public static class SpeechBoxUi
    {
        public enum TailSide
        {
            Left,
            Right,
            Top
        }

        public const float BoxSizePx = 30f;
        public const float BorderPx = 3f;
        public const float TailLengthPx = 7f;
        public const float TailHalfWidthPx = 5f;

        private static readonly Color BorderColor = new Color(0x5A / 255f, 0xC3 / 255f, 0x2B / 255f);
        private static readonly Color FillColor = Color.black;

        private static GUIStyle labelStyle;

        /// <param name="boxRect">The 30px square body. The tail extends outside this rect.</param>
        public static void Draw(Rect boxRect, TailSide tail, string text = null, int fontSize = 9, float textOffsetX = 0f)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            GetTailPoints(boxRect, tail, out var tip, out var attachA, out var attachB);
            FillTriangle(tip, attachA, attachB, FillColor);
            GUI.color = FillColor;
            GUI.DrawTexture(boxRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBorder(boxRect, tail, tip, attachA, attachB);

            if (!string.IsNullOrEmpty(text))
            {
                EnsureLabelStyle(fontSize);
                labelStyle.normal.textColor = BorderColor;
                var labelRect = boxRect;
                labelRect.x += textOffsetX;
                GUI.Label(labelRect, text, labelStyle);
            }
        }

        public static void DrawTailLeft(Rect boxRect, string text = null, int fontSize = 9, float textOffsetX = 0f) =>
            Draw(boxRect, TailSide.Left, text, fontSize, textOffsetX);

        public static void DrawTailRight(Rect boxRect, string text = null, int fontSize = 9, float textOffsetX = 0f) =>
            Draw(boxRect, TailSide.Right, text, fontSize, textOffsetX);

        public static void DrawTailTop(Rect boxRect, string text = null, int fontSize = 9, float textOffsetX = 0f) =>
            Draw(boxRect, TailSide.Top, text, fontSize, textOffsetX);

        public static Rect GetBoxRectForTailTip(
            Vector2 tailTip,
            TailSide tail,
            float boxWidth = BoxSizePx,
            float boxHeight = BoxSizePx)
        {
            return tail switch
            {
                TailSide.Left => new Rect(tailTip.x + TailLengthPx, tailTip.y - boxHeight * 0.5f, boxWidth, boxHeight),
                TailSide.Right => new Rect(tailTip.x - TailLengthPx - boxWidth, tailTip.y - boxHeight * 0.5f, boxWidth, boxHeight),
                TailSide.Top => new Rect(tailTip.x - boxWidth * 0.5f, tailTip.y + TailLengthPx, boxWidth, boxHeight),
                _ => new Rect(tailTip.x, tailTip.y, boxWidth, boxHeight)
            };
        }

        private static void GetTailPoints(Rect box, TailSide tail, out Vector2 tip, out Vector2 attachA, out Vector2 attachB)
        {
            switch (tail)
            {
                case TailSide.Right:
                    tip = new Vector2(box.xMax + TailLengthPx, box.center.y);
                    attachA = new Vector2(box.xMax, box.center.y - TailHalfWidthPx);
                    attachB = new Vector2(box.xMax, box.center.y + TailHalfWidthPx);
                    break;

                case TailSide.Top:
                    tip = new Vector2(box.center.x, box.yMin - TailLengthPx);
                    attachA = new Vector2(box.center.x - TailHalfWidthPx, box.yMin);
                    attachB = new Vector2(box.center.x + TailHalfWidthPx, box.yMin);
                    break;

                default:
                    tip = new Vector2(box.xMin - TailLengthPx, box.center.y);
                    attachA = new Vector2(box.xMin, box.center.y - TailHalfWidthPx);
                    attachB = new Vector2(box.xMin, box.center.y + TailHalfWidthPx);
                    break;
            }
        }

        private static void DrawBorder(Rect box, TailSide tail, Vector2 tip, Vector2 attachA, Vector2 attachB)
        {
            var t = BorderPx;

            if (tail == TailSide.Top)
            {
                DrawBar(box.xMin, box.yMin, attachA.x - box.xMin, t);
                DrawBar(attachB.x, box.yMin, box.xMax - attachB.x, t);
            }
            else
            {
                DrawBar(box.xMin, box.yMin, box.width, t);
            }

            DrawBar(box.xMin, box.yMax - t, box.width, t);

            if (tail == TailSide.Left)
            {
                DrawBar(box.xMin, box.yMin, t, attachA.y - box.yMin);
                DrawBar(box.xMin, attachB.y, t, box.yMax - attachB.y);
            }
            else
            {
                DrawBar(box.xMin, box.yMin, t, box.height);
            }

            if (tail == TailSide.Right)
            {
                DrawBar(box.xMax - t, box.yMin, t, attachA.y - box.yMin);
                DrawBar(box.xMax - t, attachB.y, t, box.yMax - attachB.y);
            }
            else
            {
                DrawBar(box.xMax - t, box.yMin, t, box.height);
            }

            DrawLine(tip, attachA, t);
            DrawLine(tip, attachB, t);
        }

        private static void DrawBar(float x, float y, float width, float height)
        {
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            var previous = GUI.color;
            GUI.color = BorderColor;
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawLine(Vector2 from, Vector2 to, float thickness)
        {
            HudGuiUtility.DrawScreenLine(from, to, BorderColor, thickness, Texture2D.whiteTexture);
        }

        private static void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;

            var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y));
            var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y));
            for (var y = minY; y <= maxY; y++)
            {
                var scanY = y + 0.5f;
                var xs = GatherScanlineIntersections(scanY, a, b, c);
                if (xs.Count >= 2)
                {
                    xs.Sort();
                    var x0 = xs[0];
                    var x1 = xs[xs.Count - 1];
                    if (x1 > x0)
                    {
                        GUI.DrawTexture(new Rect(x0, y, x1 - x0, 1f), Texture2D.whiteTexture);
                    }
                }
            }

            GUI.color = previous;
        }

        private static List<float> GatherScanlineIntersections(
            float scanY,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            var xs = new List<float>(3);
            TryAddEdgeIntersection(scanY, a, b, xs);
            TryAddEdgeIntersection(scanY, a, c, xs);
            TryAddEdgeIntersection(scanY, b, c, xs);
            return xs;
        }

        private static void TryAddEdgeIntersection(float scanY, Vector2 p0, Vector2 p1, List<float> xs)
        {
            if (Mathf.Abs(p0.y - p1.y) < 0.001f)
            {
                return;
            }

            var yMin = Mathf.Min(p0.y, p1.y);
            var yMax = Mathf.Max(p0.y, p1.y);
            if (scanY < yMin || scanY >= yMax)
            {
                return;
            }

            var t = (scanY - p0.y) / (p1.y - p0.y);
            xs.Add(Mathf.Lerp(p0.x, p1.x, t));
        }

        private static void EnsureLabelStyle(int fontSize)
        {
            if (labelStyle != null && labelStyle.fontSize == fontSize)
            {
                return;
            }

            labelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                BorderColor);
        }
    }
}
