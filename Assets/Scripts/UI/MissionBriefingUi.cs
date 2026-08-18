using F89.Core;
using UnityEngine;

namespace F89.UI
{
    /// <summary>Shared TOP SECRET mission brief layout for pre-mission scene and in-flight review.</summary>
    public static class MissionBriefingUi
    {
        public enum FooterMode
        {
            PreMissionAcceptBailOut,
            InFlightContinueOnly
        }

        private const float MarginDesign = 40f;
        private const float BorderSidePaddingDesign = 10f;
        private const int SecurityBorderXCount = 80;
        private const string TopSecretStampResourcePath = "MissionBriefing/top_secret_stamp";
        private const float TopSecretStampWidthDesign = 285f;
        private const string BurnBagStampResourcePath = "MissionBriefing/burn_bag_only_stamp";
        private const float BurnBagStampWidthDesign = 260f;
        private const float BurnBagStampGapAboveAcceptDesign = 10f;

        private static GUIStyle centerStyle;
        private static GUIStyle securityLineStyle;
        private static GUIStyle securityBorderStyle;
        private static GUIStyle leftBodyStyle;
        private static GUIStyle missionObjectiveStyle;
        private static Texture2D topSecretStampTexture;
        private static Texture2D burnBagStampTexture;

        public static void Draw(
            FooterMode footerMode,
            ref bool showBailOutConfirm,
            System.Action onAccept,
            System.Action onConfirmBailOut,
            System.Action onContinue)
        {
            EnsureStyles();
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            var subjectName = save != null ? save.DisplayRankAndName : "OPERATOR";
            var paper = GetPaperRect();
            var borderPad = UiFitCanvas.Px(BorderSidePaddingDesign);
            var margin = UiFitCanvas.Px(MarginDesign);
            var contentX = paper.x + borderPad;
            var innerWidth = paper.width - borderPad * 2f;
            var y = paper.y + margin;

            y = DrawHeader(contentX, y, innerWidth);
            y = DrawMissionMetadata(contentX, y, subjectName, innerWidth);
            y = DrawSecurityBlock(contentX, y, innerWidth);
            var bodyBottomLimit = footerMode == FooterMode.InFlightContinueOnly
                ? GetInFlightFooterTop(paper) - UiFitCanvas.Px(12f)
                : paper.yMax;
            DrawMissionBody(contentX, y, innerWidth, bodyBottomLimit);
            DrawTopSecretStamp(paper);

            if (footerMode == FooterMode.InFlightContinueOnly)
            {
                DrawContinueButton(paper, onContinue);
            }
            else
            {
                DrawPreMissionButtons(paper, onAccept, ref showBailOutConfirm);
            }

            if (footerMode == FooterMode.PreMissionAcceptBailOut && showBailOutConfirm)
            {
                var dialogResult = BailOutConfirmDialog.Draw(true);
                if (dialogResult == BailOutConfirmDialog.Result.Confirmed)
                {
                    onConfirmBailOut?.Invoke();
                }
                else if (dialogResult == BailOutConfirmDialog.Result.Cancelled)
                {
                    showBailOutConfirm = false;
                }
            }
        }

