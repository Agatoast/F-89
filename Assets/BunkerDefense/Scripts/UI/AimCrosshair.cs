using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;
using UnityEngine.UI;

namespace SaveAntarctica.BunkerDefense.UI
{
    public sealed class AimCrosshair : MonoBehaviour
    {
        private const int ReticlePixels = 25;

        private RectTransform _reticle;

        public static void EnsureInScene()
        {
            if (FindAnyObjectByType<AimCrosshair>() != null)
            {
                return;
            }

            var go = new GameObject("AimCrosshair");
            go.AddComponent<AimCrosshair>();
        }

        private void Awake()
        {
            var canvasGo = new GameObject("AimCrosshairCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var imageGo = new GameObject("Reticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            _reticle = imageGo.GetComponent<RectTransform>();
            _reticle.anchorMin = Vector2.zero;
            _reticle.anchorMax = Vector2.zero;
            _reticle.pivot = new Vector2(0.5f, 0.5f);
            _reticle.sizeDelta = new Vector2(ReticlePixels, ReticlePixels);

            var image = imageGo.GetComponent<Image>();
            image.sprite = SpriteFactory.Reticle25();
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        private void LateUpdate()
        {
            if (_reticle == null)
            {
                return;
            }

            var gun = MatchController.Instance != null ? MatchController.Instance.Gun : null;
            var visible = gun != null && gun.CanFire;
            _reticle.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            _reticle.position = gun.CrosshairScreen;
        }
    }
}
