namespace F89.Core
{
    public static class MilitaryRibbonIds
    {
        public const string MedalOfHonor = "medal_of_honor";
        public const string DistinguishedServiceCross = "distinguished_service_cross";
        public const string SilverStar = "silver_star";
        public const string PurpleHeart = "purple_heart";
        public const string AirMedal = "air_medal";
        public const string JointServiceCommendation = "joint_service_commendation";
        public const string JointServiceAchievement = "joint_service_achievement";
        public const string CombatAction = "combat_action";
        public const string PrisonerOfWar = "prisoner_of_war";
        public const string GoodConduct = "good_conduct";
        public const string AntarcticaService = "antarctica_service";
        public const string FruitSalad = "fruit_salad";
    }

    public readonly struct MilitaryRibbonDefinition
    {
        public MilitaryRibbonDefinition(string id, int precedence, string resourceName, string displayName)
        {
            Id = id;
            Precedence = precedence;
            ResourceName = resourceName;
            DisplayName = displayName;
        }

        public string Id { get; }
        public int Precedence { get; }
        public string ResourceName { get; }
        public string DisplayName { get; }
    }

    public static class MilitaryRibbonCatalog
    {
        private static readonly MilitaryRibbonDefinition[] Ribbons =
        {
            new MilitaryRibbonDefinition(MilitaryRibbonIds.MedalOfHonor, 1, "01_medal_of_honor", "Medal of Honor"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.DistinguishedServiceCross, 2, "02_distinguished_service_cross", "Distinguished Service Cross"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.SilverStar, 3, "03_silver_star", "Silver Star"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.PurpleHeart, 4, "04_purple_heart", "Purple Heart"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.AirMedal, 5, "05_air_medal", "Air Medal"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.JointServiceCommendation, 6, "06_joint_service_commendation", "Joint Services Commendation"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.JointServiceAchievement, 7, "07_joint_service_achievement", "Joint Services Achievement"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.CombatAction, 8, "08_combat_action", "Combat Action"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.PrisonerOfWar, 9, "09_prisoner_of_war", "Prisoner of War"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.GoodConduct, 10, "10_good_conduct", "Good Conduct"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.AntarcticaService, 11, "11_antarctica_service", "Antarctica Service"),
            new MilitaryRibbonDefinition(MilitaryRibbonIds.FruitSalad, 12, "12_fruit_salad", "Service Ribbon")
        };

        public const int MaxRibbons = 12;
        public const int MaxRibbonsPerRow = 3;
        public const int MaxRows = 4;

        public static bool TryGetDefinition(string ribbonId, out MilitaryRibbonDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(ribbonId))
            {
                definition = default;
                return false;
            }

            foreach (var ribbon in Ribbons)
            {
                if (ribbon.Id == ribbonId)
                {
                    definition = ribbon;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool TryGetDefinitionByPrecedence(int precedence, out MilitaryRibbonDefinition definition)
        {
            foreach (var ribbon in Ribbons)
            {
                if (ribbon.Precedence == precedence)
                {
                    definition = ribbon;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static int ComparePrecedence(string leftId, string rightId)
        {
            var leftPrecedence = TryGetDefinition(leftId, out var left) ? left.Precedence : int.MaxValue;
            var rightPrecedence = TryGetDefinition(rightId, out var right) ? right.Precedence : int.MaxValue;
            return leftPrecedence.CompareTo(rightPrecedence);
        }

        public static string[] GetAllRibbonIds()
        {
            var ids = new string[Ribbons.Length];
            for (var i = 0; i < Ribbons.Length; i++)
            {
                ids[i] = Ribbons[i].Id;
            }

            return ids;
        }

        /// <summary>One-time campaign ribbons — no repeat grants.</summary>
        public static bool IsSingleAwardOnly(string ribbonId) =>
            ribbonId == MilitaryRibbonIds.CombatAction
            || ribbonId == MilitaryRibbonIds.AntarcticaService
            || ribbonId == MilitaryRibbonIds.GoodConduct;

        /// <summary>Medals 1, 2, 3, 5, 6, 7 — repeat awards show bronze/silver/gold star devices.</summary>
        public static bool SupportsAwardDevices(string ribbonId)
        {
            if (!TryGetDefinition(ribbonId, out var definition))
            {
                return false;
            }

            return definition.Precedence is 1 or 2 or 3 or 5 or 6 or 7;
        }
    }
}
