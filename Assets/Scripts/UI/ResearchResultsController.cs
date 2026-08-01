using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// End-of-mission score summary, then R&amp;D report pages.
    /// </summary>
    public sealed class ResearchResultsController : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                Continue();
            }
        }

        private void OnGUI()
        {
            if (MissionEndReportState.HasPendingReport)
            {
                DrawMissionEndReport();
                return;
            }

            if (!LandResearchResultsState.HasPendingReport)
            {
                FinishToReturnScene();
                return;
            }

            DrawResearchReport();
        }

        private static void DrawMissionEndReport()
        {
            UiFitCanvas.Begin(16f / 9f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(UiFitCanvas.Rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = HudStyleFactory.CreateLabel(
                36,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                new Color(0.78f, 0.86f, 0.95f));
            var bodyStyle = HudStyleFactory.CreateLabel(
                22,
                FontStyle.Normal,
                TextAnchor.UpperCenter,
                Color.white,
                wordWrap: true);

            GUI.Label(
                new Rect(UiFitCanvas.Rect.x, UiFitCanvas.NormY(0.12f), UiFitCanvas.Rect.width, UiFitCanvas.Px(60f)),
                "END MISSION",
                titleStyle);

            var body = BuildMissionEndReportBody();
            GUI.Label(UiFitCanvas.NormRect(0.15f, 0.28f, 0.7f, 0.42f), body, bodyStyle);

            var continueWidth = UiFitCanvas.Px(280f);
            var continueHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - continueWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - UiFitCanvas.Px(100f),
                continueWidth,
                continueHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 28))
            {
                ContinueMissionEndReport();
            }
        }

        private static string BuildMissionEndReportBody()
        {
            var lines = $"Mission Score: {MissionEndReportState.MissionScore:N0}";
            if (MissionEndReportState.IsNewBestMissionScore)
            {
                lines += "\n\nNEW BEST MISSION SCORE";
            }

            if (MissionEndReportState.RequiresCourtMartial)
            {
                lines += "\n\nGENERAL COURT-MARTIAL — total mission score fell below zero.\n"
                    + "Character imprisoned (GCMP). Record preserved; pilot cannot fly again.";
            }

            return lines;
        }

        private static void DrawResearchReport()
        {
            UiFitCanvas.Begin(16f / 9f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(UiFitCanvas.Rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = HudStyleFactory.CreateLabel(36, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.78f, 0.86f, 0.95f));
            var bodyStyle = HudStyleFactory.CreateLabel(22, FontStyle.Normal, TextAnchor.UpperCenter, Color.white, wordWrap: true);
            GUI.Label(
                new Rect(UiFitCanvas.Rect.x, UiFitCanvas.NormY(0.12f), UiFitCanvas.Rect.width, UiFitCanvas.Px(60f)),
                LandResearchResultsState.TitleForCurrent(),
                titleStyle);
            GUI.Label(
                UiFitCanvas.NormRect(0.15f, 0.28f, 0.7f, 0.4f),
                LandResearchResultsState.BodyForCurrent(),
                bodyStyle);

            var continueWidth = UiFitCanvas.Px(280f);
            var continueHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - continueWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - UiFitCanvas.Px(100f),
                continueWidth,
                continueHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 28))
            {
                ContinueResearchReport();
            }

            var pageId = LandResearchResultsState.CurrentPageId;
            if (!string.IsNullOrEmpty(pageId))
            {
                var idStyle = HudStyleFactory.CreateLabel(12, FontStyle.Normal, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.35f));
                GUI.Label(
                    new Rect(UiFitCanvas.Rect.x + UiFitCanvas.Px(16f), UiFitCanvas.Rect.yMax - UiFitCanvas.Px(28f), UiFitCanvas.Px(480f), UiFitCanvas.Px(20f)),
                    pageId,
                    idStyle);
            }
        }

        private static void Continue()
        {
            if (MissionEndReportState.HasPendingReport)
            {
                ContinueMissionEndReport();
                return;
            }

            ContinueResearchReport();
        }

        private static void ContinueMissionEndReport()
        {
            var courtMartialed = MissionEndReportState.RequiresCourtMartial;
            MissionEndReportState.Clear();
            if (courtMartialed)
            {
                LandResearchResultsState.Clear();
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.SelectionPage);
                return;
            }

            if (!LandResearchResultsState.HasPendingReport)
            {
                FinishToReturnScene();
            }
        }

        private static void ContinueResearchReport()
        {
            if (LandResearchResultsState.Advance())
            {
                return;
            }

            FinishToReturnScene();
        }

        private static void FinishToReturnScene()
        {
            var next = LandResearchResultsState.ReturnSceneName;
            LandResearchResultsState.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(next);
        }
    }
}
