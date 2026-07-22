using UnityEngine;

namespace F89.Enemies
{
    [CreateAssetMenu(fileName = "BasicTankConfig", menuName = "F-89/Enemies/Basic Tank Config")]
    public class BasicTankConfig : ScriptableObject
    {
        public const string DefaultUnitName = "Basic Tank";

        [Header("Identity")]
        public string unitName = DefaultUnitName;

        [Header("Engagement")]
        public float acquisitionRangeMiles = 25f;
        public float launchRangeMiles = 20f;
        public int missileCapacity = 6;
        public int missilesPerSalvo = 3;
        public float salvoLaunchIntervalSeconds = 2f;
        public float retreatSpeedMph = 20f;

        [Header("Missile")]
        public float missileSpeedMph = 1535f;
        public float missileRangeMiles = 20f;
        [Range(0f, 1f)] public float missileLockHitChance = 0.85f;
        [Range(0f, 1f)] public float missilePlayerDestroyChanceOnHit = 0.5f;
        public float flareRetargetRangeMiles = 10f;
        [Range(0f, 1f)] public float flareRetargetChance = 1f;
        [Range(0f, 1f)] public float flareBurnoutReacquireChance = 0.1f;

        public float MissileSpeedMilesPerSecond => missileSpeedMph / 3600f;
        public float RetreatSpeedMilesPerSecond => retreatSpeedMph / 3600f;
    }
}
