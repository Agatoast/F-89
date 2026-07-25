using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandGroundCameraFollow : MonoBehaviour
    {
        private Transform target;

        public void SetTarget(Transform followTarget) => target = followTarget;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var position = transform.position;
            position.x = target.position.x;
            position.y = target.position.y;
            transform.position = position;
        }
    }
}
