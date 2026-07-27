using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Current player HP for the active mission, preserved across ground and bunker scenes.</summary>
    public static class LandMissionHealthState
    {
        private static bool hasHealth;
        private static float currentHealth;

        public static float GetOrInitialize(float maxHealth)
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            if (!hasHealth)
            {
                hasHealth = true;
                currentHealth = maxHealth;
            }

            return Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        public static void Set(float health, float maxHealth)
        {
            hasHealth = true;
            currentHealth = Mathf.Clamp(health, 0f, Mathf.Max(1f, maxHealth));
        }

        /// <summary>Called only once the mission has ended (carrier completion or terminal outcome).</summary>
        public static void Clear()
        {
            hasHealth = false;
            currentHealth = 0f;
        }
    }
}
