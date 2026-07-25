using UnityEngine;

namespace F89.LandCombat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LandPlayerMotor : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 moveInput;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);

        public void SetAimDirection(Vector2 worldPoint)
        {
            var dir = worldPoint - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                AimDirection = dir.normalized;
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = moveInput * LandGameConstants.PlayerMoveSpeed;
        }
    }
}
