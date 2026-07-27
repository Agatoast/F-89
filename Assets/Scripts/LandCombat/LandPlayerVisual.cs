using UnityEngine;

namespace F89.LandCombat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LandPlayerVisual : MonoBehaviour
    {
        private const float IdleFps = 8f;
        private const float MoveFps = 10f;
        private const float RunFps = 12f;
        private const float ShotFps = 14f;
        private const float DeadFps = 8f;
        /// <summary>Move rating at/above this uses the Run clip while moving.</summary>
        private const int RunMoveRatingThreshold = 9;

        private SpriteRenderer spriteRenderer;
        private LandPlayerMotor motor;
        private LandPlayerCombat combat;
        private LandPlayerHealth health;
        private LandPlayerAttributes attributes;
        private LandPlayerSpriteSheet.Clip activeClip = LandPlayerSpriteSheet.Clip.Idle;
        private int frameIndex;
        private float frameTimer;
        private bool deadHold;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            motor = GetComponent<LandPlayerMotor>();
            combat = GetComponent<LandPlayerCombat>();
            health = GetComponent<LandPlayerHealth>();
            attributes = GetComponent<LandPlayerAttributes>();
            LandPlayerSpriteSheet.EnsureLoaded();
            ApplyFrame(LandPlayerSpriteSheet.Clip.Idle, 0);
        }

        private void LateUpdate()
        {
            var clip = ResolveClip();
            if (clip != activeClip)
            {
                activeClip = clip;
                frameIndex = 0;
                frameTimer = 0f;
                deadHold = false;
            }

            var frames = LandPlayerSpriteSheet.GetClip(activeClip);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (activeClip == LandPlayerSpriteSheet.Clip.Dead && deadHold)
            {
                ApplyFrame(activeClip, frames.Length - 1);
                UpdateFacing();
                return;
            }

            frameTimer += Time.deltaTime;
            var fps = GetFps(activeClip);
            var frameDuration = 1f / Mathf.Max(1f, fps);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                if (activeClip == LandPlayerSpriteSheet.Clip.Dead)
                {
                    if (frameIndex < frames.Length - 1)
                    {
                        frameIndex++;
                    }
                    else
                    {
                        deadHold = true;
                    }
                }
                else
                {
                    frameIndex = (frameIndex + 1) % frames.Length;
                }
            }

            ApplyFrame(activeClip, frameIndex);
            UpdateFacing();
        }

        private LandPlayerSpriteSheet.Clip ResolveClip()
        {
            if (health != null && health.IsUnconscious)
            {
                return LandPlayerSpriteSheet.Clip.Dead;
            }

            if (combat != null && combat.IsFiring)
            {
                return LandPlayerSpriteSheet.Clip.ShotHip;
            }

            var moving = motor != null && motor.CurrentSpeedNormalized > 0.05f;
            if (!moving)
            {
                return LandPlayerSpriteSheet.Clip.Idle;
            }

            var moveRating = attributes != null ? attributes.Move : LandGameConstants.DefaultMove;
            return moveRating >= RunMoveRatingThreshold
                ? LandPlayerSpriteSheet.Clip.Run
                : LandPlayerSpriteSheet.Clip.Move;
        }

        private void ApplyFrame(LandPlayerSpriteSheet.Clip clip, int index)
        {
            var frames = LandPlayerSpriteSheet.GetClip(clip);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, frames.Length - 1);
            spriteRenderer.sprite = frames[index];
            spriteRenderer.color = Color.white;
        }

        private void UpdateFacing()
        {
            if (motor == null)
            {
                return;
            }

            var aimX = motor.AimDirection.x;
            if (Mathf.Abs(aimX) > 0.05f)
            {
                spriteRenderer.flipX = aimX < 0f;
            }
        }

        private static float GetFps(LandPlayerSpriteSheet.Clip clip)
        {
            switch (clip)
            {
                case LandPlayerSpriteSheet.Clip.Move:
                    return MoveFps;
                case LandPlayerSpriteSheet.Clip.Run:
                    return RunFps;
                case LandPlayerSpriteSheet.Clip.ShotHip:
                case LandPlayerSpriteSheet.Clip.ShotAim:
                    return ShotFps;
                case LandPlayerSpriteSheet.Clip.Dead:
                    return DeadFps;
                default:
                    return IdleFps;
            }
        }
    }
}
