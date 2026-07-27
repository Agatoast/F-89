using F89.Core;
using F89.LandCombat;

namespace F89.UI
{
    /// <summary>
    /// Pending Death / POW / Escaped report after reaching 0 HP on the ground.
    /// FrozenToDeath shows FrozenDeath, then DeathScreen.
    /// </summary>
    public static class LandDownedOutcomeState
    {
        private static LandDownedOutcome pendingOutcome = LandDownedOutcome.None;
        private static int pageIndex;

        public static bool HasPendingOutcome => pendingOutcome != LandDownedOutcome.None;

        public static LandDownedOutcome Outcome => pendingOutcome;

        public static string PageId
        {
            get
            {
                if (pendingOutcome == LandDownedOutcome.FrozenToDeath)
                {
                    return pageIndex == 0 ? GameScenes.FrozenDeath : GameScenes.DeathScreen;
                }

                return LandDownedResolver.GetPendingScreenName(pendingOutcome);
            }
        }

        public static void Begin(LandDownedOutcome outcome)
        {
            pendingOutcome = outcome == LandDownedOutcome.None
                ? LandDownedOutcome.Death
                : outcome;
            pageIndex = 0;
        }

        public static void Clear()
        {
            pendingOutcome = LandDownedOutcome.None;
            pageIndex = 0;
        }

        /// <summary>Advances to the next page. Returns false when the flow is finished.</summary>
        public static bool Advance()
        {
            if (pendingOutcome == LandDownedOutcome.FrozenToDeath && pageIndex == 0)
            {
                pageIndex = 1;
                return true;
            }

            Clear();
            return false;
        }

        public static string TitleForCurrent()
        {
            if (pendingOutcome == LandDownedOutcome.FrozenToDeath)
            {
                return pageIndex == 0 ? "FROZEN TO DEATH" : "KILLED IN ACTION";
            }

            return pendingOutcome switch
            {
                LandDownedOutcome.PrisonerOfWar => "PRISONER OF WAR",
                LandDownedOutcome.Escaped => "ESCAPED",
                _ => "KILLED IN ACTION"
            };
        }

        public static string BodyForCurrent()
        {
            if (pendingOutcome == LandDownedOutcome.FrozenToDeath)
            {
                return pageIndex == 0
                    ? "Two hours of continuous exposure in the Antarctic cold have claimed your life.\n\nYou froze to death."
                    : "You have been killed in action.\n\nYour mission ends here.";
            }

            return pendingOutcome switch
            {
                LandDownedOutcome.PrisonerOfWar =>
                    "You have been captured by enemy forces.\n\nYour mission ends here.",
                LandDownedOutcome.Escaped =>
                    "Against all odds, you escaped capture and made it back to friendly lines.\n\nYour mission ends here.",
                _ =>
                    "You have been killed in action.\n\nYour mission ends here."
            };
        }
    }
}
