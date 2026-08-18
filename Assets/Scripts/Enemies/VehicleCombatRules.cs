using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Hit rolls for vehicle/troop weapons. Ground units never auto-hit — they roll Chance to Hit Ground/Air.
    /// </summary>
    public static class VehicleCombatRules
    {
        public static bool RollGroundHit(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return Random.value <= Mathf.Clamp01(definition.chanceToHitGround);
        }

        public static bool RollAirHit(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return Random.value <= Mathf.Clamp01(definition.chanceToHitAir);
        }

        public static bool RollKillOnHit(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return Random.value <= Mathf.Clamp01(definition.chanceToKill);
        }

        public static float RollFireDelaySeconds(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return 1f;
            }

            var min = Mathf.Max(0f, definition.fireRateMinSeconds);
            var max = Mathf.Max(min, definition.fireRateMaxSeconds);
            return Random.Range(min, max);
        }

        /// <summary>Spread first-shot timing across a platoon so units do not volley together.</summary>
        public static float RollInitialFireDelaySeconds(VehicleUnitDefinition definition, int staggerSeed)
        {
            if (definition == null)
            {
                return RollEngagementStaggerSeconds(staggerSeed);
            }

            var min = Mathf.Max(0f, definition.fireRateMinSeconds);
            var max = Mathf.Max(min, definition.fireRateMaxSeconds);
            var window = max + (max - min);
            return min + Seed01(staggerSeed, 37) * window;
        }

        /// <summary>Delay before the first shot after a valid target is acquired.</summary>
        public static float RollEngagementStaggerSeconds(int staggerSeed)
        {
            return Mathf.Lerp(0.25f, 2.75f, Seed01(staggerSeed, 11));
        }

        /// <summary>Retry cadence while scanning for a target.</summary>
        public static float RollTargetScanDelaySeconds(int staggerSeed)
        {
            return Mathf.Lerp(0.45f, 1.6f, Seed01(staggerSeed, 23));
        }

        /// <summary>Post-shot cooldown with per-unit jitter so cycles do not resync.</summary>
        public static float RollFireCycleDelaySeconds(VehicleUnitDefinition definition, int staggerSeed)
        {
            return RollFireDelaySeconds(definition) + Mathf.Lerp(0.12f, 1.1f, Seed01(staggerSeed, 53));
        }

        private static float Seed01(int staggerSeed, int salt)
        {
            var mixed = (staggerSeed * 73856093) ^ (salt * 19349663);
            return (Mathf.Abs(mixed) % 1000) / 1000f;
        }
    }
}
