namespace F89.Core
{
    /// <summary>Air-strike objectives required before a campaign sortie counts as complete.</summary>
    public static class OutpostPrimaryObjective
    {
        public const string BunkerBuildingLabel = "Bunker-0";
        public const string RunwayTowerLabel = "RunwayTower-0";

        public static bool AreAirObjectivesDestroyed(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            return AntarcticaOutpostState.IsTargetDestroyed(outpostName, BunkerBuildingLabel)
                && AntarcticaOutpostState.IsTargetDestroyed(outpostName, RunwayTowerLabel);
        }

        public static bool IsMissionHostileBuilding(OutpostBuildingType type, string targetLabel)
        {
            if (type == OutpostBuildingType.Bunker)
            {
                return true;
            }

            return string.Equals(targetLabel, RunwayTowerLabel, System.StringComparison.Ordinal);
        }

        public static F89.Weapons.TargetAffiliation AffiliationForBuilding(
            OutpostBuildingType type,
            string targetLabel)
        {
            return IsMissionHostileBuilding(type, targetLabel)
                ? F89.Weapons.TargetAffiliation.Hostile
                : F89.Weapons.TargetAffiliation.Neutral;
        }
    }

}
