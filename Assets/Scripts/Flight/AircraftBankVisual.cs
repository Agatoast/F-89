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

        private void Reset()
        {
            visualRoot = transform;
        }

        public void Configure(AircraftController aircraftController, Transform pivot, Transform meshTransform)
        {
            controller = aircraftController;
            visualRoot = pivot;
            aircraftMesh = meshTransform;

            if (aircraftMesh != null)
            {
                baseMeshScale = aircraftMesh.localScale;
                baseMeshPosition = aircraftMesh.localPosition;
            }

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
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
