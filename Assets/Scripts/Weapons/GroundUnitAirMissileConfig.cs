using System.Collections.Generic;
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
        private static readonly Dictionary<int, GroundUnitAirMissileConfig> ConfigByDefinitionId = new();        [SerializeField] private float rangeMiles = 20f;
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
            if (definition == null)
            {
                return CreateInstance<GroundUnitAirMissileConfig>();
            }

            var definitionId = definition.GetEntityId().GetHashCode();
            if (ConfigByDefinitionId.TryGetValue(definitionId, out var cached) && cached != null)
            {
                return cached;
            }

            var config = CreateInstance<GroundUnitAirMissileConfig>();
            config.Configure(
                definition.abbreviation,
                definition.airRangeMiles,
                definition.chanceToHitAir,
                definition.airDamage,
                ResolveLaunchSpeedMilesPerSecond(definition));
            ConfigByDefinitionId[definitionId] = config;
            return config;
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
