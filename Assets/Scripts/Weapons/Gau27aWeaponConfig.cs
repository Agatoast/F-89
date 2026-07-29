using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Gau27aWeaponConfig", menuName = "F-89/Weapons/GAU-27A Config")]
    public class Gau27aWeaponConfig : ScriptableObject
    {
        [Header("Range")]
        public float maxRangeMiles = 2f;
        public float minCrosshairMiles = 0.5f;

        [Header("Crosshair")]
        [Tooltip("Move the mouse along the nose line to set gun range between min and max miles.")]

        [Header("Firing")]
        public float roundsPerSecond = 10f;
        public float roundSpeedMilesPerSecond = 0.66f;
        [Tooltip("World radius of the central crosshair dot. Targets under the dot auto-hit.")]
        public float crosshairDotRadiusTics = 0.1f;

        [Header("Ground Hit Points")]
        [Tooltip("Each round that hits deals this much GHP to any air or ground target.")]
        public int groundHitPointsPerHit = PlaneWeaponGhp.Gau27PerHit;

        [Header("Loadout")]
        public int startingRounds = 300;

        public string WeaponName => "GAU-27A";
        public WeaponAimMode AimMode => WeaponAimMode.ForwardOnly;
    }
}
