using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class VehicleMissile : MonoBehaviour
    {
        private enum Phase
        {
            Flying,
            Holding
        }

        private const float HoldDuration = 1f;
        private const float MinScale = 0.2f;
        private const float MaxScale = 0.52f;

        private Vector2 _start;
        private Vector2 _end;
        private Vector2 _direction;
        private float _flightDuration;
        private float _flightElapsed;
        private float _holdElapsed;
        private int _damage;
        private Phase _phase = Phase.Flying;
        private SpriteRenderer _streakRenderer;
        private SpriteRenderer _bloomRenderer;

        public static void Launch(Vector2 from, Vector2 to, int damage)
        {
            var go = new GameObject("VehicleMissile");
            go.transform.position = from;

            var streak = go.AddComponent<SpriteRenderer>();
            streak.sprite = FlareSprite.Streak;
            streak.sortingLayerName = "Projectiles & FX";
            streak.sortingOrder = 54;

            var bloomGo = new GameObject("FlareBloom");
            bloomGo.transform.SetParent(go.transform, false);
            var bloom = bloomGo.AddComponent<SpriteRenderer>();
            bloom.sprite = FlareSprite.Bloom;
            bloom.sortingLayerName = "Projectiles & FX";
            bloom.sortingOrder = 55;

            var end = to + new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.08f, 0.08f));
            var direction = end - from;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.down;
            }
            else
            {
                direction.Normalize();
            }

            go.transform.up = direction;
            go.transform.localScale = Vector3.one * MinScale;
            bloomGo.transform.localPosition = direction * 0.42f;
            bloomGo.transform.localScale = Vector3.one * 0.55f;

            var missile = go.AddComponent<VehicleMissile>();
            missile._streakRenderer = streak;
            missile._bloomRenderer = bloom;
            missile._start = from;
            missile._end = end;
            missile._direction = direction;
            missile._flightDuration = 0.95f;
            missile._damage = damage;
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Flying:
                    UpdateFlying();
                    break;
                case Phase.Holding:
                    UpdateHolding();
                    break;
            }
        }

        private void UpdateFlying()
        {
            _flightElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_flightElapsed / _flightDuration);
            transform.position = Vector2.Lerp(_start, _end, t);
            var scale = Mathf.Lerp(MinScale, MaxScale, t);
            transform.localScale = Vector3.one * scale;
            transform.up = _direction;

            PulseBloom(t);

            if (t < 1f)
            {
                return;
            }

            transform.position = _end;
            transform.localScale = Vector3.one * MaxScale;
            _phase = Phase.Holding;
            _holdElapsed = 0f;
        }

        private void UpdateHolding()
        {
            _holdElapsed += Time.deltaTime;
            PulseBloom(1f);

            if (_holdElapsed < HoldDuration)
            {
                return;
            }

            FxService.SpawnExplosion(_end, 1.65f);
            ScreenShake.Play(0.26f, 0.45f);
            CombatAudio.PlayRocketExplosion();
            HangarTarget.Instance?.ApplyDamage(_damage);
            Destroy(gameObject);
        }

        private void PulseBloom(float intensity)
        {
            if (_bloomRenderer == null)
            {
                return;
            }

            var pulse = 0.82f + 0.18f * Mathf.PerlinNoise(Time.time * 14f, transform.position.x * 0.5f);
            var color = new Color(1.2f, 0.72f, 0.18f, 0.55f + intensity * 0.35f) * pulse;
            _bloomRenderer.color = color;

            if (_streakRenderer != null)
            {
                _streakRenderer.color = new Color(1f, 0.92f, 0.78f, 0.88f + intensity * 0.12f);
            }
        }
    }
}
