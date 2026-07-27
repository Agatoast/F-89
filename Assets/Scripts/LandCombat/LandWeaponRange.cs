using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Ground-combat distance units ONLY (namespace F89.LandCombat).
    /// Do not use for flight, world map, Antarctica miles, or any non-land scene.
    /// 10 units = character to the left or right screen edge in GroundAttack.
    /// Weapon Range is in these units; convert with <see cref="ToWorld"/> for Unity transforms.
    /// </summary>
    public static class LandUnits
    {
        /// <summary>Converts land-combat range units to Unity world distance for the ground camera.</summary>
        public static float ToWorld(float landUnits)
        {
            return Mathf.Max(0f, landUnits) * WorldSizePerUnit();
        }

        /// <summary>Converts Unity world distance to land-combat range units.</summary>
        public static float ToLand(float worldDistance)
        {
            var perUnit = WorldSizePerUnit();
            return perUnit > 0f ? Mathf.Max(0f, worldDistance) / perUnit : 0f;
        }

        /// <summary>Unity world distance of one land-combat unit (screen half-width / 10).</summary>
        public static float WorldSizePerUnit()
        {
            return ResolveHalfScreenWidthWorld() / LandGameConstants.LandUnitsToScreenEdge;
        }

        public static float ResolveHalfScreenWidthWorld()
        {
            var camera = Camera.main;
            if (camera != null && camera.orthographic)
            {
                return camera.orthographicSize * camera.aspect;
            }

            return LandGameConstants.ArenaHalfSizeWorldUnits;
        }
    }

    /// <summary>Legacy name for <see cref="LandUnits"/> (land combat only).</summary>
    public static class LandWeaponRange
    {
        public static float ToWorldUnits(float rangeInLandUnits) => LandUnits.ToWorld(rangeInLandUnits);

        public static float ResolveHalfScreenWidth() => LandUnits.ResolveHalfScreenWidthWorld();
    }
}
