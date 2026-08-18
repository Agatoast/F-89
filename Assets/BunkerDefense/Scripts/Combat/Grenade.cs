using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class Grenade : MonoBehaviour
    {
        private Vector2 _start;
        private Vector2 _end;
        private float _duration;
        private float _elapsed;
        private int _damage;

        public static void Throw(Vector2 from, Vector2 to, int damage)
        {
            var go = new GameObject("Grenade");
            go.transform.position = from;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Circle(new Color(0.22f, 0.32f, 0.18f, 1f), 16);
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = 50;
            go.transform.localScale = Vector3.one * 0.22f;

            var grenade = go.AddComponent<Grenade>();
            grenade._start = from;
            grenade._end = to + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.15f, 0.2f));
            grenade._duration = 0.82f;
            grenade._damage = damage;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var pos = Vector2.Lerp(_start, _end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 1.55f;
            transform.position = pos;
            transform.Rotate(0f, 0f, 420f * Time.deltaTime);

            if (t < 1f)
            {
                return;
            }

            FxService.SpawnExplosion(_end, 1.15f);
            ScreenShake.Play(0.14f, 0.28f);
            CombatAudio.PlayGrenadeExplosion();
            HangarTarget.Instance?.ApplyDamage(_damage);
            Destroy(gameObject);
        }
    }
}
