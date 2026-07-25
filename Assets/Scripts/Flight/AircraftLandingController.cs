using F89.Controls;
using F89.UI;
using UnityEngine;

namespace F89.Flight
{
    public class AircraftLandingController : MonoBehaviour
    {
        public const float ShrinkDurationSeconds = 5f;
        public const float TargetVisualScale = 0.1f;

        private static AircraftLandingController activeInstance;

        private AircraftController aircraft;
        private PlayerAircraftInput input;
        private Rigidbody body;
        private Transform visualPivot;
        private Vector3 initialVisualScale = Vector3.one;
        private float sequenceStartTime;
        private float currentVisualScale = 1f;
        private bool sequenceActive;
        private bool landingComplete;

        public static bool IsLandingActive => activeInstance != null && activeInstance.sequenceActive;
        public static bool IsLandingComplete => activeInstance != null && activeInstance.landingComplete;
        public static float VisualScaleMultiplier => activeInstance?.currentVisualScale ?? 1f;

        private void Awake()
        {
            aircraft = GetComponent<AircraftController>();
            input = GetComponent<PlayerAircraftInput>();
            body = GetComponent<Rigidbody>();
            visualPivot = transform.Find("VisualPivot");
            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        public void BeginLanding()
        {
            if (sequenceActive || landingComplete)
            {
                return;
            }

            sequenceActive = true;
            activeInstance = this;
            sequenceStartTime = Time.time;

            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }

            var autopilot = GetComponent<AutopilotController>();
            autopilot?.DisengageAutopilot("Landing.");

            if (input != null)
            {
                input.enabled = false;
            }
        }

        private void Update()
        {
            if (!sequenceActive)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            currentVisualScale = Mathf.Lerp(1f, TargetVisualScale, progress);

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * currentVisualScale;
            }

            if (progress >= 1f)
            {
                CompleteLanding();
            }
        }

        private void FixedUpdate()
        {
            if (!sequenceActive || aircraft == null || body == null)
            {
                return;
            }

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            body.linearVelocity = forward.normalized * aircraft.CurrentSpeed;
        }

        private void CompleteLanding()
        {
            sequenceActive = false;
            landingComplete = true;
            currentVisualScale = TargetVisualScale;

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            if (aircraft != null)
            {
                aircraft.SetLandingLocked(true);
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }

            Time.timeScale = 0f;
        }
    }
}
