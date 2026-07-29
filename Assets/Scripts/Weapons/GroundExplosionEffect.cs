using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Building / vehicle destruction explosions. Weapon-specific sprites will be supplied later.
    /// </summary>
    public static class GroundExplosionEffect
    {
        public static void PlayBuildingExplosion(Vector3 worldPosition, string sourceLabel = null)
        {
            PlayPlaceholder(worldPosition, "building", sourceLabel);
        }

        public static void PlayVehicleExplosion(Vector3 worldPosition, string sourceLabel = null)
        {
            PlayPlaceholder(worldPosition, "vehicle", sourceLabel);
        }

        private static void PlayPlaceholder(Vector3 worldPosition, string kind, string sourceLabel)
        {
            // Sprites TBD — keep a clear log hook so wiring is ready when art lands.
            var label = string.IsNullOrWhiteSpace(sourceLabel) ? kind : sourceLabel;
            Debug.Log($"F-89: {kind} explosion at {worldPosition} ({label}) — sprite pending.");
        }
    }
}
