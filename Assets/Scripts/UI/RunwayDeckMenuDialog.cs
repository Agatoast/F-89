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
            public bool EndMissionEnabled;
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

        private static GUIStyle messageStyle;

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

            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "TAKE OFF",
                    fontSize: ButtonFontSize))
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
        }
    }
}
