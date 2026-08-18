using UnityEngine;

namespace F89.Weapons
{
    /// <summary>Building / vehicle destruction and player missile-hit explosions.</summary>
    public static class GroundExplosionEffect
    {
        public static void PlayBuildingExplosion(Vector3 worldPosition, string sourceLabel = null)
        {
            PlayFullExplosion(worldPosition);
        }

        public static void PlayVehicleExplosion(Vector3 worldPosition, string sourceLabel = null)
        {
            PlayFullExplosion(worldPosition);
        }

        public static void PlayPlayerMissileHit(Vector3 worldPosition)
        {
            GroundExplosionVisual.PlayFirstFrame(worldPosition);
        }

        /// <summary>Explosion flash on the player aircraft when hit by enemy weapons.</summary>
        public static void PlayPlayerAircraftHit(Vector3 worldPosition)
        {
            GroundExplosionVisual.PlayAirHit(worldPosition);
        }

        private static void PlayFullExplosion(Vector3 worldPosition)
        {
            GroundExplosionVisual.PlayFullAnimation(worldPosition);
        }
    }
}
