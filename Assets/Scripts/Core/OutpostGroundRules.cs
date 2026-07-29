namespace F89.Core
{
    /// <summary>
    /// Shared sizing and spacing rules for outpost buildings, vehicles, and troops
    /// in the flight / bombing world.
    /// </summary>
    public static class OutpostGroundRules
    {
        /// <summary>Map / world footprint for vehicles, troops, and building bases.</summary>
        public const float UnitFootprintTacs = 1f;

        /// <summary>Minimum edge-to-edge gap between any solid ground units/buildings.</summary>
        public const float MinSeparationTics = 0.1f;

        /// <summary>Minimum center distance between type-3 buildings.</summary>
        public const float MinType3SeparationTacs = 10f;

        public const int Type3CountMin = 1;
        public const int Type3CountMax = 2;

        public const int OtherBuildingCountMin = 30;
        public const int OtherBuildingCountMax = 40;

        /// <summary>
        /// Vehicles and troops must stay outside this radius from the building-cluster
        /// center (bunker when present, otherwise outpost center).
        /// </summary>
        public const float BuildingClusterKeepOutRadiusTacs = 14f;

        /// <summary>Default cruise height for helicopters that fly over ground units.</summary>
        public const float HelicopterCruiseAltitudeTacs = 2f;

        public static float FootprintWorld(float ticSizeWorldUnits = 1f)
        {
            return TacScale.TacsToWorld(UnitFootprintTacs, ticSizeWorldUnits);
        }

        /// <summary>
        /// Minimum center-to-center distance for two 1-tac solids with a 0.1-tic gap.
        /// </summary>
        public static float MinCenterSeparationWorld(float ticSizeWorldUnits = 1f)
        {
            return FootprintWorld(ticSizeWorldUnits)
                + TacScale.TicsToWorld(MinSeparationTics, ticSizeWorldUnits);
        }

        public static float KeepOutRadiusWorld(float ticSizeWorldUnits = 1f)
        {
            return TacScale.TacsToWorld(BuildingClusterKeepOutRadiusTacs, ticSizeWorldUnits);
        }
    }
}
