using UnityEngine;

namespace F89.Flight
{
    public class AircraftBankVisual : MonoBehaviour
    {
        [SerializeField] private AircraftController controller;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform aircraftMesh;

        private Vector3 baseMeshScale = Vector3.one;
        private Vector3 baseMeshPosition = new Vector3(0f, 0.01f, 0f);
        private float foreshortenVelocity;
        private bool baseScaleCaptured;

        private void Reset()
        {
            visualRoot = transform;
        }

        private void Awake()
        {
            EnsureReferences();
            CaptureBaseMeshTransformIfNeeded();
        }

        public void Configure(AircraftController aircraftController, Transform pivot, Transform meshTransform)
        {
            controller = aircraftController;
            visualRoot = pivot;
            aircraftMesh = meshTransform;
            CaptureBaseMeshTransform(force: true);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void EnsureReferences()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (aircraftMesh == null && visualRoot != null)
            {
                var visual = visualRoot.Find("AircraftVisual");
                if (visual != null)
                {
                    aircraftMesh = visual;
                }
            }

            if (controller == null)
            {
                controller = GetComponentInParent<AircraftController>();
            }
        }

        private void CaptureBaseMeshTransformIfNeeded()
        {
            if (!baseScaleCaptured)
            {
                CaptureBaseMeshTransform(force: false);
            }
        }

        private void CaptureBaseMeshTransform(bool force)
        {
            EnsureReferences();
            if (!force && baseScaleCaptured)
            {
                return;
            }

            if (aircraftMesh == null)
            {
                return;
            }

            baseMeshScale = aircraftMesh.localScale;
            baseMeshPosition = aircraftMesh.localPosition;
            baseScaleCaptured = true;
        }

        private void LateUpdate()
        {
            CaptureBaseMeshTransformIfNeeded();
            if (controller == null || controller.Profile == null || aircraftMesh == null)
            {
                return;
            }

            var profile = controller.Profile;
            var input = controller.GetComponent<F89.Controls.PlayerAircraftInput>();
            var turnInput = input != null ? input.Current.turn : 0f;

            // Top-down flat mesh: real roll clips into the ground. Fake turn depth with foreshortening.
            var targetForeshorten = Mathf.Abs(turnInput) * profile.turnForeshortenAtFullInput;
            var foreshorten = Mathf.SmoothDamp(
                GetCurrentForeshorten(),
                targetForeshorten,
                ref foreshortenVelocity,
                profile.bankSmoothTime);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }

            var widthScale = 1f - foreshorten;
            aircraftMesh.localScale = new Vector3(
                baseMeshScale.x * widthScale,
                baseMeshScale.y,
                baseMeshScale.z);
            aircraftMesh.localPosition = baseMeshPosition + new Vector3(turnInput * profile.turnLateralShift, 0f, 0f);
        }

        private float GetCurrentForeshorten()
        {
            if (baseMeshScale.x <= 0f)
            {
                return 0f;
            }

            return 1f - aircraftMesh.localScale.x / baseMeshScale.x;
        }
    }
}
