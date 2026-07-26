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
            var origin = (Vector2)transform.position;
            var aim = motor.AimDirection;
            var lifetime = weapon.RangeTiles / Mathf.Max(0.1f, weapon.ProjectileSpeed);

            switch (weapon.Kind)
            {
                case LandWeaponKind.Spread:
                    FireSpread(origin, aim, weapon);
                    break;
                default:
                    projectilePool.Spawn(
                        origin,
                        aim,
                        weapon.ProjectileSpeed,
                        weapon.Damage,
                        lifetime,
                        weapon.ProjectileColor,
                        weapon.ProjectileScale);
                    break;
            }
        }

        private void FireSpread(Vector2 origin, Vector2 aim, LandWeaponDefinition spreadWeapon)
        {
            var halfAngle = spreadWeapon.SpreadAngle * 0.5f;
            var count = Mathf.Max(2, spreadWeapon.SpreadCount);
            var lifetime = spreadWeapon.RangeTiles / Mathf.Max(0.1f, spreadWeapon.ProjectileSpeed);
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : i / (float)(count - 1);
                var angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                var direction = Rotate(aim, angle * Mathf.Deg2Rad);
                projectilePool.Spawn(
                    origin,
                    direction,
                    spreadWeapon.ProjectileSpeed,
                    spreadWeapon.Damage,
                    lifetime,
                    spreadWeapon.ProjectileColor,
                    spreadWeapon.ProjectileScale * 0.85f);
            }
        }

        private static Vector2 Rotate(Vector2 vector, float radians)
        {
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }
    }
}
