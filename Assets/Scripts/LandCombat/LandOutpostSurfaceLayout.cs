using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Fixed dismount layout: plane at origin, bunker pad ahead, guards between.</summary>
    public static class LandOutpostSurfaceLayout
    {
        public const float BunkerRangeLandUnits = 55f;
        private const float GuardArcRadiusLandUnits = 42f;
        private const float GuardArcHalfWidthLandUnits = 14f;

        public static Vector2 BunkerPositionFromPlane(Vector2 planePosition) =>
            planePosition + Vector2.up * LandUnits.ToWorld(BunkerRangeLandUnits);

        public static Vector2 GetGuardPosition(Vector2 planePosition, int guardIndex, int guardCount)
        {
            var bunker = BunkerPositionFromPlane(planePosition);
            if (guardCount <= 0)
            {
                return bunker;
            }

            var center = Vector2.Lerp(planePosition, bunker, 0.55f);
            var spread = guardCount > 1
                ? (guardIndex - (guardCount - 1) * 0.5f) / (guardCount - 1)
                : 0f;
            var lateral = spread * LandUnits.ToWorld(GuardArcHalfWidthLandUnits);
            var forward = LandUnits.ToWorld(GuardArcRadiusLandUnits);
            return center + new Vector2(lateral, forward * 0.15f);
        }
    }
}
