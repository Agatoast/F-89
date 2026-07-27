using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>Boss-bunker completion page before returning the player to the paired surface area.</summary>
    public sealed class MissionCompleteController : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            GUI.color = new Color(0.03f, 0.05f, 0.07f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = HudStyleFactory.CreateLabel(42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var detail = HudStyleFactory.CreateLabel(18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.82f, 0.78f, 0.52f));
            GUI.Label(new Rect(0f, Screen.height * 0.33f, Screen.width, 60f), "MISSION COMPLETE", title);
            GUI.Label(
                new Rect(0f, Screen.height * 0.43f, Screen.width, 34f),
                $"{LandMissionCompleteState.CompletedBunkerCode} CLEARED",
                detail);

            var buttonWidth = UiFitCanvas.Px(280f);
            var buttonHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - UiFitCanvas.Px(40f),
                buttonWidth,
                buttonHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 16))
            {
                CharacterGearSession.PersistActive();
                if (LandMissionCompleteState.IsCarrierLanding)
                {
                    LandMissionHealthState.Clear();
                    LandMissionCompleteState.Clear();
                    SceneManager.LoadScene(GameScenes.CharacterPage);
                    return;
                }

                LandSurfaceSession.BeginReturnAtBunker();
                LandMissionCompleteState.Clear();
                SceneManager.LoadScene(GameScenes.GroundAttack);
            }
        }
    }
}
