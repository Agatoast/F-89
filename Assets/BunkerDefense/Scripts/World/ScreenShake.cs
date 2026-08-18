using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public sealed class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        private Vector3 _restPosition;
        private float _remaining;
        private float _duration;
        private float _magnitude;

        public static void Ensure(Camera camera)
        {
            if (camera == null || camera.GetComponent<ScreenShake>() != null)
            {
                return;
            }

            camera.gameObject.AddComponent<ScreenShake>();
        }

        public static void Play(float magnitude, float duration)
        {
            if (Instance == null)
            {
                Ensure(Camera.main);
            }

            Instance?.Trigger(magnitude, duration);
        }

        private void Awake()
        {
            Instance = this;
            _restPosition = BattlefieldLayout.CameraPosition;
            transform.position = _restPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Trigger(float magnitude, float duration)
        {
            magnitude = Mathf.Max(0f, magnitude);
            duration = Mathf.Max(0.01f, duration);
            _magnitude = Mathf.Max(_magnitude, magnitude);
            _remaining = Mathf.Max(_remaining, duration);
            _duration = _remaining;
        }

        private void LateUpdate()
        {
            if (_remaining <= 0f)
            {
                transform.position = _restPosition;
                _magnitude = 0f;
                return;
            }

            _remaining -= Time.deltaTime;
            var damp = _duration > 0.0001f ? Mathf.Clamp01(_remaining / _duration) : 0f;
            var offset = Random.insideUnitCircle * (_magnitude * damp);
            transform.position = _restPosition + new Vector3(offset.x, offset.y, 0f);

            if (_remaining <= 0f)
            {
                transform.position = _restPosition;
                _magnitude = 0f;
            }
        }
    }
}
