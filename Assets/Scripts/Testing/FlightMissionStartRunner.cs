using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Testing
{
    /// <summary>
    /// Safety net when scene bootstrap order leaves the player unplaced before Start runs.
    /// </summary>
    [DefaultExecutionOrder(450)]
    public sealed class FlightMissionStartRunner : MonoBehaviour
    {
        private const float RetryWindowSeconds = 4f;

        private float sceneStartTime;
        private float lastRetryTime;
        private int retryCount;

        public static void EnsureOn(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<FlightMissionStartRunner>() == null)
            {
                player.AddComponent<FlightMissionStartRunner>();
            }
        }

        private void OnEnable()
        {
            sceneStartTime = Time.unscaledTime;
        }

        private void Start()
        {
            TryBootstrap();
        }

        private void Update()
        {
            if (AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsRunwayDeckMenuVisible
                || FlightMissionStartBootstrap.SortieStartupComplete
                || Time.unscaledTime - sceneStartTime > RetryWindowSeconds)
            {
                return;
            }

            if (FlightGroundReturnService.ShouldApplySortieReturn()
                && !AircraftLandingController.IsTakeoffActive
                && !AircraftLandingController.IsRunwayDeckMenuVisible)
            {
                TryBootstrap();
                return;
            }

            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return;
            }

            if (!FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                && !FlightMissionLaunchState.HasPersistedFreshCarrierLaunch()
                && !FlightMissionLaunchState.HasPendingCarrierLaunch
                && !FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                return;
            }

            // Retry a few times in case bootstrap ran before the carrier existed in-scene.
            if (retryCount >= 8)
            {
                return;
            }

            if (Time.unscaledTime - lastRetryTime < 0.25f)
            {
                return;
            }

            lastRetryTime = Time.unscaledTime;
            retryCount++;
            TryBootstrap();
        }

        private void TryBootstrap()
        {
            var aircraft = GetComponent<AircraftController>();
            if (aircraft == null)
            {
                return;
            }

            if (!FlightMissionStartBootstrap.SortieStartupComplete)
            {
                FlightMissionStartBootstrap.Apply(aircraft, gameObject);
                return;
            }

            FlightMissionStartBootstrap.TryForceCarrierDeckTakeoff(aircraft, gameObject);
        }
    }
}
