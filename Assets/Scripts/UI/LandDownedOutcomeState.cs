using System.Collections.Generic;
using F89.Core;
using F89.LandCombat;

namespace F89.UI
{
    public enum LandDownedPage
    {
        None = 0,
        FrozenDeath = 1,
        KIA = 2,
        PrisonerOfWar = 3,
        Escaped = 4,
        PurpleHeartAward = 5
    }

    /// <summary>
    /// Pending Death / POW / Escaped report after reaching 0 HP on the ground.
    /// KIA flows show the death report then Purple Heart before returning to character select.
    /// Repeat grants increment ribbon devices — only one Purple Heart ribbon on the character page.
    /// </summary>
    public static class LandDownedOutcomeState
    {
        private static readonly Queue<LandDownedPage> PageQueue = new();
        private static LandDownedOutcome pendingOutcome = LandDownedOutcome.None;
        private static bool purpleHeartGranted;

        public static bool HasPendingOutcome => PageQueue.Count > 0;

        public static LandDownedOutcome Outcome => pendingOutcome;

        public static LandDownedPage CurrentPage =>
            PageQueue.Count > 0 ? PageQueue.Peek() : LandDownedPage.None;

        public static string PageId => PageIdFor(CurrentPage);

        public static void Begin(LandDownedOutcome outcome)
        {
            PageQueue.Clear();
            pendingOutcome = outcome == LandDownedOutcome.None
                ? LandDownedOutcome.Death
                : outcome;
            purpleHeartGranted = false;

            switch (pendingOutcome)
            {
                case LandDownedOutcome.FrozenToDeath:
                    PageQueue.Enqueue(LandDownedPage.FrozenDeath);
                    PageQueue.Enqueue(LandDownedPage.KIA);
                    PageQueue.Enqueue(LandDownedPage.PurpleHeartAward);
                    break;
                case LandDownedOutcome.PrisonerOfWar:
                    PageQueue.Enqueue(LandDownedPage.PrisonerOfWar);
                    break;
                case LandDownedOutcome.Escaped:
                    PageQueue.Enqueue(LandDownedPage.Escaped);
                    break;
                default:
                    PageQueue.Enqueue(LandDownedPage.KIA);
                    PageQueue.Enqueue(LandDownedPage.PurpleHeartAward);
                    break;
            }
        }

        public static void EnsurePurpleHeartGranted()
        {
            if (purpleHeartGranted || CurrentPage != LandDownedPage.PurpleHeartAward)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.TryGrantRibbon(save, MilitaryRibbonIds.PurpleHeart);
            }

            purpleHeartGranted = true;
        }

        public static void Clear()
        {
            PageQueue.Clear();
            pendingOutcome = LandDownedOutcome.None;
            purpleHeartGranted = false;
        }

        /// <summary>Advances to the next page. Returns false when the flow is finished.</summary>
        public static bool Advance()
        {
            if (CurrentPage == LandDownedPage.PurpleHeartAward)
            {
                EnsurePurpleHeartGranted();
            }

            if (PageQueue.Count > 0)
            {
                PageQueue.Dequeue();
            }

            return PageQueue.Count > 0;
        }

        public static string PageIdFor(LandDownedPage page) =>
            page switch
            {
                LandDownedPage.FrozenDeath => GameScenes.FrozenDeath,
                LandDownedPage.KIA => GameScenes.DeathScreen,
                LandDownedPage.PrisonerOfWar => GameScenes.POWScreen,
                LandDownedPage.Escaped => GameScenes.EscapedScreen,
                LandDownedPage.PurpleHeartAward => GameScenes.PurpleHeartAward,
                _ => string.Empty
            };

        public static string TitleForCurrent()
        {
            return CurrentPage switch
            {
                LandDownedPage.FrozenDeath => "FROZEN TO DEATH",
                LandDownedPage.KIA => "KILLED IN ACTION",
                LandDownedPage.PrisonerOfWar => "PRISONER OF WAR",
                LandDownedPage.Escaped => "ESCAPED",
                LandDownedPage.PurpleHeartAward => "PURPLE HEART AWARDED",
                _ => "KILLED IN ACTION"
            };
        }

        public static string BodyForCurrent()
        {
            return CurrentPage switch
            {
                LandDownedPage.FrozenDeath =>
                    "Two hours of continuous exposure in the Antarctic cold have claimed your life.\n\nYou froze to death.",
                LandDownedPage.KIA =>
                    "You have been killed in action.\n\nYour mission ends here.",
                LandDownedPage.PrisonerOfWar =>
                    "You have been captured by enemy forces.\n\nYour mission ends here.",
                LandDownedPage.Escaped =>
                    "Against all odds, you escaped capture and made it back to friendly lines.\n\nYour mission ends here.",
                LandDownedPage.PurpleHeartAward =>
                    "For wounds received in action resulting in your death,\n\nyou have been awarded the Purple Heart.",
                _ => string.Empty
            };
        }
    }
}
