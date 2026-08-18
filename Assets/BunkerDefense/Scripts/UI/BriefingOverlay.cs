using SaveAntarctica.BunkerDefense.Combat;
using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;
using F89.UI;

namespace SaveAntarctica.BunkerDefense.UI
{
    /// <summary>Pre-fight briefing — uses IMGUI buttons like the rest of F-89.</summary>
    public sealed class BriefingOverlay : MonoBehaviour
    {
        private bool _started;
        private string _siteLabel = string.Empty;
        private GUIStyle _titleStyle;
        private GUIStyle _siteStyle;
        private GUIStyle _bodyStyle;

        public static void Show(string siteLabel = null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            CombatAudio.PlayBriefingClaxon();

            var go = new GameObject("BriefingOverlay");
            var overlay = go.AddComponent<BriefingOverlay>();
            overlay._siteLabel = siteLabel ?? string.Empty;
        }

        private void Update()
        {
            if (_started)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                HandleStart();
            }
        }

        private void OnGUI()
        {
            if (_started)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureStyles();

            UiFitCanvas.Begin(16f / 9f);

            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            var panelWidth = UiFitCanvas.Px(860f);
            var panelHeight = UiFitCanvas.Px(420f);
            var panelRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - panelWidth) * 0.5f,
                UiFitCanvas.Rect.y + (UiFitCanvas.Rect.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleRect = new Rect(
                panelRect.x,
                panelRect.y + UiFitCanvas.Px(36f),
                panelRect.width,
                UiFitCanvas.Px(56f));
            GUI.Label(titleRect, "MAN THE GUN", _titleStyle);

            if (!string.IsNullOrWhiteSpace(_siteLabel))
            {
                var siteRect = new Rect(
                    panelRect.x,
                    panelRect.y + UiFitCanvas.Px(96f),
                    panelRect.width,
                    UiFitCanvas.Px(28f));
                GUI.Label(siteRect, _siteLabel.ToUpperInvariant(), _siteStyle);
            }

            var bodyRect = new Rect(
                panelRect.x + UiFitCanvas.Px(24f),
                panelRect.y + UiFitCanvas.Px(140f),
                panelRect.width - UiFitCanvas.Px(48f),
                UiFitCanvas.Px(120f));
            GUI.Label(
                bodyRect,
                "THE HANGAR IS UNDER ATTACK!\nPROTECT YOUR PLANE AT ALL COSTS\nENGAGE AND ELIMINATE ALL ENEMY THREATS",
                _bodyStyle);

            var buttonWidth = UiFitCanvas.Px(240f);
            var buttonHeight = UiFitCanvas.Px(56f);
            var buttonRect = new Rect(
                panelRect.x + (panelRect.width - buttonWidth) * 0.5f,
                panelRect.yMax - buttonHeight - UiFitCanvas.Px(36f),
                buttonWidth,
                buttonHeight);

            if (StartPageMenuStyles.DrawMenuButton(buttonRect, "OPEN FIRE", fontSize: 16))
            {
                HandleStart();
            }

            var backRect = new Rect(
                panelRect.x + UiFitCanvas.Px(24f),
                panelRect.y + UiFitCanvas.Px(24f),
                UiFitCanvas.Px(240f),
                UiFitCanvas.Px(44f));
            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK TO MISSIONS", fontSize: 14))
            {
                BunkerDefenseExitNavigation.ReturnToMissionMenu();
            }
        }

        private void HandleStart()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            CombatAudio.StopBriefingClaxon();
            MatchController.Instance?.BeginFighting();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            CombatAudio.StopBriefingClaxon();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = HudStyleFactory.CreateLabel(
                48,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                HudColorPalette.MfdGreen);
            _siteStyle = HudStyleFactory.CreateLabel(
                20,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                HudColorPalette.TextMuted);
            _bodyStyle = HudStyleFactory.CreateLabel(
                24,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                HudColorPalette.TextPrimary,
                wordWrap: true);
        }
    }
}
