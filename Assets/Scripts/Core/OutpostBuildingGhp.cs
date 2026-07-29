namespace F89.Core
{
    public static class OutpostBuildingGhp
    {
        public const int Type1 = 6;
        public const int Type2 = 6;
        public const int Type3 = 7;
        public const int Bunker = 12;

        public static int ForType(OutpostBuildingType type)
        {
            return type switch
            {
                OutpostBuildingType.Type1 => Type1,
                OutpostBuildingType.Type2 => Type2,
                OutpostBuildingType.Type3 => Type3,
                OutpostBuildingType.Bunker => Bunker,
                _ => Type1
            };
        }
    }
}
