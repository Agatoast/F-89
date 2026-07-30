using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.CameraSystems
{
    public class TopDownFollowCamera : MonoBehaviour
    {
        private const float DefaultPlaneViewportY = 0.25f;
        private const float CenterPlaneViewportY = 0.5f;

        [SerializeField] private Transform target;
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Vector3 offset = new Vector3(0f, 38f, -12f);
        [SerializeField] private float followSmoothTime = 0.18f;
        [SerializeField] private float viewShiftSmoothTime = 0.2f;
        [SerializeField] private float viewShiftDistanceFallback = 14f;
        [SerializeField] private float maxVisibleHorizontalMiles = 20f;
        [SerializeField] private float zoomScrollSensitivity = 0.12f;
        [SerializeField] private float maxZoomOffsetScaleFallback = 4.5f;

        private Vector3 velocity;
        private bool isViewShifted = true;
        private float currentViewShift;
        private float viewShiftVelocity;
        private float zoomLevel;
        private float maxOffsetScale = 1f;
        private bool zoomLimitsReady;
        private bool zoomChangedThisFrame;
        private float computedViewShiftDistance;

        public bool IsViewShifted => isViewShifted;
        public float ZoomLevel => zoomLevel;
        public float MaxVisibleHorizontalMiles => maxVisibleHorizontalMiles;

        public float GetVisibleHorizontalMiles()
        {
            if (target == null)
            {
                return 0f;
            }

            var milesPerWorldUnit = GetMilesPerWorldUnit();
            var zoomScale = Mathf.Lerp(1f, maxOffsetScale, zoomLevel);
            var worldOffset = target.rotation * (offset * zoomScale);
            var widthWorld = MeasureVisibleHorizontalWidth(target.position, worldOffset);
            return widthWorld * milesPerWorldUnit;
        }

        public float GetVisibleVerticalMiles()
        {
            if (target == null)
            {
                return 0f;
            }

            var milesPerWorldUnit = GetMilesPerWorldUnit();
            var zoomScale = Mathf.Lerp(1f, maxOffsetScale, zoomLevel);
            var worldOffset = target.rotation * (offset * zoomScale);
            var heightWorld = MeasureVisibleVerticalHeight(target.position, worldOffset);
            return heightWorld * milesPerWorldUnit;
        }

        public void Configure(Transform followTarget, AircraftController aircraftController)
        {
            target = followTarget;
            aircraft = aircraftController;
            velocity = Vector3.zero;
            isViewShifted = true;
            currentViewShift = 0f;
            viewShiftVelocity = 0f;
            zoomLevel = 0f;
            zoomLimitsReady = false;
            maxOffsetScale = maxZoomOffsetScaleFallback;
            computedViewShiftDistance = viewShiftDistanceFallback;
            SnapToTarget();
        }

        private void Update()
        {
            if (GamePauseController.IsPaused || AntarcticaMapOverlay.IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                isViewShifted = !isViewShifted;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                zoomLevel = Mathf.Clamp01(zoomLevel - scroll * zoomScrollSensitivity);
                zoomChangedThisFrame = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            EnsureZoomLimits();

            var zoomScale = Mathf.Lerp(1f, maxOffsetScale, zoomLevel);
            var worldOffset = target.rotation * (offset * zoomScale);

            computedViewShiftDistance = ComputeViewShiftForwardDistance(
                isViewShifted ? DefaultPlaneViewportY : CenterPlaneViewportY,
                worldOffset);

            var viewShiftTarget = computedViewShiftDistance;
            currentViewShift = Mathf.SmoothDamp(
                currentViewShift,
                viewShiftTarget,
                ref viewShiftVelocity,
                viewShiftSmoothTime);

            var focusPoint = target.position + GetHorizontalForward() * currentViewShift;
            var desiredPosition = focusPoint + worldOffset;

            if (zoomChangedThisFrame)
            {
                transform.position = desiredPosition;
                velocity = Vector3.zero;
                zoomChangedThisFrame = false;
            }
            else
            {
                var smoothTime = AutopilotController.Instance != null && AutopilotController.Instance.IsFlying
                    ? 0.04f
                    : followSmoothTime;
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref velocity,
                    smoothTime);
            }

            transform.LookAt(focusPoint);
        }

        private float ComputeViewShiftForwardDistance(float targetViewportY, Vector3 worldOffset)
        {
            var camera = GetComponent<Camera>();
            if (camera == null || target == null)
            {
                return viewShiftDistanceFallback;
            }

            var planePos = target.position;
            var forward = GetHorizontalForward();
            var savedPosition = transform.position;
            var savedRotation = transform.rotation;

            var low = 0f;
            var high = 32f;
            for (var expand = 0; expand < 8; expand++)
            {
                ApplyCameraPose(planePos + forward * high, worldOffset);
                var viewport = camera.WorldToViewportPoint(planePos);
                if (viewport.z > 0f && viewport.y <= targetViewportY)
                {
                    break;
                }

                high *= 1.5f;
            }

            for (var i = 0; i < 20; i++)
            {
                var mid = (low + high) * 0.5f;
                ApplyCameraPose(planePos + forward * mid, worldOffset);
                var viewport = camera.WorldToViewportPoint(planePos);
                if (viewport.y > targetViewportY)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            transform.position = savedPosition;
            transform.rotation = savedRotation;

            return (low + high) * 0.5f;
        }

        private void ApplyCameraPose(Vector3 focusPoint, Vector3 worldOffset)
        {
            transform.position = focusPoint + worldOffset;
            transform.LookAt(focusPoint);
        }

        private Vector3 GetHorizontalForward()
        {
            var forward = target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            var zoomScale = 1f;
            var worldOffset = target.rotation * (offset * zoomScale);
            computedViewShiftDistance = ComputeViewShiftForwardDistance(
                isViewShifted ? DefaultPlaneViewportY : CenterPlaneViewportY,
                worldOffset);
            currentViewShift = computedViewShiftDistance;

            var focusPoint = target.position + GetHorizontalForward() * currentViewShift;
            transform.position = focusPoint + worldOffset;
            transform.LookAt(focusPoint);
        }

        private void EnsureZoomLimits()
        {
            if (zoomLimitsReady)
            {
                return;
            }

            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return;
            }

            maxOffsetScale = ComputeOffsetScaleForHorizontalMiles(maxVisibleHorizontalMiles);
            zoomLimitsReady = true;
        }

        private float ComputeOffsetScaleForHorizontalMiles(float horizontalMiles)
        {
            if (target == null || horizontalMiles <= 0f)
            {
                return maxZoomOffsetScaleFallback;
            }

            var targetWidthWorld = horizontalMiles * GetWorldUnitsPerMile();
            if (targetWidthWorld <= 0f)
            {
                return maxZoomOffsetScaleFallback;
            }

            var low = 1f;
            var high = 50f;
            for (var i = 0; i < 20; i++)
            {
                var mid = (low + high) * 0.5f;
                var width = MeasureVisibleHorizontalWidth(target.position, offset * mid);
                if (width < targetWidthWorld)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return Mathf.Max(high, 1f);
        }

        private float GetWorldUnitsPerMile()
        {
            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return 20f;
            }

            var worldUnitsPerGrid = aircraft.WorldMap.GridSpacingTics * aircraft.Profile.ticSizeWorldUnits;
            return worldUnitsPerGrid / aircraft.WorldMap.milesPerGrid;
        }

        private float GetMilesPerWorldUnit()
        {
            var worldUnitsPerMile = GetWorldUnitsPerMile();
            return worldUnitsPerMile > 0f ? 1f / worldUnitsPerMile : 1f / 20f;
        }

        private float MeasureVisibleHorizontalWidth(Vector3 focusPoint, Vector3 cameraOffset)
        {
            var camera = GetComponent<Camera>();
            if (camera == null)
            {
                return 0f;
            }

            var savedPosition = transform.position;
            var savedRotation = transform.rotation;

            transform.position = focusPoint + cameraOffset;
            transform.LookAt(focusPoint);

            var groundY = focusPoint.y;
            var leftRay = camera.ViewportPointToRay(new Vector3(0f, 0.5f, 0f));
            var rightRay = camera.ViewportPointToRay(new Vector3(1f, 0.5f, 0f));

            var width = 0f;
            if (TryIntersectHorizontalPlane(leftRay, groundY, out var leftHit)
                && TryIntersectHorizontalPlane(rightRay, groundY, out var rightHit))
            {
                var delta = rightHit - leftHit;
                delta.y = 0f;
                width = delta.magnitude;
            }

            transform.position = savedPosition;
            transform.rotation = savedRotation;

            return width;
        }

        private float MeasureVisibleVerticalHeight(Vector3 focusPoint, Vector3 cameraOffset)
        {
            var camera = GetComponent<Camera>();
            if (camera == null)
            {
                return 0f;
            }

            var savedPosition = transform.position;
            var savedRotation = transform.rotation;

            transform.position = focusPoint + cameraOffset;
            transform.LookAt(focusPoint);

            var groundY = focusPoint.y;
            var bottomRay = camera.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
            var topRay = camera.ViewportPointToRay(new Vector3(0.5f, 1f, 0f));

            var height = 0f;
            if (TryIntersectHorizontalPlane(bottomRay, groundY, out var bottomHit)
                && TryIntersectHorizontalPlane(topRay, groundY, out var topHit))
            {
                var delta = topHit - bottomHit;
                delta.y = 0f;
                height = delta.magnitude;
            }

            transform.position = savedPosition;
            transform.rotation = savedRotation;

            return height;
        }

        private static bool TryIntersectHorizontalPlane(Ray ray, float planeY, out Vector3 point)
        {
            point = default;
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            var t = (planeY - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * t;
            return true;
        }
    }
}
