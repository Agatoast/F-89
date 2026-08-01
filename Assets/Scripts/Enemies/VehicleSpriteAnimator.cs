using UnityEngine;

namespace F89.Enemies
{
    /// <summary>Cycles a SpriteRenderer through fixed frames (e.g. TDP saucer spin).</summary>
    public sealed class VehicleSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private float fps = 12f;
        private float frameTimer;
        private int frameIndex;

        public void Configure(SpriteRenderer renderer, Sprite[] animationFrames, float animationFps)
        {
            spriteRenderer = renderer;
            frames = animationFrames;
            fps = Mathf.Max(0.01f, animationFps);
            frameTimer = 0f;
            frameIndex = 0;
            if (spriteRenderer != null && frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (spriteRenderer == null || frames == null || frames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = 1f / fps;
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
                if (frames[frameIndex] != null)
                {
                    spriteRenderer.sprite = frames[frameIndex];
                }
            }
        }
    }
}
