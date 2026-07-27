using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        private const string SplashResourcePath = "Loading/first_flash_landing_page";

        private Texture2D splashTexture;
        private AsyncOperation loadOperation;
        private bool isReadyToStart;
        private bool hasTransitioned;

        private void Start()
        {
            splashTexture = Resources.Load<Texture2D>(SplashResourcePath);
            loadOperation = SceneManager.LoadSceneAsync(GameScenes.StartPage, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = false;
            }
            else
            {
                isReadyToStart = true;
            }
        }

        private void Update()
        {
            if (isReadyToStart || loadOperation == null)
            {
                return;
            }

            if (loadOperation.progress >= 0.9f)
            {
                isReadyToStart = true;
            }
        }

        private void OnGUI()
        {
            DrawSplash();
            if (isReadyToStart && !hasTransitioned)
            {
                DrawStartButton();
            }
        }

        private void DrawSplash()
        {
            if (splashTexture == null)
            {
                UiFitCanvas.Begin(16f / 9f);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }

            UiFitCanvas.DrawLetterboxedBackground(splashTexture);
        }

        private void DrawStartButton()
        {
            var buttonWidth = UiFitCanvas.Px(280f);
            var buttonHeight = UiFitCanvas.Px(48f);
            var bottomMargin = UiFitCanvas.Px(56f);
            var buttonRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - bottomMargin,
                buttonWidth,
                buttonHeight);

            var shadowRect = new Rect(buttonRect.x + 2f, buttonRect.y + 2f, buttonRect.width, buttonRect.height);
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(shadowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (StartPageMenuStyles.DrawMenuButton(buttonRect, "Click to Start"))
            {
                TransitionToStartPage();
            }
        }

        private void TransitionToStartPage()
        {
            if (hasTransitioned)
            {
                return;
            }

            hasTransitioned = true;
            SceneManager.LoadScene(GameScenes.StartPage, LoadSceneMode.Single);
        }
    }
}
