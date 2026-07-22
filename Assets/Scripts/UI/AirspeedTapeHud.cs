using F89.Flight;
using UnityEngine;

namespace F89.UI
{
    public class AirspeedTapeHud : MonoBehaviour
    {
        private const float LayoutScale = 0.85f;
        private const float LeftMargin = 18f;
        private const float TapeWidth = 76f;
        private const float TapeHeight = 320f;
        private const float VisibleSpanMph = 200f;
        private const float WindowHeight = 34f;
        private const float MajorTickIntervalMph = 50f;
        private const float MinorTickIntervalMph = 10f;
        private static readonly Color SpeedNumberColor = Color.white;

        [SerializeField] private AircraftController aircraft;

        private GUIStyle tickLabelStyle;
        private GUIStyle speedReadoutStyle;
        private GUIStyle headerStyle;
        private Color lastHudColor = Color.clear;

        public void Configure(AircraftController aircraftController)
        {
            aircraft = aircraftController;
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null
                || aircraft.Profile == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();
            DrawTape(FlightHudColorPalette.Current, aircraft.CurrentSpeedMph);
        }

        private void DrawTape(Color hudColor, float currentSpeed)
        {
            var scale = LayoutScale;
            var tapeWidth = TapeWidth * scale;
            var tapeHeight = TapeHeight * scale;
            var centerY = Screen.height * 0.5f;
            var tapeRect = new Rect(LeftMargin, centerY - tapeHeight * 0.5f, tapeWidth, tapeHeight);
            var pixelsPerMph = tapeHeight / VisibleSpanMph;
            var windowHeight = WindowHeight * scale;
            var minSpeed = currentSpeed - VisibleSpanMph * 0.5f;
            var maxSpeed = currentSpeed + VisibleSpanMph * 0.5f;
            var startTick = Mathf.FloorToInt(minSpeed / MinorTickIntervalMph) * (int)MinorTickIntervalMph;
            var endTick = Mathf.CeilToInt(maxSpeed / MinorTickIntervalMph) * (int)MinorTickIntervalMph;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.08f);
            GUI.DrawTexture(tapeRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            headerStyle.normal.textColor = hudColor;
            GUI.Label(
                new Rect(tapeRect.x, tapeRect.y - 18f * scale, tapeRect.width, 14f * scale),
                "SPD",
                headerStyle);

            GUI.BeginGroup(tapeRect);
            var localCenterY = tapeRect.height * 0.5f;
            var localRailX = tapeRect.width - 6f * scale;

            HudGuiUtility.DrawScreenLine(
                new Vector2(localRailX, 0f),
                new Vector2(localRailX, tapeRect.height),
                new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f),
                1.5f * scale,
                Texture2D.whiteTexture);

            tickLabelStyle.normal.textColor = SpeedNumberColor;
            for (var speed = startTick; speed <= endTick; speed += (int)MinorTickIntervalMph)
            {
                if (speed < 0)
                {
                    continue;
                }

                var tickY = localCenterY - (speed - currentSpeed) * pixelsPerMph;
                if (tickY < -12f * scale || tickY > tapeRect.height + 12f * scale)
                {
                    continue;
                }

                var isMajor = Mathf.Abs(speed % MajorTickIntervalMph) < 0.01f;
                var isMedium = !isMajor && Mathf.Abs(speed % 20f) < 0.01f;
                var tickLength = (isMajor ? 18f : isMedium ? 12f : 7f) * scale;

                HudGuiUtility.DrawScreenLine(
                    new Vector2(localRailX - tickLength, tickY),
                    new Vector2(localRailX, tickY),
                    hudColor,
                    isMajor ? 2f * scale : 1f * scale,
                    Texture2D.whiteTexture);

                if (isMajor)
                {
                    GUI.Label(
                        new Rect(0f, tickY - 8f * scale, localRailX - tickLength - 4f * scale, 16f * scale),
                        speed.ToString("0"),
                        tickLabelStyle);
                }
            }

            DrawCenterWindowLocal(localCenterY, windowHeight, currentSpeed, hudColor, scale, localRailX, tapeRect.width);

