using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SaveAntarctica.BunkerDefense.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            BuildUi();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            camGo.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem() => BunkerDefenseEventSystem.Ensure();

        private void BuildUi()
        {
            var canvasGo = new GameObject("MainMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = CreateStretchPanel(canvasGo.transform, "Root");
            root.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.04f, 1f);
            UiSolidImage.Apply(root.GetComponent<Image>());

            var content = CreateStretchPanel(root.transform, "Content");
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.offsetMin = new Vector2(120f, 80f);
            contentRect.offsetMax = new Vector2(-120f, -80f);

            var title = CreateText(content.transform, "Title", "SAVE ANTARCTICA: BUNKER DEFENSE", 42, FontStyle.Bold, HudColorPalette.MfdGreen);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 64f);
            title.alignment = TextAnchor.MiddleCenter;

            var subtitle = CreateText(content.transform, "Subtitle", "SELECT MISSION - SAME 18 SITES AS BASE DEFENSE", 20, FontStyle.Bold, HudColorPalette.TextMuted);
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0f, 1f);
            subtitleRect.anchorMax = new Vector2(1f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -72f);
            subtitleRect.sizeDelta = new Vector2(0f, 28f);
            subtitle.alignment = TextAnchor.MiddleCenter;

            CreateMapList(content.transform);
        }

        private void CreateMapList(Transform parent)
        {
            var scrollGo = new GameObject("MapScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = new Vector2(0f, -112f);

            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.06f, 0.07f, 0.92f);
            UiSolidImage.Apply(scrollBg);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = CreateStretchPanel(scrollGo.transform, "Viewport");
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            UiSolidImage.Apply(viewport.GetComponent<Image>());
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var listGo = new GameObject("MapList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(viewport.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = listGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = listRect;

            for (var map = 1; map <= DefenseSiteCatalog.MissionCount; map++)
            {
                CreateMapButton(listGo.transform, map);
            }
        }

        private static void CreateMapButton(Transform parent, int mapNumber)
        {
            var go = new GameObject($"Map{mapNumber:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 52f;
            go.GetComponent<LayoutElement>().preferredHeight = 52f;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.11f, 1f);
            UiSolidImage.Apply(image);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var capturedMap = mapNumber;
            button.onClick.AddListener(() => LaunchMap(capturedMap));

            var text = CreateText(go.transform, "Label", DefenseSiteCatalog.GetMapLabel(mapNumber), 22, FontStyle.Bold, HudColorPalette.TextPrimary);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 0f);
            textRect.offsetMax = new Vector2(-16f, 0f);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
        }

        private static void LaunchMap(int mapNumber)
        {
            FlightMissionData.Instance?.SetStandaloneMap(mapNumber);
            FlightMissionData.Instance?.SetReturnSceneName(GameScenes.MainMenuScene);
            SceneManager.LoadScene(GameScenes.PlayScene);
        }

        private static GameObject CreateStretchPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle fontStyle, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = MilitaryFont.LabelFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }
    }
}
