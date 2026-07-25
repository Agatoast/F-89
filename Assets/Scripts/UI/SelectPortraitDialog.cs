using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class SelectPortraitDialog
    {
        public enum Result
        {
            None = 0,
            Selected = 1,
            Browse = 2,
            Cancel = 3
        }

        private const float DialogWidth = 620f;
        private const float DialogHeight = 430f;
        private const float OptionSize = 88f;
        private const float OptionSpacing = 16f;
        private const float ChoiceWidth = 140f;
        private const float ChoiceHeight = 38f;

        private static GUIStyle titleStyle;
        private static GUIStyle sectionStyle;
        private static GUIStyle buttonStyle;

        public static Result Draw(bool visible, out string selectedPortraitId)
        {
            selectedPortraitId = null;
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
                "Select Portrait",
                titleStyle);

            var rowWidth = OptionSize * 3f + OptionSpacing * 2f;
            var rowX = dialogRect.x + (dialogRect.width - rowWidth) * 0.5f;
            var maleY = dialogRect.y + 58f;
            var femaleY = maleY + OptionSize + 34f;

            GUI.Label(new Rect(rowX, maleY - 22f, rowWidth, 20f), "Male", sectionStyle);
            GUI.Label(new Rect(rowX, femaleY - 22f, rowWidth, 20f), "Female", sectionStyle);

            for (var i = 0; i < CharacterPortraitIds.MalePresets.Length; i++)
            {
                var optionRect = new Rect(
                    rowX + i * (OptionSize + OptionSpacing),
                    maleY,
                    OptionSize,
                    OptionSize);
                if (DrawPortraitOption(optionRect, CharacterPortraitIds.MalePresets[i]))
                {
                    selectedPortraitId = CharacterPortraitIds.MalePresets[i];
                    return Result.Selected;
                }
            }

            for (var i = 0; i < CharacterPortraitIds.FemalePresets.Length; i++)
            {
                var optionRect = new Rect(
                    rowX + i * (OptionSize + OptionSpacing),
                    femaleY,
                    OptionSize,
                    OptionSize);
                if (DrawPortraitOption(optionRect, CharacterPortraitIds.FemalePresets[i]))
                {
                    selectedPortraitId = CharacterPortraitIds.FemalePresets[i];
                    return Result.Selected;
                }
            }

            var choiceY = dialogRect.yMax - ChoiceHeight - 24f;
            var browseRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f - ChoiceWidth - 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);
            var cancelRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f + 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);

            DrawChoiceButton(browseRect, "Browse...");
            DrawChoiceButton(cancelRect, "Cancel");

            if (GUI.Button(browseRect, GUIContent.none, GUIStyle.none))
            {
                return Result.Browse;
            }

            if (GUI.Button(cancelRect, GUIContent.none, GUIStyle.none))
            {
                return Result.Cancel;
            }

            return Result.None;
        }

        private static bool DrawPortraitOption(Rect rect, string portraitId)
        {
            HudGuiUtility.DrawWireBox(rect, 2f);

            var texture = CharacterPortraitService.GetPresetPortraitTexture(portraitId);
            var innerRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
            PortraitDisplayUtility.DrawPortraitFrame(innerRect, texture, null, null);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void DrawChoiceButton(Rect rect, string label)
        {
            HudGuiUtility.DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            sectionStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.UpperLeft, Color.black);
            buttonStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
        }
    }
}
