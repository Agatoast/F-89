using System;
using F89.Core;
using F89.LandCombat;

namespace F89.UI
{
    public static class RunwayDeckMenuOptions
    {
        public static RunwayDeckMenuDialog.Options BuildFlightDeckOptions()
        {
            return BuildOptions(ResolveActiveOutpostName());
        }

        public static RunwayDeckMenuDialog.Options BuildGroundPlaneOptions()
        {
            return BuildOptions(ResolveActiveOutpostName());
        }

        private static RunwayDeckMenuDialog.Options BuildOptions(string outpostName)
        {
            RefreshGuardClearance(outpostName);
            var servicesAvailable = HasRunwayServices(outpostName);
            var showEndMission = ShouldShowEndMission(outpostName);
            return BuildCoreOptions(
                showRearm: servicesAvailable,
                rearmEnabled: servicesAvailable,
                showRefuel: servicesAvailable,
                refuelEnabled: servicesAvailable,
                showEndMission: showEndMission,
                showDismount: true,
                dismountEnabled: true,
                subtitle: BuildSubtitle(outpostName, servicesAvailable, showEndMission));
        }

        private static string BuildSubtitle(string outpostName, bool servicesAvailable, bool showEndMission)
        {
            if (servicesAvailable)
            {
                return "Runway";
            }

            if (showEndMission)
            {
                return "Runway — end mission to secure this outpost";
            }

            return "Runway";
        }

        /// <summary>
        /// END MISSION appears only at a friendly base or the assigned outpost after air objectives are cleared.
        /// Carrier deck uses its own mission-complete screen.
        /// </summary>
        private static bool ShouldShowEndMission(string outpostName)
        {
            if (!GamePlayModeState.IsCampaign)
            {
                return false;
            }

            if (AntarcticaOutpostState.IsFriendlyOccupied(outpostName))
            {
                return true;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null
                || !LandBossMissionAssignment.HasActiveAssignment(save)
                || !LandBossMissionAssignment.IsPrimaryMissionComplete(save))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(outpostName)
                && string.Equals(outpostName, save.AssignedBossOutpostName, StringComparison.Ordinal);
        }

        private static string ResolveActiveOutpostName()
        {
            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                outpostName = LandOutpostLandingState.ActiveOutpostName;
            }

            return outpostName;
        }

        private static bool HasRunwayServices(string outpostName) =>
            AntarcticaOutpostState.IsFriendlyOccupied(outpostName)
            || OutpostRunwayDeckState.IsFriendlyOutpost;

        private static void RefreshGuardClearance(string outpostName)
        {
            if (!string.IsNullOrWhiteSpace(outpostName))
            {
                OutpostRunwayDeckState.RefreshSurfaceGuardsCleared(outpostName);
            }
        }

        private static RunwayDeckMenuDialog.Options BuildCoreOptions(
            bool showRearm,
            bool rearmEnabled,
            bool showRefuel,
            bool refuelEnabled,
            bool showEndMission,
            bool showDismount,
            bool dismountEnabled,
            string subtitle)
        {
            return new RunwayDeckMenuDialog.Options
            {
                ShowRearm = showRearm,
                RearmEnabled = rearmEnabled,
                ShowRefuel = showRefuel,
                RefuelEnabled = refuelEnabled,
                ShowEndMission = showEndMission,
                ShowDismount = showDismount,
                DismountEnabled = dismountEnabled,
                Subtitle = subtitle
            };
        }
    }
}
