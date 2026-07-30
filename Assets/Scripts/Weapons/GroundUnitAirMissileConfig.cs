using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Runtime missile config for vehicle/troop/building air shots at the player (or other air targets).
    /// </summary>
    public sealed class GroundUnitAirMissileConfig : ScriptableObject, IMissileWeaponConfig
    {
        [SerializeField] private float rangeMiles = 20f;
        [SerializeField] private float lockHitChance = 0.5f;
        [SerializeField] private int airDamage = 1;
        [SerializeField] private float speedMilesPerSecond = 0.42f;
        [SerializeField] private string weaponName = "Ground Air Missile";

        [Header("Seeker / Flares")]
        public float flareRetargetRangeMiles = 10f;
        [Range(0f, 1f)] public float flareRetargetChance = 1f;
        [Range(0f, 1f)] public float flareBurnoutReacquireChance = 0.1f;
        public float seekerForwardHalfAngleDegrees = 90f;

        public int AirDamage => airDamage;

        public void Configure(
            string label,
            float airRangeMiles,
            float chanceToHitAir,
            int airGhpDamage,
            float launchSpeedMilesPerSecond)
        {
            weaponName = string.IsNullOrWhiteSpace(label) ? "Ground Air Missile" : label;
            rangeMiles = Mathf.Max(0.5f, airRangeMiles);
            lockHitChance = Mathf.Clamp01(chanceToHitAir);
            airDamage = Mathf.Max(1, airGhpDamage);
            speedMilesPerSecond = Mathf.Max(0.05f, launchSpeedMilesPerSecond);
        }

        public static GroundUnitAirMissileConfig FromDefinition(
            Enemies.VehicleUnitDefinition definition,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            var config = CreateInstance<GroundUnitAirMissileConfig>();
            if (definition == null)
            {
                return config;
            }

            config.Configure(
                definition.abbreviation,
                ResolveAirRangeMiles(definition, worldMap, profile),
                definition.chanceToHitAir,
                definition.airDamage,
                ResolveLaunchSpeedMilesPerSecond(definition));
            return config;
        }

        private static float ResolveAirRangeMiles(
            Enemies.VehicleUnitDefinition definition,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null || definition.airRangeTics <= 0f)
            {
                return definition.airRangeTics * ticSize / (20f * ticSize);
            }

            var worldUnitsPerMile = worldMap.GridSpacingTics * ticSize / worldMap.milesPerGrid;
            if (worldUnitsPerMile <= 0f)
            {
                return 20f;
            }

            return definition.airRangeTics * ticSize / worldUnitsPerMile;
        }

        private static float ResolveLaunchSpeedMilesPerSecond(Enemies.VehicleUnitDefinition definition)
        {
            if (definition.isFlier)
            {
                return 0.55f;
            }

            if (definition.isTroop)
            {
                return 0.35f;
            }

            return 0.42f;
        }

        float ILockCapableWeapon.RangeMiles => rangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => 0f;
        float ILockCapableWeapon.MaxBeepInterval => 0.5f;
        float ILockCapableWeapon.MinBeepInterval => 0.5f;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => LockableTargetKind.Air;
        WeaponAimMode ILockCapableWeapon.AimMode => WeaponAimMode.ForwardLock;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => seekerForwardHalfAngleDegrees;
        WeaponEngagementType ILockCapableWeapon.EngagementType => WeaponEngagementType.AirToAirMissile;
        float IMissileWeaponConfig.LockHitChance => lockHitChance;
        float IMissileWeaponConfig.SpeedMilesPerSecond =>
            speedMilesPerSecond * EnemyMissileBalance.SpeedMultiplier;
        float IMissileWeaponConfig.MissileLifetimeSeconds => 60f;
        float IMissileWeaponConfig.UnlockedHitConeHalfAngleDegrees => 1.5f;
        float IMissileWeaponConfig.CollisionRadiusTics => 0.5f;
        string IMissileWeaponConfig.WeaponName => weaponName;
        WeaponAimMode IMissileWeaponConfig.AimMode => WeaponAimMode.ForwardLock;
    }
}
