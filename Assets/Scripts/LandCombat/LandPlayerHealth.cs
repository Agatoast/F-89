using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandPlayerHealth : MonoBehaviour, ILandDamageable
    {
        private LandPlayerAttributes attributes;
        private float currentHealth;
        private bool unconscious;
        private LandDownedOutcome downedOutcome;

        public bool IsAlive => !unconscious && currentHealth > 0f;
        public bool IsUnconscious => unconscious;
        public LandDownedOutcome DownedOutcome => downedOutcome;
        public float MaxHealth => attributes != null
            ? attributes.MaxHitPoints
            : LandGameConstants.DefaultMaxHitPoints;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
        public float HealthPercent => HealthNormalized * 100f;

        /// <summary>Fired once when HP first reaches 0 and an outcome is rolled.</summary>
        public event System.Action<LandDownedOutcome> Unconscious;

        private void Awake()
        {
            attributes = GetComponent<LandPlayerAttributes>();
            currentHealth = MaxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (unconscious || amount <= 0f)
            {
                return;
            }

            var resistance = attributes != null ? attributes.EffectiveDamageResistance : 0;
            var afterDr = Mathf.Max(0f, amount - resistance);
            if (afterDr <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - afterDr);
            if (currentHealth <= 0f)
            {
                BecomeUnconscious();
            }
        }

        public void Heal(float amount)
        {
            if (unconscious || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        }

        public void RestoreFullHealth()
        {
            unconscious = false;
            downedOutcome = LandDownedOutcome.None;
            currentHealth = MaxHealth;
        }

        /// <summary>Forces unconsciousness with a fixed outcome (e.g. frozen to death).</summary>
        public void ForceOutcome(LandDownedOutcome outcome)
        {
            if (unconscious || outcome == LandDownedOutcome.None)
            {
                return;
            }

            unconscious = true;
            currentHealth = 0f;
            downedOutcome = outcome;
            Unconscious?.Invoke(downedOutcome);
            Debug.Log(
                $"F-89 Land: Unconscious → {downedOutcome} ({LandDownedResolver.GetPendingScreenName(downedOutcome)}).");
        }

        private void BecomeUnconscious()
        {
            if (unconscious)
            {
                return;
            }

            unconscious = true;
            currentHealth = 0f;
            downedOutcome = LandDownedResolver.RollOutcome();
            Unconscious?.Invoke(downedOutcome);
            Debug.Log(
                $"F-89 Land: Unconscious → {downedOutcome} ({LandDownedResolver.GetPendingScreenName(downedOutcome)}).");
        }
    }
}
