using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MissionBriefingController : MonoBehaviour
    {
        private const float Margin = 40f;
        private const float BorderSidePaddingPx = 10f;
        private const int SecurityBorderXCount = 80;
        private const string TopSecretStampResourcePath = "MissionBriefing/top_secret_stamp";
        private const float TopSecretStampWidthPx = 285f;
        private const string BurnBagStampResourcePath = "MissionBriefing/burn_bag_only_stamp";
        private const float BurnBagStampWidthPx = 260f;
        private const float BurnBagStampGapAboveAcceptPx = 10f;

        private GUIStyle centerStyle;
        private GUIStyle securityLineStyle;
        private GUIStyle securityBorderStyle;
        private GUIStyle leftBodyStyle;

        private float contentWidth;
        private float innerContentWidth;

        private bool showBailOutConfirm;
        private Texture2D topSecretStampTexture;
        private Texture2D burnBagStampTexture;

        private void Awake()
        {
            topSecretStampTexture = Resources.Load<Texture2D>(TopSecretStampResourcePath);
            if (topSecretStampTexture == null)
            {
                Debug.LogWarning("F-89: TOP SECRET stamp missing from Resources/MissionBriefing/top_secret_stamp.");
            }

            burnBagStampTexture = Resources.Load<Texture2D>(BurnBagStampResourcePath);
            if (burnBagStampTexture == null)
            {
                Debug.LogWarning("F-89: BURN BAG ONLY stamp missing from Resources/MissionBriefing/burn_bag_only_stamp.");
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            var subjectName = save != null ? save.DisplayRankAndName : "OPERATOR";
            var contentX = (Screen.width - contentWidth) * 0.5f;
            var innerX = contentX + BorderSidePaddingPx;
            var y = Margin;

            y = DrawHeader(innerX, y);
            y = DrawMissionMetadata(innerX, y, subjectName);
            y = DrawSecurityBlock(innerX, y);
            y = DrawMissionBody(innerX, y);
            DrawTopSecretStamp(contentX);
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
        }

        private void DrawTopSecretStamp(float pageLeftX)
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
            var width = TopSecretStampWidthPx;
            var height = width * aspect;
            // Pin stamp top to page top and right edge to page right border.
            GUI.DrawTexture(
                new Rect(pageLeftX + contentWidth - width, 0f, width, height),
                topSecretStampTexture,
                ScaleMode.StretchToFill,
                true);
        }

        private float DrawHeader(float contentX, float y)
        {
            var rect = new Rect(contentX, y, innerContentWidth, 24f);
            GUI.Label(rect, "JOINT SPECIAL OPERATIONS COMMAND", centerStyle);
            y += 26f;
            GUI.Label(new Rect(contentX, y, innerContentWidth, 24f), "DEPARTMENT OF WAR", centerStyle);
            y += 26f;
            GUI.Label(new Rect(contentX, y, innerContentWidth, 24f), "WASHINGTON, DC 20301", centerStyle);
            return y + 36f;
        }

        private float DrawMissionMetadata(float contentX, float y, string subjectName)
        {
            GUI.Label(new Rect(contentX, y, innerContentWidth, 24f), $"Operation: {MissionBriefingState.OperationName}", leftBodyStyle);
            y += 28f;
            GUI.Label(
                new Rect(contentX, y, innerContentWidth, 24f),
                $"Subject: {subjectName} CURRENT ASSIGNMENT",
                leftBodyStyle);
            return y + 40f;
        }

        private float DrawSecurityBlock(float contentX, float y)
        {
            const float borderLineHeight = 18f;
            var securityLines = new[]
            {
                "TS: SAP CLEARANCE REQUIRED",
                "PROCESSING THESE ORDERS REQUIRES THE USER TO FOLLOW ALL REGULATIONS SET FORTH",
                "IN 50 W.S.C. § 3341 (SECURITY CLEARANCES). IF YOU ARE READING THIS AND YOU ARE NOT",
                "AUTHORIZED YOU MUST STOP AND DISPOSE OF THIS DOCUMENT AS SET FORTH IN",
                "5 CFR § 1312.29 and 36 CFR § 1226.24",
                "FOR OFFICIAL USE ONLY"
            };

            DrawSecurityBorder(contentX, y, innerContentWidth, borderLineHeight);
            y += borderLineHeight;

            foreach (var line in securityLines)
            {
                var lineHeight = securityLineStyle.CalcHeight(new GUIContent(line), innerContentWidth);
                GUI.Label(new Rect(contentX, y, innerContentWidth, lineHeight), line, securityLineStyle);
                y += lineHeight;
            }

            DrawSecurityBorder(contentX, y, innerContentWidth, borderLineHeight);
            return y + borderLineHeight + 28f;
        }

        private void DrawSecurityBorder(float contentX, float y, float width, float height)
        {
            var border = new string('X', SecurityBorderXCount);
            GUI.Label(new Rect(contentX, y, width, height), border, securityBorderStyle);
        }

        private float DrawMissionBody(float contentX, float y)
        {
            GUI.Label(new Rect(contentX, y, innerContentWidth, 24f), "PART ONE: REMEMBER TO READ YOUR ORDERS COMPLETELY!", leftBodyStyle);
            y += 34f;
            GUI.Label(new Rect(contentX, y, innerContentWidth, 24f), "MISSION OBJECTIVE:", leftBodyStyle);
            y += 26f;

            var objectiveHeight = leftBodyStyle.CalcHeight(new GUIContent(MissionBriefingState.MissionObjective), innerContentWidth);
            GUI.Label(new Rect(contentX, y, innerContentWidth, objectiveHeight), MissionBriefingState.MissionObjective, leftBodyStyle);
            y += objectiveHeight + 24f;

            GUI.Label(
                new Rect(contentX, y, innerContentWidth, 24f),
                "PART TWO: MISSION DEBRIEF TO BE CONDUCTED UPON OPERATOR RETURN.",
                leftBodyStyle);
            return y + 30f;
        }

        private void DrawActionButtons()
        {
            const float buttonGap = 12f;
            const float buttonWidth = 220f;
            StartPageMenuStyles.GetMenuButtonSize(out _, out var buttonHeight);
            var totalHeight = buttonHeight * 2f + buttonGap;
            var startX = (Screen.width - buttonWidth) * 0.5f;
            var startY = Screen.height - totalHeight - Margin;

            var acceptRect = new Rect(startX, startY, buttonWidth, buttonHeight);
            var bailRect = new Rect(startX, startY + buttonHeight + buttonGap, buttonWidth, buttonHeight);

            DrawBurnBagStampAbove(acceptRect);

            if (StartPageMenuStyles.DrawMenuButton(acceptRect, "ACCEPT") && !showBailOutConfirm)
            {
                AcceptMission();
            }

            if (StartPageMenuStyles.DrawMenuButton(bailRect, "BAIL OUT?"))
            {
                showBailOutConfirm = true;
            }
        }

        private void DrawBurnBagStampAbove(Rect acceptRect)
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
            var width = BurnBagStampWidthPx;
            var height = width * aspect;
            var x = acceptRect.center.x - width * 0.5f;
            var y = acceptRect.y - BurnBagStampGapAboveAcceptPx - height;
            GUI.DrawTexture(
                new Rect(x, y, width, height),
                burnBagStampTexture,
                ScaleMode.StretchToFill,
                true);
        }

        private static void AcceptMission()
        {
            Time.timeScale = 1f;
            CharacterLoadoutNavState.MarkEnteredFromMissionBrief();
            SceneManager.LoadScene(GameScenes.CharacterLoadout);
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

        private void DrawPageBackground()
        {
            var pageX = (Screen.width - contentWidth) * 0.5f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(pageX, 0f, contentWidth, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (centerStyle != null)
            {
                return;
            }

            centerStyle = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            securityBorderStyle = HudStyleFactory.CreateLabel(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            securityLineStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            leftBodyStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.black, wordWrap: true);

            centerStyle.clipping = TextClipping.Clip;
            securityBorderStyle.clipping = TextClipping.Clip;
            securityLineStyle.clipping = TextClipping.Clip;
            leftBodyStyle.clipping = TextClipping.Clip;

            var borderSize = securityBorderStyle.CalcSize(new GUIContent(new string('X', SecurityBorderXCount)));
            innerContentWidth = borderSize.x;
            contentWidth = innerContentWidth + BorderSidePaddingPx * 2f;
        }
    }
}
