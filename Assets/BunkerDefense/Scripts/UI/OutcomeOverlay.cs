using F89.Core;
using F89.UI;
using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveAntarctica.BunkerDefense.UI
{
    /// <summary>Post-match outcome — IMGUI OK button returns to F-89 character page.</summary>
    public sealed class OutcomeOverlay : MonoBehaviour
    {
        public static void ShowVictory() => Show(GameConstants.VictoryHeadline, "+1 HIT FOR THE PLANE UNTIL THE END OF NEXT FLIGHT MISSION", HudColorPalette.MfdGreen);

        public static void ShowDefeat() => Show(GameConstants.DefeatHeadline, "-1 HIT FOR THE PLANE ON NEXT FLIGHT MISSION", HudColorPalette.Danger);

        private static void Show(string headline, string detail, Color accent)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var go = new GameObject("OutcomeOverlay");
            var overlay = go.AddComponent<OutcomeOverlay>();
            overlay._headline = headline;
            overlay._detail = detail;
            overlay._accent = accent;
            Time.timeScale = 0f;
        }

        private string _headline = string.Empty;
        private string _detail = string.Empty;
        private Color _accent;
        private bool _closed;
        private GUIStyle _headlineStyle;
        private GUIStyle _detailStyle;

        private void Update()
        {
            if (_closed)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (_closed)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureStyles();

            UiFitCanvas.Begin(16f / 9f);

            GUI.color = new Color(0.02f, 0.025f, 0.03f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            var panelWidth = UiFitCanvas.Px(920f);
            var panelHeight = UiFitCanvas.Px(340f);
            var panelRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - panelWidth) * 0.5f,
                UiFitCanvas.Rect.y + (UiFitCanvas.Rect.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.color = new Color(0.04f, 0.05f, 0.06f, 1f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var headlineRect = new Rect(
                panelRect.x,
                panelRect.y + UiFitCanvas.Px(70f),
                panelRect.width,
                UiFitCanvas.Px(80f));
            GUI.Label(headlineRect, _headline, _headlineStyle);

            var detailRect = new Rect(
                panelRect.x + UiFitCanvas.Px(24f),
                panelRect.y + UiFitCanvas.Px(150f),
                panelRect.width - UiFitCanvas.Px(48f),
                UiFitCanvas.Px(80f));
            GUI.Label(detailRect, _detail, _detailStyle);

            var buttonWidth = UiFitCanvas.Px(220f);
            var buttonHeight = UiFitCanvas.Px(56f);
            var buttonRect = new Rect(
                panelRect.x + (panelRect.width - buttonWidth) * 0.5f,
                panelRect.yMax - buttonHeight - UiFitCanvas.Px(36f),
                buttonWidth,
                buttonHeight);

            if (StartPageMenuStyles.DrawMenuButton(buttonRect, "OK", fontSize: 16))
            {
                Close();
            }
        }

        private void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            Time.timeScale = 1f;

            var data = FlightMissionData.Instance;
            var missionId = data != null ? data.ActiveDefenseMissionId : string.Empty;
            if (!string.IsNullOrWhiteSpace(missionId))
            {
                BunkerDefenseIntegration.MarkDefenseMissionCompleted(missionId);
            }

            data?.CommitOkReturnToHost();

            if (data != null
                && data.TryConsumeHostResume(out var baseId, out var uiRequest)
                && BunkerDefenseHostResumeRegistry.Handler != null)
            {
                BunkerDefenseHostResumeRegistry.Handler.ResumeToLastLanding(baseId, uiRequest);
                Destroy(gameObject);
                return;
            }

            if (TryLoad(F89.Core.GameScenes.CharacterPage)
                || TryLoad(F89.Core.GameScenes.AircraftLoadout)
                || TryLoad(F89.Core.GameScenes.MainMenu))
            {
                Destroy(gameObject);
                return;
            }

            Debug.LogWarning("F-89: bunker defense outcome could not return to host — no scene in Build Settings.");
            Destroy(gameObject);
        }

        private static bool TryLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(sceneName);
            return true;
        }

        private void EnsureStyles()
        {
            if (_headlineStyle != null)
            {
                return;
            }

            _headlineStyle = HudStyleFactory.CreateLabel(
                48,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                _accent);
            _detailStyle = HudStyleFactory.CreateLabel(
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                HudColorPalette.TextPrimary,
                wordWrap: true);
        }
    }
}
