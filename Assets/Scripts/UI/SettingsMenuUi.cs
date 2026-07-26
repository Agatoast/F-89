using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class SettingsMenuUi
    {
        public enum View
        {
            Root = 0,
            Keymap = 1
        }

        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 48f;
        private const float ButtonSpacing = 14f;
        private const float KeymapRowHeight = 40f;

        private static GUIStyle titleStyle;
        private static GUIStyle rowLabelStyle;
        private static GUIStyle rowKeyStyle;
        private static GUIStyle promptStyle;
        private static Vector2 keymapScrollPosition;

        public static View Draw(View view, System.Action onExitSettings)
        {
            EnsureStyles();
            DrawDarkBackground();

            if (GameKeyBindings.TryHandleListenEvent(Event.current))
            {
                return view;
            }

            if (view == View.Keymap)
            {
                return DrawKeymapMenu() ? View.Root : View.Keymap;
            }

            return DrawRootMenu(onExitSettings);
        }

        private static void DrawDarkBackground()
        {
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static View DrawRootMenu(System.Action onExitSettings)
        {
            DrawTitle("SETTINGS");

            var buttonX = (Screen.width - ButtonWidth) * 0.5f;
            var buttonY = Screen.height * 0.34f;

            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    "KEYMAP",
                    fontSize: 24))
            {
                return View.Keymap;
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight),
                    GameSettings.DisplayLabel.ToUpperInvariant(),
                    fontSize: 24))
            {
                GameSettings.ToggleFullscreen();
            }

            var backRect = GetBackButtonRect();
            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: 28))
            {
                onExitSettings?.Invoke();
            }

            return View.Root;
        }

        private static bool DrawKeymapMenu()
        {
            DrawTitle("KEYMAP");

            var backRect = GetBackButtonRect();
            var defaultsRect = new Rect(
                backRect.x,
                backRect.y - ButtonSpacing - ButtonHeight,
                backRect.width,
                backRect.height);

            var listTop = Screen.height * 0.14f;
            var listRect = new Rect(
                Screen.width * 0.18f,
                listTop,
                Screen.width * 0.64f,
                defaultsRect.y - listTop - ButtonSpacing);
            var contentHeight = GameKeyBindingCatalog.Bindings.Length * KeymapRowHeight;
            var viewRect = new Rect(0f, 0f, listRect.width - 18f, contentHeight);

            GUI.BeginGroup(listRect);
            keymapScrollPosition = GUI.BeginScrollView(
                new Rect(0f, 0f, listRect.width, listRect.height),
                keymapScrollPosition,
                viewRect);

            for (var i = 0; i < GameKeyBindingCatalog.Bindings.Length; i++)
            {
                var binding = GameKeyBindingCatalog.Bindings[i];
                var rowRect = new Rect(0f, i * KeymapRowHeight, viewRect.width, KeymapRowHeight - 6f);
                DrawKeymapRow(rowRect, binding);
            }

            GUI.EndScrollView();
            GUI.EndGroup();

            if (GameKeyBindings.IsListening)
            {
                GUI.Label(
                    new Rect(listRect.x, defaultsRect.y - 30f, listRect.width, 24f),
                    "Press a key or mouse button. Escape cancels.",
                    promptStyle);
            }

            if (StartPageMenuStyles.DrawMenuButton(defaultsRect, "RESTORE DEFAULTS", fontSize: 22))
            {
                GameKeyBindings.ResetToDefaults();
                GameKeyBindings.CancelListening();
            }

            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: 28))
            {
                GameKeyBindings.CancelListening();
                return true;
            }

            return false;
        }

        private static void DrawKeymapRow(Rect rowRect, GameKeyBindingDefinition binding)
        {
            StartPageMenuStyles.DrawMenuButtonChrome(rowRect, panelAlpha: 0.88f);

            var labelRect = new Rect(rowRect.x + 14f, rowRect.y, rowRect.width * 0.58f, rowRect.height);
            var keyRect = new Rect(
                rowRect.x + rowRect.width * 0.58f,
                rowRect.y,
                rowRect.width * 0.42f - 14f,
                rowRect.height);

            var listening = GameKeyBindings.ListeningBindingId == binding.Id;
            GUI.Label(labelRect, binding.Label.ToUpperInvariant(), rowLabelStyle);
            GUI.Label(
                keyRect,
                listening ? "PRESS KEY..." : GameKeyBindings.GetDisplayLabel(binding.Id).ToUpperInvariant(),
                rowKeyStyle);

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                GameKeyBindings.BeginListening(binding.Id);
            }
        }

        private static void DrawTitle(string title)
        {
            titleStyle.normal.textColor = new Color(0.78f, 0.86f, 0.95f);
            GUI.Label(new Rect(0f, Screen.height * 0.06f, Screen.width, 48f), title, titleStyle);
        }

        private static Rect GetBackButtonRect()
        {
            return new Rect(
                (Screen.width - ButtonWidth) * 0.5f,
                Screen.height - ButtonHeight - Screen.height * 0.05f,
                ButtonWidth,
                ButtonHeight);
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            rowLabelStyle = HudStyleFactory.CreateLabel(
                16,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.82f, 0.88f, 0.94f));
            rowKeyStyle = HudStyleFactory.CreateLabel(
                16,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                new Color(0.82f, 0.88f, 0.94f));
            promptStyle = HudStyleFactory.CreateLabel(
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 0.86f, 0.95f));
        }
    }
}
