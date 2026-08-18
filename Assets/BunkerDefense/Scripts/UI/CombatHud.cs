using F89.UI;
using SaveAntarctica.BunkerDefense.Combat;
using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace SaveAntarctica.BunkerDefense.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private Text _hangarText;
        private Text _waveText;
        private Text _heatText;
        private Image _hangarFill;
        private Image _heatFill;
        private Button[] _boardClearButtons;

        public static void EnsureInScene()
        {
            if (FindAnyObjectByType<CombatHud>() != null)
            {
                return;
            }

            EnsureEventSystem();
            var go = new GameObject("CombatHud");
            go.AddComponent<CombatHud>();
        }

        private void Awake()
        {
            var canvasGo = new GameObject("CombatHudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _hangarText = CreateLabel(canvasGo.transform, "HangarLabel", "HANGAR 100", 22, new Vector2(40f, -28f), TextAnchor.UpperLeft);
            var site = FlightMissionData.Instance != null && !string.IsNullOrEmpty(FlightMissionData.Instance.SiteCode)
                ? FlightMissionData.Instance.SiteCode
                : "OP-SOUTH";
            _waveText = CreateLabel(canvasGo.transform, "WaveLabel", $"{site}   WAVE 1 / 3", 22, new Vector2(0f, -28f), TextAnchor.UpperCenter);
            _heatText = CreateLabel(canvasGo.transform, "HeatLabel", "MINIGUN HEAT", 20, new Vector2(0f, -66f), TextAnchor.UpperCenter);

            _hangarFill = CreateBar(canvasGo.transform, "HangarBar", new Vector2(40f, -64f), new Vector2(0f, 1f), new Vector2(0f, 1f), HudColorPalette.MfdGreen);
            _heatFill = CreateCenterBar(canvasGo.transform, "HeatBar", new Vector2(0f, -100f), new Color(0.95f, 0.55f, 0.15f, 1f));
            CreateBoardClearGrenades(canvasGo.transform);
        }

        private void Update()
        {
            var match = MatchController.Instance;
            if (match == null)
            {
                return;
            }

            if (match.Hangar != null)
            {
                _hangarText.text = $"HANGAR {match.Hangar.CurrentHitPoints}";
                var hangarT = match.Hangar.MaxHitPoints <= 0
                    ? 0f
                    : match.Hangar.CurrentHitPoints / (float)match.Hangar.MaxHitPoints;
                _hangarFill.rectTransform.sizeDelta = new Vector2(280f * hangarT, 14f);
                _hangarFill.color = hangarT < 0.35f ? HudColorPalette.Danger : HudColorPalette.MfdGreen;
            }

            if (match.Waves != null)
            {
                var site = FlightMissionData.Instance != null && !string.IsNullOrEmpty(FlightMissionData.Instance.SiteCode)
                    ? FlightMissionData.Instance.SiteCode
                    : "OP-SOUTH";
                _waveText.text = $"{site}   WAVE {Mathf.Max(1, match.Waves.CurrentWave)} / {match.Waves.TotalWaves}   KILLS {match.Waves.Kills}";
            }

            if (match.Gun != null)
            {
                _heatFill.rectTransform.sizeDelta = new Vector2(280f * match.Gun.Heat01, 14f);
                _heatFill.color = match.Gun.IsOverheated
                    ? HudColorPalette.Danger
                    : new Color(0.95f, 0.55f, 0.15f, 1f);
                _heatText.text = match.Gun.IsOverheated ? "OVERHEAT" : "MINIGUN HEAT";
                _heatText.color = match.Gun.IsOverheated ? HudColorPalette.Danger : HudColorPalette.MfdGreen;
            }

            RefreshBoardClearButtons();
        }

        private void OnGUI()
        {
            var match = MatchController.Instance;
            if (match == null || !match.IsFighting)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UiFitCanvas.Begin(16f / 9f);

            var buttonRect = new Rect(
                UiFitCanvas.Rect.x + UiFitCanvas.Px(40f),
                UiFitCanvas.Rect.y + UiFitCanvas.Px(136f),
                UiFitCanvas.Px(280f),
                UiFitCanvas.Px(34f));

            if (StartPageMenuStyles.DrawMenuButton(buttonRect, "BACK TO MISSIONS", fontSize: 16))
            {
                BunkerDefenseExitNavigation.ReturnToMissionMenu();
            }
        }

        private static Text CreateLabel(Transform parent, string name, string text, int size, Vector2 anchored, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            if (align == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (align == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }

            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(520f, 36f);
            var label = go.GetComponent<Text>();
            label.font = MilitaryFont.LabelFont;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.color = HudColorPalette.MfdGreen;
            label.alignment = align;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateBar(Transform parent, string name, Vector2 anchored, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin.x < 0.5f ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(280f, 14f);
            var image = go.GetComponent<Image>();
            image.color = color;
            UiSolidImage.Apply(image);
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateCenterBar(Transform parent, string name, Vector2 anchored, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(280f, 14f);
            var image = go.GetComponent<Image>();
            image.color = color;
            UiSolidImage.Apply(image);
            image.raycastTarget = false;
            return image;
        }

        private void CreateBoardClearGrenades(Transform parent)
        {
            var sprite = LoadGrenadeSprite();
            _boardClearButtons = new Button[BoardClearGrenade.MaxCharges];
            const float size = 74f;
            const float gap = 10f;
            const float marginX = 10f;
            const float marginY = 10f;

            for (var i = 0; i < _boardClearButtons.Length; i++)
            {
                var x = -marginX - (BoardClearGrenade.MaxCharges - 1 - i) * (size + gap);
                _boardClearButtons[i] = CreateGrenadeButton(
                    parent,
                    $"BoardClearGrenade{i + 1}",
                    sprite,
                    new Vector2(x, marginY),
                    size);
                _boardClearButtons[i].onClick.AddListener(HandleBoardClearGrenade);
            }

            RefreshBoardClearButtons();
        }

        private static Sprite LoadGrenadeSprite()
        {
            var texture = Resources.Load<Texture2D>(GameConstants.BoardClearGrenadeResource);
            if (texture == null)
            {
                Debug.LogWarning($"Bunker Defense: missing grenade art at Resources/{GameConstants.BoardClearGrenadeResource}.");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Button CreateGrenadeButton(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchored,
            float size)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(size, size);

            var image = buttonGo.GetComponent<Image>();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.35f, 0.4f, 0.2f, 1f);
                UiSolidImage.Apply(image);
            }

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private void HandleBoardClearGrenade()
        {
            if (BoardClearGrenade.TryUse())
            {
                RefreshBoardClearButtons();
            }
        }

        private void RefreshBoardClearButtons()
        {
            if (_boardClearButtons == null)
            {
                return;
            }

            var firstVisible = BoardClearGrenade.MaxCharges - BoardClearGrenade.Remaining;
            for (var i = 0; i < _boardClearButtons.Length; i++)
            {
                var button = _boardClearButtons[i];
                if (button == null)
                {
                    continue;
                }

                var visible = i >= firstVisible;
                button.gameObject.SetActive(visible);
                button.interactable = visible && BoardClearGrenade.CanUse();
            }
        }

        private static void EnsureEventSystem() => BunkerDefenseEventSystem.Ensure();
    }
}
