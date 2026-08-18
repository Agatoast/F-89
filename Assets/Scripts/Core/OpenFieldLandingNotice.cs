using F89.Enemies;
using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>One-shot OK prompt when an open-field landing enters a grid square with ambient enemy infantry.</summary>
    public static class OpenFieldLandingNotice
    {
        public static bool IsPending { get; private set; }

        public static void EvaluateFromSortie(LandSortieSnapshot snapshot)
        {
            IsPending = false;
            if (!snapshot.IsOpenFieldLanding || snapshot.HasOutpostBunker)
            {
                return;
            }

            if (LandingMileFlagState.HasActiveFlag)
            {
                GridSquareVehicleSpawner.EnsureLandingCellResolved(LandingMileFlagState.LandingMiles);
                IsPending = GridSquareSpawnState.LandingCellHasHostileSpawn(LandingMileFlagState.LandingMiles);
                return;
            }

            if (!snapshot.HasLandingMiles)
            {
                return;
            }

            var landingMiles = new Vector2(snapshot.LandingMileX, snapshot.LandingMileY);
            GridSquareVehicleSpawner.EnsureLandingCellResolved(landingMiles);
            IsPending = GridSquareSpawnState.LandingCellHasHostileSpawn(landingMiles);
        }

        public static void Clear() => IsPending = false;
    }
}
