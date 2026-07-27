using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Marks a world region as shelter from Antarctica cold.
    /// Buildings, bunkers, and the landed plane use this to reset outdoor exposure.
    /// </summary>
    public sealed class LandShelterZone : MonoBehaviour
    {
        public enum ShelterKind
        {
            Building = 0,
            Bunker = 1,
            Plane = 2
        }

        [SerializeField] private ShelterKind kind = ShelterKind.Building;
        [SerializeField] private Vector2 size = new Vector2(4f, 3f);

        public ShelterKind Kind => kind;

        public void Configure(ShelterKind shelterKind, Vector2 shelterSize)
        {
            kind = shelterKind;
            size = new Vector2(Mathf.Max(0.5f, shelterSize.x), Mathf.Max(0.5f, shelterSize.y));
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            var center = (Vector2)transform.position;
            var half = size * 0.5f;
            return worldPoint.x >= center.x - half.x
                   && worldPoint.x <= center.x + half.x
                   && worldPoint.y >= center.y - half.y
                   && worldPoint.y <= center.y + half.y;
        }

        public static bool IsPointInAnyShelter(Vector2 worldPoint, out ShelterKind kind)
        {
            kind = ShelterKind.Building;
            var zones = Object.FindObjectsByType<LandShelterZone>(FindObjectsSortMode.None);
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null || !zone.isActiveAndEnabled)
                {
                    continue;
                }

                if (!zone.ContainsWorldPoint(worldPoint))
                {
                    continue;
                }

                kind = zone.kind;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, 0.1f));
        }
#endif
    }
}
