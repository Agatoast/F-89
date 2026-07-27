using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Follows the ground player. Press X to shift the view so the avatar sits halfway down
    /// the screen (same idea as flight HUD TopDownFollowCamera), then X again to center.
    /// </summary>
    public sealed class LandGroundCameraFollow : MonoBehaviour
    {
        private const float ShiftedPlayerViewportY = 0.25f;
        private const float ViewShiftSmoothTime = 0.2f;

        private Transform target;
        private bool isViewShifted;
        private float currentViewShift;
        private float viewShiftVelocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            isViewShifted = false;
            currentViewShift = 0f;
            viewShiftVelocity = 0f;
        }

        private void Update()
        {
            if (GamePauseController.IsPaused || LandLootBagSession.IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                isViewShifted = !isViewShifted;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var camera = GetComponent<Camera>();
            var fullShift = camera != null && camera.orthographic
                ? (0.5f - ShiftedPlayerViewportY) * 2f * camera.orthographicSize
                : 0f;

            var viewShiftTarget = isViewShifted ? fullShift : 0f;
            currentViewShift = Mathf.SmoothDamp(
                currentViewShift,
                viewShiftTarget,
                ref viewShiftVelocity,
                ViewShiftSmoothTime);

            var position = transform.position;
            position.x = target.position.x;
            // Camera moves up so the player appears lower on screen.
            position.y = target.position.y + currentViewShift;
            transform.position = position;
        }
    }
}
