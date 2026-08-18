using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>TDP lightning from saucer to the center of the viewport — 1 hangar hit.</summary>
    public static class SaucerLightningFx
    {
        public static void Strike(Vector2 fromWorld, Vector2 strikeWorld, int hangarDamage = 1)
        {
            var go = new GameObject("SaucerLightning");
            var bolt = go.AddComponent<SaucerLightningBolt>();
            bolt.Play(fromWorld, strikeWorld, hangarDamage);
        }
    }

    public sealed class SaucerLightningBolt : MonoBehaviour
    {
        private float _age;
        private const float Duration = 0.14f;

        public void Play(Vector2 from, Vector2 to, int hangarDamage)
        {
            BuildBolt(from, to);
            HangarTarget.Instance?.ApplyDamage(hangarDamage);
            ScreenShake.Play(0.11f, 0.22f);
            FxService.SpawnHitSpark(to);
            CombatAudio.PlayLightningStrike();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= Duration)
            {
                Destroy(gameObject);
            }
        }

        private static void BuildBolt(Vector2 from, Vector2 to)
        {
            const int segments = 9;
            var points = new Vector2[segments + 1];
            points[0] = from;
            points[segments] = to;
            for (var i = 1; i < segments; i++)
            {
                var t = i / (float)segments;
                var basePoint = Vector2.Lerp(from, to, t);
                var lateral = Vector2.Perpendicular((to - from).normalized);
                var wobble = Random.Range(-0.22f, 0.22f) * (1f - Mathf.Abs(t - 0.5f) * 1.6f);
                points[i] = basePoint + lateral * wobble;
            }

            for (var i = 0; i < segments; i++)
            {
                SpawnSegment(points[i], points[i + 1], i);
            }

            SpawnImpactFlash(to);
        }

        private static void SpawnSegment(Vector2 a, Vector2 b, int index)
        {
            var delta = b - a;
            var length = delta.magnitude;
            if (length < 0.02f)
            {
                return;
            }

            var go = new GameObject($"LightningSeg_{index}");
            go.transform.SetParent(null, true);
            var renderer = go.AddComponent<SpriteRenderer>();
            var core = index % 2 == 0;
            renderer.sprite = SpriteFactory.Solid(
                core ? new Color(0.78f, 0.92f, 1f, 0.98f) : new Color(0.42f, 0.62f, 1f, 0.92f),
                4,
                4);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 50 + index;
            go.transform.position = (a + b) * 0.5f;
            go.transform.right = delta.normalized;
            go.transform.localScale = new Vector3(length, core ? 0.07f : 0.11f, 1f);
            Object.Destroy(go, 0.14f);
        }

        private static void SpawnImpactFlash(Vector2 position)
        {
            var go = new GameObject("LightningImpact");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Circle(new Color(0.72f, 0.88f, 1f, 0.95f), 28);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 65;
            go.transform.localScale = Vector3.one * Random.Range(0.35f, 0.5f);
            Object.Destroy(go, 0.1f);
        }
    }
}
