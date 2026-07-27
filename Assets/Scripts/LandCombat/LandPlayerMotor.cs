using UnityEngine;

namespace F89.LandCombat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LandPlayerMotor : MonoBehaviour
    {
        private Rigidbody2D body;
        private LandPlayerAttributes attributes;
        private LandPlayerHealth health;
        private Vector2 moveInput;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        /// <summary>0–1 of configured move speed, from current planar velocity.</summary>
        public float CurrentSpeedNormalized { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            attributes = GetComponent<LandPlayerAttributes>();
            health = GetComponent<LandPlayerHealth>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);

        public void SetAimDirection(Vector2 worldPoint)
        {
            var from = LandSpriteAnchor.GetVisualCenter(this);
            var dir = worldPoint - from;
            if (dir.sqrMagnitude > 0.001f)
            {
                AimDirection = dir.normalized;
            }
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsUnconscious)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            var speed = attributes != null
                ? attributes.MoveSpeedWorldUnits
                : LandGameConstants.PlayerMoveSpeed;
            body.linearVelocity = moveInput * speed;
            CurrentSpeedNormalized = speed > 0.001f
                ? Mathf.Clamp01(body.linearVelocity.magnitude / speed)
                : 0f;
        }
    }
}
