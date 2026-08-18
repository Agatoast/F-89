using F89.LandCombat;

namespace F89.Core
{
    /// <summary>Primary air/ground force counts for catalog missions (from MissionCatalog spawn table).</summary>
    public readonly struct CampaignMissionPrimarySpawnPlan
    {
        public CampaignMissionPrimarySpawnPlan(int infantryCount, int vehicleCount, int vehicleLevel, int level7Count)
        {
            InfantryCount = infantryCount;
            VehicleCount = vehicleCount;
            VehicleLevel = vehicleLevel;
            Level7Count = level7Count;
        }

        public int InfantryCount { get; }
        public int VehicleCount { get; }
        public int VehicleLevel { get; }
        public int Level7Count { get; }
    }

    public static class CampaignMissionPrimarySpawnCatalog
    {
        // (col1_count, col1_level, col2_level7_count) — col1 level 7 upgraded to 8.
        private static readonly int[] Col1Count =
        {
            0, 5, 6, 5, 6, 8, 9, 10, 5, 6, 7, 8, 9, 10, 6, 7, 8, 9, 10, 6, 7, 8, 9, 10, 5, 6, 7, 8, 9, 10,
            5, 6, 7, 8, 9, 10, 5, 6, 7, 8, 9, 10, 5, 6, 7, 8, 9, 10, 5, 6, 7, 8, 9, 10
        };

        private static readonly int[] Col1Level =
        {
            0, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10
        };

        private static readonly int[] Col2Level7Count =
        {
            0, 1, 2, 2, 4, 5, 1, 2, 3, 1, 2, 3, 10, 3, 1, 3, 1, 2, 3, 1, 2, 3, 10, 1, 3, 1, 2, 3, 2, 1,
            2, 3, 1, 2, 3, 1, 10, 1, 3, 2, 1, 2, 3, 1, 2, 3, 1, 3, 1, 3, 1, 2, 3, 10
        };

        public static bool TryGetPlan(int missionNumber, out CampaignMissionPrimarySpawnPlan plan)
        {
            plan = default;
            if (missionNumber <= 0 || missionNumber >= Col1Count.Length)
            {
                return false;
            }

            plan = new CampaignMissionPrimarySpawnPlan(
                infantryCount: 3,
                vehicleCount: Col1Count[missionNumber],
                vehicleLevel: Col1Level[missionNumber],
                level7Count: Col2Level7Count[missionNumber]);
            return true;
        }

        /// <summary>Uniform infantry level for all troops on a catalog mission.</summary>
        public static int GetInfantryLevelForMission(int missionNumber) =>
            LandUrEnemyStats.ScaleMissionTroopLevel(GetDesignInfantryLevelForMission(missionNumber));

        private static int GetDesignInfantryLevelForMission(int missionNumber)
        {
            if (missionNumber >= 50)
            {
                return 10;
            }

            if (missionNumber >= 46)
            {
                return 9;
            }

            if (missionNumber >= 40)
            {
                return 8;
            }

            if (missionNumber >= 35)
            {
                return 7;
            }

            if (missionNumber >= 28)
            {
                return 6;
            }

            if (missionNumber >= 22)
            {
                return 5;
            }

            if (missionNumber >= 15)
            {
                return 4;
            }

            if (missionNumber >= 8)
            {
                return 3;
            }

            if (missionNumber >= 4)
            {
                return 2;
            }

            return 1;
        }
    }
}
