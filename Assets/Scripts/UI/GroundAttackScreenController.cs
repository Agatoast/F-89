using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class GroundAttackScreenController : MonoBehaviour
    {
        private void OnGUI()
        {
            DrawTitle();
            DrawPlaceholder();
            DrawReturnButton();
        }

        private static void DrawTitle()
        {
            var titleStyle = HudStyleFactory.CreateLabel(
                28,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                Color.white);
            GUI.Label(new Rect(0f, 72f, Screen.width, 40f), "Ground Attack", titleStyle);
        }

        private static void DrawPlaceholder()
        {
            var messageStyle = HudStyleFactory.CreateLabel(
                16,
                FontStyle.Normal,
                TextAnchor.UpperCenter,
                Color.white,
                wordWrap: true);
            GUI.Label(
                new Rect(Screen.width * 0.2f, Screen.height * 0.38f, Screen.width * 0.6f, 80f),
                "Character ground combat screen — coming soon.",
                messageStyle);
        }

        private static void DrawReturnButton()
        {
            const float buttonWidth = 220f;
            const float buttonHeight = 40f;
            var buttonX = (Screen.width - buttonWidth) * 0.5f;

            if (GUI.Button(new Rect(buttonX, Screen.height * 0.58f, buttonWidth, buttonHeight), "Return to Flight"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.FlightTest);
            }
        }
    }
}
