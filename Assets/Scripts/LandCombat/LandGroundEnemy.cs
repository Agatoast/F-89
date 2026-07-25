using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandGroundEnemy : MonoBehaviour, ILandDamageable
    {
        [SerializeField] private float maxHealth = 30f;
        private float currentHealth;
        private SpriteRenderer spriteRenderer;

        public bool IsAlive => currentHealth > 0f;

        public void Initialize(Vector2 position, Color color)
        {
            transform.position = position;
            currentHealth = maxHealth;
            EnsureVisuals();
            spriteRenderer.color = color;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                LandGroundSceneController.RegisterKill();
                Destroy(gameObject);
            }
        }

        private void EnsureVisuals()
        {
            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = LandPlaceholderArt.GetPixelCircle();
                spriteRenderer.sortingOrder = 5;
            }

            transform.localScale = Vector3.one * 0.35f;

            if (!TryGetComponent(out CircleCollider2D collider))
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
            }

            if (!TryGetComponent(out Rigidbody2D body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }
}
