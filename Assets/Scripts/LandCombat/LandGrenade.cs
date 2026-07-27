using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Thrown grenade that travels to the crosshair point, then blasts a radius.</summary>
    public sealed class LandGrenade : MonoBehaviour
    {
        private Vector2 target;
        private float speed;
        private bool exploded;
        private SpriteRenderer spriteRenderer;

        public static void Throw(Vector2 origin, Vector2 targetWorld)
        {
            var go = new GameObject("LandGrenade");
            go.transform.position = origin;
            var grenade = go.AddComponent<LandGrenade>();
            grenade.Configure(origin, targetWorld);
        }

        private void Configure(Vector2 origin, Vector2 targetWorld)
        {
            target = targetWorld;
            speed = LandGameConstants.GrenadeThrowSpeed;
            EnsureVisual();

            var delta = target - origin;
            if (delta.sqrMagnitude < 0.05f)
            {
                // Crosshair on top of thrower — detonate immediately.
                Explode();
            }
        }

        private void EnsureVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LandPlaceholderArt.GetPixelCircle();
            spriteRenderer.color = new Color(0.25f, 0.55f, 0.2f, 1f);
            spriteRenderer.sortingOrder = 12;
            transform.localScale = Vector3.one * 0.28f;
        }

        private void Update()
        {
            if (exploded)
            {
                return;
            }

            var pos = (Vector2)transform.position;
            var toTarget = target - pos;
            var step = speed * Time.deltaTime;
            if (toTarget.sqrMagnitude <= step * step)
            {
                transform.position = target;
                Explode();
                return;
            }

            transform.position = pos + toTarget.normalized * step;
        }

        private void Explode()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            var center = (Vector2)transform.position;
            var radius = LandUnits.ToWorld(LandGameConstants.GrenadeBlastRadiusLandUnits);
            var hits = Physics2D.OverlapCircleAll(center, radius);
            var damaged = 0;

            for (var i = 0; i < hits.Length; i++)
            {
                var damageable = hits[i].GetComponentInParent<ILandDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                // Avoid double-hitting the same actor when multiple colliders overlap.
                if (AlreadyDamagedThisFrame(damageable, i, hits))
                {
                    continue;
                }

                damageable.ApplyDamage(LandGameConstants.GrenadeDamage);
                damaged++;
            }

            SpawnBlastFlash(center, radius);
            Debug.Log(
                $"F-89 Land: Grenade exploded ({LandCombatConsumables.GrenadeCount} left) — "
                + $"hit {damaged} in {LandGameConstants.GrenadeBlastRadiusLandUnits:0} units.");
            Destroy(gameObject);
        }

        private static bool AlreadyDamagedThisFrame(ILandDamageable damageable, int index, Collider2D[] hits)
        {
            for (var j = 0; j < index; j++)
            {
                if (hits[j].GetComponentInParent<ILandDamageable>() == damageable)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SpawnBlastFlash(Vector2 center, float radius)
        {
            var flash = new GameObject("GrenadeBlast");
            flash.transform.position = new Vector3(center.x, center.y, 0f);
            var renderer = flash.AddComponent<SpriteRenderer>();
            renderer.sprite = LandPlaceholderArt.GetPixelCircle();
            renderer.color = new Color(1f, 0.55f, 0.15f, 0.55f);
            renderer.sortingOrder = 20;
            var diameter = Mathf.Max(0.5f, radius * 2f);
            flash.transform.localScale = Vector3.one * diameter;
            Object.Destroy(flash, 0.2f);
        }
    }
}
