using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Entry point for the optional land shooter module. Flight gameplay must never depend on this type.
    /// </summary>
    public static class LandCombatModule
    {
        public const string ModuleVersion = "0.1.0-scaffold";

        public static bool IsActive { get; private set; }

        public static void EnterFromHandoff()
        {
            if (!LandMissionHandoffState.TryConsumeEnterFromFlight(out var snapshot))
            {
                Debug.LogWarning("[LandCombat] Enter called without a pending flight handoff.");
                snapshot = LandSortieSnapshot.Empty;
            }

            IsActive = true;
            OpenFieldLandingState.BeginFromSortie(snapshot);
            LandOutpostLandingState.BeginFromSortie(snapshot);
            WaypointLandingState.BeginFromSortie(snapshot);
            OpenFieldLandingNotice.EvaluateFromSortie(snapshot);
            Debug.Log($"[LandCombat] Module active ({ModuleVersion}). Return scene: {snapshot.ReturnSceneName}");
        }

        public static void ExitToFlight(LandGroundSessionResult result)
        {
            var returnSnapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (!returnSnapshot.IsValid)
            {
                Debug.LogError(
                    "[LandCombat] Exit requested without a stored landing snapshot — cannot restore flight position.");
                return;
            }

            if (string.IsNullOrEmpty(returnSnapshot.ReturnSceneName))
            {
                returnSnapshot.ReturnSceneName = GameScenes.FlightTest;
            }

            if (OutpostRunwayDeckState.ConsumePendingFriendlyRunwayTakeoffOnRestore()
                || returnSnapshot.RestoreWithImmediateTakeoff
                || LandingMileFlagState.HasActiveFlag
                || returnSnapshot.HasLandingMiles
                || returnSnapshot.IsOpenFieldLanding
                || !OutpostRunwayDeckState.IsParkedAtRunway)
            {
                returnSnapshot.ReturnToRunwayDeck = false;
                returnSnapshot.RestoreWithImmediateTakeoff = true;
            }
            else if (OutpostRunwayDeckState.IsParkedAtRunway)
            {
                OutpostGroundGuardState.SyncClearanceFromGroundSession();
                OutpostRunwayDeckState.RefreshSurfaceGuardsCleared(OutpostRunwayDeckState.ParkedOutpostName);
                if (string.IsNullOrWhiteSpace(returnSnapshot.OutpostName))
                {
                    returnSnapshot.OutpostName = OutpostRunwayDeckState.ParkedOutpostName;
                }

                returnSnapshot.ReturnToRunwayDeck = true;
            }

            if (returnSnapshot.IsOpenFieldLanding && OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
            {
                returnSnapshot.HasLandingMiles = true;
                returnSnapshot.LandingMileX = OpenFieldLandingState.LandingMiles.x;
                returnSnapshot.LandingMileY = OpenFieldLandingState.LandingMiles.y;
                LandingMileFlagState.SetFromMiles(
                    OpenFieldLandingState.LandingMiles,
                    returnSnapshot.LandingRotationY);
            }
            else if (WaypointLandingState.IsActive || !string.IsNullOrWhiteSpace(returnSnapshot.WaypointSiteCode))
            {
                returnSnapshot.ReturnToRunwayDeck = false;
                returnSnapshot.RestoreWithImmediateTakeoff = true;
                FlightGroundReturnService.EnsureWaypointLandingFlag(returnSnapshot);
                LandingMileFlagState.TryApplyToSnapshot(ref returnSnapshot);
                if (!returnSnapshot.HasLandingMiles)
                {
                    LandingMileFlagState.ApplyToSnapshot(ref returnSnapshot);
                }
            }
            else
            {
                LandingMileFlagState.TryApplyToSnapshot(ref returnSnapshot);
            }

            LandSurfaceSession.Clear();
            LandBossAreaState.Clear();
            LandOutpostLandingState.Clear();
            WaypointLandingState.Clear();
            OpenFieldLandingState.Clear();
            OpenFieldLandingNotice.Clear();
            LandMissionHandoffState.BeginReturnToFlight(returnSnapshot, result);

            if (returnSnapshot.RestoreWithImmediateTakeoff
                || LandingMileFlagState.HasActiveFlag
                || returnSnapshot.HasLandingMiles
                || !string.IsNullOrWhiteSpace(returnSnapshot.WaypointSiteCode))
            {
                FlightMissionLaunchState.ClearStaleCarrierWhenSortieReturnPending();
            }

            IsActive = false;
            var locationLabel = returnSnapshot.HasLandingMiles
                ? CampaignMapCoordinates.FormatMilesLabel(
                    new Vector2(returnSnapshot.LandingMileX, returnSnapshot.LandingMileY))
                : CampaignMapCoordinates.FormatMilesLabel(
                    CampaignMapCoordinates.WorldToMiles(returnSnapshot.AircraftWorldPosition));
            Debug.Log($"[LandCombat] Module exited; flight return queued at {locationLabel}.");
        }

        public static void ShutdownWithoutHandoff()
        {
            IsActive = false;
            LandSurfaceSession.Clear();
            LandBossAreaState.Clear();
            LandOutpostLandingState.Clear();
            WaypointLandingState.Clear();
            OpenFieldLandingState.Clear();
            OpenFieldLandingNotice.Clear();
            LandMissionHandoffState.Clear();
        }
    }
}
