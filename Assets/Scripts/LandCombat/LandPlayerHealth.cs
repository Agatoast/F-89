using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandPlayerHealth : MonoBehaviour, ILandDamageable
    {
        private float currentHealth;

        public bool IsAlive => currentHealth > 0f;
        public float MaxHealth => LandGameConstants.PlayerMaxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
        public float HealthPercent => HealthNormalized * 100f;

        private void Awake()
        {
            currentHealth = MaxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        }

        public void RestoreFullHealth()
        {
            currentHealth = MaxHealth;
        }
    }
}
