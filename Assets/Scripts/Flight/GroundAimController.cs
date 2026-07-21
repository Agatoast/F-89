using F89.Controls;
using UnityEngine;

namespace F89.Flight
{
    public class GroundAimController : MonoBehaviour
    {
        [SerializeField] private Transform aimPivot;
        [SerializeField] private PlayerAircraftInput inputSource;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float groundHeight;
        [SerializeField] private float rotateSpeed = 720f;
        [SerializeField] private bool aimIndependentOfBody = true;

        public Vector3 AimWorldPoint { get; private set; }

        private void Reset()
        {
            aimPivot = transform;
            aimCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (aimPivot == null || inputSource == null)
            {
                return;
            }

            var camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera == null || !inputSource.Current.hasAimScreenPosition)
            {
                if (!aimIndependentOfBody)
                {
                    return;
                }

                AimWorldPoint = aimPivot.position + aimPivot.forward * 20f;
                return;
            }

            var ray = camera.ScreenPointToRay(inputSource.Current.aimScreenPosition);
            var groundPlane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            if (!groundPlane.Raycast(ray, out var distance))
            {
                return;
            }

            AimWorldPoint = ray.GetPoint(distance);
            var direction = AimWorldPoint - aimPivot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(AimWorldPoint, 0.75f);
            if (aimPivot != null)
            {
                Gizmos.DrawLine(aimPivot.position, AimWorldPoint);
            }
        }
    }
}
