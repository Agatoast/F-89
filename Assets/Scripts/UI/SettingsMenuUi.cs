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

        private const float ButtonWidthDesign = 280f;
        private const float ButtonHeightDesign = 44f;
        private const float ButtonSpacingDesign = 10f;
        private const float KeymapRowHeightDesign = 40f;
        private const float VolumeRowHeightDesign = 36f;
        private const float VolumeStackWidthDesign = 520f;
        private const int RootButtonFontSize = 22;

        private static GUIStyle titleStyle;
        private static GUIStyle rowLabelStyle;
        private static GUIStyle rowKeyStyle;
        private static GUIStyle promptStyle;
        private static GUIStyle volumeLabelStyle;
        private static GUIStyle volumeValueStyle;
        private static Vector2 keymapScrollPosition;

        public static View Draw(
            View view,
            System.Action onExitSettings,
            System.Action onMainMenu = null,
            System.Action onExitGame = null)
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

            return DrawRootMenu(onExitSettings, onMainMenu, onExitGame);
        }

        private static void DrawDarkBackground()
        {
            UiFitCanvas.Begin(16f / 9f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(UiFitCanvas.Rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static View DrawRootMenu(
            System.Action onExitSettings,
            System.Action onMainMenu,
            System.Action onExitGame)
        {
            DrawTitle("SETTINGS");

            var buttonWidth = UiFitCanvas.Px(ButtonWidthDesign);
            var buttonHeight = UiFitCanvas.Px(ButtonHeightDesign);
            var buttonSpacing = UiFitCanvas.Px(ButtonSpacingDesign);
            var buttonX = UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f;
            var backRect = GetBackButtonRect();

            var volumeBottom = DrawVolumeSliders();

            const int buttonCount = 5;
            var stackHeight = buttonCount * buttonHeight + (buttonCount - 1) * buttonSpacing;
            var availableTop = volumeBottom + UiFitCanvas.Px(18f);
            var availableBottom = backRect.y - UiFitCanvas.Px(16f);
            var availableHeight = Mathf.Max(stackHeight, availableBottom - availableTop);
            var buttonY = availableTop + (availableHeight - stackHeight) * 0.5f;
            buttonY = Mathf.Max(buttonY, availableTop);

            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "MAIN MENU",
                    fontSize: RootButtonFontSize))
            {
                if (onMainMenu != null)
                {
                    onMainMenu.Invoke();
                }
                else
                {
                    onExitSettings?.Invoke();
                }
            }

            buttonY += buttonHeight + buttonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    GameSettings.MissileSoundsLabel.ToUpperInvariant(),
                    fontSize: RootButtonFontSize))
            {
                GameSettings.ToggleMissileSounds();
            }

            buttonY += buttonHeight + buttonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    GameSettings.DisplayLabel.ToUpperInvariant(),
                    fontSize: RootButtonFontSize))
            {
                GameSettings.ToggleFullscreen();
            }

            buttonY += buttonHeight + buttonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "KEYMAP",
                    fontSize: RootButtonFontSize))
            {
                return View.Keymap;
            }

            buttonY += buttonHeight + buttonSpacing;
            if (StartPageMenuStyles.DrawMenuButton(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "EXIT GAME",
                    fontSize: RootButtonFontSize))
            {
                if (onExitGame != null)
                {
                    onExitGame.Invoke();
                }
                else
                {
                    QuitApplication();
                }
            }

            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: 28))
            {
                onExitSettings?.Invoke();
            }

            return View.Root;
        }

        private static float DrawVolumeSliders()
        {
            var stackWidth = UiFitCanvas.Px(VolumeStackWidthDesign);
            var rowHeight = UiFitCanvas.Px(VolumeRowHeightDesign);
            var rowGap = UiFitCanvas.Px(8f);
            var top = UiFitCanvas.NormY(0.11f);
            var x = UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - stackWidth) * 0.5f;

            DrawVolumeRow(
                new Rect(x, top, stackWidth, rowHeight),
                "SOUND",
                GameSettings.SoundVolumePercent,
                GameSettings.SetSoundVolumePercent);
            top += rowHeight + rowGap;

            DrawVolumeRow(
                new Rect(x, top, stackWidth, rowHeight),
                "MUSIC",
                GameSettings.MusicVolumePercent,
                GameSettings.SetMusicVolumePercent);
            top += rowHeight + rowGap;

            DrawVolumeRow(
                new Rect(x, top, stackWidth, rowHeight),
                "SOUND FX",
                GameSettings.SfxVolumePercent,
                GameSettings.SetSfxVolumePercent);
            top += rowHeight;

            return top;
        }

        private static void DrawVolumeRow(Rect row, string label, int percent, System.Action<int> setPercent)
        {
            var labelWidth = UiFitCanvas.Px(120f);
            var valueWidth = UiFitCanvas.Px(64f);
            var gap = UiFitCanvas.Px(12f);
            var labelRect = new Rect(row.x, row.y, labelWidth, row.height);
            var valueRect = new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height);
            var sliderRect = new Rect(
                labelRect.xMax + gap,
                row.y + row.height * 0.28f,
                valueRect.x - labelRect.xMax - gap * 2f,
                row.height * 0.44f);

            GUI.Label(labelRect, label, volumeLabelStyle);
            GUI.Label(valueRect, $"{percent}%", volumeValueStyle);

            var previous = GUI.color;
            GUI.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            GUI.DrawTexture(sliderRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.42f, 0.72f, 1f, 0.95f);
            var fillWidth = sliderRect.width * (percent / 100f);
            if (fillWidth > 0f)
            {
                GUI.DrawTexture(
                    new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height),
                    Texture2D.whiteTexture);
            }

            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(sliderRect, 1f);
            GUI.color = previous;

            var evt = Event.current;
            if (evt != null
                && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                && evt.button == 0
                && sliderRect.Contains(evt.mousePosition))
            {
                var t = (evt.mousePosition.x - sliderRect.x) / Mathf.Max(1f, sliderRect.width);
                var newPercent = Mathf.RoundToInt(Mathf.Clamp01(t) * 100f);
                if (newPercent != percent)
                {
                    setPercent(newPercent);
                }

                evt.Use();
            }
        }

        private static bool DrawKeymapMenu()
        {
            DrawTitle("KEYMAP");

            var buttonWidth = UiFitCanvas.Px(ButtonWidthDesign);
            var buttonHeight = UiFitCanvas.Px(ButtonHeightDesign);
            var buttonSpacing = UiFitCanvas.Px(ButtonSpacingDesign);
            var rowHeight = UiFitCanvas.Px(KeymapRowHeightDesign);
            var backRect = GetBackButtonRect();
            var defaultsRect = new Rect(
                backRect.x,
                backRect.y - buttonSpacing - buttonHeight,
                backRect.width,
                backRect.height);

            var listTop = UiFitCanvas.NormY(0.14f);
            var listRect = new Rect(
                UiFitCanvas.NormX(0.18f),
                listTop,
                UiFitCanvas.Rect.width * 0.64f,
                defaultsRect.y - listTop - buttonSpacing);
            var contentHeight = GameKeyBindingCatalog.Bindings.Length * rowHeight;
            var viewRect = new Rect(0f, 0f, listRect.width - 18f, contentHeight);

            GUI.BeginGroup(listRect);
            keymapScrollPosition = GUI.BeginScrollView(
                new Rect(0f, 0f, listRect.width, listRect.height),
                keymapScrollPosition,
                viewRect);

            for (var i = 0; i < GameKeyBindingCatalog.Bindings.Length; i++)
            {
                var binding = GameKeyBindingCatalog.Bindings[i];
                var rowRect = new Rect(0f, i * rowHeight, viewRect.width, rowHeight - 6f);
                DrawKeymapRow(rowRect, binding);
            }

            GUI.EndScrollView();
            GUI.EndGroup();

            if (GameKeyBindings.IsListening)
            {
                GUI.Label(
                    new Rect(listRect.x, defaultsRect.y - UiFitCanvas.Px(30f), listRect.width, UiFitCanvas.Px(24f)),
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
            GUI.Label(
                new Rect(UiFitCanvas.Rect.x, UiFitCanvas.NormY(0.04f), UiFitCanvas.Rect.width, UiFitCanvas.Px(44f)),
                title,
                titleStyle);
        }

        private static Rect GetBackButtonRect()
        {
            var buttonWidth = UiFitCanvas.Px(ButtonWidthDesign);
            var buttonHeight = UiFitCanvas.Px(ButtonHeightDesign);
            return new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - UiFitCanvas.Px(40f),
                buttonWidth,
                buttonHeight);
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            volumeLabelStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.82f, 0.88f, 0.94f));
            volumeValueStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                new Color(0.82f, 0.88f, 0.94f));
        }
    }
}
