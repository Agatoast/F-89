using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public static class FxService
    {
        public static void SpawnTracer(Vector2 from, Vector2 to)
        {
            var go = new GameObject("Tracer");
            go.transform.position = from;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Solid(new Color(1f, 0.85f, 0.35f, 0.95f), 32, 4);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 20;

            var delta = to - from;
            var length = Mathf.Max(0.2f, delta.magnitude);
            go.transform.right = delta.sqrMagnitude > 0.0001f ? (Vector3)delta.normalized : Vector3.right;
            go.transform.localScale = new Vector3(length, 0.045f, 1f);
            Object.Destroy(go, 0.045f);
        }

        public static void SpawnMuzzleFlash(Vector2 position)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Circle(new Color(1f, 0.78f, 0.28f, 0.95f), 24);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 115;
            go.transform.localScale = Vector3.one * Random.Range(0.22f, 0.34f);
            Object.Destroy(go, 0.04f);
        }

        public static void SpawnBarrelFlame(Vector2 muzzle, Vector2 aim)
        {
            var direction = aim - muzzle;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }
            else
            {
                direction.Normalize();
            }

            var go = new GameObject("BarrelFlame");
            go.transform.position = muzzle + direction * Random.Range(0.02f, 0.05f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.VerticalGradient(
                new Color(1f, 0.42f, 0.04f, 0.95f),
                new Color(1f, 0.92f, 0.38f, 0.55f),
                6,
                32);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 112;
            go.transform.right = direction;
            var length = Random.Range(0.14f, 0.26f);
            var thickness = Random.Range(0.035f, 0.06f);
            go.transform.localScale = new Vector3(length, thickness, 1f);

            var flash = new GameObject("BarrelFlash");
            flash.transform.SetParent(go.transform, false);
            flash.transform.localPosition = Vector3.zero;
            var flashRenderer = flash.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = SpriteFactory.Circle(new Color(1f, 0.88f, 0.45f, 0.9f), 16);
            flashRenderer.sortingLayerName = "Projectiles & FX";
            flashRenderer.sortingOrder = 113;
            flash.transform.localScale = Vector3.one * Random.Range(0.08f, 0.12f);

            Object.Destroy(go, Random.Range(0.035f, 0.055f));
        }

        public static void SpawnBarrelFlames(Vector2[] muzzles, Vector2 aim)
        {
            if (muzzles == null)
            {
                return;
            }

            for (var i = 0; i < muzzles.Length; i++)
            {
                SpawnBarrelFlame(muzzles[i], aim);
            }
        }

        public static void SpawnHitSpark(Vector2 position)
        {
            var go = new GameObject("HitSpark");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Circle(new Color(1f, 0.55f, 0.2f, 1f), 20);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 22;
            go.transform.localScale = Vector3.one * 0.28f;
            Object.Destroy(go, 0.08f);
        }

        public static void SpawnExplosion(Vector2 position, float scale = 1f, int sortingOrder = 30)
        {
            ExplosionFx.Play(position, 0.85f * scale, sortingOrder);
        }
    }
}
