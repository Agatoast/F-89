using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class StartPageController : MonoBehaviour
    {
        private const string BackgroundResourcePath = "StartPage/main_menu";
        private const string SaveAntarcticaLogoResourcePath = "CharacterPage/save_antarctica_logo";

        private static readonly string[] ButtonLabels = { "PLAY", "STORY", "RULES", "SETTINGS", "CREDITS" };

        private Texture2D backgroundTexture;
        private Texture2D saveAntarcticaLogoTexture;

        private void Start()
        {
            backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);
        }

        private void OnGUI()
        {
            StartPageMenuStyles.DrawFullscreenBackground(backgroundTexture);
            DrawSaveAntarcticaLogo();
            DrawButtons();
        }

        private void DrawSaveAntarcticaLogo()
        {
            if (saveAntarcticaLogoTexture == null)
            {
                saveAntarcticaLogoTexture = Resources.Load<Texture2D>(SaveAntarcticaLogoResourcePath);
            }

            StartPageMenuStyles.DrawSaveAntarcticaLogo(saveAntarcticaLogoTexture);
        }

        private static void DrawButtons()
        {
            for (var i = 0; i < ButtonLabels.Length; i++)
            {
                var rect = StartPageMenuStyles.GetMenuButtonRect(i, ButtonLabels.Length);
                if (!StartPageMenuStyles.DrawMenuButton(rect, ButtonLabels[i]))
                {
                    continue;
                }

                HandleButton(ButtonLabels[i]);
            }
        }

        private static void HandleButton(string label)
        {
            Time.timeScale = 1f;

            switch (label)
            {
                case "PLAY":
                    SceneManager.LoadScene(GameScenes.SelectionPage);
                    break;
                case "STORY":
                    OpenSubpage("STORY", "Story mode — coming soon.");
                    break;
                case "RULES":
                    OpenSubpage("RULES", "Rules and briefing — coming soon.");
                    break;
                case "SETTINGS":
                    MenuNavigationState.Mode = MenuNavigationState.SubpageMode.Settings;
                    MenuNavigationState.SubpageTitle = "SETTINGS";
                    MenuNavigationState.SubpageMessage = string.Empty;
                    SceneManager.LoadScene(GameScenes.MenuSubpage);
                    break;
                case "CREDITS":
                    OpenSubpage("CREDITS", "Credits — coming soon.");
                    break;
            }
        }

        private static void OpenSubpage(string title, string message)
        {
            MenuNavigationState.Mode = MenuNavigationState.SubpageMode.Message;
            MenuNavigationState.SubpageTitle = title;
            MenuNavigationState.SubpageMessage = message;
            SceneManager.LoadScene(GameScenes.MenuSubpage);
        }
    }
}
