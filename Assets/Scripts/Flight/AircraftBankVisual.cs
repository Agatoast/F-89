using UnityEngine;

namespace F89.Flight
{
    public class AircraftBankVisual : MonoBehaviour
    {
        private const float UnityPlaneHalfExtent = 5f;

        [SerializeField] private AircraftController controller;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform aircraftMesh;

        private Vector3 basePivotPosition = Vector3.zero;
        private Vector3 baseMeshScale = Vector3.one;
        private Vector3 baseMeshPosition = new Vector3(0f, 0.01f, 0f);
        private Quaternion baseMeshRotation = Quaternion.identity;
        private float currentRoll;
        private float rollVelocity;
        private bool baseTransformCaptured;

        private void Reset()
        {
            visualRoot = transform;
        }

        private void Awake()
        {
            EnsureReferences();
            CaptureBaseTransformsIfNeeded();
        }

        public void Configure(AircraftController aircraftController, Transform pivot, Transform meshTransform)
        {
            controller = aircraftController;
            visualRoot = pivot;
            aircraftMesh = meshTransform;
            CaptureBaseTransforms(force: true);
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

        private void CaptureBaseTransformsIfNeeded()
        {
            if (!baseTransformCaptured)
            {
                CaptureBaseTransforms(force: false);
            }
        }

        private void CaptureBaseTransforms(bool force)
        {
            EnsureReferences();
            if (!force && baseTransformCaptured)
            {
                return;
            }

            if (visualRoot != null)
            {
                basePivotPosition = visualRoot.localPosition;
            }

            if (aircraftMesh == null)
            {
                return;
            }

            baseMeshScale = aircraftMesh.localScale;
            baseMeshPosition = aircraftMesh.localPosition;
            baseMeshRotation = aircraftMesh.localRotation;
            baseTransformCaptured = true;
        }

        private void LateUpdate()
        {
            CaptureBaseTransformsIfNeeded();
            if (controller == null || controller.Profile == null || visualRoot == null || aircraftMesh == null)
            {
                return;
            }

            var profile = controller.Profile;
            var input = controller.GetComponent<F89.Controls.PlayerAircraftInput>();
            var turnInput = input != null ? input.Current.turn : 0f;

            // Roll the board around the nose-tail axis (player forward / local Z).
            // Left turn: right wing up, left wing down.
            var maxRoll = profile.turnForeshortenAtFullInput * 120f;
            var targetRoll = -turnInput * maxRoll;
            currentRoll = Mathf.SmoothDampAngle(
                currentRoll,
                targetRoll,
                ref rollVelocity,
                profile.bankSmoothTime);

            var rollRad = currentRoll * Mathf.Deg2Rad;
            var halfWidth = UnityPlaneHalfExtent * baseMeshScale.x;

            // Rotate around the fuselage axis through the board center.
            visualRoot.localRotation = Quaternion.AngleAxis(currentRoll, Vector3.forward);

            // Keep the lowered wing from clipping through the ground.
            var groundClearance = halfWidth * Mathf.Abs(Mathf.Sin(rollRad));
            visualRoot.localPosition = basePivotPosition + new Vector3(0f, groundClearance, 0f);

            // Top-down foreshortening: wingspan narrows as the board tilts.
            var widthScale = Mathf.Max(0.05f, Mathf.Abs(Mathf.Cos(rollRad)));
            aircraftMesh.localRotation = baseMeshRotation;
            aircraftMesh.localPosition = baseMeshPosition;
            aircraftMesh.localScale = new Vector3(
                baseMeshScale.x * widthScale,
                baseMeshScale.y,
                baseMeshScale.z);
        }
    }
}
