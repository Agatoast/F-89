using F89.Enemies;
using F89.LandCombat;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Level-based destroy point values from the vehicle/troop catalog.</summary>
    public static class PilotScoreService
    {
        public static int GetDestroyPointValue(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return definition.pointValue > 0
                ? definition.pointValue
                : Mathf.Clamp(definition.vehicleLevel, 1, UrKillCredit.LevelCount);
        }

        public static int GetUrVehiclePointValue(int level)
        {
            level = LandUrEnemyStats.ClampLevel(level);
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var definition = catalog.GetUrVehicleByLevel(level);
            return definition != null ? GetDestroyPointValue(definition) : level;
        }

        public static int GetUrTroopPointValue(int level)
        {
            level = LandUrEnemyStats.ClampLevel(level);
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var definition = catalog.GetUrTroopByLevel(level);
            return definition != null ? GetDestroyPointValue(definition) : level;
        }
    }
}
