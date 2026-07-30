using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Follows the ground player. Default view keeps the avatar 25% above screen bottom; X toggles center.
    /// </summary>
    public sealed class LandGroundCameraFollow : MonoBehaviour
    {
        private const float DefaultPlayerViewportY = 0.25f;
        private const float CenterPlayerViewportY = 0.5f;
        private const float ViewShiftSmoothTime = 0.2f;

        private Transform target;
        private bool isViewShifted = true;
        private float currentViewShift;
        private float viewShiftVelocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            isViewShifted = true;
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
            var targetViewportY = isViewShifted ? DefaultPlayerViewportY : CenterPlayerViewportY;
            var fullShift = camera != null && camera.orthographic
                ? (CenterPlayerViewportY - targetViewportY) * 2f * camera.orthographicSize
                : 0f;

            var viewShiftTarget = fullShift;
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
