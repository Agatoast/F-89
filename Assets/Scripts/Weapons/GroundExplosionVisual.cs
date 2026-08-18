using UnityEngine;

namespace F89.Weapons
{
    /// <summary>Billboarded explosion sprite — full animation for destroyed units, frame 0 for hits.</summary>
    public sealed class GroundExplosionVisual : MonoBehaviour
    {
        private const string RootName = "GroundExplosionEffects";

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int frameIndex;
        private float frameTimer;
        private float fps = ExplosionSpriteSheet.AnimationFps;
        private bool holdSingleFrame;
        private float holdRemaining;

        public static void PlayFullAnimation(Vector3 worldPosition, float worldWidth = 2.6f)
        {
            if (!ExplosionSpriteSheet.TryGetFrames(out var spriteFrames) || spriteFrames.Length == 0)
            {
                return;
            }

            Spawn(worldPosition, spriteFrames, worldWidth, holdSingleFrame: false, flattenToGround: true);
        }

        public static void PlayFirstFrame(
            Vector3 worldPosition,
            float holdSeconds = 0.4f,
            float worldWidth = 1.9f,
            bool flattenToGround = true)
        {
            if (!ExplosionSpriteSheet.TryGetFirstFrame(out var firstFrame) || firstFrame == null)
            {
                return;
            }

            var visual = Spawn(
                worldPosition,
                new[] { firstFrame },
                worldWidth,
                holdSingleFrame: true,
                flattenToGround);
            if (visual != null)
            {
                visual.holdRemaining = Mathf.Max(0.05f, holdSeconds);
            }
        }

        /// <summary>Hit flash at an aircraft's world position (keeps altitude for flight camera).</summary>
        public static void PlayAirHit(Vector3 worldPosition, float holdSeconds = 0.45f, float worldWidth = 12f)
        {
            PlayFirstFrame(worldPosition, holdSeconds, worldWidth, flattenToGround: false);
        }

        private static GroundExplosionVisual Spawn(
            Vector3 worldPosition,
            Sprite[] spriteFrames,
            float worldWidth,
            bool holdSingleFrame,
            bool flattenToGround)
        {
            var root = GetOrCreateRoot();
            var effectObject = new GameObject(holdSingleFrame ? "MissileHitFlash" : "VehicleExplosion");
            effectObject.transform.SetParent(root.transform, true);
            if (flattenToGround)
            {
                worldPosition.y = 0.04f;
            }
            else
            {
                worldPosition.y += 0.35f;
            }

            effectObject.transform.position = worldPosition;

            var rendererObject = new GameObject("Sprite");
            rendererObject.transform.SetParent(effectObject.transform, false);

            var renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = spriteFrames[0];
            renderer.sortingOrder = 40;

            var spriteWidth = spriteFrames[0].bounds.size.x;
            var scale = spriteWidth > 0.001f ? worldWidth / spriteWidth : 1f;
            rendererObject.transform.localScale = Vector3.one * scale;

            var visual = effectObject.AddComponent<GroundExplosionVisual>();
            visual.spriteRenderer = renderer;
            visual.frames = spriteFrames;
            visual.holdSingleFrame = holdSingleFrame;
            visual.frameIndex = 0;
            visual.frameTimer = 0f;
            visual.fps = ExplosionSpriteSheet.AnimationFps;
            return visual;
        }

        private static GameObject GetOrCreateRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                return existing;
            }

            return new GameObject(RootName);
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            }

            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            if (holdSingleFrame)
            {
                holdRemaining -= Time.deltaTime;
                if (holdRemaining <= 0f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (frames.Length <= 1)
            {
                Destroy(gameObject);
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = 1f / fps;
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex++;
                if (frameIndex >= frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }

                spriteRenderer.sprite = frames[frameIndex];
            }
        }
    }
}
