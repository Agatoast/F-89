using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Gau27aWeaponConfig", menuName = "F-89/Weapons/GAU-27A Config")]
    public class Gau27aWeaponConfig : ScriptableObject
    {
        [Header("Range")]
        [Tooltip("Maximum travel distance for each round.")]
        public float maxRangeMiles = 2f;
        [Tooltip("Maximum crosshair distance inside the forward ogive envelope.")]
        public float ogiveMaxRangeMiles = 2.5f;
        public float minCrosshairMiles = 0.1f;
        [Tooltip("Half-angle from the nose. 15° = 30° total cone centered on the aircraft forward axis.")]
        public float ogiveHalfAngleDegrees = 15f;

        [Header("Crosshair")]
        [Tooltip("Move the cursor anywhere inside the ogive envelope to aim the gun.")]

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
        public WeaponAimMode AimMode => WeaponAimMode.OgiveDirect;
    }
}
