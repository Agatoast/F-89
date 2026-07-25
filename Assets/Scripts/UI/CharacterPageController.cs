using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class CharacterPageController : MonoBehaviour
    {
        private const float Margin = 20f;
        private const int RibbonRows = 3;
        private const int RibbonColumns = 8;

        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle sectionStyle;

        private void OnGUI()
        {
            EnsureStyles();
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            DrawPageBackground();
            DrawBackButton();

            var save = CharacterSessionState.ActiveSave;
            var topBar = Margin + 34f;
            var leftWidth = Screen.width * 0.22f;
            var leftRect = new Rect(Margin, topBar, leftWidth, Screen.height - topBar - Margin);
            var rightX = leftRect.xMax + Margin;
            var rightWidth = Screen.width - rightX - Margin;
            var ribbonsHeight = Screen.height * 0.4f;
            var ribbonsRect = new Rect(rightX, topBar, rightWidth, ribbonsHeight);
            var loadoutRect = new Rect(
                rightX,
                ribbonsRect.yMax + Margin,
                rightWidth,
                Screen.height - ribbonsRect.yMax - Margin * 2f);

            if (save != null)
            {
                DrawProfilePanel(leftRect, save);
            }
            else
            {
                DrawWireBox(leftRect, 2f);
                GUI.Label(new Rect(leftRect.x + 12f, leftRect.y + 12f, leftRect.width - 24f, 40f), "No character loaded.", bodyStyle);
            }

            DrawRibbonPanel(ribbonsRect);
            DrawLoadoutPanel(loadoutRect);
            DrawNextMissionButton();
        }

        private void DrawProfilePanel(Rect rect, CharacterSaveData save)
        {
            DrawWireBox(rect, 2f);

            var x = rect.x + 14f;
            var y = rect.y + 12f;
            var innerWidth = rect.width - 28f;

            GUI.Label(new Rect(x, y, innerWidth, 28f), save.DisplayRankAndName, headerStyle);
            y += 34f;

            var pictureHeight = Mathf.Min(180f, rect.height * 0.24f);
            var pictureRect = new Rect(x, y, innerWidth, pictureHeight);
            DrawWireBox(pictureRect, 1.5f);
            GUI.Label(new Rect(pictureRect.x, pictureRect.y + pictureHeight * 0.42f, pictureRect.width, 24f), "Picture", bodyStyle);
            y = pictureRect.yMax + 16f;

            DrawLabelValue(new Rect(x, y, innerWidth, 22f), "Highest Mission Score:", save.BestMissionScore.ToString("N0"));
            y += 30f;
            DrawLabelValue(new Rect(x, y, innerWidth, 22f), "Total Score:", save.TotalScore.ToString("N0"));
            y += 38f;

            GUI.Label(new Rect(x, y, innerWidth, 20f), save.VehicleKillDisplay, labelStyle);
            y += Mathf.Max(72f, labelStyle.CalcHeight(new GUIContent(save.VehicleKillDisplay), innerWidth) + 10f);

            GUI.Label(new Rect(x, y, innerWidth, 20f), save.TroopKillDisplay, labelStyle);
        }

        private void DrawRibbonPanel(Rect rect)
        {
            DrawWireBox(rect, 2f);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 24f),
                "Ribbons in order with oak leaves",
                sectionStyle);

            var gridRect = new Rect(rect.x + 14f, rect.y + 40f, rect.width - 28f, rect.height - 54f);
            DrawWireBox(gridRect, 1.5f);

            var cellWidth = gridRect.width / RibbonColumns;
            var cellHeight = gridRect.height / RibbonRows;
            for (var row = 0; row < RibbonRows; row++)
            {
                for (var col = 0; col < RibbonColumns; col++)
                {
                    var cell = new Rect(
                        gridRect.x + col * cellWidth + 3f,
                        gridRect.y + row * cellHeight + 3f,
                        cellWidth - 6f,
                        cellHeight - 6f);
                    DrawWireBox(cell, 1f);
                }
            }
        }

        private void DrawLoadoutPanel(Rect rect)
        {
            DrawWireBox(rect, 2f);
            var inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);
            LandCharacterGearPanelUi.Draw(inner);
        }

        private void DrawNextMissionButton()
        {
            const float width = 190f;
            const float height = 44f;
            var rect = new Rect(Screen.width - width - Margin, Screen.height - height - Margin, width, height);
            DrawRoundedHeader(rect, "Next Mission");

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                BeginNextMission();
            }
        }

        private static void BeginNextMission()
        {
            Time.timeScale = 1f;
            MissionBriefingState.PrepareNextMission(CharacterSessionState.ActiveSave);
            SceneManager.LoadScene(GameScenes.MissionBriefing);
        }

        private void DrawBackButton()
        {
            const float width = 110f;
            const float height = 34f;
            var rect = new Rect(Margin, Margin, width, height);
            DrawWireBox(rect, 1.5f);
            if (GUI.Button(rect, "BACK", bodyStyle))
            {
                SceneManager.LoadScene(GameScenes.SelectionPage);
            }
        }

        private void DrawLabelValue(Rect rect, string label, string value)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.62f, rect.height), label, labelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.38f, rect.y, rect.width * 0.62f, rect.height), value, valueStyle);
        }

        private static void DrawRoundedHeader(Rect rect, string text)
        {
            DrawWireBox(rect, 2f);
            var style = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            GUI.Label(rect, text, style);
        }

        private static void DrawPageBackground()
        {
            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.black;
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
            if (headerStyle != null)
            {
                return;
            }

            headerStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperLeft, Color.black);
            bodyStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black);
            labelStyle = HudStyleFactory.CreateLabel(13, FontStyle.Normal, TextAnchor.UpperLeft, Color.black, wordWrap: true);
            valueStyle = HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.UpperRight, Color.black);
            sectionStyle = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.UpperLeft, Color.black);
        }
    }
}
