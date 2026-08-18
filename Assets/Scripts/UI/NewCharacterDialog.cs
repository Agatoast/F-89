using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class NewCharacterDialog
    {
        public enum Result
        {
            None = 0,
            Accepted = 1,
            Back = 2
        }

        private const string NameFieldControl = "NewCharacterNameField";
        private const float DialogWidth = 560f;
        private const float DialogHeight = 220f;
        private const float ChoiceWidth = 120f;
        private const float ChoiceHeight = 38f;
        public const int MaxNameLength = CharacterNameLimits.MaxLength;

        private static GUIStyle titleStyle;
        private static GUIStyle labelStyle;
        private static GUIStyle fieldStyle;
        private static GUIStyle disabledButtonLabelStyle;
        private const int ButtonFontSize = 16;

        public static Result Draw(bool visible, ref string characterName, ref bool focusField)
        {
            if (!visible)
            {
                return Result.None;
            }

            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - DialogHeight) * 0.5f,
                DialogWidth,
                DialogHeight);

            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);

            GUI.Label(
                new Rect(dialogRect.x + 20f, dialogRect.y + 16f, dialogRect.width - 40f, 32f),
                "Character Name",
                titleStyle);

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 58f, dialogRect.width - 48f, 24f),
                "Enter character name:",
                labelStyle);

            var fieldRect = new Rect(dialogRect.x + 16f, dialogRect.y + 88f, dialogRect.width - 32f, 36f);
            GUI.color = new Color(0f, 0f, 0f, 0.08f);
            GUI.DrawTexture(fieldRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(fieldRect, 1f);

            GUI.SetNextControlName(NameFieldControl);
            var previousCursorColor = GUI.skin.settings.cursorColor;
            var previousSelectionColor = GUI.skin.settings.selectionColor;
            GUI.skin.settings.cursorColor = Color.black;
            GUI.skin.settings.selectionColor = new Color(0.55f, 0.7f, 0.95f, 0.55f);
            characterName = GUI.TextField(fieldRect, characterName ?? string.Empty, fieldStyle);
            GUI.skin.settings.cursorColor = previousCursorColor;
            GUI.skin.settings.selectionColor = previousSelectionColor;
            if (characterName != null && characterName.Length > MaxNameLength)
            {
                characterName = characterName.Substring(0, MaxNameLength);
            }

            if (focusField)
            {
                GUI.FocusControl(NameFieldControl);
                focusField = false;
            }

            var choiceY = dialogRect.yMax - ChoiceHeight - 24f;
            var acceptRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f - ChoiceWidth - 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);
            var backRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f + 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);

            var canAccept = !string.IsNullOrWhiteSpace(characterName);
            if (canAccept)
            {
                if (StartPageMenuStyles.DrawMenuButton(acceptRect, "ACCEPT", fontSize: ButtonFontSize))
                {
                    return Result.Accepted;
                }
            }
            else
            {
                DrawDisabledMenuButton(acceptRect, "ACCEPT");
            }

            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: ButtonFontSize))
            {
                return Result.Back;
            }

            return Result.None;
        }

        private static void DrawDisabledMenuButton(Rect rect, string label)
        {
            StartPageMenuStyles.DrawMenuButtonChrome(rect, panelAlpha: 0.45f);
            GUI.Label(rect, label, disabledButtonLabelStyle);
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            labelStyle = HudStyleFactory.CreateLabel(16, FontStyle.Normal, TextAnchor.UpperLeft, Color.black);
            fieldStyle = HudStyleFactory.CreateLabel(16, FontStyle.Normal, TextAnchor.MiddleLeft, Color.black);
            fieldStyle.padding = new RectOffset(6, 6, 8, 8);
            fieldStyle.clipping = TextClipping.Overflow;
            fieldStyle.wordWrap = false;
            disabledButtonLabelStyle = HudStyleFactory.CreateLabel(
                ButtonFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.55f, 0.58f, 0.62f));
        }
    }
}
