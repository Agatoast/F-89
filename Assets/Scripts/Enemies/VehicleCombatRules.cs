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
    }
}
