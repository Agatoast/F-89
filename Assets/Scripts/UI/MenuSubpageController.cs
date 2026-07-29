using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MenuSubpageController : MonoBehaviour
    {
        private SettingsMenuUi.View settingsView = SettingsMenuUi.View.Root;

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
            settingsView = SettingsMenuUi.Draw(settingsView, () =>
            {
                settingsView = SettingsMenuUi.View.Root;
                SceneManager.LoadScene(GameScenes.StartPage);
            });
        }
    }
}
