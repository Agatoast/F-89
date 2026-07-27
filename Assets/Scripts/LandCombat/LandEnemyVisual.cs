using System;
using UnityEngine;

namespace F89.LandCombat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LandEnemyVisual : MonoBehaviour
    {
        private const float IdleFps = 8f;
        private const float MoveFps = 10f;
        private const float ShotFps = 14f;
        private const float DeadFps = 8f;

        private SpriteRenderer spriteRenderer;
        private Transform faceTarget;
        private LandEnemySpriteSheet.Clip activeClip = LandEnemySpriteSheet.Clip.Idle;
        private int frameIndex;
        private float frameTimer;
        private bool dying;
        private bool deadHold;
        private bool moving;
        private float shootVisualUntil;
        private Action onDeathComplete;
        private Vector2 faceDirection = Vector2.left;

        public bool IsDying => dying;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LandEnemySpriteSheet.EnsureLoaded();
            ApplyFrame(LandEnemySpriteSheet.Clip.Idle, 0);
        }

        public void SetFaceTarget(Transform target) => faceTarget = target;

        public void SetFaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                faceDirection = direction.normalized;
            }
        }

        public void SetMoving(bool isMoving) => moving = isMoving;

        public void NotifyShot()
        {
            if (dying)
            {
                return;
            }

            shootVisualUntil = Time.time + 0.28f;
            activeClip = LandEnemySpriteSheet.Clip.ShotAim;
            frameIndex = 0;
            frameTimer = 0f;
        }

        public void PlayDeath(Action completed = null)
        {
            if (dying)
            {
                return;
            }

            dying = true;
            deadHold = false;
            moving = false;
            shootVisualUntil = 0f;
            onDeathComplete = completed;
            activeClip = LandEnemySpriteSheet.Clip.Dead;
            frameIndex = 0;
            frameTimer = 0f;
        }

        private void LateUpdate()
        {
            if (!dying)
            {
                UpdateFaceFromTarget();
                LandEnemySpriteSheet.Clip desired;
                if (Time.time < shootVisualUntil)
                {
                    desired = LandEnemySpriteSheet.Clip.ShotAim;
                }
                else if (moving)
                {
                    desired = LandEnemySpriteSheet.Clip.Move;
                }
                else
                {
                    desired = LandEnemySpriteSheet.Clip.Idle;
                }

                if (activeClip != desired)
                {
                    activeClip = desired;
                    frameIndex = 0;
                    frameTimer = 0f;
                }
            }

            var frames = LandEnemySpriteSheet.GetClip(activeClip);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (dying && deadHold)
            {
                ApplyFrame(activeClip, frames.Length - 1);
                UpdateFacing();
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = 1f / Mathf.Max(1f, GetFps(activeClip));
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                if (dying)
                {
                    if (frameIndex < frames.Length - 1)
                    {
                        frameIndex++;
                    }
                    else
                    {
                        deadHold = true;
                        var callback = onDeathComplete;
                        onDeathComplete = null;
                        callback?.Invoke();
                        return;
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

        private void UpdateFaceFromTarget()
        {
            if (faceTarget == null)
            {
                return;
            }

            var delta = (Vector2)faceTarget.position - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.001f)
            {
                faceDirection = delta.normalized;
            }
        }

        private void ApplyFrame(LandEnemySpriteSheet.Clip clip, int index)
        {
            var frames = LandEnemySpriteSheet.GetClip(clip);
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
            // Sheet faces left; flip when facing right.
            if (Mathf.Abs(faceDirection.x) > 0.05f)
            {
                spriteRenderer.flipX = faceDirection.x > 0f;
            }
        }

        private static float GetFps(LandEnemySpriteSheet.Clip clip)
        {
            switch (clip)
            {
                case LandEnemySpriteSheet.Clip.Move:
                case LandEnemySpriteSheet.Clip.Run:
                    return MoveFps;
                case LandEnemySpriteSheet.Clip.ShotHip:
                case LandEnemySpriteSheet.Clip.ShotAim:
                    return ShotFps;
                case LandEnemySpriteSheet.Clip.Dead:
                    return DeadFps;
                default:
                    return IdleFps;
            }
        }
    }
}
