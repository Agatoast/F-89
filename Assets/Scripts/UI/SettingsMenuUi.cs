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

        private const float DialogWidth = 420f;
        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 40f;
        private const float ButtonSpacing = 10f;
        private const float KeymapRowHeight = 34f;

        private static GUIStyle titleStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle rowLabelStyle;
        private static GUIStyle rowKeyStyle;
        private static GUIStyle promptStyle;
        private static Vector2 keymapScrollPosition;

        public static View Draw(View view, System.Action onExitSettings)
        {
            EnsureStyles();

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

        private static View DrawRootMenu(System.Action onExitSettings)
        {
            var buttonCount = 3;
            var dialogRect = GetDialogRect(GetDialogHeight(buttonCount));
            DrawDialogFrame(dialogRect, "Settings");

            var buttonX = dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f;
            var buttonY = dialogRect.y + 64f;

            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "Keymap"))
            {
                return View.Keymap;
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), GameSettings.DisplayLabel))
            {
                GameSettings.ToggleFullscreen();
            }

            buttonY += ButtonHeight + ButtonSpacing;
            if (DrawMenuButton(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "Back"))
            {
                onExitSettings?.Invoke();
            }

            return View.Root;
        }

        private static bool DrawKeymapMenu()
        {
            var dialogHeight = Mathf.Min(Screen.height - 80f, 560f);
            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                DialogWidth,
                dialogHeight);
            DrawDialogFrame(dialogRect, "Keymap");

            var listRect = new Rect(dialogRect.x + 16f, dialogRect.y + 58f, dialogRect.width - 32f, dialogHeight - 148f);
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
                var rowRect = new Rect(0f, i * KeymapRowHeight, viewRect.width, KeymapRowHeight - 4f);
                DrawKeymapRow(rowRect, binding);
            }

            GUI.EndScrollView();
            GUI.EndGroup();

            if (GameKeyBindings.IsListening)
            {
                GUI.Label(
                    new Rect(dialogRect.x + 16f, dialogRect.yMax - 92f, dialogRect.width - 32f, 24f),
                    "Press a key or mouse button. Escape cancels.",
                    promptStyle);
            }

            var buttonX = dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f;
            var defaultsRect = new Rect(buttonX, dialogRect.yMax - 58f, ButtonWidth, ButtonHeight);
            if (DrawMenuButton(defaultsRect, "Restore Defaults"))
            {
                GameKeyBindings.ResetToDefaults();
                GameKeyBindings.CancelListening();
            }

            var backRect = new Rect(buttonX, dialogRect.yMax - ButtonHeight - 16f, ButtonWidth, ButtonHeight);
            if (DrawMenuButton(backRect, "Back"))
            {
                GameKeyBindings.CancelListening();
                return true;
            }

            return false;
        }

        private static void DrawKeymapRow(Rect rowRect, GameKeyBindingDefinition binding)
        {
            HudGuiUtility.DrawWireBox(rowRect, 1f);

            var labelRect = new Rect(rowRect.x + 10f, rowRect.y, rowRect.width * 0.58f, rowRect.height);
            var keyRect = new Rect(rowRect.x + rowRect.width * 0.58f, rowRect.y, rowRect.width * 0.42f - 8f, rowRect.height);

            var listening = GameKeyBindings.ListeningBindingId == binding.Id;
            GUI.Label(labelRect, binding.Label, rowLabelStyle);
            GUI.Label(
                keyRect,
                listening ? "Press key..." : GameKeyBindings.GetDisplayLabel(binding.Id),
                rowKeyStyle);

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                GameKeyBindings.BeginListening(binding.Id);
            }
        }

        private static void DrawDialogFrame(Rect dialogRect, string title)
        {
            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);
            GUI.Label(new Rect(dialogRect.x + 16f, dialogRect.y + 18f, dialogRect.width - 32f, 32f), title, titleStyle);
        }

        private static Rect GetDialogRect(float dialogHeight)
        {
            return new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                DialogWidth,
                dialogHeight);
        }

        private static float GetDialogHeight(int buttonCount)
        {
            return 64f + buttonCount * ButtonHeight + Mathf.Max(0, buttonCount - 1) * ButtonSpacing + 28f;
        }

        private static bool DrawMenuButton(Rect rect, string label)
        {
            HudGuiUtility.DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            buttonStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            rowLabelStyle = HudStyleFactory.CreateLabel(15, FontStyle.Normal, TextAnchor.MiddleLeft, Color.black);
            rowKeyStyle = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleRight, Color.black);
            promptStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black);
        }
    }
}
