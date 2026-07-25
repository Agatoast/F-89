using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandProjectile : MonoBehaviour
    {
        private LandProjectilePool ownerPool;
        private Vector2 velocity;
        private float damage;
        private float lifetime;
        private SpriteRenderer spriteRenderer;

        public bool IsActive { get; private set; }

        public void ConfigurePool(LandProjectilePool pool) => ownerPool = pool;

        public void Fire(Vector2 position, Vector2 direction, float speed, float projectileDamage, float projectileLifetime, Color color, float scale)
        {
            transform.position = position;
            velocity = direction.normalized * speed;
            damage = projectileDamage;
            lifetime = projectileLifetime;
            IsActive = true;
            gameObject.SetActive(true);
            EnsureVisuals();
            spriteRenderer.color = color;
            transform.localScale = Vector3.one * scale;
        }

        private void EnsureVisuals()
        {
            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = LandPlaceholderArt.GetPixelCircle();
                spriteRenderer.sortingOrder = 8;
            }

            if (!TryGetComponent(out CircleCollider2D collider))
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.45f;
            }

            if (!TryGetComponent(out Rigidbody2D body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.bodyType = RigidbodyType2D.Kinematic;
            }
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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive || other.GetComponentInParent<LandPlayerMotor>() != null)
            {
                return;
            }

            var damageable = other.GetComponentInParent<ILandDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.ApplyDamage(damage);
                Despawn();
            }
        }

        private void Despawn()
        {
            IsActive = false;
            gameObject.SetActive(false);
            ownerPool?.Release(this);
        }
    }
}
