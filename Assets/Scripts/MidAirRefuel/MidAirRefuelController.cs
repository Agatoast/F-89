using F89.Core;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Tanker belly-window refuel view: scrolling terrain fills the window aperture;
    /// opaque interior masks above and below the frame. Receiver flies in, boom refuels.
    /// </summary>
    public sealed class MidAirRefuelController : MonoBehaviour
    {
        private enum Phase
        {
            ControlsIntro,
            Countdown,
            Approach,
            Refuel,
            Complete,
            Departure
        }

        private enum DriftState
        {
            ApproachPause,
            Moving,
            Holding
        }

        private const float ApproachDurationSeconds = 4f;
        private const float PostApproachPauseSeconds = 1f;
        private const float DriftMoveDurationSeconds = 1.25f;
        private const float DriftHoldMinSeconds = 1f;
        private const float DriftHoldMaxSeconds = 5f;
        private const float DriftMaxWindowFraction = 0.10f;
        private const float ReceiverHoldYNorm = 0.22f;
        private const float ReceiverStartYNorm = -0.35f;
        private const float ReceiverDownPageFraction = 0.25f;
        private const float ReceiverWidthScreenFraction = 0.92f;
        private const float FuelPortHeightFromSpriteTop = 0.27f;
        private const float FuelPortHudBoxHalfSizeScreenFraction = 0.011f;
        private const float FuelPortHudOffsetXPx = -9f;
        private const float FuelPortHudOffsetYPx = 4f;
        private const float BoomWingBaseWidthWindowFraction = 0.52f;
        private const float BoomWingScale = 0.13f;
        private const float BoomWingWidthWindowFraction = BoomWingBaseWidthWindowFraction * BoomWingScale;
        private const float BoomWingAttachXNorm = 0.5f;
        private const float BoomWingAttachYNorm = 0.595f;
        private const float BoomWingOffsetXPx = -1f;
        private const float BoomNozzleYNorm = 0.935f;
        private const float BoomHoseWidthPx = 12f;
        private static readonly Color BoomColor = new(0x55 / 255f, 0x6D / 255f, 0x85 / 255f);
        private const float BoomMountPastWindowPx = 5f;
        private const float RetractedHoseLengthPx = 7.5f;
        private const float BoomMoveSpeedPx = 95f;
        private const float TransferRatePerSecond = 0.0275f;
        private const float StartingFuel = 0.18f;
        private const float DepartureDurationSeconds = 3.25f;
        private const float DepartureHoldSeconds = 3f;
        private const float DepartureOffScreenMarginPx = 12f;
        private const float DepartureCreepNormPerSecond = 0.42f;
        private const float CountdownDurationSeconds = 5f;
        private const float DialogWidth = 480f;
        private const float ControlsDialogHeight = 210f;
        private const float CompleteDialogHeight = 210f;
        private const float DialogChoiceHeight = 40f;
        private const int DialogButtonFontSize = 16;

        private static readonly Color FuelPortHudIdleColor = new(0.95f, 0.75f, 0.1f);
        private static readonly Color FuelPortHudConnectedColor = new(0.2f, 0.9f, 0.35f);
        private static readonly Color FuelCounterColor = new(0x5A / 255f, 0xC3 / 255f, 0x2B / 255f);
        private const int FuelCounterFontSize = 100;

        private readonly MidAirRefuelGroundScroller groundScroller = new();

        private Phase phase = Phase.ControlsIntro;
        private DriftState driftState = DriftState.ApproachPause;
        private float approachElapsed;
        private float driftTimer;
        private float driftHoldDuration;
        private float receiverYNorm = ReceiverStartYNorm;
        private Vector2 receiverDriftOffsetNorm;
        private Vector2 receiverDriftStartNorm;
        private Vector2 receiverDriftTargetNorm;
        private Vector2 boomEndScreen;
        private float fuelNormalized = StartingFuel;
        private bool returnToFlightPending;
        private LandSortieSnapshot flightReturnSnapshot;
        private float departureElapsed;
        private float departureWaitRemaining = -1f;
        private float departureEndYNorm;
        private bool departureApplyRefuel;
        private float countdownElapsed;

        private GUIStyle dialogMessageStyle;
        private GUIStyle dialogSubMessageStyle;
        private GUIStyle fuelCounterStyle;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GamePauseController.EnsureExists();
            GamePauseController.ClearPauseOnSceneLoad();

            MidAirRefuelArtCatalog.EnsureLoaded();
            groundScroller.Reset();
            groundScroller.ScrollSpeedPx = 280f;
            boomEndScreen = GetRetractedBoomHubScreen();

            if (MidAirRefuelHandoffState.TryConsumeEnterFromFlight(out flightReturnSnapshot))
            {
                returnToFlightPending = true;
                fuelNormalized = Mathf.Clamp01(flightReturnSnapshot.FuelNormalized);
            }
            else
            {
                returnToFlightPending = false;
                flightReturnSnapshot = LandSortieSnapshot.Empty;
                fuelNormalized = StartingFuel;
            }
        }

        private void Update()
        {
            if (GamePauseController.IsPaused)
            {
                return;
            }

            if (phase != Phase.ControlsIntro && phase != Phase.Countdown)
            {
                groundScroller.Tick(Time.deltaTime);
            }

            switch (phase)
            {
                case Phase.Countdown:
                    UpdateCountdown();
                    break;
                case Phase.Approach:
                    UpdateApproach();
                    break;
                case Phase.Refuel:
                    UpdateRefuel();
                    break;
                case Phase.Departure:
                    UpdateDeparture();
                    break;
            }
        }

        private void UpdateCountdown()
        {
            countdownElapsed += Time.deltaTime;
            if (countdownElapsed >= CountdownDurationSeconds)
            {
                phase = Phase.Approach;
                approachElapsed = 0f;
            }
        }

        private void UpdateApproach()
        {
            approachElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(approachElapsed / ApproachDurationSeconds);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            receiverYNorm = Mathf.Lerp(ReceiverStartYNorm, ReceiverHoldYNorm, eased);

            var boomStart = GetRetractedBoomHubScreen();
            var boomTarget = GetFuelPortScreen() + Vector2.up * MidAirRefuelViewport.LineHeightPx * 0.25f;
            boomEndScreen = Vector2.Lerp(boomStart, boomTarget, eased);
            ClampBoomEnd(ref boomEndScreen);

            if (t >= 1f)
            {
                receiverYNorm = ReceiverHoldYNorm;
                phase = Phase.Refuel;
                driftState = DriftState.ApproachPause;
                driftTimer = 0f;
                receiverDriftOffsetNorm = Vector2.zero;
                receiverDriftStartNorm = Vector2.zero;
                receiverDriftTargetNorm = Vector2.zero;
                var cap = GetFuelPortScreen();
                boomEndScreen = cap + Vector2.up * MidAirRefuelViewport.LineHeightPx * 0.25f;
                ClampBoomEnd(ref boomEndScreen);
            }
        }

        private void UpdateRefuel()
        {
            UpdateReceiverDrift();

            var input = ReadBoomInput();
            boomEndScreen += input * (BoomMoveSpeedPx * Time.deltaTime);
            ClampBoomEnd(ref boomEndScreen);

            if (IsBoomConnected())
            {
                fuelNormalized = Mathf.Clamp01(fuelNormalized + TransferRatePerSecond * Time.deltaTime);
                if (fuelNormalized >= 0.999f)
                {
                    fuelNormalized = 1f;
                    phase = Phase.Complete;
                }
            }
        }

        private void UpdateReceiverDrift()
        {
            driftTimer += Time.deltaTime;

            switch (driftState)
            {
                case DriftState.ApproachPause:
                    if (driftTimer >= PostApproachPauseSeconds)
                    {
                        BeginDriftMove();
                    }

                    break;

                case DriftState.Moving:
                {
                    var t = Mathf.Clamp01(driftTimer / DriftMoveDurationSeconds);
                    var eased = Mathf.SmoothStep(0f, 1f, t);
                    receiverDriftOffsetNorm = Vector2.Lerp(receiverDriftStartNorm, receiverDriftTargetNorm, eased);
                    if (t >= 1f)
                    {
                        receiverDriftOffsetNorm = receiverDriftTargetNorm;
                        driftState = DriftState.Holding;
                        driftTimer = 0f;
                        driftHoldDuration = Random.Range(DriftHoldMinSeconds, DriftHoldMaxSeconds);
                    }

                    break;
                }

                case DriftState.Holding:
                    if (driftTimer >= driftHoldDuration)
                    {
                        BeginDriftMove();
                    }

                    break;
            }
        }

        private void BeginDriftMove()
        {
            receiverDriftStartNorm = receiverDriftOffsetNorm;
            receiverDriftTargetNorm = new Vector2(
                Random.Range(-DriftMaxWindowFraction, DriftMaxWindowFraction),
                Random.Range(-DriftMaxWindowFraction, DriftMaxWindowFraction));
            driftState = DriftState.Moving;
            driftTimer = 0f;
        }

        private Vector2 ReadBoomInput()
        {
            var input = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                input.y -= 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                input.y += 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                input.x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                input.x += 1f;
            }

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void UpdateDeparture()
        {
            if (departureWaitRemaining >= 0f)
            {
                departureWaitRemaining -= Time.deltaTime;
                if (departureWaitRemaining <= 0f)
                {
                    FinishAfterDeparture();
                }

                return;
            }

            var window = MidAirRefuelViewport.GetWindowRect();
            departureElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(departureElapsed / DepartureDurationSeconds);
            var eased = t * t;
            receiverYNorm = Mathf.Lerp(ReceiverHoldYNorm, departureEndYNorm, eased);

            if (t >= 1f && !IsReceiverFullyAboveWindow(window))
            {
                receiverYNorm -= DepartureCreepNormPerSecond * Time.deltaTime;
            }

            if (IsReceiverFullyAboveWindow(window))
            {
                departureWaitRemaining = DepartureHoldSeconds;
            }
        }

        private void BeginDepartureReturn(bool applyRefuel)
        {
            departureApplyRefuel = applyRefuel;
            departureElapsed = 0f;
            departureWaitRemaining = -1f;
            receiverYNorm = ReceiverHoldYNorm;
            receiverDriftOffsetNorm = Vector2.zero;
            departureEndYNorm = ComputeDepartureEndYNorm();
            phase = Phase.Departure;
        }

        private float ComputeDepartureEndYNorm()
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            var rect = GetReceiverDrawRect();
            var targetCenterY = window.yMin - DepartureOffScreenMarginPx - rect.height * 0.5f;
            return (targetCenterY - window.y - Screen.height * ReceiverDownPageFraction) / window.height;
        }

        private bool IsReceiverFullyAboveWindow(Rect window)
        {
            return GetReceiverDrawRect().yMax <= window.yMin - DepartureOffScreenMarginPx;
        }

        private float GetReceiverYNorm()
        {
            return phase switch
            {
                Phase.Approach => receiverYNorm,
                Phase.Departure => receiverYNorm,
                _ => ReceiverHoldYNorm
            };
        }

        private bool ShouldDrawReceiver()
        {
            if (phase is Phase.Approach or Phase.Refuel or Phase.Complete)
            {
                return true;
            }

            if (phase != Phase.Departure || departureWaitRemaining >= 0f)
            {
                return false;
            }

            var window = MidAirRefuelViewport.GetWindowRect();
            var rect = GetReceiverDrawRect();
            return rect.yMax > window.yMin;
        }

        private Rect GetReceiverDrawRect()
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            var texture = MidAirRefuelArtCatalog.ReceiverTexture;
            var yNorm = GetReceiverYNorm();
            var centerY = window.y + window.height * yNorm + Screen.height * ReceiverDownPageFraction;
            centerY += window.height * receiverDriftOffsetNorm.y;
            var centerX = Screen.width * 0.5f + window.width * receiverDriftOffsetNorm.x;
            var center = new Vector2(centerX, centerY);

            if (texture == null)
            {
                var fallbackWidth = window.width * 0.75f;
                var fallbackHeight = window.height * 0.45f;
                return new Rect(
                    center.x - fallbackWidth * 0.5f,
                    center.y - fallbackHeight * 0.5f,
                    fallbackWidth,
                    fallbackHeight);
            }

            var width = Screen.width * ReceiverWidthScreenFraction;
            var height = width * (texture.height / (float)texture.width);
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private Vector2 GetFuelPortScreen()
        {
            var rect = GetReceiverDrawRect();
            return new Vector2(rect.center.x, rect.y + rect.height * FuelPortHeightFromSpriteTop);
        }

        private void GetBoomWingDimensions(out float width, out float height)
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            var texture = MidAirRefuelArtCatalog.BoomWingTexture;
            width = window.width * BoomWingWidthWindowFraction;
            height = texture != null && texture.width > 0
                ? width * (texture.height / (float)texture.width)
                : width * 0.45f;
        }

        private Vector2 GetRetractedBoomHubScreen()
        {
            var mount = GetBoomMountScreen();
            return new Vector2(mount.x, mount.y + RetractedHoseLengthPx);
        }

        private Rect GetBoomWingDrawRect()
        {
            GetBoomWingDimensions(out var width, out var height);

            var attach = boomEndScreen;
            var x = attach.x - width * BoomWingAttachXNorm + BoomWingOffsetXPx;
            var y = attach.y - height * BoomWingAttachYNorm;
            return new Rect(x, y, width, height);
        }

        private Vector2 GetBoomWingNozzleScreen()
        {
            var rect = GetBoomWingDrawRect();
            return new Vector2(rect.center.x, rect.y + rect.height * BoomNozzleYNorm);
        }

        private Vector2 GetBoomLineConnectScreen()
        {
            return boomEndScreen;
        }

        private void ClampBoomEnd(ref Vector2 end)
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            var retractedHub = GetRetractedBoomHubScreen();
            end.x = Mathf.Clamp(end.x, window.xMin + 12f, window.xMax - 12f);
            end.y = Mathf.Clamp(end.y, retractedHub.y, window.yMax - 12f);
        }

        private static Vector2 GetBoomMountScreen()
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            return new Vector2(window.center.x, window.y - BoomMountPastWindowPx);
        }

        private Rect GetBoomGaugeMoveBounds()
        {
            var window = MidAirRefuelViewport.GetWindowRect();
            var retractedHub = GetRetractedBoomHubScreen();
            return new Rect(
                window.xMin + 12f,
                retractedHub.y,
                window.width - 24f,
                window.yMax - 12f - retractedHub.y);
        }

        private Vector2 GetFuelPortHudCenter()
        {
            var port = GetFuelPortScreen();
            port.x += FuelPortHudOffsetXPx;
            port.y += FuelPortHudOffsetYPx;
            return port;
        }

        private float GetFuelPortHudHalfSize()
        {
            return Screen.height * FuelPortHudBoxHalfSizeScreenFraction;
        }

        private Rect GetFuelPortHudRect()
        {
            var center = GetFuelPortHudCenter();
            var half = GetFuelPortHudHalfSize();
            return new Rect(center.x - half, center.y - half, half * 2f, half * 2f);
        }

        private bool IsBoomConnected()
        {
            var probe = MidAirRefuelArtCatalog.BoomWingTexture != null
                ? GetBoomWingNozzleScreen()
                : boomEndScreen;
            return GetFuelPortHudRect().Contains(probe);
        }

        private void OnGUI()
        {
            GUI.color = MidAirRefuelViewport.InteriorColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            groundScroller.Draw(MidAirRefuelViewport.GetWindowRect());
            if (ShouldDrawReceiver())
            {
                DrawReceiver();
            }

            DrawFuelPortHud();
            DrawBoom();
            MidAirRefuelViewport.DrawFrameMasks();
            MidAirRefuelUpperConsoleOverlay.Draw();
            DrawFuelCounter();
            DrawInactiveRadarMfds();

            MidAirRefuelLowerConsoleOverlay.Draw();
            MidAirRefuelConsoleOverlay.SetBoomHub(boomEndScreen, GetBoomGaugeMoveBounds());
            MidAirRefuelConsoleOverlay.Draw();

            if (phase == Phase.ControlsIntro)
            {
                DrawControlsIntroOverlay();
            }
            else if (phase == Phase.Countdown)
            {
                DrawCountdownOverlay();
            }
            else if (phase == Phase.Complete)
            {
                DrawCompleteOverlay();
            }
        }

        private void DrawReceiver()
        {
            var rect = GetReceiverDrawRect();
            var texture = MidAirRefuelArtCatalog.ReceiverTexture;
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, alphaBlend: true);
            }
            else
            {
                GUI.color = new Color(0.3f, 0.33f, 0.38f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void DrawFuelPortHud()
        {
            if (phase != Phase.Refuel && phase != Phase.Complete)
            {
                return;
            }

            var port = GetFuelPortHudCenter();
            var half = GetFuelPortHudHalfSize();
            var arm = half * 0.58f;
            var thickness = Mathf.Max(2f, Screen.height * 0.0028f);
            GUI.color = IsBoomConnected() ? FuelPortHudConnectedColor : FuelPortHudIdleColor;
            DrawCornerBox(port, half, arm, thickness);
            GUI.color = Color.white;
        }

        private void DrawBoom()
        {
            if (phase != Phase.ControlsIntro
                && phase != Phase.Countdown
                && phase != Phase.Approach
                && phase != Phase.Refuel
                && phase != Phase.Complete)
            {
                return;
            }

            var mount = GetBoomMountScreen();
            var wingTexture = MidAirRefuelArtCatalog.BoomWingTexture;

            if (wingTexture != null)
            {
                var wingRect = GetBoomWingDrawRect();
                GUI.DrawTexture(wingRect, wingTexture, ScaleMode.ScaleToFit, alphaBlend: true);
            }

            GUI.color = BoomColor;
            DrawLine(mount, GetBoomLineConnectScreen(), BoomHoseWidthPx);
            GUI.color = Color.white;
        }

        private void DrawFuelCounter()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureFuelCounterStyle();
            var windowTop = MidAirRefuelViewport.GetWindowRect().y;
            var label = $"FUEL {Mathf.RoundToInt(fuelNormalized * 100f)}%";
            var rectHeight = FuelCounterFontSize * 1.15f;
            var fuelRect = new Rect(
                0f,
                windowTop - rectHeight * 0.85f,
                Screen.width,
                rectHeight);

            var previousDepth = GUI.depth;
            GUI.depth = -120;
            GUI.Label(fuelRect, label, fuelCounterStyle);
            GUI.depth = previousDepth;
        }

        private void EnsureFuelCounterStyle()
        {
            if (fuelCounterStyle != null)
            {
                return;
            }

            fuelCounterStyle = HudStyleFactory.CreateLabel(
                FuelCounterFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                FuelCounterColor);
        }

        private void DrawControlsIntroOverlay()
        {
            var previousDepth = GUI.depth;
            GUI.depth = -3000;

            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - ControlsDialogHeight) * 0.5f,
                DialogWidth,
                ControlsDialogHeight);
            DrawDialogBackdrop(dialogRect);
            EnsureDialogStyles();

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 32f, dialogRect.width - 48f, 32f),
                "Control Fuel Boom with W, A, S, and D",
                dialogMessageStyle);
            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 64f, dialogRect.width - 48f, 28f),
                "or arrow keys.",
                dialogSubMessageStyle);

            var okRect = new Rect(
                dialogRect.center.x - UiFitCanvas.Px(60f),
                dialogRect.yMax - DialogChoiceHeight - 24f,
                UiFitCanvas.Px(120f),
                DialogChoiceHeight);
            if (StartPageMenuStyles.DrawMenuButton(okRect, "OK", fontSize: DialogButtonFontSize))
            {
                phase = Phase.Countdown;
                countdownElapsed = 0f;
            }

            GUI.depth = previousDepth;
        }

        private void DrawCountdownOverlay()
        {
            var previousDepth = GUI.depth;
            GUI.depth = -3000;

            var window = MidAirRefuelViewport.GetWindowRect();
            var displayNumber = Mathf.Max(1, Mathf.CeilToInt(CountdownDurationSeconds - countdownElapsed));
            var showRefuelLabel = displayNumber > 2;

            var refuelStyle = HudStyleFactory.CreateLabel(36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var numberStyle = HudStyleFactory.CreateLabel(72, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            if (showRefuelLabel)
            {
                GUI.Label(
                    new Rect(window.x, window.center.y - 96f, window.width, 48f),
                    "REFUEL!",
                    refuelStyle);
            }

            GUI.Label(
                new Rect(window.x, window.center.y - 24f, window.width, 96f),
                displayNumber.ToString(),
                numberStyle);

            GUI.depth = previousDepth;
        }

        private static readonly Color DialogTextColor = new(0.82f, 0.88f, 0.94f);

        private void EnsureDialogStyles()
        {
            if (dialogMessageStyle == null)
            {
                dialogMessageStyle = HudStyleFactory.CreateLabel(
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    DialogTextColor,
                    wordWrap: true);
                dialogSubMessageStyle = HudStyleFactory.CreateLabel(
                    18,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter,
                    DialogTextColor,
                    wordWrap: true);
            }
            else
            {
                dialogMessageStyle.normal.textColor = DialogTextColor;
                dialogSubMessageStyle.normal.textColor = DialogTextColor;
            }
        }

        private static void DrawDialogBackdrop(Rect dialogRect)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            StartPageMenuStyles.DrawMenuButtonChrome(dialogRect, panelAlpha: 0.94f);
        }

        private void DrawCompleteOverlay()
        {
            var previousDepth = GUI.depth;
            GUI.depth = -3000;

            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - CompleteDialogHeight) * 0.5f,
                DialogWidth,
                CompleteDialogHeight);
            DrawDialogBackdrop(dialogRect);
            EnsureDialogStyles();

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 32f, dialogRect.width - 48f, 32f),
                "REFUEL COMPLETE",
                dialogMessageStyle);
            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 64f, dialogRect.width - 48f, 28f),
                "Tanks full.",
                dialogSubMessageStyle);

            var okRect = new Rect(
                dialogRect.center.x - UiFitCanvas.Px(60f),
                dialogRect.yMax - DialogChoiceHeight - 24f,
                UiFitCanvas.Px(120f),
                DialogChoiceHeight);
            var buttonLabel = returnToFlightPending ? "CONTINUE" : "OK";
            if (StartPageMenuStyles.DrawMenuButton(okRect, buttonLabel, fontSize: DialogButtonFontSize))
            {
                BeginDepartureReturn(applyRefuel: returnToFlightPending);
            }

            GUI.depth = previousDepth;
        }

        private void DrawInactiveRadarMfds()
        {
            if (MidAirRefuelViewport.FrameTopPx < 24f)
            {
                return;
            }

            var previousDepth = GUI.depth;
            GUI.depth = -50;
            DrawInactiveRadarBezel(
                RadarMfdBezelRenderer.ComputeTopLeftLayout(),
                RadarMfdRangeRingsRenderer.ScopeKind.LongRange);
            DrawInactiveRadarBezel(
                RadarMfdBezelRenderer.ComputeTopRightLayout(),
                RadarMfdRangeRingsRenderer.ScopeKind.ShortRange);
            GUI.depth = previousDepth;
            GUI.color = Color.white;
        }

        private static void DrawInactiveRadarBezel(
            RadarMfdBezelRenderer.Layout layout,
            RadarMfdRangeRingsRenderer.ScopeKind scopeKind)
        {
            var bandHeight = MidAirRefuelViewport.FrameTopPx;
            var scale = Mathf.Min(1f, (bandHeight - 4f) / layout.AssemblyRect.height);
            if (scale <= 0.01f)
            {
                return;
            }

            var pivot = layout.AssemblyRect.x <= 1f
                ? new Vector2(layout.AssemblyRect.x, layout.AssemblyRect.y)
                : new Vector2(layout.AssemblyRect.xMax, layout.AssemblyRect.y);

            var previousMatrix = GUI.matrix;
            GUI.matrix = ScaleMatrixAroundPoint(pivot, scale) * previousMatrix;
            GUI.color = Color.white;
            GUI.DrawTexture(layout.AssemblyRect, RadarMfdBezelRenderer.GetBezelTexture(layout));
            RadarMfdRangeRingsRenderer.Draw(layout, scopeKind);
            GUI.matrix = previousMatrix;
        }

        private static Matrix4x4 ScaleMatrixAroundPoint(Vector2 pivot, float scale)
        {
            return Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0f))
                * Matrix4x4.Scale(new Vector3(scale, scale, 1f))
                * Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0f));
        }

        private static void DrawCornerBox(Vector2 center, float half, float armLen, float thickness)
        {
            var x0 = center.x - half;
            var x1 = center.x + half;
            var y0 = center.y - half;
            var y1 = center.y + half;

            DrawLine(new Vector2(x0, y0 + armLen), new Vector2(x0, y0), thickness);
            DrawLine(new Vector2(x0, y0), new Vector2(x0 + armLen, y0), thickness);

            DrawLine(new Vector2(x1 - armLen, y0), new Vector2(x1, y0), thickness);
            DrawLine(new Vector2(x1, y0), new Vector2(x1, y0 + armLen), thickness);

            DrawLine(new Vector2(x0, y1 - armLen), new Vector2(x0, y1), thickness);
            DrawLine(new Vector2(x0, y1), new Vector2(x0 + armLen, y1), thickness);

            DrawLine(new Vector2(x1 - armLen, y1), new Vector2(x1, y1), thickness);
            DrawLine(new Vector2(x1, y1 - armLen), new Vector2(x1, y1), thickness);
        }

        private static void DrawLine(Vector2 from, Vector2 to, float thickness)
        {
            var delta = to - from;
            var length = delta.magnitude;
            if (length < 0.5f)
            {
                return;
            }

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var rect = new Rect(from.x, from.y - thickness * 0.5f, length, thickness);
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, from);
        }

        private void FinishAfterDeparture()
        {
            if (returnToFlightPending)
            {
                ReturnToFlight(departureApplyRefuel);
                return;
            }

            ReturnToMainMenu();
        }

        private static void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }

        private void ReturnToFlight(bool applyRefuel)
        {
            if (!flightReturnSnapshot.IsValid)
            {
                ReturnToMainMenu();
                return;
            }

            var snapshot = flightReturnSnapshot;
            if (applyRefuel)
            {
                SortieSnapshotFuel.ApplyMaxFuel(ref snapshot);
            }

            MidAirRefuelHandoffState.BeginReturnToFlight(snapshot);
            FlightMissionStartBootstrap.ResetForSceneLoad();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.FlightTest);
        }
    }
}
