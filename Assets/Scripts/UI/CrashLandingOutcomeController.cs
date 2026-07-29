using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// CrashLandingRescue / CrashLandingWounded / CrashLandingKIA / PurpleHeartAward pages.
    /// </summary>
    public sealed class CrashLandingOutcomeController : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;

            if (CrashLandingOutcomeState.CurrentPage == CrashLandingPage.PurpleHeartAward)
            {
                CrashLandingOutcomeState.EnsurePurpleHeartGranted();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                Continue();
            }
        }

        private void OnGUI()
        {
            if (!CrashLandingOutcomeState.HasPendingPages)
            {
                PostMissionContinuation.ContinueAfterCrashLanding();
                return;
            }

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
                CrashLandingOutcomeState.TitleForCurrent(),
                titleStyle);
            GUI.Label(
                UiFitCanvas.NormRect(0.15f, 0.28f, 0.7f, 0.4f),
                CrashLandingOutcomeState.BodyForCurrent(),
                bodyStyle);

            if (CrashLandingOutcomeState.CurrentPage == CrashLandingPage.PurpleHeartAward)
            {
                DrawPurpleHeartAward();
            }

            var continueWidth = UiFitCanvas.Px(280f);
            var continueHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - continueWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - UiFitCanvas.Px(100f),
                continueWidth,
                continueHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 16))
            {
                Continue();
            }

            var pageId = CrashLandingOutcomeState.PageId;
            if (!string.IsNullOrEmpty(pageId))
            {
                var idStyle = HudStyleFactory.CreateLabel(
                    12,
                    FontStyle.Normal,
                    TextAnchor.LowerLeft,
                    new Color(1f, 1f, 1f, 0.35f));
                GUI.Label(
                    new Rect(
                        UiFitCanvas.Rect.x + UiFitCanvas.Px(16f),
                        UiFitCanvas.Rect.yMax - UiFitCanvas.Px(28f),
                        UiFitCanvas.Px(480f),
                        UiFitCanvas.Px(20f)),
                    pageId,
                    idStyle);
            }
        }

        private static void DrawPurpleHeartAward()
        {
            var medalTexture = MilitaryMedalService.GetMedalTexture(MilitaryRibbonIds.PurpleHeart);
            if (medalTexture == null)
            {
                return;
            }

            var size = UiFitCanvas.Px(180f);
            var medalRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - size) * 0.5f,
                UiFitCanvas.NormY(0.52f),
                size,
                size * ((float)medalTexture.height / medalTexture.width));
            GUI.DrawTexture(medalRect, medalTexture, ScaleMode.ScaleToFit, true);
        }

        private static void Continue()
        {
            if (CrashLandingOutcomeState.Advance())
            {
                return;
            }

            PostMissionContinuation.ContinueAfterCrashLanding();
        }
    }
}
