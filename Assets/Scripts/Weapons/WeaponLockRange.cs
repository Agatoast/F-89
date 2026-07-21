using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    public static class WeaponLockRange
    {
        public static float ToWorldUnits(float rangeMiles, WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            return WorldMapConfig.RangeMilesToWorldUnits(rangeMiles, worldMap, ticSizeWorldUnits);
        }

        public static bool IsWithinRange(
            Vector3 origin,
            Vector3 targetPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            var rangeWorld = ToWorldUnits(rangeMiles, worldMap, ticSizeWorldUnits);
            if (rangeWorld <= 0f)
            {
                rangeWorld = rangeMiles * 20f * ticSizeWorldUnits;
            }

            var delta = targetPosition - origin;
            delta.y = 0f;
            return delta.sqrMagnitude <= rangeWorld * rangeWorld;
        }
    }
}
