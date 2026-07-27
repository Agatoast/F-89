using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Large procedural ice quad that follows the player so the ground map is infinite.
    /// </summary>
    public sealed class LandGroundTerrainFollow : MonoBehaviour
    {
        private Transform followTarget;

        public void SetTarget(Transform target) => followTarget = target;

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            var position = followTarget.position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