            if (aircraft.IsAfterburning)
            {
                DrawRedlineMarkerLocal(
                    localCenterY,
                    tapeRect.height,
                    aircraft.Profile.maxThrottleMph,
                    currentSpeed,
                    pixelsPerMph,
                    hudColor,
                    scale,
                    localRailX);
            }

            GUI.EndGroup();
        }

        private static void DrawChevron(Vector2 tip, float size, bool pointsRight, Color color)
        {
            var direction = pointsRight ? 1f : -1f;
            var baseX = tip.x - direction * size;
            HudGuiUtility.DrawScreenLine(tip, new Vector2(baseX, tip.y - size), color, 1.5f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(tip, new Vector2(baseX, tip.y + size), color, 1.5f, Texture2D.whiteTexture);
        }

        private void DrawCenterWindowLocal(
            float centerY,
            float windowHeight,
            float currentSpeed,
            Color hudColor,
            float scale,
            float railX,
            float tapeWidth)
        {
            var windowRect = new Rect(0f, centerY - windowHeight * 0.5f, tapeWidth, windowHeight);

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(windowRect, Texture2D.whiteTexture);

            var bracketInset = 4f * scale;
            var bracketWidth = windowRect.width - bracketInset * 2f;
            var bracketY = windowRect.y;
            var bracketBottom = windowRect.yMax;

            GUI.color = hudColor;
            HudGuiUtility.DrawScreenLine(
                new Vector2(windowRect.x + bracketInset, bracketY),
                new Vector2(windowRect.x + bracketInset + bracketWidth, bracketY),
                hudColor,
                2f * scale,
                Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(
                new Vector2(windowRect.x + bracketInset, bracketBottom),
                new Vector2(windowRect.x + bracketInset + bracketWidth, bracketBottom),
                hudColor,
                2f * scale,
                Texture2D.whiteTexture);

            DrawChevron(new Vector2(windowRect.x + 10f * scale, centerY), 5f * scale, true, hudColor);
            DrawChevron(new Vector2(windowRect.xMax - 10f * scale, centerY), 5f * scale, false, hudColor);

            speedReadoutStyle.normal.textColor = SpeedNumberColor;
            GUI.Label(
                new Rect(windowRect.x, centerY - 12f * scale, windowRect.width - 8f * scale, 24f * scale),
                currentSpeed.ToString("0"),
                speedReadoutStyle);

            HudGuiUtility.DrawScreenLine(
                new Vector2(railX, windowRect.y),
                new Vector2(railX, windowRect.yMax),
                hudColor,
                2f * scale,
                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private static void DrawRedlineMarkerLocal(
            float centerY,
            float tapeHeight,
            float redlineMph,
            float currentSpeed,
            float pixelsPerMph,
            Color hudColor,
            float scale,
            float railX)
        {
            var markerY = centerY - (redlineMph - currentSpeed) * pixelsPerMph;
            if (markerY < 0f || markerY > tapeHeight)
            {
                return;
            }

            var red = new Color(0.95f, 0.2f, 0.15f, hudColor.a);
            HudGuiUtility.DrawScreenLine(
                new Vector2(6f * scale, markerY),
                new Vector2(railX, markerY),
                red,
                2f * scale,
                Texture2D.whiteTexture);
        }

        private void EnsureStyles()
        {
            var hudColor = FlightHudColorPalette.Current;
            var scale = LayoutScale;

            if (speedReadoutStyle == null)
            {
                tickLabelStyle = HudStyleFactory.CreateLabel(
                    Mathf.Max(8, Mathf.RoundToInt(10f * scale)),
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    SpeedNumberColor);
                speedReadoutStyle = HudStyleFactory.CreateLabel(
                    Mathf.Max(12, Mathf.RoundToInt(18f * scale)),
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    SpeedNumberColor);
            }

            if (headerStyle != null && hudColor == lastHudColor)
            {
                return;
            }

            lastHudColor = hudColor;
            headerStyle = HudStyleFactory.CreateLabel(
                Mathf.Max(8, Mathf.RoundToInt(9f * scale)),
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                hudColor);
        }
    }
}
