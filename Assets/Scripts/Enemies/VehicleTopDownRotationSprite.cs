using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Picks a heading frame from a five-angle top-down sheet (0°–180°, mirrored for 180°–360°).
    /// Cancels inherited parent yaw so pre-baked rotation frames stay world-aligned.
    /// </summary>
    public sealed class VehicleTopDownRotationSprite : MonoBehaviour
    {
        private const float FlipHysteresisDegrees = 8f;

        private static readonly Quaternion FlatWorldRotation = Quaternion.Euler(90f, 180f, 0f);

        private Transform headingSource;
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int lastFrameIndex = -1;
        private bool lastFlipX;

        public void Configure(Transform source, SpriteRenderer renderer, Sprite[] rotationFrames)
        {
            headingSource = source;
            spriteRenderer = renderer;
            frames = rotationFrames;
            lastFrameIndex = -1;
            ApplyFrame(force: true);
        }

        private void LateUpdate()
        {
            ApplyFrame(force: false);
        }

        private void ApplyFrame(bool force)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            transform.rotation = FlatWorldRotation;

            var source = headingSource != null ? headingSource : transform.parent;
            if (source == null)
            {
                return;
            }

            var forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (heading < 0f)
            {
                heading += 360f;
            }

            var flipX = ResolveFlipX(heading, force);
            var mirrorHeading = flipX ? 360f - heading : heading;
            // Floor + half-step bias avoids frame oscillation at 45° boundaries.
            var frameIndex = Mathf.Clamp(
                Mathf.FloorToInt((mirrorHeading + 22.5f) / 45f),
                0,
                frames.Length - 1);

            if (!force && frameIndex == lastFrameIndex && flipX == lastFlipX)
            {
                return;
            }

            lastFrameIndex = frameIndex;
            lastFlipX = flipX;
            spriteRenderer.flipX = flipX;
            var frame = frames[frameIndex];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        private bool ResolveFlipX(float heading, bool force)
        {
            if (force || lastFrameIndex < 0)
            {
                return heading > 180f && heading < 360f;
            }

            if (!lastFlipX)
            {
                return heading > 180f + FlipHysteresisDegrees;
            }

            return heading >= 180f - FlipHysteresisDegrees;
        }
    }
}
