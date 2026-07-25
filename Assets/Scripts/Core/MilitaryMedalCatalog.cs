namespace F89.Core
{
    public static class MilitaryMedalIds
    {
        public const string FruitSalad = "fruit_salad";
        public const string None = "none";

        public static string DefaultForNewCharacter => FruitSalad;
    }

    public readonly struct MilitaryMedalDefinition
    {
        public MilitaryMedalDefinition(string id, int value, string resourceName)
        {
            Id = id;
            Value = value;
            ResourceName = resourceName;
        }

        public string Id { get; }
        public int Value { get; }
        public string ResourceName { get; }
    }

    public static class MilitaryMedalCatalog
    {
        private static readonly MilitaryMedalDefinition[] Medals =
        {
            new MilitaryMedalDefinition(MilitaryMedalIds.FruitSalad, 1, "fruit_salad_medal")
        };

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

        public static string NormalizeAwardId(string awardId)
        {
            if (string.IsNullOrWhiteSpace(awardId) || awardId == MilitaryMedalIds.None)
            {
                return MilitaryMedalIds.DefaultForNewCharacter;
            }

            return awardId.Trim();
        }
    }
}
