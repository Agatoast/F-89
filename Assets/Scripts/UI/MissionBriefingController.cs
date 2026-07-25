using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MissionBriefingController : MonoBehaviour
    {
        private const float Margin = 40f;
        private const float ButtonWidth = 220f;
        private const float ButtonHeight = 48f;
        private const float BorderSidePaddingPx = 10f;
        private const int SecurityBorderXCount = 80;

        private GUIStyle centerStyle;
        private GUIStyle securityLineStyle;
        private GUIStyle securityBorderStyle;
        private GUIStyle leftBodyStyle;
        private GUIStyle buttonStyle;

        private float contentWidth;
        private float innerContentWidth;

        private bool showBailOutConfirm;

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
            var totalHeight = ButtonHeight * 2f + buttonGap;
            var startX = (Screen.width - ButtonWidth) * 0.5f;
            var startY = Screen.height - totalHeight - Margin;

            var acceptRect = new Rect(startX, startY, ButtonWidth, ButtonHeight);
            var bailRect = new Rect(startX, startY + ButtonHeight + buttonGap, ButtonWidth, ButtonHeight);

            DrawActionButton(acceptRect, "Accept");
            DrawActionButton(bailRect, "Bail Out?");

            if (!showBailOutConfirm && GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
            {
                AcceptMission();
            }

            if (GUI.Button(bailRect, GUIContent.none, GUIStyle.none))
            {
                showBailOutConfirm = true;
            }
        }

        private void DrawActionButton(Rect rect, string label)
        {
            DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
        }

        private static void AcceptMission()
        {
            Time.timeScale = 1f;
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

        private static void DrawWireBox(Rect rect, float thickness)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
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
            buttonStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);

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
