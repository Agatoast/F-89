using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Outdoor Antarctica exposure: 2 hours continuous time outside freezes the player.
    /// Entering a building, bunker, or the landed plane resets the timer.
    /// </summary>
    public sealed class LandColdExposure : MonoBehaviour
    {
        private float outdoorElapsedSeconds;
        private bool isSheltered;
        private LandShelterZone.ShelterKind currentShelter;
        private LandPlayerHealth health;
        private bool froze;

        public bool IsSheltered => isSheltered;
        public float OutdoorElapsedSeconds => outdoorElapsedSeconds;
        public float RemainingSeconds =>
            Mathf.Max(0f, LandGameConstants.OutdoorExposureLimitSeconds - outdoorElapsedSeconds);
        public float RemainingNormalized =>
            LandGameConstants.OutdoorExposureLimitSeconds > 0f
                ? RemainingSeconds / LandGameConstants.OutdoorExposureLimitSeconds
                : 0f;

        private void Awake()
        {
            health = GetComponent<LandPlayerHealth>();
            outdoorElapsedSeconds = 0f;
        }

        private void Update()
        {
            if (froze || GamePauseController.IsPaused)
            {
                return;
            }

            if (health != null && health.IsUnconscious)
            {
                return;
            }

            isSheltered = LandShelterZone.IsPointInAnyShelter(transform.position, out currentShelter);
            if (isSheltered)
            {
                outdoorElapsedSeconds = 0f;
                return;
            }

            outdoorElapsedSeconds += Time.deltaTime;
            if (outdoorElapsedSeconds < LandGameConstants.OutdoorExposureLimitSeconds)
            {
                return;
            }

            FreezeToDeath();
        }

        private void FreezeToDeath()
        {
            if (froze)
            {
                return;
            }

            froze = true;
            outdoorElapsedSeconds = LandGameConstants.OutdoorExposureLimitSeconds;
            Debug.Log("F-89 Land: Froze to death after 2 hours outdoors.");
            if (health != null)
            {
                health.ForceOutcome(LandDownedOutcome.FrozenToDeath);
            }
        }
    }
}
