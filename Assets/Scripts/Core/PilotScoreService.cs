using F89.Enemies;
using F89.LandCombat;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Career scoring from UR destroy point values (catalog) and per-level kill folders.</summary>
    public static class PilotScoreService
    {
        public static int GetDestroyPointValue(VehicleUnitDefinition definition)
        {
            if (definition == null || !definition.IsHostile)
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

        /// <summary>Sum of point values for every UR vehicle and troop credited in kill folders.</summary>
        public static int ComputeTotalDestroyScore(CharacterSaveData save)
        {
            if (save == null)
            {
                return 0;
            }

            var total = 0;
            if (save.UrVehicleKillsByLevel != null)
            {
                for (var level = 1; level <= UrKillCredit.LevelCount; level++)
                {
                    var index = UrKillCredit.LevelToIndex(level);
                    if (index < save.UrVehicleKillsByLevel.Length)
                    {
                        total += save.UrVehicleKillsByLevel[index] * GetUrVehiclePointValue(level);
                    }
                }
            }

            if (save.UrTroopKillsByLevel != null)
            {
                for (var level = 1; level <= UrKillCredit.LevelCount; level++)
                {
                    var index = UrKillCredit.LevelToIndex(level);
                    if (index < save.UrTroopKillsByLevel.Length)
                    {
                        total += save.UrTroopKillsByLevel[index] * GetUrTroopPointValue(level);
                    }
                }
            }

            return total;
        }
    }
}
