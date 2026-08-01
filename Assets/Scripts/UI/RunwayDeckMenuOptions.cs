using F89.Core;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class RunwayDeckMenuOptions
    {
        public static RunwayDeckMenuDialog.Options BuildFlightDeckOptions()
        {
            return BuildFriendlyBaseDeckOptions(ResolveActiveOutpostName());
        }

        public static RunwayDeckMenuDialog.Options BuildFriendlyBaseDeckOptions(string outpostName)
        {
            RefreshGuardClearance(outpostName);
            var subtitle = string.IsNullOrWhiteSpace(outpostName) ? "Friendly Base" : outpostName;
            return BuildFullServiceOptions(subtitle, ShouldShowEndMission(outpostName));
        }

        public static RunwayDeckMenuDialog.Options BuildCarrierDeckOptions()
        {
            return BuildFullServiceOptions(
                "USS MARTIN VAN BUREN",
                GamePlayModeState.IsCampaign);
        }

        public static RunwayDeckMenuDialog.Options BuildGroundPlaneOptions()
        {
            if (OpenFieldLandingState.IsActive)
            {
                return BuildGroundOnlyOptions(ResolveOpenFieldSubtitle());
            }

            if (LandOutpostLandingState.HasActiveOutpost)
            {
                return BuildGroundOnlyOptions(ResolveOutpostGroundSubtitle());
            }

            return BuildGroundOnlyOptions("Runway");
        }

        private static RunwayDeckMenuDialog.Options BuildFullServiceOptions(string subtitle, bool endMissionEnabled)
        {
            return new RunwayDeckMenuDialog.Options
            {
                RefuelEnabled = !DeckLandingServiceState.RefuelUsedThisLanding,
                RearmEnabled = !DeckLandingServiceState.RearmUsedThisLanding,
                DismountEnabled = false,
                EndMissionEnabled = endMissionEnabled,
                Subtitle = subtitle
            };
        }

        private static RunwayDeckMenuDialog.Options BuildGroundOnlyOptions(string subtitle)
        {
            return new RunwayDeckMenuDialog.Options
            {
                RefuelEnabled = false,
                RearmEnabled = false,
                DismountEnabled = true,
                EndMissionEnabled = false,
                Subtitle = subtitle,
                DialogVerticalAnchor = 0.68f,
                ButtonVerticalOffset = 30f
            };
        }

        private static string ResolveOpenFieldSubtitle()
        {
            if (OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
            {
                return CampaignMapCoordinates.FormatMilesLabel(OpenFieldLandingState.LandingMiles);
            }

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (snapshot.HasLandingMiles)
            {
                return CampaignMapCoordinates.FormatMilesLabel(
                    new Vector2(snapshot.LandingMileX, snapshot.LandingMileY));
            }

            return "Open Field";
        }

        private static string ResolveOutpostGroundSubtitle()
        {
            var outpostName = LandOutpostLandingState.ActiveOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return "Enemy Outpost";
            }

            return AntarcticaOutpostState.IsNeutralOutpost(outpostName)
                ? $"{outpostName} (Neutral)"
                : outpostName;
        }

        /// <summary>
        /// END MISSION at a friendly base or the carrier deck.
        /// Cleared hostile/neutral outposts stay non-friendly until END MISSION turns them green.
        /// </summary>
        private static bool ShouldShowEndMission(string outpostName)
        {
            if (!GamePlayModeState.IsCampaign)
            {
                return false;
            }

            return AntarcticaOutpostState.IsFriendlyOccupied(outpostName);
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

        private static void RefreshGuardClearance(string outpostName)
        {
            if (!string.IsNullOrWhiteSpace(outpostName))
            {
                OutpostRunwayDeckState.RefreshSurfaceGuardsCleared(outpostName);
            }
        }
    }
}