        private static Rect GetPaperRect()
        {
            var borderPad = UiFitCanvas.Px(BorderSidePaddingDesign);
            var measuredInner = securityBorderStyle != null
                ? securityBorderStyle.CalcSize(new GUIContent(new string('X', SecurityBorderXCount))).x
                : UiFitCanvas.Px(720f);
            var paperWidth = Mathf.Min(
                UiFitCanvas.Rect.width * 0.92f,
                measuredInner + borderPad * 2f);
            return new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - paperWidth) * 0.5f,
                UiFitCanvas.Rect.y,
                paperWidth,
                UiFitCanvas.Rect.height);
        }

        private static void DrawTopSecretStamp(Rect paper)
        {
            if (topSecretStampTexture == null)
            {
                topSecretStampTexture = Resources.Load<Texture2D>(TopSecretStampResourcePath);
                if (topSecretStampTexture == null)
                {
                    return;
                }
            }

            var aspect = topSecretStampTexture.height > 0
                ? (float)topSecretStampTexture.height / topSecretStampTexture.width
                : 1f;
            var width = UiFitCanvas.Px(TopSecretStampWidthDesign);
            var height = width * aspect;
            GUI.DrawTexture(
                new Rect(paper.xMax - width, paper.y, width, height),
                topSecretStampTexture,
                ScaleMode.StretchToFill,
                true);
        }

        private static float DrawHeader(float contentX, float y, float innerWidth)
        {
            var rect = new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f));
            GUI.Label(rect, "JOINT SPECIAL OPERATIONS COMMAND", centerStyle);
            y += UiFitCanvas.Px(26f);
            GUI.Label(new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)), "DEPARTMENT OF WAR", centerStyle);
            y += UiFitCanvas.Px(26f);
            GUI.Label(new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)), "WASHINGTON, DC 20301", centerStyle);
            return y + UiFitCanvas.Px(36f);
        }

        private static float DrawMissionMetadata(float contentX, float y, string subjectName, float innerWidth)
        {
            GUI.Label(new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)), $"Operation: {MissionBriefingState.OperationName}", leftBodyStyle);
            y += UiFitCanvas.Px(28f);
            GUI.Label(
                new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)),
                $"Subject: {subjectName} CURRENT ASSIGNMENT",
                leftBodyStyle);
            return y + UiFitCanvas.Px(40f);
        }

        private static float DrawSecurityBlock(float contentX, float y, float innerWidth)
        {
            var borderLineHeight = UiFitCanvas.Px(18f);
            var securityLines = new[]
            {
                "TS: SAP CLEARANCE REQUIRED",
                "PROCESSING THESE ORDERS REQUIRES THE USER TO FOLLOW ALL REGULATIONS SET FORTH",
                "IN 50 W.S.C. § 3341 (SECURITY CLEARANCES). IF YOU ARE READING THIS AND YOU ARE NOT",
                "AUTHORIZED YOU MUST STOP AND DISPOSE OF THIS DOCUMENT AS SET FORTH IN",
                "5 CFR § 1312.29 and 36 CFR § 1226.24",
                "FOR OFFICIAL USE ONLY"
            };

            DrawSecurityBorder(contentX, y, innerWidth, borderLineHeight);
            y += borderLineHeight;

            foreach (var line in securityLines)
            {
                var lineHeight = securityLineStyle.CalcHeight(new GUIContent(line), innerWidth);
                GUI.Label(new Rect(contentX, y, innerWidth, lineHeight), line, securityLineStyle);
                y += lineHeight;
            }

            DrawSecurityBorder(contentX, y, innerWidth, borderLineHeight);
            return y + borderLineHeight + UiFitCanvas.Px(28f);
        }

        private static void DrawSecurityBorder(float contentX, float y, float width, float height)
        {
            var border = new string('X', SecurityBorderXCount);
            GUI.Label(new Rect(contentX, y, width, height), border, securityBorderStyle);
        }

        private static void DrawMissionBody(float contentX, float y, float innerWidth, float maxBottomY)
        {
            GUI.Label(new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)), "PART ONE: REMEMBER TO READ YOUR ORDERS COMPLETELY!", leftBodyStyle);
            y += UiFitCanvas.Px(34f);
            GUI.Label(
                new Rect(contentX, y, innerWidth, UiFitCanvas.Px(40f)),
                "MISSION OBJECTIVE:",
                missionObjectiveStyle);
            y += UiFitCanvas.Px(44f);

            var remainingHeight = Mathf.Max(UiFitCanvas.Px(48f), maxBottomY - y);
            var objectiveRect = new Rect(contentX, y, innerWidth, remainingHeight);
            GUI.Label(objectiveRect, MissionBriefingState.MissionObjective, missionObjectiveStyle);
            y += remainingHeight + UiFitCanvas.Px(24f);

            if (y <= maxBottomY - UiFitCanvas.Px(24f))
            {
                GUI.Label(
                    new Rect(contentX, y, innerWidth, UiFitCanvas.Px(24f)),
                    "PART TWO: MISSION DEBRIEF TO BE CONDUCTED UPON OPERATOR RETURN.",
                    leftBodyStyle);
            }
        }

        private static float GetInFlightFooterTop(Rect paper)
        {
            StartPageMenuStyles.GetMenuButtonSize(out _, out var buttonHeight);
            return paper.yMax - buttonHeight - UiFitCanvas.Px(MarginDesign);
        }

        private static void DrawPreMissionButtons(Rect paper, System.Action onAccept, ref bool showBailOutConfirm)
        {
            var buttonWidth = UiFitCanvas.Px(220f);
            StartPageMenuStyles.GetMenuButtonSize(out _, out var buttonHeight);
            var buttonGap = UiFitCanvas.Px(12f);
            var totalHeight = buttonHeight * 2f + buttonGap;
            var startX = paper.x + (paper.width - buttonWidth) * 0.5f;
            var startY = paper.yMax - totalHeight - UiFitCanvas.Px(MarginDesign);

            var acceptRect = new Rect(startX, startY, buttonWidth, buttonHeight);
            var bailRect = new Rect(startX, startY + buttonHeight + buttonGap, buttonWidth, buttonHeight);

            DrawBurnBagStampAbove(acceptRect);

            if (StartPageMenuStyles.DrawMenuButton(acceptRect, "ACCEPT") && !showBailOutConfirm)
            {
                onAccept?.Invoke();
            }

            if (StartPageMenuStyles.DrawMenuButton(bailRect, "BAIL OUT?"))
            {
                showBailOutConfirm = true;
            }
        }

        private static void DrawContinueButton(Rect paper, System.Action onContinue)
        {
            var buttonWidth = UiFitCanvas.Px(220f);
            StartPageMenuStyles.GetMenuButtonSize(out _, out var buttonHeight);
            var startX = paper.x + (paper.width - buttonWidth) * 0.5f;
            var startY = GetInFlightFooterTop(paper);
            var continueRect = new Rect(startX, startY, buttonWidth, buttonHeight);

            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 16))
            {
                onContinue?.Invoke();
            }

            if (Event.current != null
                && Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return
                    || Event.current.keyCode == KeyCode.KeypadEnter
                    || Event.current.keyCode == KeyCode.Space))
            {
                Event.current.Use();
                onContinue?.Invoke();
            }
        }

        private static void DrawBurnBagStampAbove(Rect acceptRect)
        {
            if (burnBagStampTexture == null)
            {
                burnBagStampTexture = Resources.Load<Texture2D>(BurnBagStampResourcePath);
                if (burnBagStampTexture == null)
                {
                    return;
                }
            }

            var aspect = burnBagStampTexture.height > 0
                ? (float)burnBagStampTexture.height / burnBagStampTexture.width
                : 0.3f;
            var width = UiFitCanvas.Px(BurnBagStampWidthDesign);
            var height = width * aspect;
            var x = acceptRect.center.x - width * 0.5f;
            var y = acceptRect.y - UiFitCanvas.Px(BurnBagStampGapAboveAcceptDesign) - height;
            GUI.DrawTexture(
                new Rect(x, y, width, height),
                burnBagStampTexture,
                ScaleMode.StretchToFill,
                true);
        }

        private static void DrawPageBackground()
        {
            UiFitCanvas.Begin(16f / 9f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(GetPaperRect(), Texture2D.whiteTexture);
        }

        private static void EnsureStyles()
        {
            if (centerStyle != null && missionObjectiveStyle != null)
            {
                return;
            }

            centerStyle = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            securityBorderStyle = HudStyleFactory.CreateLabel(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            securityLineStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            leftBodyStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.black, wordWrap: true);
            missionObjectiveStyle = HudStyleFactory.CreateLabel(28, FontStyle.Normal, TextAnchor.UpperLeft, Color.black, wordWrap: true);

            centerStyle.clipping = TextClipping.Clip;
            securityBorderStyle.clipping = TextClipping.Clip;
            securityLineStyle.clipping = TextClipping.Clip;
            leftBodyStyle.clipping = TextClipping.Clip;
            missionObjectiveStyle.clipping = TextClipping.Clip;
        }
    }
}
