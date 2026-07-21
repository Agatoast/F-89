using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private void OnGUI()
        {
            DrawTitle();
            DrawButtons();
        }

        private static void DrawTitle()
        {
            var titleStyle = HudStyleFactory.CreateLabel(28, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);

            GUI.Label(new Rect(0f, 80f, Screen.width, 40f), "F-89 Stealth Fighter-Bomber", titleStyle);
        }

        private static void DrawButtons()
        {
            const float buttonWidth = 220f;
            const float buttonHeight = 40f;
            var buttonX = (Screen.width - buttonWidth) * 0.5f;

            if (GUI.Button(new Rect(buttonX, 180f, buttonWidth, buttonHeight), "Flight Test"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.FlightTest);
            }
        }
    }
}
