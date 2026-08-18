using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Active open-field landing session. Supersedes last-land base while active; cleared on VTOL.</summary>
    public static class OpenFieldLandingState
    {
        public static bool IsActive { get; private set; }
        public static Vector2 LandingMiles { get; private set; }

        public static void BeginFromSortie(LandSortieSnapshot snapshot)
        {
            IsActive = snapshot.IsOpenFieldLanding;
            LandingMiles = snapshot.HasLandingMiles
                ? new Vector2(snapshot.LandingMileX, snapshot.LandingMileY)
                : Vector2.zero;
        }

        public static void Clear()
        {
            IsActive = false;
            LandingMiles = Vector2.zero;
        }
    }
}
