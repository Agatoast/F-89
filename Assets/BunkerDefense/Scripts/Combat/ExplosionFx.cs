using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class ExplosionFx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _fps;
        private float _time;
        private int _index = -1;

        public static void Play(Vector2 position, float scale, int sortingOrder, float fps = ExplosionSpriteSheet.FramesPerSecond)
        {
            ExplosionSpriteSheet.EnsureLoaded();
            var frames = ExplosionSpriteSheet.Frames;
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            var go = new GameObject("Explosion");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = "Projectiles & FX";
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = Vector3.one * scale;

            var fx = go.AddComponent<ExplosionFx>();
            fx._renderer = renderer;
            fx._frames = frames;
            fx._fps = fps;
            Object.Destroy(go, ExplosionSpriteSheet.Duration + 0.05f);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            var frame = Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(_time * _fps));
            if (frame == _index)
            {
                return;
            }

            _index = frame;
            _renderer.sprite = _frames[_index];
        }
    }
}
