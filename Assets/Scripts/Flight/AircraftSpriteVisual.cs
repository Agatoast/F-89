using UnityEngine;

namespace F89.Flight
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AircraftSpriteVisual : MonoBehaviour
    {
        [SerializeField] private AircraftController controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool scaleToOneTic = true;
        [SerializeField] private bool useWingspanAsTicReference;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Configure(AircraftController aircraftController, SpriteRenderer renderer)
        {
            controller = aircraftController;
            spriteRenderer = renderer;
            ApplyTicScale();
            ApplyTopDownOrientation();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyTicScale();
            ApplyTopDownOrientation();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyTicScale();
        }

        public void ApplyTicScale()
        {
            if (!scaleToOneTic || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var profile = controller != null ? controller.Profile : null;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var bounds = spriteRenderer.sprite.bounds.size;
            var referenceLength = useWingspanAsTicReference
                ? Mathf.Max(bounds.x, bounds.y)
                : bounds.y;

            if (referenceLength <= 0f)
            {
                return;
            }

            var scale = ticSize / referenceLength * AircraftVisualFactory.VisualSizeMultiplier;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private void ApplyTopDownOrientation()
        {
            // Art has nose pointing up; parent forward is +Z at yaw 0.
            transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        }
    }
}
