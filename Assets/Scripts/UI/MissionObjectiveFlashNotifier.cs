using UnityEngine;

namespace F89.UI
{
    /// <summary>Flashes objective-complete text under the missile-acquisition HUD band.</summary>
    public static class MissionObjectiveFlashNotifier
    {
        private static string flashText = string.Empty;
        private static float flashUntilUnscaled;

        public static void FlashPrimaryEliminated() =>
            Flash("All Primary Targets Eliminated");

        public static void FlashSecondaryEliminated() =>
            Flash("All Secondary Targets Eliminated");

        public static void Flash(string message, float durationSeconds = 5f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            flashText = message.Trim();
            flashUntilUnscaled = Time.unscaledTime + durationSeconds;
        }

        public static void Clear()
        {
            flashText = string.Empty;
            flashUntilUnscaled = 0f;
        }

        public static bool TryGetActiveFlash(out string message)
        {
            message = string.Empty;
            if (string.IsNullOrEmpty(flashText) || Time.unscaledTime >= flashUntilUnscaled)
            {
                flashText = string.Empty;
                return false;
            }

            message = flashText;
            return true;
        }

        public static bool ShouldBlinkVisible(float blinkPeriodSeconds = 0.55f)
        {
            if (!TryGetActiveFlash(out _))
            {
                return false;
            }

            return (Mathf.FloorToInt(Time.unscaledTime / blinkPeriodSeconds) % 2) == 0;
        }
    }
}
