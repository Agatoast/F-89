namespace F89.Core
{
    /// <summary>Air-strike objectives required before a campaign sortie counts as complete.</summary>
    public static class OutpostPrimaryObjective
    {
        public const string BunkerBuildingLabel = "Bunker-0";
        public const string RunwayTowerLabel = "RunwayTower-0";

        public static bool AreAirObjectivesDestroyed(string outpostNameOrSiteCode)
        {
            if (string.IsNullOrWhiteSpace(outpostNameOrSiteCode))
            {
                return false;
            }

            if (AreAirPairDestroyed(outpostNameOrSiteCode))
            {
                return true;
            }

            // Accept either BaseName or SiteCode persist keys.
            if (CampaignMapLayoutState.TryGetSite(outpostNameOrSiteCode, out var site) && site != null)
            {
                if (!string.IsNullOrWhiteSpace(site.SiteCode)
                    && AreAirPairDestroyed(site.SiteCode))
                {
                    return true;
                }

                var label = CampaignMapLayoutState.NormalizeSiteName(site.Label);
                if (!string.IsNullOrWhiteSpace(label) && AreAirPairDestroyed(label))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreAirPairDestroyed(string key)
        {
            return AntarcticaOutpostState.IsTargetDestroyed(key, BunkerBuildingLabel)
                && AntarcticaOutpostState.IsTargetDestroyed(key, RunwayTowerLabel);
        }

        public static bool IsMissionHostileBuilding(OutpostBuildingType type, string targetLabel)
        {
            return IsMissionHostileBuilding(type, targetLabel, baseSite: null);
        }

        public static bool IsMissionHostileBuilding(
            OutpostBuildingType type,
            string targetLabel,
            AntarcticaBase baseSite)
        {
            if (IsFriendlyBaseSite(baseSite))
            {
                return false;
            }

            if (type == OutpostBuildingType.Bunker)
            {
                return true;
            }

            return string.Equals(targetLabel, RunwayTowerLabel, System.StringComparison.Ordinal);
        }

        public static bool IsMissionHostileTarget(F89.Weapons.LockableTarget target)
        {
            if (target == null)
            {
                return false;
            }

            var building = target.GetComponent<OutpostBuilding>();
            if (building == null)
            {
                return false;
            }

            return IsMissionHostileBuilding(
                building.BuildingType,
                target.TargetLabel,
                target.GetComponentInParent<AntarcticaBase>());
        }

        public static F89.Weapons.TargetAffiliation AffiliationForBuilding(
            OutpostBuildingType type,
            string targetLabel)
        {
            return AffiliationForBuilding(type, targetLabel, baseSite: null);
        }

        public static F89.Weapons.TargetAffiliation AffiliationForBuilding(
            OutpostBuildingType type,
            string targetLabel,
            AntarcticaBase baseSite)
        {
            return IsMissionHostileBuilding(type, targetLabel, baseSite)
                ? F89.Weapons.TargetAffiliation.Hostile
                : F89.Weapons.TargetAffiliation.Neutral;
        }

        private static bool IsFriendlyBaseSite(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return false;
            }

            if (baseSite.Control == BaseControl.Friendly)
            {
                return true;
            }

            if (AntarcticaOutpostState.IsFriendlyOccupied(baseSite.BaseName))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(baseSite.SiteCode)
                && AntarcticaOutpostState.IsFriendlyOccupied(baseSite.SiteCode);
        }
    }
}
