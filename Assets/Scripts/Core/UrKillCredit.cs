using F89.Enemies;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Credits UR vehicle/troop destroys to character kill folders by catalog level.</summary>
    public static class UrKillCredit
    {
        public const int LevelCount = 10;

        public static void RegisterDestroy(VehicleUnitDefinition definition)
        {
            if (definition == null || !definition.IsHostile)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureUrKillArrays(save);
            var index = LevelToIndex(definition.vehicleLevel);
            var points = PilotScoreService.GetDestroyPointValue(definition);
            if (definition.isTroop)
            {
                save.UrTroopKillsByLevel[index]++;
                save.EnemyTroopsKilled = Sum(save.UrTroopKillsByLevel);
            }
            else
            {
                save.UrVehicleKillsByLevel[index]++;
                save.EnemyVehiclesKilled = Sum(save.UrVehicleKillsByLevel);
            }

            MissionScoreState.AddPoints(points);
            CharacterSaveRepository.ReconcileTotalScore(save);
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static void RegisterUrTroopKillByLevel(int level)
        {
            if (CharacterSessionState.ActiveSave == null)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            CharacterSaveRepository.EnsureUrKillArrays(save);
            var index = LevelToIndex(level);
            var points = PilotScoreService.GetUrTroopPointValue(level);
            save.UrTroopKillsByLevel[index]++;
            save.EnemyTroopsKilled = Sum(save.UrTroopKillsByLevel);
            MissionScoreState.AddPoints(points);
            CharacterSaveRepository.ReconcileTotalScore(save);
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static int GetVehicleKillsAtLevel(CharacterSaveData save, int level)
        {
            if (save?.UrVehicleKillsByLevel == null)
            {
                return 0;
            }

            var index = LevelToIndex(level);
            return index < save.UrVehicleKillsByLevel.Length ? save.UrVehicleKillsByLevel[index] : 0;
        }

        public static int GetTroopKillsAtLevel(CharacterSaveData save, int level)
        {
            if (save?.UrTroopKillsByLevel == null)
            {
                return 0;
            }

            var index = LevelToIndex(level);
            return index < save.UrTroopKillsByLevel.Length ? save.UrTroopKillsByLevel[index] : 0;
        }

        public static int LevelToIndex(int level)
        {
            return Mathf.Clamp(level, 1, LevelCount) - 1;
        }

        public static int Sum(int[] values)
        {
            if (values == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < values.Length; i++)
            {
                total += values[i];
            }

            return total;
        }
    }
}
