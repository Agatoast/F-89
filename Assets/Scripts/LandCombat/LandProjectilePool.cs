using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandProjectilePool : MonoBehaviour
    {
        [SerializeField] private int poolSize = LandGameConstants.ProjectilePoolSize;

        private LandProjectile[] pool;

        public void Warm()
        {
            pool = new LandProjectile[Mathf.Max(32, poolSize)];
            for (var i = 0; i < pool.Length; i++)
            {
                var projectileObject = new GameObject($"LandProjectile_{i}");
                projectileObject.transform.SetParent(transform, false);
                projectileObject.SetActive(false);
                var projectile = projectileObject.AddComponent<LandProjectile>();
                projectile.ConfigurePool(this);
                pool[i] = projectile;
            }
        }

        public LandProjectile Spawn(
            Vector2 position,
            Vector2 direction,
            float speed,
            float damage,
            float lifetime,
            Color color,
            float scale)
        {
            if (pool == null)
            {
                Warm();
            }

            for (var i = 0; i < pool.Length; i++)
            {
                if (!pool[i].IsActive)
                {
                    pool[i].Fire(position, direction, speed, damage, lifetime, color, scale);
                    return pool[i];
                }
            }

            return null;
        }

        public void Release(LandProjectile projectile)
        {
        }
    }
}
