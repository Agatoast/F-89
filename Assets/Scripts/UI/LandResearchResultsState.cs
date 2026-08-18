using System.Collections.Generic;
using F89.LandCombat;

namespace F89.UI
{
    /// <summary>
    /// Pending end-of-mission R&amp;D report pages (NoBreakPage or discovery pages).
    /// </summary>
    public static class LandResearchResultsState
    {
        private static readonly Queue<LandResearchDiscovery> DiscoveryQueue = new();
        private static bool showingNoBreakthroughs;
        private static string returnSceneName = string.Empty;

        public static bool HasPendingReport =>
            showingNoBreakthroughs || DiscoveryQueue.Count > 0;

        public static string ReturnSceneName =>
            string.IsNullOrEmpty(returnSceneName) ? F89.Core.GameScenes.CharacterPage : returnSceneName;

        public static string CurrentPageId
        {
            get
            {
                if (showingNoBreakthroughs)
                {
                    return F89.Core.GameScenes.NoBreakPage;
                }

                if (DiscoveryQueue.Count == 0)
                {
                    return string.Empty;
                }

                return PageIdFor(DiscoveryQueue.Peek().Page);
            }
        }

        public static bool TryGetCurrentDiscovery(out LandResearchDiscovery discovery)
        {
            if (showingNoBreakthroughs || DiscoveryQueue.Count == 0)
            {
                discovery = default;
                return false;
            }

            discovery = DiscoveryQueue.Peek();
            return true;
        }

        public static void BeginMissionEndReports(
            IReadOnlyList<LandResearchDiscovery> discoveries,
            string continueToScene)
        {
            DiscoveryQueue.Clear();
            showingNoBreakthroughs = false;
            returnSceneName = string.IsNullOrEmpty(continueToScene)
                ? F89.Core.GameScenes.CharacterPage
                : continueToScene;

            if (discoveries == null || discoveries.Count == 0)
            {
                showingNoBreakthroughs = true;
                return;
            }

            for (var i = 0; i < discoveries.Count; i++)
            {
                DiscoveryQueue.Enqueue(discoveries[i]);
            }
        }

        public static bool Advance()
        {
            if (showingNoBreakthroughs)
            {
                showingNoBreakthroughs = false;
                return false;
            }

            if (DiscoveryQueue.Count > 0)
            {
                DiscoveryQueue.Dequeue();
            }

            return DiscoveryQueue.Count > 0;
        }

        public static void Clear()
        {
            DiscoveryQueue.Clear();
            showingNoBreakthroughs = false;
            returnSceneName = string.Empty;
        }

        /// <summary>Mission score and/or medal pages without R&amp;D rolls (e.g. survived crash landing).</summary>
        public static void BeginScoreAndMedalsOnly(string continueToScene)
        {
            DiscoveryQueue.Clear();
            showingNoBreakthroughs = false;
            returnSceneName = string.IsNullOrEmpty(continueToScene)
                ? F89.Core.GameScenes.CharacterPage
                : continueToScene;
        }

        public static string PageIdFor(LandResearchReportPage page) =>
            page switch
            {
                LandResearchReportPage.NoBreakPage => F89.Core.GameScenes.NoBreakPage,
                LandResearchReportPage.HelmetRnD => F89.Core.GameScenes.HelmetRnDPage,
                LandResearchReportPage.VestRnD => F89.Core.GameScenes.VestRnDPage,
                LandResearchReportPage.WeaponRnD => F89.Core.GameScenes.WeaponRnDPage,
                LandResearchReportPage.BootsRnD => F89.Core.GameScenes.BootsRnDPage,
                _ => F89.Core.GameScenes.NoBreakPage
            };

        public static string TitleForCurrent()
        {
            if (showingNoBreakthroughs)
            {
                return "NO BREAKTHROUGHS";
            }

            if (!TryGetCurrentDiscovery(out var discovery))
            {
                return "R&D";
            }

            return discovery.Page switch
            {
                LandResearchReportPage.HelmetRnD => "HELMET R&D",
                LandResearchReportPage.VestRnD => "VEST R&D",
                LandResearchReportPage.WeaponRnD => "WEAPON R&D",
                LandResearchReportPage.BootsRnD => "BOOTS R&D",
                _ => "R&D"
            };
        }

        public static string BodyForCurrent()
        {
            if (showingNoBreakthroughs)
            {
                return "No research breakthroughs this mission.\n\nKeep feeding the R&D benches to raise your odds.";
            }

            if (!TryGetCurrentDiscovery(out var discovery))
            {
                return string.Empty;
            }

            return $"Breakthrough!\n\n{discovery.ItemDisplayName} (TL {discovery.UnlockedTechLevel}) is now available in Basic Loadout.";
        }
    }
}
