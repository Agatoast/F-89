namespace F89.Core
{
    public static class CharacterLoadoutNavState
    {
        /// <summary>
        /// True when Character Loadout was opened from Mission Brief ACCEPT.
        /// False when opened from Character Page for free loadout editing.
        /// </summary>
        public static bool EnteredFromMissionBrief { get; private set; }
        public static bool EnteredFromFriendlyOutpostTakeoff { get; private set; }

        public static void MarkEnteredFromMissionBrief()
        {
            EnteredFromMissionBrief = true;
            EnteredFromFriendlyOutpostTakeoff = false;
        }

        public static void MarkEnteredFromCharacterPage()
        {
            EnteredFromMissionBrief = false;
            EnteredFromFriendlyOutpostTakeoff = false;
        }

        public static void MarkEnteredFromFriendlyOutpostTakeoff()
        {
            EnteredFromMissionBrief = false;
            EnteredFromFriendlyOutpostTakeoff = true;
        }
    }
}
