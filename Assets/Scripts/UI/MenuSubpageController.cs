using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MenuSubpageController : MonoBehaviour
    {
        private SettingsMenuUi.View settingsView = SettingsMenuUi.View.Root;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (MenuNavigationState.Mode == MenuNavigationState.SubpageMode.Settings)
            {
                HandleSettingsEscape();
                return;
            }

            SceneManager.LoadScene(GameScenes.StartPage);
        }

        private void HandleSettingsEscape()
        {
            if (GameKeyBindings.IsListening)
            {
                GameKeyBindings.CancelListening();
                return;
            }

            if (settingsView == SettingsMenuUi.View.Keymap)
            {
                settingsView = SettingsMenuUi.View.Root;
                return;
            }

            SceneManager.LoadScene(GameScenes.StartPage);
        }

        private void OnGUI()
        {
            if (MenuNavigationState.Mode == MenuNavigationState.SubpageMode.Settings)
            {
                DrawSettingsSubpage();
                return;
            }

            StartPageMenuStyles.DrawSubpage(
                MenuNavigationState.SubpageTitle,
                MenuNavigationState.SubpageMessage);
        }

        private void DrawSettingsSubpage()
        {
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            settingsView = SettingsMenuUi.Draw(settingsView, () =>
            {
                settingsView = SettingsMenuUi.View.Root;
                SceneManager.LoadScene(GameScenes.StartPage);
            });
        }
    }
}
