using F89.Core;
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

            returnSnapshot.ReturnSceneName = GameScenes.FlightTest;
            LandMissionHandoffState.BeginReturnToFlight(returnSnapshot, result);
            IsActive = false;
            Debug.Log(
                $"[LandCombat] Module exited; flight return queued at {returnSnapshot.AircraftWorldPosition}.");
        }

        public static void ShutdownWithoutHandoff()
        {
            IsActive = false;
            LandMissionHandoffState.Clear();
        }
    }
}
