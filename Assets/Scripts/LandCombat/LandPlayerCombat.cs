using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandPlayerCombat : MonoBehaviour
    {
        private LandPlayerMotor motor;
        private LandProjectilePool projectilePool;
        private LandWeaponDefinition weapon;
        private float nextFireTime;
        private bool fireHeld;

        public bool IsFiring => fireHeld;

        public void Initialize(LandPlayerMotor playerMotor, LandProjectilePool pool)
        {
            motor = playerMotor;
            projectilePool = pool;
        }

        public void EquipWeapon(LandWeaponDefinition definition)
        {
            weapon = definition;
            nextFireTime = 0f;
        }

        public void SetFireHeld(bool held)
        {
            if (held && !fireHeld)
            {
                TryFire();
            }

            fireHeld = held;
        }

        private void Update()
        {
            if (fireHeld)
            {
                TryFire();
            }
        }

        private void TryFire()
        {
            if (weapon == null || Time.time < nextFireTime || motor == null || projectilePool == null)
            {
                return;
            }

            nextFireTime = Time.time + 1f / Mathf.Max(0.1f, weapon.RateOfFire);
            var aim = motor.AimDirection;
            var origin = LandSpriteAnchor.GetMuzzle(this, aim);
            var worldRange = LandUnits.ToWorld(weapon.Range);
            var muzzleSpeed = Mathf.Max(0.1f, weapon.ProjectileSpeed);
            var inherit = ResolveShooterVelocity();
            var lifetime = ComputeLifetime(aim, muzzleSpeed, inherit, worldRange);

            switch (weapon.Kind)
            {
                case LandWeaponKind.Spread:
                    FireSpread(origin, aim, weapon, inherit);
                    break;
                default:
                    projectilePool.Spawn(
                        origin,
                        aim,
                        muzzleSpeed,
                        weapon.Damage,
                        lifetime,
                        weapon.ProjectileColor,
                        weapon.ProjectileScale,
                        LandProjectileTeam.Player,
                        inherit);
                    break;
            }
        }

        private void FireSpread(Vector2 origin, Vector2 aim, LandWeaponDefinition spreadWeapon, Vector2 inherit)
        {
            var halfAngle = spreadWeapon.SpreadAngle * 0.5f;
            var count = Mathf.Max(2, spreadWeapon.SpreadCount);
            var worldRange = LandUnits.ToWorld(spreadWeapon.Range);
            var muzzleSpeed = Mathf.Max(0.1f, spreadWeapon.ProjectileSpeed);
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : i / (float)(count - 1);
                var angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                var direction = Rotate(aim, angle * Mathf.Deg2Rad);
                var lifetime = ComputeLifetime(direction, muzzleSpeed, inherit, worldRange);
                projectilePool.Spawn(
                    origin,
                    direction,
                    muzzleSpeed,
                    spreadWeapon.Damage,
                    lifetime,
                    spreadWeapon.ProjectileColor,
                    spreadWeapon.ProjectileScale * 0.85f,
                    LandProjectileTeam.Player,
                    inherit);
            }
        }

        private Vector2 ResolveShooterVelocity()
        {
            if (TryGetComponent<Rigidbody2D>(out var body))
            {
                return body.linearVelocity;
            }

            return Vector2.zero;
        }

        private static float ComputeLifetime(Vector2 aim, float muzzleSpeed, Vector2 inherit, float worldRange)
        {
            var aimDir = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            var alongAim = muzzleSpeed + Vector2.Dot(inherit, aimDir);
            return worldRange / Mathf.Max(0.1f, alongAim);
        }

        private static Vector2 Rotate(Vector2 vector, float radians)
        {
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }
    }
}
