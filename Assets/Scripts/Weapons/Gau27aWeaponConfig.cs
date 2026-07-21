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
        public float hitRadiusTics = 2f;
        [Range(0f, 1f)] public float hitChancePerRound = 0.85f;

        [Header("Loadout")]
        public int startingRounds = 9999;

        public string WeaponName => "GAU-27A";
        public WeaponAimMode AimMode => WeaponAimMode.ForwardOnly;
    }
}
