using UnityEngine;

namespace F89.LandCombat
{
    public enum LandProjectileTeam
    {
        Player = 0,
        Enemy = 1
    }

    public sealed class LandProjectile : MonoBehaviour
    {
        private LandProjectilePool ownerPool;
        private Vector2 velocity;
        private float damage;
        private float lifetime;
        private LandProjectileTeam team;
        private SpriteRenderer spriteRenderer;
        private CircleCollider2D hitCollider;

        public bool IsActive { get; private set; }

        public void ConfigurePool(LandProjectilePool pool) => ownerPool = pool;

        public void Fire(
            Vector2 position,
            Vector2 direction,
            float speed,
            float projectileDamage,
            float projectileLifetime,
            Color color,
            float scale,
            LandProjectileTeam projectileTeam = LandProjectileTeam.Player,
            Vector2 inheritedVelocity = default)
        {
            EnsureVisuals();
            transform.position = position;
            var aim = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            // Muzzle velocity + shooter motion so runners can't overtake their own shots.
            velocity = aim * speed + inheritedVelocity;
            damage = projectileDamage;
            lifetime = projectileLifetime;
            team = projectileTeam;
            IsActive = true;
            gameObject.SetActive(true);
            spriteRenderer.sprite = projectileTeam == LandProjectileTeam.Player
                ? LandPlaceholderArt.GetPlayerPixelOblongBullet()
                : LandPlaceholderArt.GetPixelOblongBullet();
            // Player tracers: bright yellow so they read on arctic snow and bunker dark alike.
            // Black outline pixels stay black under tint. Enemy keep saturated red.
            spriteRenderer.color = projectileTeam == LandProjectileTeam.Player
                ? new Color(1f, 0.95f, 0.2f, 1f)
                : color;
            spriteRenderer.sortingOrder = projectileTeam == LandProjectileTeam.Player ? 9 : 8;
            transform.localScale = projectileTeam == LandProjectileTeam.Player
                ? new Vector3(1.45f, 1.45f, 1f)
                : Vector3.one;
            AlignToVelocity();
        }

        private void EnsureVisuals()
        {
            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = 8;
            }

            if (!TryGetComponent(out hitCollider))
            {
                hitCollider = gameObject.AddComponent<CircleCollider2D>();
                hitCollider.isTrigger = true;
            }

            // Match roughly half the oblong length in world units.
            hitCollider.radius = 0.08f;

            if (!TryGetComponent(out Rigidbody2D body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void AlignToVelocity()
        {
            if (velocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Despawn();
                return;
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            AlignToVelocity();

            if (team == LandProjectileTeam.Enemy)
            {
                TryHitPlayerOverlap();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive)
            {
                return;
            }

            if (team == LandProjectileTeam.Player)
            {
                if (other.GetComponentInParent<LandPlayerMotor>() != null
                    || other.GetComponentInParent<LandPlayerHealth>() != null)
                {
                    return;
                }

                var damageable = other.GetComponentInParent<ILandDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.ApplyDamage(damage);
                    Despawn();
                }

                return;
            }

            TryApplyEnemyHit(other.GetComponentInParent<LandPlayerHealth>());
        }

        private void TryHitPlayerOverlap()
        {
            var radius = hitCollider != null ? hitCollider.radius : 0.45f;
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            for (var i = 0; i < hits.Length; i++)
            {
                if (TryApplyEnemyHit(hits[i].GetComponentInParent<LandPlayerHealth>()))
                {
                    return;
                }
            }
        }

        private bool TryApplyEnemyHit(LandPlayerHealth health)
        {
            if (health == null || !health.IsAlive)
            {
                return false;
            }

            health.ApplyDamage(damage);
            Despawn();
            return true;
        }

        private void Despawn()
        {
            IsActive = false;
            gameObject.SetActive(false);
            ownerPool?.Release(this);
        }
    }
}
