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
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (splashTexture == null)
            {
                return;
            }

            var imageRect = GetFullscreenImageRect(splashTexture);
            GUI.DrawTexture(imageRect, splashTexture, ScaleMode.StretchToFill, true);
        }

        private void DrawStartButton()
        {
            const float buttonWidth = 280f;
            const float buttonHeight = 48f;
            const float bottomMargin = 56f;
            var buttonRect = new Rect(
                (Screen.width - buttonWidth) * 0.5f,
                Screen.height - buttonHeight - bottomMargin,
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

        private static Rect GetFullscreenImageRect(Texture2D texture)
        {
            var screenAspect = (float)Screen.width / Screen.height;
            var textureAspect = (float)texture.width / texture.height;

            if (textureAspect > screenAspect)
            {
                var height = Screen.width / textureAspect;
                return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
            }

            var width = Screen.height * textureAspect;
            return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
        }
    }
}
