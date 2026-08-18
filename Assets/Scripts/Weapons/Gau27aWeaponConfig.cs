using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Gau27aWeaponConfig", menuName = "F-89/Weapons/GAU-27A Config")]
    public class Gau27aWeaponConfig : ScriptableObject, ILockCapableWeapon
    {
        [Header("Range")]
        [Tooltip("Maximum travel distance for each round.")]
        public float maxRangeMiles = 2f;
        [Tooltip("Radius of the ogive (circular arc about the aircraft).")]
        public float ogiveMaxRangeMiles = 2.5f;
        public float minCrosshairMiles = 0.1f;
        [Tooltip("Half-angle from the nose. 15° ⇒ 30° arc at the far end, centered on the plane.")]
        public float ogiveHalfAngleDegrees = 15f;

        [Header("Crosshair")]
        [Tooltip("Gun cursor is constrained to the 30° ogive sector centered on the aircraft.")]

        [Header("Lock-On")]
        public float lockTimeSeconds = 2f;
        public float maxBeepInterval = 0.55f;
        public float minBeepInterval = 0.12f;

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

        float ILockCapableWeapon.RangeMiles => ogiveMaxRangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => lockTimeSeconds;
        float ILockCapableWeapon.MaxBeepInterval => maxBeepInterval;
        float ILockCapableWeapon.MinBeepInterval => minBeepInterval;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => LockableTargetKind.Ground;
        WeaponAimMode ILockCapableWeapon.AimMode => WeaponAimMode.OgiveDirect;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => ogiveHalfAngleDegrees;
        WeaponEngagementType ILockCapableWeapon.EngagementType => WeaponEngagementType.ForwardGun;
    }
}
