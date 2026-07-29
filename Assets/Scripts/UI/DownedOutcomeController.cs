using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// DeathScreen / POWScreen / EscapedScreen / FrozenDeath report after ground unconsciousness.
    /// Frozen death: FrozenDeath → DeathScreen → Main Menu.
    /// Other outcomes: single page → Main Menu.
    /// </summary>
    public sealed class DownedOutcomeController : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
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
            if (!LandDownedOutcomeState.HasPendingOutcome)
            {
                FinishToMainMenu();
                return;
            }

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
                LandDownedOutcomeState.TitleForCurrent(),
                titleStyle);
            GUI.Label(
                UiFitCanvas.NormRect(0.15f, 0.28f, 0.7f, 0.4f),
                LandDownedOutcomeState.BodyForCurrent(),
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
                Continue();
            }

            var pageId = LandDownedOutcomeState.PageId;
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
            if (LandDownedOutcomeState.Advance())
            {
                return;
            }

            FinishToMainMenu();
        }

        private static void FinishToMainMenu()
        {
            LandDownedOutcomeState.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
