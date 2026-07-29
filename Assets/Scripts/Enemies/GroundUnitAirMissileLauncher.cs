using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Launches visible homing missiles for ground unit / building air shots at the player.
    /// </summary>
    public static class GroundUnitAirMissileLauncher
    {
        public static void LaunchAtAirTarget(
            VehicleUnitDefinition definition,
            Transform launchTransform,
            LockableTarget airTarget,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            if (definition == null
                || launchTransform == null
                || airTarget == null
                || !airTarget.IsAlive
                || worldMap == null
                || profile == null)
            {
                return;
            }

            var config = GroundUnitAirMissileConfig.FromDefinition(definition, worldMap, profile);
            LaunchWithConfig(config, launchTransform, airTarget, worldMap, profile, ResolveBodyColor(definition));
        }

        /// <summary>For future building air weapons.</summary>
        public static void LaunchAtAirTarget(
            GroundUnitAirMissileConfig config,
            Transform launchTransform,
            LockableTarget airTarget,
            WorldMapConfig worldMap,
            FlightProfile profile,
            Color bodyColor)
        {
            if (config == null
                || launchTransform == null
                || airTarget == null
                || !airTarget.IsAlive)
            {
                return;
            }

            LaunchWithConfig(config, launchTransform, airTarget, worldMap, profile, bodyColor);
        }

        private static void LaunchWithConfig(
            GroundUnitAirMissileConfig config,
            Transform launchTransform,
            LockableTarget airTarget,
            WorldMapConfig worldMap,
            FlightProfile profile,
            Color bodyColor)
        {
            var launchOrigin = launchTransform.position;
            launchOrigin.y = airTarget.transform.position.y;
            var toTarget = airTarget.transform.position - launchOrigin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                toTarget = launchTransform.forward;
            }

            var seeker = MissileSeekerSettings.FromGroundUnitAir(config, airTarget);
            HomingMissile.Launch(
                config,
                worldMap,
                profile,
                launchOrigin + Vector3.up * 0.35f,
                toTarget.normalized,
                airTarget,
                lockedShot: true,
                bodyColor,
                accuracyMultiplier: 1f,
                launchVelocityWorld: Vector3.zero,
                seekerSettings: seeker);
        }

        private static Color ResolveBodyColor(VehicleUnitDefinition definition)
        {
            if (definition.isTroop)
            {
                return new Color(0.72f, 0.26f, 0.10f);
            }

            if (definition.isFlier)
            {
                return new Color(0.82f, 0.18f, 0.12f);
            }

            return new Color(0.78f, 0.22f, 0.08f);
        }
    }
}
