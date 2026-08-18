using F89.Core;
using F89.Flight;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class FuelGaugeHud : MonoBehaviour
    {
        private static readonly Color GaugeGreen = new Color(0.016f, 0.878f, 0.063f, 1f);
        private static readonly Color TickGreen = new Color(0.016f, 0.878f, 0.063f, 1f);
        private static readonly Color TickWhite = new Color(0.82f, 0.82f, 0.82f, 1f);
        private static readonly Color TickRed = new Color(0.92f, 0.18f, 0.18f, 1f);
        private static readonly Color LabelWhite = Color.white;
        private static readonly Color DisplayBackground = new Color(0.02f, 0.025f, 0.03f, 1f);
        private static readonly Color RequestFuelFrameFill = new Color(0.02f, 0.025f, 0.03f, 1f);

        [SerializeField] private AircraftController aircraft;

        private GUIStyle headerStyle;
        private GUIStyle readoutStyle;
        private GUIStyle unitStyle;
        private GUIStyle footerStyle;
        private GUIStyle rangeLabelStyle;
        private GUIStyle rangeValueStyle;
        private GUIStyle requestFuelLabelStyle;
        private int lastFontSize = -1;
        private int requestFuelLabelFontSize = -1;
        private bool requestFuelButtonLit;
        private float requestFuelLitUntilUnscaled;
        private bool requestFuelConfirmVisible;

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
                || aircraft.WorldMap == null)
            {
                return;
            }

            if (requestFuelConfirmVisible)
            {
                DrawRequestFuelConfirmDialog();
                return;
            }

            TryHandleRequestFuelButton();

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawFuelGauge();
            DrawRequestFuelButton();
        }

        private void DrawFuelGauge()
        {
            var layout = RadarMfdBezelRenderer.ComputeFuelGaugeLayout();
            var scope = layout.ScopeRect;
            var fontSize = GetGaugeFontSize(scope);
            EnsureStyles(fontSize);

            var previousDepth = GUI.depth;
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -100;
            GUI.color = DisplayBackground;
            GUI.DrawTexture(scope, Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawGaugeContent(scope, fontSize);

            GUI.depth = previousDepth;
            GUI.matrix = previousMatrix;
            GUI.color = Color.white;
        }

        private static int GetGaugeFontSize(Rect scope)
        {
            var fitSize = Mathf.FloorToInt(scope.height / 9.5f);
            var storesSize = RadarMfdBezelRenderer.GetStoresPanelFontSize();
            return Mathf.Clamp(Mathf.Min(fitSize, storesSize), 7, storesSize);
        }

        private void DrawGaugeContent(Rect scope, int fontSize)
        {
            var line = Mathf.Max(1f, RadarMfdBezelRenderer.LayoutScale * 0.85f);
            var pad = scope.width * 0.04f;
            var lineHeight = fontSize + 1f;
            var headerHeight = lineHeight;
            var readoutHeight = lineHeight * 1.55f;
            var footerHeight = lineHeight * 3.1f;
            var footerTop = scope.yMax - pad - footerHeight;
            var readoutTop = footerTop - readoutHeight;
            var gaugeTop = scope.y + pad + headerHeight;
            var gaugeHeight = readoutTop - gaugeTop - pad * 0.25f;
            var centerX = scope.x + scope.width * 0.5f;
            var columnWidth = (scope.width - pad * 2f) * 0.5f;
            var leftColumnX = scope.x + pad;
            var rightColumnX = centerX;

            headerStyle.normal.textColor = LabelWhite;
            GUI.Label(new Rect(leftColumnX, scope.y + pad, columnWidth, headerHeight), "LEFT", headerStyle);
            GUI.Label(new Rect(rightColumnX, scope.y + pad, columnWidth, headerHeight), "RIGHT", headerStyle);

            DrawTankLine(
                leftColumnX + columnWidth * 0.5f,
                gaugeTop,
                gaugeHeight,
                aircraft.LeftTankNormalized,
                line,
                ticksOnRight: true);

            DrawTankLine(
                rightColumnX + columnWidth * 0.5f,
                gaugeTop,
                gaugeHeight,
                aircraft.RightTankNormalized,
                line,
                ticksOnRight: false);

            DrawTankReadout(new Rect(leftColumnX, readoutTop, columnWidth, readoutHeight), aircraft.LeftTankNormalized);
            DrawTankReadout(new Rect(rightColumnX, readoutTop, columnWidth, readoutHeight), aircraft.RightTankNormalized);

            footerStyle.normal.textColor = LabelWhite;
            var fuelLabelY = footerTop + pad * 0.15f;
            GUI.Label(new Rect(scope.x, fuelLabelY, scope.width, lineHeight), "FUEL", footerStyle);

            rangeLabelStyle.normal.textColor = LabelWhite;
            rangeValueStyle.normal.textColor = GaugeGreen;
            var rangeLabelY = fuelLabelY + lineHeight;
            GUI.Label(new Rect(scope.x, rangeLabelY, scope.width, lineHeight * 0.9f), "Projected Range", rangeLabelStyle);
            GUI.Label(
                new Rect(scope.x, rangeLabelY + lineHeight * 0.85f, scope.width, lineHeight),
                $"{aircraft.ProjectedRangeMiles:0} MI",
                rangeValueStyle);
        }

        private static Rect GetRequestFuelButtonRect()
        {
            var gauge = RadarMfdBezelRenderer.ComputeFuelGaugeLayout().ScopeRect;
            var height = RadarMfdBezelRenderer.EdgeOsbSize * 1.75f;
            return new Rect(0f, gauge.yMax, gauge.width, height);
        }

        private void TryHandleRequestFuelButton()
        {
            if (requestFuelConfirmVisible || !CanRequestMidAirRefuel())
            {
                return;
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
            {
                return;
            }

            var rect = GetRequestFuelButtonRect();
            if (!rect.Contains(GetGuiMousePosition()))
            {
                return;
            }

            requestFuelButtonLit = true;
            requestFuelLitUntilUnscaled = Time.unscaledTime + 0.35f;
            Event.current.Use();
            requestFuelConfirmVisible = true;
            Time.timeScale = 0f;
        }

        private void DrawRequestFuelConfirmDialog()
        {
            var previousDepth = GUI.depth;
            GUI.depth = -10000;

            var result = RequestFuelConfirmDialog.Draw(true);
            if (result == RequestFuelConfirmDialog.Result.Confirmed)
            {
                requestFuelConfirmVisible = false;
                BeginMidAirRefuelRequest();
            }
            else if (result == RequestFuelConfirmDialog.Result.Cancelled)
            {
                requestFuelConfirmVisible = false;
                Time.timeScale = 1f;
            }

            GUI.depth = previousDepth;
        }

        private void BeginMidAirRefuelRequest()
        {
            if (aircraft == null)
            {
                return;
            }

            var player = aircraft.gameObject;
            var snapshot = AircraftLandingController.CaptureMidAirRefuelSnapshot(player);
            snapshot.HasInFlightSpeed = true;
            snapshot.InFlightSpeedMph = aircraft.CurrentSpeedMph;
            snapshot.InFlightAutopilotActive = aircraft.IsAutopilotActive;

            MidAirRefuelHandoffState.BeginEnterFromFlight(snapshot);
            FlightMissionStartBootstrap.ResetForSceneLoad();
            SceneManager.LoadScene(GameScenes.MARefuel);
        }

        private void DrawRequestFuelButton()
        {
            if (!CanRequestMidAirRefuel())
            {
                return;
            }

            var rect = GetRequestFuelButtonRect();
            var hudColor = FlightHudColorPalette.Mfd;
            var mouseOver = rect.Contains(GetGuiMousePosition());
            var lit = requestFuelButtonLit && Time.unscaledTime < requestFuelLitUntilUnscaled;
            var lowFuel = aircraft.TotalFuelCapacityGallons > 0f
                && aircraft.TotalFuelGallons / aircraft.TotalFuelCapacityGallons < 0.3f;
            var highlighted = lit || mouseOver || lowFuel;
            var textAlpha = highlighted ? 1f : 0.48f;
            var borderColor = highlighted ? Color.white : new Color(1f, 1f, 1f, 0.82f);

            var previousDepth = GUI.depth;
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -101;

            var previous = GUI.color;
            GUI.color = RequestFuelFrameFill;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            DrawMfdBorder(rect, 1f, borderColor);
            GUI.color = previous;

            var fontSize = GetRequestFuelFontSize(rect);
            if (requestFuelLabelStyle == null || requestFuelLabelFontSize != fontSize)
            {
                requestFuelLabelFontSize = fontSize;
                requestFuelLabelStyle = HudStyleFactory.CreateLabel(
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    hudColor);
            }

            requestFuelLabelStyle.normal.textColor = new Color(hudColor.r, hudColor.g, hudColor.b, textAlpha);
            var lineHeight = rect.height * 0.5f;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, lineHeight), "REQUEST", requestFuelLabelStyle);
            GUI.Label(new Rect(rect.x, rect.y + lineHeight, rect.width, lineHeight), "FUEL", requestFuelLabelStyle);

            GUI.depth = previousDepth;
            GUI.matrix = previousMatrix;
        }

        private static int GetRequestFuelFontSize(Rect rect)
        {
            var lineHeight = rect.height * 0.5f;
            var heightCap = Mathf.FloorToInt(lineHeight * 0.88f);
            for (var size = heightCap; size >= 7; size--)
            {
                var style = HudStyleFactory.CreateLabel(size, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                var requestSize = style.CalcSize(new GUIContent("REQUEST"));
                var fuelSize = style.CalcSize(new GUIContent("FUEL"));
                if (requestSize.x <= rect.width - 4f
                    && requestSize.y <= lineHeight - 1f
                    && fuelSize.x <= rect.width - 4f
                    && fuelSize.y <= lineHeight - 1f)
                {
                    return size;
                }
            }

            return 9;
        }

        private static bool CanRequestMidAirRefuel()
        {
            if (SceneManager.GetActiveScene().name != GameScenes.FlightTest)
            {
                return false;
            }

            if (AircraftLandingController.IsLandingActive
                || AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsRunwayDeckMenuVisible
                || AircraftLandingController.IsParkedAtRunway
                || AircraftLandingController.IsCarrierApproachPromptVisible)
            {
                return false;
            }

            return true;
        }

        private static Vector2 GetGuiMousePosition()
        {
            var mouse = Input.mousePosition;
            return new Vector2(mouse.x, Screen.height - mouse.y);
        }

        private static void DrawMfdBorder(Rect rect, float thickness, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawTankLine(
            float centerX,
            float top,
            float height,
            float fillNormalized,
            float lineThickness,
            bool ticksOnRight)
        {
            var tankLineWidth = Mathf.Max(2f, lineThickness * 2f);
            var tickLength = 5f;
            var tickGap = 1.5f;
            var lineRect = new Rect(centerX - tankLineWidth * 0.5f, top, tankLineWidth, height);
            var emptyTop = lineRect.y + lineRect.height * (1f - Mathf.Clamp01(fillNormalized));

            if (fillNormalized > 0f)
            {
                GUI.color = GaugeGreen;
                GUI.DrawTexture(new Rect(lineRect.x, emptyTop, lineRect.width, lineRect.yMax - emptyTop), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            DrawTickScale(
                top,
                height,
                ticksOnRight ? lineRect.xMax + tickGap : lineRect.xMin - tickGap - tickLength,
                tickLength,
                lineThickness,
                ticksOnRight);
        }

        private static void DrawTickScale(
            float top,
            float height,
            float tickX,
            float tickLength,
            float lineThickness,
            bool ticksExtendRight)
        {
            const int ticksPerBand = 5;
            const int bands = 3;
            var tickCount = ticksPerBand * bands;
            var spacing = height / (tickCount - 1);

            for (var i = 0; i < tickCount; i++)
            {
                var band = i / ticksPerBand;
                var color = band == 0 ? TickGreen : band == 1 ? TickWhite : TickRed;
                var y = top + i * spacing;
                var tickRect = ticksExtendRight
                    ? new Rect(tickX, y, tickLength, lineThickness)
                    : new Rect(tickX, y, tickLength, lineThickness);
                GUI.color = color;
                GUI.DrawTexture(tickRect, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawTankReadout(Rect rect, float normalized)
        {
            readoutStyle.normal.textColor = LabelWhite;
            unitStyle.normal.textColor = LabelWhite;

            var percent = Mathf.Clamp01(normalized) * 100f;
            var valueText = percent >= 99.5f ? "100" : percent.ToString("0");
            GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height * 0.62f), valueText, readoutStyle);
            GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.52f, rect.width, rect.height * 0.48f), "%", unitStyle);
        }

        private void EnsureStyles(int fontSize)
        {
            if (headerStyle != null && lastFontSize == fontSize)
            {
                return;
            }

            var unitSize = Mathf.Max(6, fontSize - 2);
            headerStyle = HudStyleFactory.CreateLabel(fontSize, FontStyle.Bold, TextAnchor.UpperCenter, LabelWhite);
            readoutStyle = HudStyleFactory.CreateLabel(fontSize, FontStyle.Bold, TextAnchor.LowerCenter, LabelWhite);
            unitStyle = HudStyleFactory.CreateLabel(unitSize, FontStyle.Normal, TextAnchor.UpperCenter, LabelWhite);
            footerStyle = HudStyleFactory.CreateLabel(fontSize, FontStyle.Bold, TextAnchor.UpperCenter, LabelWhite);
            rangeLabelStyle = HudStyleFactory.CreateLabel(unitSize, FontStyle.Normal, TextAnchor.UpperCenter, LabelWhite);
            rangeValueStyle = HudStyleFactory.CreateLabel(fontSize, FontStyle.Bold, TextAnchor.UpperCenter, GaugeGreen);
            lastFontSize = fontSize;
        }
    }
}
