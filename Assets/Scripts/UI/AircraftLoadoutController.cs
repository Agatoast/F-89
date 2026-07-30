using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class AircraftLoadoutController : MonoBehaviour
    {
        private static AircraftLoadoutController activeInstance;

        public static bool BlocksPauseMenu => activeInstance != null && activeInstance.isEditingGunRounds;

        private const string MockupResourcePath = "Loadout/plane_loadout";
        private const float ActionButtonMargin = 20f;
        private const float ActionButtonWidth = 285f;
        private const float ActionButtonHeight = 66f;
        private const float PayloadButtonWidth = 380f;
        private static readonly Color HudYellow = new Color(1f, 0.88f, 0f);

        private GUIStyle loadoutValueStyle;
        private GUIStyle speedDecreaseLabelStyle;
        private GUIStyle instructionStyle;
        private GUIStyle weightParagraphLabelStyle;
        private GUIStyle weaponCounterStyle;
        private GUIStyle gunCounterFieldStyle;
        private GUIStyle payloadConfirmStyle;
        private GUIStyle disabledActionButtonStyle;

        private Texture2D mockupTexture;
        private Rect mockupRect;
        private bool showBailOutConfirm;
        private bool showOverweightWarning;
        private PayloadConfirmAction pendingPayloadConfirm;
        private bool isDraggingWeapon;
        private bool isEditingGunRounds;
        private bool gunRoundsFocusPending;
        private string gunRoundsEditText = string.Empty;
        private Rect gunRoundsEditRect;

        private enum GunRoundsEditSource
        {
            None = 0,
            Plane = 1,
            Tray = 2
        }

        private enum PayloadConfirmAction
        {
            None = 0,
            SetDefault = 1,
            Clear = 2
        }

        private GunRoundsEditSource gunRoundsEditSource;
        private const float GunCounterTextPaddingPx = 2f;
        private int gunRoundsEditFontSize = 14;
        private bool gunRoundsSelectAllPending;
        private AircraftLoadoutWeapon dragWeapon;
        private readonly HardpointHit[] hardpointHits = new HardpointHit[24];
        private int hardpointHitCount;
        private int heldGunArrowIndex = -1;
        private float gunArrowHoldStartTime;
        private float nextGunArrowRepeatTime;

        private const float GunArrowInitialRepeatDelaySeconds = 0.3f;
        private const float GunArrowMaxRepeatIntervalSeconds = 0.16f;
        private const float GunArrowMinRepeatIntervalSeconds = 0.008f;
        private const float GunArrowRepeatRampSeconds = 0.9f;

        private enum HardpointSlotType
        {
            LinkedPair,
            WingTip
        }

        private struct HardpointHit
        {
            public Rect Rect;
            public Vector2 CenterPx;
            public int HardpointNumber;
            public HardpointSlotType SlotType;
            public int PairIndex;
        }

        // Rear tip of the baked aircraft on plane_loadout.png (1024x674).
        private static readonly Vector2 AircraftTailTipCenterPx = new Vector2(512f, 530f);

        private void OnEnable()
        {
            activeInstance = this;
            AircraftLoadoutState.LoadCharacterDefault(CharacterSessionState.ActiveSave);
            mockupTexture = Resources.Load<Texture2D>(MockupResourcePath);
            ResetDragState();
        }

        private void OnDisable()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            UpdateHeldGunArrowRepeat();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawMockupBackground();
            if (mockupTexture == null)
            {
                return;
            }

            SyncArtLockedFontSizes();
            hardpointHitCount = 0;
            BuildHardpointHitRects();
            HandleDragAndDropInput();
            DrawAssignedWeaponIcons();
            DrawWeaponTrayCounters();
            DrawLoadoutWeightParagraph();
            DrawSpeedDecrease();
            DrawLoadoutInstructions();
            DrawGunRoundsEditOverlay();
            if (!showOverweightWarning && !showBailOutConfirm && pendingPayloadConfirm == PayloadConfirmAction.None)
            {
                DrawDraggedWeapon();
            }

            DrawActionButtons();

            if (showBailOutConfirm)
            {
                var dialogResult = BailOutConfirmDialog.Draw(true);
                if (dialogResult == BailOutConfirmDialog.Result.Confirmed)
                {
                    ConfirmBailOut();
                }
                else if (dialogResult == BailOutConfirmDialog.Result.Cancelled)
                {
                    showBailOutConfirm = false;
                }
            }
            else if (showOverweightWarning)
            {
                if (LoadoutOverweightDialog.Draw(true))
                {
                    showOverweightWarning = false;
                }
            }
            else if (pendingPayloadConfirm != PayloadConfirmAction.None)
            {
                DrawPayloadConfirmDialog();
            }
        }

        private void ResetDragState()
        {
            isDraggingWeapon = false;
            dragWeapon = AircraftLoadoutWeapon.None;
            heldGunArrowIndex = -1;
            CancelGunRoundsEdit();
        }

        private void DrawMockupBackground()
        {
            if (mockupTexture == null)
            {
                UiFitCanvas.Begin(16f / 9f);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                mockupRect = UiFitCanvas.Rect;
                return;
            }

            UiFitCanvas.DrawLetterboxedBackground(mockupTexture);
            mockupRect = UiFitCanvas.Rect;
        }

        private void EnsureMockupRect()
        {
            if (mockupTexture == null)
            {
                UiFitCanvas.Begin(16f / 9f);
                mockupRect = UiFitCanvas.Rect;
                return;
            }

            UiFitCanvas.Begin(mockupTexture);
            mockupRect = UiFitCanvas.Rect;
        }

        private void BuildHardpointHitRects()
        {
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            for (var i = 0; i < AircraftLoadoutLayout.LinkedHardpointPairCount; i++)
            {
                var pair = AircraftLoadoutLayout.GetLinkedHardpointPair(i);
                AddHardpointHit(
                    pair.LeftCenterPx,
                    AircraftLoadoutLayout.HardpointHitRect(
                        pair.LeftCenterPx,
                        mockupRect,
                        textureWidth,
                        textureHeight,
                        AircraftLoadoutLayout.GetHardpointHitExtraBottomPx(pair.LeftNumber),
                        pair.LeftNumber),
                    pair.LeftNumber,
                    HardpointSlotType.LinkedPair,
                    i);
                AddHardpointHit(
                    pair.RightCenterPx,
                    AircraftLoadoutLayout.HardpointHitRect(
                        pair.RightCenterPx,
                        mockupRect,
                        textureWidth,
                        textureHeight,
                        AircraftLoadoutLayout.GetHardpointHitExtraBottomPx(pair.RightNumber),
                        pair.RightNumber),
                    pair.RightNumber,
                    HardpointSlotType.LinkedPair,
                    i);
            }

            AddHardpointHit(
                AircraftLoadoutLayout.LeftWingTipCenterPx,
                AircraftLoadoutLayout.HardpointHitRect(AircraftLoadoutLayout.LeftWingTipCenterPx, mockupRect, textureWidth, textureHeight),
                hardpointNumber: 0,
                HardpointSlotType.WingTip,
                pairIndex: 0);
            AddHardpointHit(
                AircraftLoadoutLayout.RightWingTipCenterPx,
                AircraftLoadoutLayout.HardpointHitRect(AircraftLoadoutLayout.RightWingTipCenterPx, mockupRect, textureWidth, textureHeight),
                hardpointNumber: 0,
                HardpointSlotType.WingTip,
                pairIndex: 0);
        }

        private void AddHardpointHit(
            Vector2 centerPx,
            Rect rect,
            int hardpointNumber,
            HardpointSlotType slotType,
            int pairIndex)
        {
            if (hardpointHitCount >= hardpointHits.Length)
            {
                return;
            }

            hardpointHits[hardpointHitCount++] = new HardpointHit
            {
                Rect = rect,
                CenterPx = centerPx,
                HardpointNumber = hardpointNumber,
                SlotType = slotType,
                PairIndex = pairIndex
            };
        }

        private void HandleDragAndDropInput()
        {
            if (showBailOutConfirm || showOverweightWarning || pendingPayloadConfirm != PayloadConfirmAction.None)
            {
                return;
            }

            HandleGunArrowInput();
            HandleGunCounterInput();

            if (isEditingGunRounds)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.Used)
            {
                return;
            }

            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (isDraggingWeapon)
                {
                    if (TryDropDraggedWeapon(currentEvent.mousePosition)
                        || !TryFindHardpointHit(currentEvent.mousePosition, out _))
                    {
                        ResetDragState();
                    }

                    currentEvent.Use();
                    return;
                }

                if (TryBeginTrayDrag(textureWidth, textureHeight, currentEvent.mousePosition))
                {
                    currentEvent.Use();
                    return;
                }

                if (TryBeginHardpointDrag(currentEvent.mousePosition))
                {
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDrag && isDraggingWeapon)
            {
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && isDraggingWeapon)
            {
                if (TryDropDraggedWeapon(currentEvent.mousePosition))
                {
                    ResetDragState();
                }

                currentEvent.Use();
            }
        }

        private bool TryBeginHardpointDrag(Vector2 mousePosition)
        {
            if (!TryFindHardpointHit(mousePosition, out var hit))
            {
                return false;
            }

            var weapon = GetHardpointWeapon(hit);
            if (weapon == AircraftLoadoutWeapon.None)
            {
                return false;
            }

            ClearHardpoint(hit);
            isDraggingWeapon = true;
            dragWeapon = weapon;
            return true;
        }

        private bool TryBeginTrayDrag(int textureWidth, int textureHeight, Vector2 mousePosition)
        {
            foreach (var slot in AircraftLoadoutLayout.WeaponTraySlots)
            {
                if (slot.ShowGunRounds)
                {
                    continue;
                }

                var pickRect = AircraftLoadoutLayout.WeaponPickRect(slot, mockupRect, textureWidth, textureHeight);
                if (!pickRect.Contains(mousePosition))
                {
                    continue;
                }

                isDraggingWeapon = true;
                dragWeapon = slot.Weapon;
                return true;
            }

            return false;
        }

        private bool TryDropDraggedWeapon(Vector2 mousePosition)
        {
            if (dragWeapon == AircraftLoadoutWeapon.None)
            {
                return false;
            }

            if (!TryFindHardpointHit(mousePosition, out var hit))
            {
                return false;
            }

            return TryAssignWeaponToHardpoint(hit, dragWeapon);
        }

        private bool TryFindHardpointHit(Vector2 mousePosition, out HardpointHit hit)
        {
            hit = default;
            var bestDistanceSq = float.MaxValue;
            var found = false;
            var scale = mockupRect.width / mockupTexture.width;
            var mockupYPx = (mousePosition.y - mockupRect.y) / mockupRect.height * mockupTexture.height;

            for (var i = 0; i < hardpointHitCount; i++)
            {
                var candidate = hardpointHits[i];
                if (AircraftLoadoutLayout.IsFuselageHardpoint(candidate.HardpointNumber)
                    && !AircraftLoadoutLayout.MatchesFuselageHardpointBand(candidate.HardpointNumber, mockupYPx))
                {
                    continue;
                }

                var slopPx = candidate.HardpointNumber > 0
                    ? AircraftLoadoutLayout.GetHardpointHitSlopPx(candidate.HardpointNumber)
                    : AircraftLoadoutLayout.HardpointHitSlopPx * 0.5f;
                var slop = slopPx * scale;
                var expandedRect = candidate.Rect;
                expandedRect.xMin -= slop;
                expandedRect.yMin -= slop;
                expandedRect.xMax += slop;
                expandedRect.yMax += slop;

                var minTopPx = AircraftLoadoutLayout.GetHardpointHitMinTopPx(candidate.HardpointNumber);
                if (minTopPx > 0f)
                {
                    var minTopScreen = AircraftLoadoutLayout.GetHardpointHitMinTopScreenY(
                        minTopPx,
                        mockupRect,
                        mockupTexture.height);
                    expandedRect.yMin = Mathf.Max(expandedRect.yMin, minTopScreen);
                }

                if (!expandedRect.Contains(mousePosition))
                {
                    continue;
                }

                var center = candidate.Rect.center;
                var distanceSq = (center - mousePosition).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                hit = candidate;
                found = true;
            }

            return found;
        }

        private void ShowOverweightWarning()
        {
            ResetDragState();
            Cursor.visible = true;
            showOverweightWarning = true;
        }

        private bool TryAssignWeaponToHardpoint(HardpointHit hit, AircraftLoadoutWeapon weapon)
        {
            switch (hit.SlotType)
            {
                case HardpointSlotType.LinkedPair:
                    if (AircraftLoadoutState.WouldExceedMaxLoadoutForLinkedPair(hit.PairIndex, weapon))
                    {
                        ShowOverweightWarning();
                        return false;
                    }

                    if (!AircraftLoadoutState.CanAssignToLinkedPair(hit.PairIndex, weapon))
                    {
                        return false;
                    }

                    AircraftLoadoutState.AssignLinkedPair(hit.PairIndex, weapon);
                    return true;
                case HardpointSlotType.WingTip:
                    if (AircraftLoadoutState.WouldExceedMaxLoadoutForWingTip(weapon))
                    {
                        ShowOverweightWarning();
                        return false;
                    }

                    if (!AircraftLoadoutState.CanAssignToWingTip(weapon))
                    {
                        return false;
                    }

                    AircraftLoadoutState.AssignWingTip(weapon);
                    return true;
                default:
                    return false;
            }
        }

        private static AircraftLoadoutWeapon GetHardpointWeapon(HardpointHit hit)
        {
            switch (hit.SlotType)
            {
                case HardpointSlotType.LinkedPair:
                    return AircraftLoadoutState.GetLinkedPairWeapon(hit.PairIndex);
                case HardpointSlotType.WingTip:
                    return AircraftLoadoutState.WingTipWeapon;
                default:
                    return AircraftLoadoutWeapon.None;
            }
        }

        private static void ClearHardpoint(HardpointHit hit)
        {
            switch (hit.SlotType)
            {
                case HardpointSlotType.LinkedPair:
                    AircraftLoadoutState.AssignLinkedPair(hit.PairIndex, AircraftLoadoutWeapon.None);
                    break;
                case HardpointSlotType.WingTip:
                    AircraftLoadoutState.AssignWingTip(AircraftLoadoutWeapon.None);
                    break;
            }
        }

        private void EnsureStyles()
        {
            if (loadoutValueStyle != null
                && speedDecreaseLabelStyle != null
                && instructionStyle != null
                && weightParagraphLabelStyle != null
                && weaponCounterStyle != null
                && gunCounterFieldStyle != null
                && payloadConfirmStyle != null
                && disabledActionButtonStyle != null)
            {
                speedDecreaseLabelStyle.font = HudStyleFactory.ArialFont;
                speedDecreaseLabelStyle.fontStyle = FontStyle.Bold;
                speedDecreaseLabelStyle.alignment = TextAnchor.MiddleLeft;
                speedDecreaseLabelStyle.wordWrap = false;
                instructionStyle.font = HudStyleFactory.ArialFont;
                instructionStyle.fontStyle = FontStyle.Bold;
                instructionStyle.alignment = TextAnchor.UpperCenter;
                weightParagraphLabelStyle.font = HudStyleFactory.ArialFont;
                weightParagraphLabelStyle.fontStyle = FontStyle.Bold;
                loadoutValueStyle.alignment = TextAnchor.MiddleCenter;
                SyncArtLockedFontSizes();
                return;
            }

            loadoutValueStyle = HudStyleFactory.CreateLabel(36, FontStyle.Bold, TextAnchor.MiddleCenter, HudYellow);
            speedDecreaseLabelStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                HudYellow,
                wordWrap: false,
                font: HudStyleFactory.ArialFont);
            instructionStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                HudYellow,
                wordWrap: true,
                font: HudStyleFactory.ArialFont);
            weightParagraphLabelStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                HudYellow,
                font: HudStyleFactory.ArialFont);
            weaponCounterStyle = HudStyleFactory.CreateLabel(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            gunCounterFieldStyle = HudStyleFactory.CreateLabel(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            gunCounterFieldStyle.normal.background = Texture2D.whiteTexture;
            gunCounterFieldStyle.focused.background = Texture2D.whiteTexture;
            gunCounterFieldStyle.active.background = Texture2D.whiteTexture;
            gunCounterFieldStyle.hover.background = Texture2D.whiteTexture;
            payloadConfirmStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            disabledActionButtonStyle = HudStyleFactory.CreateLabel(
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.88f, 0.94f));
            SyncArtLockedFontSizes();
        }

        /// <summary>
        /// Scales art-locked copy with the fitted mockup so text tracks the plane graphic
        /// across resolutions and aspect letterboxing.
        /// </summary>
        private void SyncArtLockedFontSizes()
        {
            var scale = mockupRect.height > 1f
                ? mockupRect.height / AircraftLoadoutLayout.MockupReferenceHeightPx
                : UiFitCanvas.Scale;
            scale = Mathf.Clamp(scale, 0.5f, 1f);

            if (instructionStyle != null)
            {
                instructionStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * scale));
            }

            if (weightParagraphLabelStyle != null)
            {
                weightParagraphLabelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * scale));
            }

            if (speedDecreaseLabelStyle != null)
            {
                speedDecreaseLabelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * scale));
                speedDecreaseLabelStyle.wordWrap = false;
            }

            if (loadoutValueStyle != null)
            {
                loadoutValueStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(36f * scale));
            }
        }

        /// <summary>Offset in mockup pixels → screen pixels (resolution-independent vs the art).</summary>
        private float MockupPxToScreenX(float mockupPixels)
        {
            if (mockupTexture == null || mockupTexture.width <= 0)
            {
                return UiFitCanvas.Px(mockupPixels);
            }

            return mockupPixels / mockupTexture.width * mockupRect.width;
        }

        private float MockupPxToScreenY(float mockupPixels)
        {
            if (mockupTexture == null || mockupTexture.height <= 0)
            {
                return UiFitCanvas.Px(mockupPixels);
            }

            return mockupPixels / mockupTexture.height * mockupRect.height;
        }

        private void DrawWeaponTrayCounters()
        {
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            foreach (var slot in AircraftLoadoutLayout.WeaponTraySlots)
            {
                var rect = AircraftLoadoutLayout.WeaponCounterRect(slot, mockupRect, textureWidth, textureHeight);
                if (slot.ShowGunRounds)
                {
                    DrawGunRoundsCounter(rect, GunRoundsEditSource.Tray);
                    continue;
                }

                DrawCounter(rect, AircraftLoadoutState.GetMountedCount(slot.Weapon).ToString());
            }

            var gunRect = AircraftLoadoutLayout.GunCounterRect(mockupRect, textureWidth, textureHeight);
            DrawGunRoundsCounter(gunRect, GunRoundsEditSource.Plane);
        }

        private void DrawGunRoundsCounter(Rect containerRect, GunRoundsEditSource source)
        {
            var displayText = isEditingGunRounds
                ? gunRoundsEditText
                : AircraftLoadoutState.GunRounds.ToString();

            if (isEditingGunRounds && gunRoundsEditSource == source)
            {
                return;
            }

            DrawCounter(containerRect, displayText);
        }

        private void BeginGunRoundsEdit(GunRoundsEditSource source, Rect containerRect)
        {
            isEditingGunRounds = true;
            gunRoundsEditSource = source;
            gunRoundsEditText = AircraftLoadoutState.GunRounds.ToString();
            gunRoundsFocusPending = true;
            gunRoundsSelectAllPending = true;
            heldGunArrowIndex = -1;
            GetGunRoundsTextRect(containerRect, gunRoundsEditText, gunCounterFieldStyle, out gunRoundsEditFontSize);
            gunRoundsEditRect = GetGunRoundsTextRect(containerRect, gunRoundsEditText, gunCounterFieldStyle, out _);
        }

        private static Rect GetGunRoundsTextRect(Rect containerRect, string text, GUIStyle style, out int fontSize)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(containerRect.height * 0.48f), 14, 32);
            style.fontSize = fontSize;

            var content = new GUIContent(string.IsNullOrEmpty(text) ? "0" : text);
            var contentSize = style.CalcSize(content);
            while (style.fontSize > 12 && contentSize.x > containerRect.width - GunCounterTextPaddingPx * 2f)
            {
                style.fontSize--;
                contentSize = style.CalcSize(content);
            }

            fontSize = style.fontSize;
            var paddedWidth = contentSize.x + GunCounterTextPaddingPx * 2f;
            var paddedHeight = contentSize.y + GunCounterTextPaddingPx * 2f;
            return new Rect(
                containerRect.x + (containerRect.width - paddedWidth) * 0.5f,
                containerRect.y + (containerRect.height - paddedHeight) * 0.5f,
                paddedWidth,
                paddedHeight);
        }

        private void DrawGunRoundsEditOverlay()
        {
            if (!isEditingGunRounds || mockupTexture == null)
            {
                return;
            }

            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;
            var containerRect = gunRoundsEditSource == GunRoundsEditSource.Tray
                ? GetGunTrayCounterRect(textureWidth, textureHeight)
                : AircraftLoadoutLayout.GunCounterRect(mockupRect, textureWidth, textureHeight);

            gunRoundsEditRect = GetGunRoundsTextRect(containerRect, gunRoundsEditText, gunCounterFieldStyle, out _);
            DrawEditableGunCounter(gunRoundsEditRect);
        }

        private Rect GetGunTrayCounterRect(int textureWidth, int textureHeight)
        {
            foreach (var slot in AircraftLoadoutLayout.WeaponTraySlots)
            {
                if (slot.ShowGunRounds)
                {
                    return AircraftLoadoutLayout.WeaponCounterRect(slot, mockupRect, textureWidth, textureHeight);
                }
            }

            return Rect.zero;
        }

        private void DrawEditableGunCounter(Rect rect)
        {
            gunCounterFieldStyle.fontSize = gunRoundsEditFontSize;

            GUI.SetNextControlName("GunRoundsField");
            var editedText = GUI.TextField(rect, gunRoundsEditText, 4, gunCounterFieldStyle);
            gunRoundsEditText = FilterGunRoundsInput(editedText);

            if (gunRoundsFocusPending || GUI.GetNameOfFocusedControl() != "GunRoundsField")
            {
                GUI.FocusControl("GunRoundsField");
                gunRoundsFocusPending = false;
            }

            if (gunRoundsSelectAllPending && Event.current.type == EventType.Repaint)
            {
                var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                textEditor.SelectAll();
                gunRoundsSelectAllPending = false;
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    CommitGunRoundsEdit();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    CancelGunRoundsEdit();
                    Event.current.Use();
                }
            }
        }

        private static string FilterGunRoundsInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var filtered = string.Empty;
            foreach (var character in text)
            {
                if (character >= '0' && character <= '9')
                {
                    filtered += character;
                }
            }

            if (filtered.Length > 4)
            {
                filtered = filtered.Substring(0, 4);
            }

            return filtered;
        }

        private void HandleGunCounterInput()
        {
            if (isDraggingWeapon || showBailOutConfirm || showOverweightWarning || mockupTexture == null)
            {
                return;
            }

            var currentEvent = Event.current;

            if (isEditingGunRounds)
            {
                if (currentEvent.type == EventType.MouseDown
                    && currentEvent.button == 0
                    && !gunRoundsEditRect.Contains(currentEvent.mousePosition))
                {
                    CommitGunRoundsEdit();
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (IsMouseOverGunArrow(currentEvent.mousePosition))
            {
                return;
            }

            EnsureMockupRect();
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            var gunRect = AircraftLoadoutLayout.GunCounterRect(mockupRect, textureWidth, textureHeight);
            if (TryBeginGunRoundsEditAt(gunRect, GunRoundsEditSource.Plane, currentEvent.mousePosition))
            {
                currentEvent.Use();
                return;
            }

            foreach (var slot in AircraftLoadoutLayout.WeaponTraySlots)
            {
                if (!slot.ShowGunRounds)
                {
                    continue;
                }

                var trayRect = AircraftLoadoutLayout.WeaponCounterRect(slot, mockupRect, textureWidth, textureHeight);
                if (TryBeginGunRoundsEditAt(trayRect, GunRoundsEditSource.Tray, currentEvent.mousePosition))
                {
                    currentEvent.Use();
                    return;
                }
            }
        }

        private bool TryBeginGunRoundsEditAt(Rect containerRect, GunRoundsEditSource source, Vector2 mousePosition)
        {
            var displayText = AircraftLoadoutState.GunRounds.ToString();
            var textRect = GetGunRoundsTextRect(containerRect, displayText, weaponCounterStyle, out _);
            if (!textRect.Contains(mousePosition))
            {
                return false;
            }

            BeginGunRoundsEdit(source, containerRect);
            return true;
        }

        private bool IsMouseOverGunArrow(Vector2 mousePosition)
        {
            if (mockupTexture == null)
            {
                return false;
            }

            EnsureMockupRect();
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            for (var i = 0; i < AircraftLoadoutLayout.GunArrowHits.Length; i++)
            {
                var hit = AircraftLoadoutLayout.GunArrowHits[i];
                var rect = AircraftLoadoutLayout.GunArrowHitRect(hit, mockupRect, textureWidth, textureHeight);
                if (rect.Contains(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void CommitGunRoundsEdit()
        {
            if (!isEditingGunRounds)
            {
                return;
            }

            isEditingGunRounds = false;
            gunRoundsEditSource = GunRoundsEditSource.None;
            gunRoundsFocusPending = false;
            gunRoundsSelectAllPending = false;
            GUI.FocusControl(null);

            int rounds;
            if (string.IsNullOrEmpty(gunRoundsEditText))
            {
                rounds = 0;
            }
            else if (!int.TryParse(gunRoundsEditText, out rounds))
            {
                gunRoundsEditText = AircraftLoadoutState.GunRounds.ToString();
                return;
            }

            if (!AircraftLoadoutState.TrySetGunRounds(rounds))
            {
                gunRoundsEditText = AircraftLoadoutState.GunRounds.ToString();
                ShowOverweightWarning();
                return;
            }

            gunRoundsEditText = AircraftLoadoutState.GunRounds.ToString();
        }

        private void CancelGunRoundsEdit()
        {
            isEditingGunRounds = false;
            gunRoundsEditSource = GunRoundsEditSource.None;
            gunRoundsFocusPending = false;
            gunRoundsSelectAllPending = false;
            gunRoundsEditText = AircraftLoadoutState.GunRounds.ToString();
            GUI.FocusControl(null);
        }

        private void DrawCounter(Rect rect, string countText)
        {
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.48f), 14, 32);
            weaponCounterStyle.fontSize = fontSize;

            var contentSize = weaponCounterStyle.CalcSize(new GUIContent(countText));
            while (weaponCounterStyle.fontSize > 12 && contentSize.x > rect.width - 2f)
            {
                weaponCounterStyle.fontSize--;
                contentSize = weaponCounterStyle.CalcSize(new GUIContent(countText));
            }

            GUI.Label(rect, countText, weaponCounterStyle);
        }

        private void DrawAssignedWeaponIcons()
        {
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            for (var i = 0; i < AircraftLoadoutLayout.LinkedHardpointPairCount; i++)
            {
                var pair = AircraftLoadoutLayout.GetLinkedHardpointPair(i);
                var weapon = AircraftLoadoutState.GetLinkedPairWeapon(i);
                DrawHardpointWeapon(pair.LeftCenterPx, weapon, textureWidth, textureHeight);
                DrawHardpointWeapon(pair.RightCenterPx, weapon, textureWidth, textureHeight);
            }

            DrawHardpointWeapon(AircraftLoadoutLayout.LeftWingTipCenterPx, AircraftLoadoutState.WingTipWeapon, textureWidth, textureHeight);
            DrawHardpointWeapon(AircraftLoadoutLayout.RightWingTipCenterPx, AircraftLoadoutState.WingTipWeapon, textureWidth, textureHeight);
        }

        private void DrawHardpointWeapon(Vector2 centerPx, AircraftLoadoutWeapon weapon, int textureWidth, int textureHeight)
        {
            if (weapon == AircraftLoadoutWeapon.None)
            {
                return;
            }

            var rect = AircraftLoadoutLayout.HardpointRect(centerPx, mockupRect, textureWidth, textureHeight);
            var maskVerticalPadding = AircraftLoadoutLayout.HardpointMaskVerticalPaddingPx / textureHeight * mockupRect.height;
            LoadoutWeaponIconLibrary.DrawHardpointMount(rect, weapon, maskVerticalPadding);
        }

        private struct LoadoutWeightParagraphLayout
        {
            public Rect MaximumLabelRect;
            public Rect MaximumValueRect;
            public Rect CurrentLabelRect;
            public Rect CurrentValueRect;
        }

        private LoadoutWeightParagraphLayout GetLoadoutWeightParagraphLayout()
        {
            var tip = AircraftLoadoutLayout.MockupPixelCenterRect(
                AircraftTailTipCenterPx.x,
                AircraftTailTipCenterPx.y,
                1f,
                1f,
                mockupRect,
                mockupTexture.width,
                mockupTexture.height);
            var center = tip.center;

            var maxLabel = "MAXIMUM LOADOUT";
            var maxValue = $"{AircraftLoadoutState.MaxLoadoutLbs:N0} lbs";
            var currentLabel = "CURRENT LOADOUT";
            var currentValue = $"{AircraftLoadoutState.ComputeCurrentLoadoutLbs():N0} lbs";

            var lineWidth = mockupRect.width * 0.36f;
            var maxLabelHeight = weightParagraphLabelStyle.CalcHeight(new GUIContent(maxLabel), lineWidth);
            var maxValueHeight = loadoutValueStyle.CalcHeight(new GUIContent(maxValue), lineWidth);
            var blankLineHeight = weightParagraphLabelStyle.CalcHeight(new GUIContent("A"), lineWidth);
            var currentLabelHeight = weightParagraphLabelStyle.CalcHeight(new GUIContent(currentLabel), lineWidth);
            var currentValueHeight = loadoutValueStyle.CalcHeight(new GUIContent(currentValue), lineWidth);
            var totalHeight = maxLabelHeight
                + maxValueHeight
                + blankLineHeight
                + currentLabelHeight
                + currentValueHeight;
            var threeLetterWidth = weightParagraphLabelStyle.CalcSize(new GUIContent("MAX")).x;
            var twoLetterWidth = weightParagraphLabelStyle.CalcSize(new GUIContent("MA")).x;
            // 85 mockup-px down from tail tip — locked to plane_loadout art.
            var y = center.y - totalHeight * 0.5f + MockupPxToScreenY(85f);
            var x = center.x - lineWidth * 0.5f + threeLetterWidth - twoLetterWidth;
            var currentBlockY = y + maxLabelHeight + maxValueHeight + blankLineHeight;

            var layout = new LoadoutWeightParagraphLayout
            {
                MaximumLabelRect = new Rect(x, y, lineWidth, maxLabelHeight),
                MaximumValueRect = new Rect(x, y + maxLabelHeight, lineWidth, maxValueHeight),
                CurrentLabelRect = new Rect(x, currentBlockY, lineWidth, currentLabelHeight),
                CurrentValueRect = new Rect(
                    x,
                    currentBlockY + currentLabelHeight,
                    lineWidth,
                    currentValueHeight)
            };
            return layout;
        }

        private void DrawLoadoutWeightParagraph()
        {
            var layout = GetLoadoutWeightParagraphLayout();
            GUI.Label(layout.MaximumLabelRect, "MAXIMUM LOADOUT", weightParagraphLabelStyle);
            GUI.Label(
                layout.MaximumValueRect,
                $"{AircraftLoadoutState.MaxLoadoutLbs:N0} lbs",
                loadoutValueStyle);
            GUI.Label(layout.CurrentLabelRect, "CURRENT LOADOUT", weightParagraphLabelStyle);
            GUI.Label(
                layout.CurrentValueRect,
                $"{AircraftLoadoutState.ComputeCurrentLoadoutLbs():N0} lbs",
                loadoutValueStyle);
        }

        private void DrawSpeedDecrease()
        {
            if (mockupTexture == null)
            {
                return;
            }

            var layout = GetLoadoutWeightParagraphLayout();

            var maxLabelContent = new GUIContent("MAXIMUM LOADOUT");
            var maxLabelTextWidth = weightParagraphLabelStyle.CalcSize(maxLabelContent).x;
            var payloadLeft = layout.MaximumLabelRect.center.x
                + maxLabelTextWidth * 0.5f
                + MockupPxToScreenX(24f);

            var labelContent = new GUIContent("PAYLOAD EFFECT ON MAXIMUM SPEED");
            var labelWidth = speedDecreaseLabelStyle.CalcSize(labelContent).x;
            var labelRect = new Rect(
                payloadLeft,
                layout.MaximumLabelRect.y,
                labelWidth,
                layout.MaximumLabelRect.height);

            var percentText = $"-{AircraftLoadoutState.ComputeSpeedDecreasePercent()}%";
            var valueRect = new Rect(
                payloadLeft,
                layout.MaximumValueRect.y,
                labelWidth,
                layout.MaximumValueRect.height);

            var previousLabelAlign = speedDecreaseLabelStyle.alignment;
            var previousValueAlign = loadoutValueStyle.alignment;
            speedDecreaseLabelStyle.alignment = TextAnchor.MiddleLeft;
            loadoutValueStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(labelRect, labelContent, speedDecreaseLabelStyle);
            GUI.Label(valueRect, percentText, loadoutValueStyle);
            speedDecreaseLabelStyle.alignment = previousLabelAlign;
            loadoutValueStyle.alignment = previousValueAlign;
        }

        private void DrawLoadoutInstructions()
        {
            if (mockupTexture == null)
            {
                return;
            }

            var rect = AircraftLoadoutLayout.InstructionsRect(
                mockupRect,
                mockupTexture.width,
                mockupTexture.height);
            const string instructions =
                "DRAG WEAPON TO HARDPOINT\n\n" +
                "HARDPOINT ON OTHER WING WILL\n" +
                "HAVE IDENTICAL WEAPON ADDED\n\n" +
                "USE ARROWS ON GUN TO\n" +
                "INCREASE GAU-27A ROUNDS\n" +
                "MAXIMUM 3,000 ROUNDS";

            var fontSize = instructionStyle.fontSize;
            while (fontSize > 10
                   && instructionStyle.CalcHeight(new GUIContent(instructions), rect.width) > rect.height)
            {
                fontSize--;
                instructionStyle.fontSize = fontSize;
            }

            GUI.Label(rect, instructions, instructionStyle);
        }

        private void DrawActionButtons()
        {
            if (showBailOutConfirm || showOverweightWarning || pendingPayloadConfirm != PayloadConfirmAction.None)
            {
                return;
            }

            var buttonWidth = UiFitCanvas.Px(ActionButtonWidth);
            var buttonHeight = UiFitCanvas.Px(ActionButtonHeight);
            var margin = UiFitCanvas.Px(ActionButtonMargin);
            var payloadButtonWidth = UiFitCanvas.Px(PayloadButtonWidth);
            var defaultRect = new Rect(UiFitCanvas.Rect.x + margin, UiFitCanvas.Rect.y + margin, payloadButtonWidth, buttonHeight);
            var clearRect = new Rect(defaultRect.x, defaultRect.yMax + UiFitCanvas.Px(20f), payloadButtonWidth, buttonHeight);
            if (DrawActionButton(defaultRect, "SET AS DEFAULT PAYLOAD", 20))
            {
                ResetDragState();
                pendingPayloadConfirm = PayloadConfirmAction.SetDefault;
            }

            if (DrawActionButton(clearRect, "CLEAR PAYLOAD", 20))
            {
                ResetDragState();
                pendingPayloadConfirm = PayloadConfirmAction.Clear;
            }

            var gap = UiFitCanvas.Px(32f);
            var fromCarrierResupply = CarrierResupplyState.IsResupplyFromCarrier;
            var y = UiFitCanvas.Rect.yMax - buttonHeight - margin;

            if (fromCarrierResupply)
            {
                var continueWidth = UiFitCanvas.Px(320f);
                var continueRect = new Rect(
                    UiFitCanvas.Rect.xMax - continueWidth - margin + UiFitCanvas.Px(160f),
                    y,
                    continueWidth,
                    buttonHeight);
                if (DrawActionButton(continueRect, "CONTINUE MISSION", 20))
                {
                    heldGunArrowIndex = -1;
                    CancelGunRoundsEdit();
                    StartMission();
                }

                return;
            }

            var totalWidth = buttonWidth * 2f + gap;
            var startX = UiFitCanvas.Rect.xMax - totalWidth - margin + UiFitCanvas.Px(160f);
            var bailRect = new Rect(startX, y, buttonWidth, buttonHeight);
            var startRect = new Rect(startX + buttonWidth + gap, y, buttonWidth, buttonHeight);

            if (DrawActionButton(bailRect, "BAIL OUT?", 20))
            {
                heldGunArrowIndex = -1;
                CancelGunRoundsEdit();
                showBailOutConfirm = true;
            }

            if (DrawActionButton(startRect, "START MISSION", 20))
            {
                heldGunArrowIndex = -1;
                CancelGunRoundsEdit();
                StartMission();
            }
        }

        private bool DrawActionButton(Rect rect, string label, int fontSize)
        {
            if (!isDraggingWeapon)
            {
                return StartPageMenuStyles.DrawMenuButton(rect, label, fontSize: fontSize);
            }

            StartPageMenuStyles.DrawMenuButtonChrome(rect);
            disabledActionButtonStyle.fontSize = fontSize;
            GUI.Label(rect, label, disabledActionButtonStyle);
            return false;
        }

        private void DrawPayloadConfirmDialog()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var width = UiFitCanvas.Px(500f);
            var height = UiFitCanvas.Px(210f);
            var dialog = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - width) * 0.5f,
                UiFitCanvas.Rect.y + (UiFitCanvas.Rect.height - height) * 0.5f,
                width,
                height);
            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialog, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialog, 2f);

            var message = pendingPayloadConfirm == PayloadConfirmAction.SetDefault
                ? "Save the currently mounted weapons and gun rounds as this character's default payload?"
                : "Remove every mounted weapon and all gun rounds from this payload?";
            GUI.Label(
                new Rect(dialog.x + UiFitCanvas.Px(28f), dialog.y + UiFitCanvas.Px(28f), dialog.width - UiFitCanvas.Px(56f), UiFitCanvas.Px(84f)),
                message,
                payloadConfirmStyle);

            var buttonWidth = UiFitCanvas.Px(140f);
            var buttonHeight = UiFitCanvas.Px(42f);
            var y = dialog.yMax - buttonHeight - UiFitCanvas.Px(24f);
            var yes = new Rect(dialog.center.x - buttonWidth - UiFitCanvas.Px(12f), y, buttonWidth, buttonHeight);
            var no = new Rect(dialog.center.x + UiFitCanvas.Px(12f), y, buttonWidth, buttonHeight);
            if (StartPageMenuStyles.DrawMenuButton(yes, "YES", fontSize: 16))
            {
                if (pendingPayloadConfirm == PayloadConfirmAction.SetDefault)
                {
                    AircraftLoadoutState.SaveCharacterDefault(CharacterSessionState.ActiveSave);
                }
                else
                {
                    AircraftLoadoutState.ClearPayload();
                }

                pendingPayloadConfirm = PayloadConfirmAction.None;
            }
            else if (StartPageMenuStyles.DrawMenuButton(no, "NO", fontSize: 16))
            {
                pendingPayloadConfirm = PayloadConfirmAction.None;
            }
        }

        private void HandleGunArrowInput()
        {
            if (isDraggingWeapon
                || showBailOutConfirm
                || showOverweightWarning
                || pendingPayloadConfirm != PayloadConfirmAction.None
                || isEditingGunRounds
                || mockupTexture == null)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                heldGunArrowIndex = -1;
                return;
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            EnsureMockupRect();
            var textureWidth = mockupTexture.width;
            var textureHeight = mockupTexture.height;

            for (var i = 0; i < AircraftLoadoutLayout.GunArrowHits.Length; i++)
            {
                var hit = AircraftLoadoutLayout.GunArrowHits[i];
                var rect = AircraftLoadoutLayout.GunArrowHitRect(hit, mockupRect, textureWidth, textureHeight);
                if (!rect.Contains(currentEvent.mousePosition))
                {
                    continue;
                }

                AircraftLoadoutState.AdjustGunRounds(hit.RoundDelta);
                heldGunArrowIndex = i;
                gunArrowHoldStartTime = Time.unscaledTime;
                nextGunArrowRepeatTime = Time.unscaledTime + GunArrowInitialRepeatDelaySeconds;
                currentEvent.Use();
                return;
            }
        }

        private void UpdateHeldGunArrowRepeat()
        {
            if (heldGunArrowIndex < 0)
            {
                return;
            }

            if (!Input.GetMouseButton(0)
                || mockupTexture == null
                || isDraggingWeapon
                || isEditingGunRounds
                || showBailOutConfirm
                || showOverweightWarning)
            {
                heldGunArrowIndex = -1;
                return;
            }

            if (Time.unscaledTime < nextGunArrowRepeatTime)
            {
                return;
            }

            var heldHit = AircraftLoadoutLayout.GunArrowHits[heldGunArrowIndex];
            AircraftLoadoutState.AdjustGunRounds(heldHit.RoundDelta);
            nextGunArrowRepeatTime = Time.unscaledTime + GetGunArrowRepeatInterval();
        }

        private float GetGunArrowRepeatInterval()
        {
            var holdDuration = Time.unscaledTime - gunArrowHoldStartTime;
            var ramp = Mathf.Clamp01(holdDuration / GunArrowRepeatRampSeconds);
            ramp *= ramp;
            return Mathf.Lerp(GunArrowMaxRepeatIntervalSeconds, GunArrowMinRepeatIntervalSeconds, ramp);
        }

        private void DrawDraggedWeapon()
        {
            if (!isDraggingWeapon || dragWeapon == AircraftLoadoutWeapon.None)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent.type != EventType.Repaint
                && currentEvent.type != EventType.MouseDrag
                && currentEvent.type != EventType.MouseDown
                && currentEvent.type != EventType.MouseUp)
            {
                return;
            }

            var dragRect = AircraftLoadoutLayout.HardpointRectAtScreenPoint(
                currentEvent.mousePosition,
                mockupTexture.width,
                mockupTexture.height,
                mockupRect);

            LoadoutWeaponIconLibrary.DrawWeaponIcon(
                dragRect,
                dragWeapon,
                LoadoutWeaponIconLibrary.GetHardpointMountSizeMultiplier(dragWeapon));
        }

        private static void StartMission()
        {
            AircraftLoadoutState.MarkConfigured();
            var resupplyFromCarrier = CarrierResupplyState.IsResupplyFromCarrier;
            var fromFriendlyOutpost = FriendlyOutpostTakeoffState.HasPending;
            CarrierResupplyState.Clear();
            Time.timeScale = 1f;

            if (resupplyFromCarrier || fromFriendlyOutpost)
            {
                LandBossEncounter.ResetUndefeatedBossHealthOnRearm();
            }

            if (FriendlyOutpostTakeoffState.TryConsume(out var takeoffOutpost))
            {
                FlightMissionLaunchState.BeginOutpostLaunch(takeoffOutpost, vtolTakeoff: true);
                SceneManager.LoadScene(GameScenes.FlightTest);
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (!resupplyFromCarrier)
            {
                LandBossMissionAssignment.MarkAssignedMissionRun(save);
            }

            if (resupplyFromCarrier)
            {
                FlightMissionLaunchState.BeginCarrierLaunch();
            }
            else if (save != null && !string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName))
            {
                FlightMissionLaunchState.BeginOutpostLaunch(save.MissionLaunchOutpostName);
            }
            else
            {
                FlightMissionLaunchState.BeginCarrierLaunch();
            }

            SceneManager.LoadScene(GameScenes.FlightTest);
        }

        private static void ConfirmBailOut()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }
    }
}
