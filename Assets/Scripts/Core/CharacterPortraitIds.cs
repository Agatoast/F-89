namespace F89.Core
{
    public static class CharacterPortraitIds
    {
        public const string Custom = "custom";

        public static readonly string[] MalePresets = { "male_1", "male_2", "male_3" };
        public static readonly string[] FemalePresets = { "female_1", "female_2", "female_3" };

        public static bool IsPreset(string portraitId)
        {
            if (string.IsNullOrEmpty(portraitId) || portraitId == Custom)
            {
                return false;
            }

            foreach (var preset in MalePresets)
            {
                if (preset == portraitId)
                {
                    return true;
                }
            }

            foreach (var preset in FemalePresets)
            {
                if (preset == portraitId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
