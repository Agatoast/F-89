using UnityEngine;

namespace F89.Flight
{
    /// <summary>Starts crash visuals and forced crash landing when player GHP is depleted.</summary>
    public sealed class PlayerAircraftCrashController : MonoBehaviour
    {
        private bool crashStarted;

        public static bool IsCrashActive { get; private set; }

        public static void ClearActiveState()
        {
            IsCrashActive = false;
        }

        public void BeginCrash(string causeWeaponName)
        {
            if (crashStarted)
            {
                return;
            }

            crashStarted = true;
            IsCrashActive = true;
            Debug.LogWarning($"F-89: PLAYER AIRCRAFT CRASHING — structural failure from {causeWeaponName}.");

            var visual = GetComponent<AircraftCrashDamageVisual>();
            if (visual == null)
            {
                visual = gameObject.AddComponent<AircraftCrashDamageVisual>();
            }

            visual.Begin();

            var landing = GetComponent<AircraftLandingController>()
                ?? gameObject.AddComponent<AircraftLandingController>();
            landing.BeginCrashLanding();
        }

        private void OnDestroy()
        {
            if (crashStarted)
            {
                IsCrashActive = false;
            }
        }
    }
}
