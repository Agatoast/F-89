using System.Collections.Generic;
using F89.Core;
using F89.Flight;

namespace F89.UI
{
    public enum CrashLandingPage
    {
        None = 0,
        Rescue = 1,
        Wounded = 2,
        KIA = 3,
        PurpleHeartAward = 4
    }

    /// <summary>
    /// Pending crash landing report pages after player aircraft structural failure.
    /// Wounded flow continues to PurpleHeartAward before post-mission navigation.
    /// </summary>
    public static class CrashLandingOutcomeState
    {
        private static readonly Queue<CrashLandingPage> PageQueue = new();
        private static CrashLandingOutcome outcome = CrashLandingOutcome.None;
        private static bool purpleHeartGranted;

        public static bool HasPendingPages => PageQueue.Count > 0;

        public static CrashLandingOutcome Outcome => outcome;

        public static string PageId => PageIdFor(CurrentPage);

        public static CrashLandingPage CurrentPage =>
            PageQueue.Count > 0 ? PageQueue.Peek() : CrashLandingPage.None;

        public static void Begin(CrashLandingOutcome rolledOutcome)
        {
            PageQueue.Clear();
            outcome = rolledOutcome;
            purpleHeartGranted = false;

            switch (rolledOutcome)
            {
                case CrashLandingOutcome.Rescued:
                    PageQueue.Enqueue(CrashLandingPage.Rescue);
                    break;
                case CrashLandingOutcome.Wounded:
                    PageQueue.Enqueue(CrashLandingPage.Wounded);
                    PageQueue.Enqueue(CrashLandingPage.PurpleHeartAward);
                    break;
                default:
                    PageQueue.Enqueue(CrashLandingPage.KIA);
                    break;
            }
        }

        public static void EnsurePurpleHeartGranted()
        {
            if (purpleHeartGranted || CurrentPage != CrashLandingPage.PurpleHeartAward)
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

        /// <summary>Advances to the next page. Returns false when the flow is finished.</summary>
        public static bool Advance()
        {
            if (CurrentPage == CrashLandingPage.PurpleHeartAward)
            {
                EnsurePurpleHeartGranted();
            }

            if (PageQueue.Count > 0)
            {
                PageQueue.Dequeue();
            }

            return PageQueue.Count > 0;
        }

        public static void Clear()
        {
            PageQueue.Clear();
            outcome = CrashLandingOutcome.None;
            purpleHeartGranted = false;
        }

        public static string PageIdFor(CrashLandingPage page) =>
            page switch
            {
                CrashLandingPage.Rescue => GameScenes.CrashLandingRescue,
                CrashLandingPage.Wounded => GameScenes.CrashLandingWounded,
                CrashLandingPage.KIA => GameScenes.CrashLandingKIA,
                CrashLandingPage.PurpleHeartAward => GameScenes.PurpleHeartAward,
                _ => string.Empty
            };

        public static string TitleForCurrent()
        {
            return CurrentPage switch
            {
                CrashLandingPage.Rescue => "CRASH LANDING — RESCUED",
                CrashLandingPage.Wounded => "CRASH LANDING — WOUNDED",
                CrashLandingPage.KIA => "CRASH LANDING — KIA",
                CrashLandingPage.PurpleHeartAward => "PURPLE HEART AWARDED",
                _ => "CRASH LANDING"
            };
        }

        public static string BodyForCurrent()
        {
            return CurrentPage switch
            {
                CrashLandingPage.Rescue =>
                    "Your aircraft went down, but search and rescue reached you in time.\n\nYou will return to base for debrief.",
                CrashLandingPage.Wounded =>
                    "You survived the crash landing but were seriously wounded during extraction.\n\nMedical has stabilized your condition.",
                CrashLandingPage.KIA =>
                    "Your aircraft went down and rescue teams could not reach you in time.\n\nYou have been killed in action.",
                CrashLandingPage.PurpleHeartAward =>
                    "For wounds received in action during your crash landing,\n\nyou have been awarded the Purple Heart.",
                _ => string.Empty
            };
        }
    }
}
