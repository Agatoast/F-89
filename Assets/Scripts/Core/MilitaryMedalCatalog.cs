using System.Collections.Generic;

namespace F89.Core
{
    public static class MilitaryMedalIds
    {
        public const string None = "none";

        public static string DefaultForNewCharacter => None;
    }

    public readonly struct MilitaryMedalDefinition
    {
        public MilitaryMedalDefinition(string id, int precedence, string resourceName)
        {
            Id = id;
            Precedence = precedence;
            ResourceName = resourceName;
        }

        public string Id { get; }
        public int Precedence { get; }
        public string ResourceName { get; }
    }

    public static class MilitaryMedalCatalog
    {
        private static readonly MilitaryMedalDefinition[] Medals =
        {
            new MilitaryMedalDefinition(MilitaryRibbonIds.MedalOfHonor, 1, "01_medal_of_honor"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.DistinguishedServiceCross, 2, "02_distinguished_service_cross"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.SilverStar, 3, "03_silver_star"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.PurpleHeart, 4, "04_purple_heart"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.AirMedal, 5, "05_air_medal"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.JointServiceCommendation, 6, "06_joint_service_commendation"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.JointServiceAchievement, 7, "07_joint_service_achievement"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.CombatAction, 8, "08_combat_action"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.PrisonerOfWar, 9, "09_prisoner_of_war"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.GoodConduct, 10, "10_good_conduct"),
            new MilitaryMedalDefinition(MilitaryRibbonIds.AntarcticaService, 11, "11_antarctica_service")
        };

        public static bool HasMedal(string ribbonOrMedalId)
        {
            return TryGetDefinition(ribbonOrMedalId, out _);
        }

        public static bool TryGetDefinition(string medalId, out MilitaryMedalDefinition definition)
        {
            if (string.IsNullOrEmpty(medalId) || medalId == MilitaryMedalIds.None)
            {
                definition = default;
                return false;
            }

            foreach (var medal in Medals)
            {
                if (medal.Id == medalId)
                {
                    definition = medal;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool TryGetDefinitionByPrecedence(int precedence, out MilitaryMedalDefinition definition)
        {
            foreach (var medal in Medals)
            {
                if (medal.Precedence == precedence)
                {
                    definition = medal;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static string GetHighestMedalIdFromEarnedRibbons(IReadOnlyList<string> earnedRibbonIds)
        {
            if (earnedRibbonIds == null || earnedRibbonIds.Count == 0)
            {
                return MilitaryMedalIds.None;
            }

            var bestPrecedence = int.MaxValue;
            string bestMedalId = MilitaryMedalIds.None;

            foreach (var ribbonId in earnedRibbonIds)
            {
                if (!TryGetDefinition(ribbonId, out var medal))
                {
                    continue;
                }

                if (medal.Precedence >= bestPrecedence)
                {
                    continue;
                }

                bestPrecedence = medal.Precedence;
                bestMedalId = medal.Id;
            }

            return bestMedalId;
        }

        public static string NormalizeAwardId(string awardId)
        {
            if (string.IsNullOrWhiteSpace(awardId) || awardId == MilitaryMedalIds.None)
            {
                return MilitaryMedalIds.None;
            }

            var trimmed = awardId.Trim();
            return TryGetDefinition(trimmed, out _) ? trimmed : MilitaryMedalIds.None;
        }
    }
}
