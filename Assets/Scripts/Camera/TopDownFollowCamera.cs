using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.CameraSystems
{
    public class TopDownFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Vector3 offset = new Vector3(0f, 38f, -12f);
        [SerializeField] private float followSmoothTime = 0.18f;
        [SerializeField] private float lookAheadDistance = 8f;
        [SerializeField] private float lookAheadSpeedThreshold = 20f;
        [SerializeField] private float viewShiftDistance = 14f;
        [SerializeField] private float viewShiftSmoothTime = 0.2f;
        [SerializeField] private float maxVisibleHorizontalMiles = 20f;
        [SerializeField] private float zoomScrollSensitivity = 0.12f;
        [SerializeField] private float maxZoomOffsetScaleFallback = 4.5f;

        private Vector3 velocity;
        private bool isViewShifted;
        private float currentViewShift;
        private float viewShiftVelocity;
        private float zoomLevel;
        private float maxOffsetScale = 1f;
        private bool zoomLimitsReady;
        private bool zoomChangedThisFrame;

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
            isViewShifted = false;
            currentViewShift = 0f;
            viewShiftVelocity = 0f;
            zoomLevel = 0f;
            zoomLimitsReady = false;
            maxOffsetScale = maxZoomOffsetScaleFallback;
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

            var viewShiftTarget = isViewShifted ? viewShiftDistance : 0f;
            currentViewShift = Mathf.SmoothDamp(
                currentViewShift,
                viewShiftTarget,
                ref viewShiftVelocity,
                viewShiftSmoothTime);

            var lookAhead = Vector3.zero;
            if (!isViewShifted && aircraft != null && aircraft.CurrentSpeed >= lookAheadSpeedThreshold)
            {
                lookAhead = target.forward * lookAheadDistance;
            }

            var focusPoint = target.position + target.forward * currentViewShift + lookAhead;
            var zoomScale = Mathf.Lerp(1f, maxOffsetScale, zoomLevel);
            var desiredPosition = focusPoint + target.rotation * (offset * zoomScale);

            if (zoomChangedThisFrame)
            {
                transform.position = desiredPosition;
                velocity = Vector3.zero;
                zoomChangedThisFrame = false;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref velocity,
                    followSmoothTime);
            }

            transform.LookAt(focusPoint);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            var focusPoint = target.position;
            transform.position = focusPoint + target.rotation * offset;
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
