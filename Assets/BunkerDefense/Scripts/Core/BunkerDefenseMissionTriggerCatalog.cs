using System;
using System.Collections.Generic;

namespace SaveAntarctica.BunkerDefense.Core
{
    /// <summary>
    /// Same selected F-89 missions as Tower Defense. Bunker Defense is the replacement minigame.
    /// Accepts site codes (OP-01) and campaign mission numbers (5, 05, Mission 05).
    /// </summary>
    public static class BunkerDefenseMissionTriggerCatalog
    {
        private static readonly HashSet<string> DefenseMissionIds = new(StringComparer.OrdinalIgnoreCase);

        static BunkerDefenseMissionTriggerCatalog()
        {
            for (var map = 1; map <= DefenseSiteCatalog.MissionCount; map++)
            {
                RegisterMission(DefenseSiteCatalog.GetSiteCode(map));
                var missionNumber = DefenseSiteCatalog.GetF89MissionNumber(map);
                RegisterMission(missionNumber.ToString());
                RegisterMission(missionNumber.ToString("00"));
                RegisterMission($"Mission {missionNumber:00}");
            }

            // First campaign bunker defense runs before the OP/STN assault missions.
            RegisterMission("3");
            RegisterMission("03");
            RegisterMission("Mission 03");
        }

        public static void RegisterMission(string missionId)
        {
            if (!string.IsNullOrWhiteSpace(missionId))
            {
                DefenseMissionIds.Add(missionId.Trim());
            }
        }

        public static void RegisterMissions(IEnumerable<string> missionIds)
        {
            if (missionIds == null)
            {
                return;
            }

            foreach (var missionId in missionIds)
            {
                RegisterMission(missionId);
            }
        }

        public static bool IsDefenseMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return false;
            }

            var normalized = missionId.Trim();
            return DefenseMissionIds.Contains(normalized) || DefenseSiteCatalog.GetMapNumber(normalized) > 0;
        }

        public static IReadOnlyCollection<string> RegisteredMissionIds => DefenseMissionIds;
    }

    /// <summary>Tower Defense name kept so F-89 can swap the plug-in without a catalog rename.</summary>
    public static class BaseDefenseMissionTriggerCatalog
    {
        public static void RegisterMission(string missionId) =>
            BunkerDefenseMissionTriggerCatalog.RegisterMission(missionId);

        public static void RegisterMissions(IEnumerable<string> missionIds) =>
            BunkerDefenseMissionTriggerCatalog.RegisterMissions(missionIds);

        public static bool IsDefenseMission(string missionId) =>
            BunkerDefenseMissionTriggerCatalog.IsDefenseMission(missionId);

        public static IReadOnlyCollection<string> RegisteredMissionIds =>
            BunkerDefenseMissionTriggerCatalog.RegisteredMissionIds;
    }
}
