using F89.UI;
using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Fixed fuel-alignment console anchored to the bottom center of the belly window.
    /// </summary>
    public static class MidAirRefuelConsoleOverlay
    {
        private const string ConsolePath = "MidAirRefuel/refuel_console";
        private const float ConsoleScale = 0.5f;
        private const float WindowWidthFraction = 0.88f * ConsoleScale;
        private const float WindowHeightFraction = 0.48f * ConsoleScale;
        private const float IndicatorTextOffsetX = 1f;
        private const float ConsoleVerticalOffsetPx = -5f;

        // Normalized to refuel_console.png art (342 x 111).
        private const float LeftGaugeXNorm = 0.111f;
        private const float RightGaugeXNorm = 0.880f;
        private const float BottomGaugeYNorm = 0.712f;
        private const float SideTravelMinYNorm = 0.198f;
        private const float SideTravelMaxYNorm = 0.676f;
        private const float SideRedTopYNorm = 0.072f;
        private const float SideRedBottomYNorm = 0.928f;
        private const float BottomTravelMinXNorm = 0.404f;
        private const float BottomTravelMaxXNorm = 0.599f;
        private const float IndicatorBoxWidthPx = 46f;
        private const float IndicatorBoxHeightPx = SpeechBoxUi.BoxSizePx;
        private const int IndicatorFontSize = 18;
        private const int SideReadingMax = 100;
        private const int HorizontalReadingMax = 12;

        private static Texture2D consoleTexture;
        private static Vector2 boomHubScreen;
        private static Rect boomMoveBounds;
        private static bool hasBoomSample;

        public static void SetBoomHub(Vector2 hubScreen, Rect moveBounds)
        {
            boomHubScreen = hubScreen;
            boomMoveBounds = moveBounds;
            hasBoomSample = moveBounds.width > 0f && moveBounds.height > 0f;
        }

        public static bool TryGetConsoleRect(out Rect rect)
        {
            EnsureLoaded();
            if (consoleTexture == null)
            {
                rect = default;
                return false;
            }

            var window = MidAirRefuelViewport.GetWindowRect();
            var maxWidth = window.width * WindowWidthFraction;
            var maxHeight = window.height * WindowHeightFraction;
            var aspect = consoleTexture.height / (float)consoleTexture.width;
            var width = maxWidth;
            var height = width * aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height / aspect;
            }

            rect = new Rect(
                window.center.x - width * 0.5f,
                window.yMax - height + ConsoleVerticalOffsetPx,
                width,
                height);
            return true;
        }

        public static void Draw()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureLoaded();
            if (consoleTexture == null || !TryGetConsoleRect(out var rect))
            {
                return;
            }

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = -2000;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, consoleTexture, ScaleMode.ScaleToFit, alphaBlend: true);
            DrawGaugeIndicators(rect);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private static void DrawGaugeIndicators(Rect console)
        {
            if (!hasBoomSample)
            {
                return;
            }

            var boomVerticalT = GetBoomVerticalT();
            var leftDisplayNorm = GetLeftDisplayNorm(boomVerticalT);
            var rightDisplayNorm = GetRightDisplayNorm(boomVerticalT);
            var bottomXNorm = MapBoomXToBottomGaugeNorm();
            var leftReading = GetLeftReading(boomVerticalT);
            var rightReading = GetRightReading(boomVerticalT);
            var horizontalReading = GetHorizontalReading();

            var leftTip = new Vector2(
                console.x + console.width * LeftGaugeXNorm,
                console.y + console.height * leftDisplayNorm);
            var rightTip = new Vector2(
                console.x + console.width * RightGaugeXNorm,
                console.y + console.height * rightDisplayNorm);
            var bottomTip = new Vector2(
                console.x + console.width * bottomXNorm,
                console.y + console.height * BottomGaugeYNorm);

            SpeechBoxUi.DrawTailRight(
                SpeechBoxUi.GetBoxRectForTailTip(
                    leftTip,
                    SpeechBoxUi.TailSide.Right,
                    IndicatorBoxWidthPx,
                    IndicatorBoxHeightPx),
                FormatReading(leftReading),
                IndicatorFontSize,
                IndicatorTextOffsetX);
            SpeechBoxUi.DrawTailLeft(
                SpeechBoxUi.GetBoxRectForTailTip(
                    rightTip,
                    SpeechBoxUi.TailSide.Left,
                    IndicatorBoxWidthPx,
                    IndicatorBoxHeightPx),
                FormatReading(rightReading),
                IndicatorFontSize,
                IndicatorTextOffsetX);
            SpeechBoxUi.DrawTailTop(
                SpeechBoxUi.GetBoxRectForTailTip(
                    bottomTip,
                    SpeechBoxUi.TailSide.Top,
                    IndicatorBoxWidthPx,
                    IndicatorBoxHeightPx),
                FormatReading(horizontalReading),
                IndicatorFontSize,
                IndicatorTextOffsetX);
        }

        private static float GetBoomVerticalT()
        {
            return Mathf.InverseLerp(boomMoveBounds.yMax, boomMoveBounds.yMin, boomHubScreen.y);
        }

        private static float GetLeftDisplayNorm(float boomVerticalT)
        {
            var unclamped = Mathf.Lerp(SideTravelMaxYNorm, SideRedTopYNorm, boomVerticalT);
            return Mathf.Clamp(unclamped, SideTravelMinYNorm, SideTravelMaxYNorm);
        }

        private static float GetRightDisplayNorm(float boomVerticalT)
        {
            var unclamped = Mathf.Lerp(SideTravelMinYNorm, SideTravelMaxYNorm, boomVerticalT);
            return Mathf.Clamp(unclamped, SideTravelMinYNorm, SideTravelMaxYNorm);
        }

        private static int GetLeftReading(float boomVerticalT)
        {
            var unclamped = Mathf.Lerp(SideTravelMaxYNorm, SideRedTopYNorm, boomVerticalT);
            return ScaleSideReading(unclamped - SideTravelMinYNorm);
        }

        private static int GetRightReading(float boomVerticalT)
        {
            var unclamped = Mathf.Lerp(SideTravelMinYNorm, SideRedBottomYNorm, boomVerticalT);
            return ScaleSideReading(SideTravelMaxYNorm - unclamped);
        }

        private static int GetHorizontalReading()
        {
            var centerX = boomMoveBounds.xMin + boomMoveBounds.width * 0.5f;
            var halfWidth = boomMoveBounds.width * 0.5f;
            if (halfWidth <= 0f)
            {
                return 0;
            }

            var normalized = (boomHubScreen.x - centerX) / halfWidth;
            return Mathf.Clamp(Mathf.RoundToInt(normalized * HorizontalReadingMax), -HorizontalReadingMax, HorizontalReadingMax);
        }

        private static int ScaleSideReading(float offsetNorm)
        {
            var sideSpan = SideTravelMaxYNorm - SideTravelMinYNorm;
            if (sideSpan <= 0f)
            {
                return 0;
            }

            return Mathf.RoundToInt(offsetNorm / sideSpan * SideReadingMax);
        }

        private static float MapBoomXToBottomGaugeNorm()
        {
            var t = Mathf.InverseLerp(boomMoveBounds.xMin, boomMoveBounds.xMax, boomHubScreen.x);
            return Mathf.Lerp(BottomTravelMinXNorm, BottomTravelMaxXNorm, t);
        }

        private static string FormatReading(int value) => value.ToString();

        private static void EnsureLoaded()
        {
            if (consoleTexture != null)
            {
                return;
            }

            consoleTexture = Resources.Load<Texture2D>(ConsolePath);
            if (consoleTexture == null)
            {
                var textures = Resources.LoadAll<Texture2D>("MidAirRefuel");
                for (var i = 0; i < textures.Length; i++)
                {
                    var texture = textures[i];
                    if (texture != null && texture.name == "refuel_console")
                    {
                        consoleTexture = texture;
                        break;
                    }
                }
            }

            if (consoleTexture == null)
            {
                Debug.LogWarning($"F-89 MA Refuel: Missing fuel console at Resources/{ConsolePath}.");
            }
        }
    }
}
