using UnityEngine;

namespace F89.UI
{
    public static class RunwayDeckMenuDialog
    {
        public enum Result
        {
            None = 0,
            Rearm = 1,
            Refuel = 2,
            Dismount = 3,
            TakeOff = 4,
            EndMission = 5
        }

        public struct Options
        {
            public bool RefuelEnabled;
            public bool RearmEnabled;
            public bool DismountEnabled;
            public bool TakeOffEnabled;
            public bool EndMissionEnabled;
            public bool ShowMissionStatus;
            public bool PrimaryMissionComplete;
            public bool SecondaryMissionComplete;
            public string Subtitle;
            /// <summary>Vertical anchor for the dialog (0.5 = center). Higher values sit lower on screen.</summary>
            public float DialogVerticalAnchor;
            /// <summary>Extra downward offset applied to the button column.</summary>
            public float ButtonVerticalOffset;
        }

        private const float DialogWidth = 480f;
        private const float ButtonWidth = 220f;
        private const float ButtonHeight = 44f;
        private const float ButtonGap = 12f;
        private const int ButtonFontSize = 16;
        private const int ButtonCount = 5;
        private const float MissionStatusColumnWidth = 72f;
        private const float MissionStatusLightSize = 18f;

        private static readonly Color MissionIncompleteLightColor = new Color(0.82f, 0.12f, 0.1f);
        private static readonly Color MissionCompleteLightColor = new Color(0.12f, 0.72f, 0.18f);

        private static GUIStyle messageStyle;
        private static GUIStyle missionStatusHeadingStyle;
        private static GUIStyle missionStatusSubheadingStyle;

        public static Result Draw(bool visible, in Options options)
        {
            if (!visible)
            {
                return Result.None;
            }

            EnsureStyles();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var dialogHeight = 120f + ButtonCount * (ButtonHeight + ButtonGap);
            var anchorY = options.DialogVerticalAnchor > 0f ? options.DialogVerticalAnchor : 0.5f;

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                Screen.height * anchorY - dialogHeight * 0.5f,
                DialogWidth,
                dialogHeight);

            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);

            var subtitle = string.IsNullOrWhiteSpace(options.Subtitle) ? "Runway" : options.Subtitle;
            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 20f, dialogRect.width - 48f, 36f),
                subtitle,
                messageStyle);

            var buttonX = dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f;
            var buttonY = dialogRect.y + 64f + options.ButtonVerticalOffset;

            if (options.ShowMissionStatus)
            {
                DrawMissionStatusColumn(
                    new Rect(
                        dialogRect.x + 16f,
                        dialogRect.y + 58f + options.ButtonVerticalOffset,
                        MissionStatusColumnWidth,
                        72f),
                    "PRIMARY",
                    showMissionLabel: true,
                    options.PrimaryMissionComplete);
                DrawMissionStatusColumn(
                    new Rect(
                        dialogRect.x + dialogRect.width - 16f - MissionStatusColumnWidth,
                        dialogRect.y + 58f + options.ButtonVerticalOffset,
                        MissionStatusColumnWidth,
                        72f),
                    "SECONDARY",
                    showMissionLabel: true,
                    options.SecondaryMissionComplete);
            }

            if (DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "REFUEL",
                    options.RefuelEnabled,
                    out var refuelClicked)
                && refuelClicked)
            {
                return Result.Refuel;
            }

            buttonY += ButtonHeight + ButtonGap;

            if (DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "REARM",
                    options.RearmEnabled,
                    out var rearmClicked)
                && rearmClicked)
            {
                return Result.Rearm;
            }

            buttonY += ButtonHeight + ButtonGap;

            if (DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "DISMOUNT",
                    options.DismountEnabled,
                    out var dismountClicked)
                && dismountClicked)
            {
                return Result.Dismount;
            }

            buttonY += ButtonHeight + ButtonGap;

            // Default TakeOffEnabled to true when callers omit the field (struct default is false only
            // for newly constructed options that set it — Build* helpers always assign it).
            if (DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "TAKE OFF",
                    options.TakeOffEnabled,
                    out var takeOffClicked)
                && takeOffClicked)
            {
                return Result.TakeOff;
            }

            buttonY += ButtonHeight + ButtonGap;

            if (DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "END MISSION",
                    options.EndMissionEnabled,
                    out var endMissionClicked)
                && endMissionClicked)
            {
                return Result.EndMission;
            }

            return Result.None;
        }

        private static bool DrawMenuButton(Rect rect, string label, bool enabled, out bool clicked)
        {
            clicked = false;
            if (enabled)
            {
                clicked = StartPageMenuStyles.DrawMenuButton(rect, label, fontSize: ButtonFontSize);
                return true;
            }

            StartPageMenuStyles.DrawMenuButtonChrome(rect, panelAlpha: 0.45f);
            var mutedStyle = HudStyleFactory.CreateLabel(
                ButtonFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.35f, 0.35f, 0.35f),
                wordWrap: false);
            GUI.Label(rect, label, mutedStyle);
            return true;
        }

        private static void DrawMissionStatusColumn(
            Rect columnRect,
            string heading,
            bool showMissionLabel,
            bool complete)
        {
            var headingRect = new Rect(columnRect.x, columnRect.y, columnRect.width, 18f);
            GUI.Label(headingRect, heading, missionStatusHeadingStyle);

            var lightRect = new Rect(
                columnRect.x + (columnRect.width - MissionStatusLightSize) * 0.5f,
                columnRect.y + 22f,
                MissionStatusLightSize,
                MissionStatusLightSize);
            DrawMissionStatusLight(lightRect, complete);

            if (showMissionLabel)
            {
                var missionRect = new Rect(columnRect.x, columnRect.y + 44f, columnRect.width, 18f);
                GUI.Label(missionRect, "MISSION", missionStatusSubheadingStyle);
            }
        }

        private static void DrawMissionStatusLight(Rect rect, bool complete)
        {
            GUI.color = complete ? MissionCompleteLightColor : MissionIncompleteLightColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(rect, 1f);
            GUI.color = Color.white;
        }

        private static void EnsureStyles()
        {
            if (messageStyle != null)
            {
                return;
            }

            messageStyle = HudStyleFactory.CreateLabel(
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black,
                wordWrap: true);
            missionStatusHeadingStyle = HudStyleFactory.CreateLabel(
                12,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black,
                wordWrap: false);
            missionStatusSubheadingStyle = HudStyleFactory.CreateLabel(
                12,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black,
                wordWrap: false);
        }
    }
}
