using UnityEngine;

namespace F89.Core
{
    public enum BaseControl
    {
        Friendly,
        Hostile
    }

    public enum BaseSiteKind
    {
        Land,
        Carrier
    }

    public class AntarcticaBase : MonoBehaviour
    {
        [SerializeField] private string baseName = "Base";
        [SerializeField] private BaseControl control = BaseControl.Hostile;
        [SerializeField] private BaseSiteKind siteKind = BaseSiteKind.Land;
        [SerializeField] private bool isMissionObjective;
        [SerializeField] private bool isActive = true;
        [SerializeField] private bool isDestroyed;
        [SerializeField] private Vector2 positionMiles;
        private float worldUnitsPerMile;

        public string BaseName => baseName;
        public Vector2 PositionMiles => positionMiles;
        public BaseControl Control => control;
        public BaseSiteKind SiteKind => siteKind;
        public bool IsMissionObjective => isMissionObjective;
        public bool IsActive => isActive;
        public bool IsDestroyed => isDestroyed;

        public void Configure(
            string name,
            BaseControl baseControl,
            Vector2 miles,
            float worldUnitsPerMile,
            bool active = true,
            BaseSiteKind kind = BaseSiteKind.Land,
            bool missionObjective = false)
        {
            baseName = name;
            control = baseControl;
            siteKind = kind;
            isMissionObjective = missionObjective;
            isActive = active;
            isDestroyed = kind == BaseSiteKind.Land && AntarcticaOutpostState.IsDestroyed(name);
            positionMiles = miles;
            ApplyWorldPosition(worldUnitsPerMile);
            ApplyDestroyedState();
        }

        public void SetPositionMiles(Vector2 miles, float worldUnitsPerMile)
        {
            positionMiles = miles;
            ApplyWorldPosition(worldUnitsPerMile);
        }

        public void ApplyWorldPosition(float worldUnitsPerMile)
        {
            this.worldUnitsPerMile = worldUnitsPerMile;
            transform.position = WorldMapConfig.MileOffsetToWorld(positionMiles, worldUnitsPerMile);
        }

        public void RefreshPersistedWorldState(float worldUnitsPerMile)
        {
            if (siteKind == BaseSiteKind.Land)
            {
                isDestroyed = AntarcticaOutpostState.IsDestroyed(baseName);
            }

            ApplyWorldPosition(worldUnitsPerMile);
            ApplyDestroyedState();
        }

        public void SetBaseName(string name)
        {
            baseName = name;
            gameObject.name = name;
        }

        public void SetMissionObjective(bool missionObjective)
        {
            isMissionObjective = missionObjective;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        public void Capture()
        {
            if (isDestroyed)
            {
                return;
            }

            control = BaseControl.Friendly;
        }

        public void Destroy()
        {
            isDestroyed = true;
            if (siteKind == BaseSiteKind.Land)
            {
                AntarcticaOutpostState.MarkDestroyed(baseName);
            }

            ApplyDestroyedState();
        }

        private void ApplyDestroyedState()
        {
            if (siteKind != BaseSiteKind.Land)
            {
                return;
            }

            if (!isDestroyed)
            {
                RestoreIntactOutpostVisuals();
                return;
            }

            // Keep the outpost object active so its one-mile landing square still resolves
            // to the bunker, but remove the intact structure from the rebuilt flight world.
            var lockable = GetComponent<F89.Weapons.LockableTarget>();
            lockable?.ExpireWithoutHit();

            var marker = transform.Find("DestroyedOutpostMarker");
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (marker != null && renderer.transform.IsChildOf(marker))
                {
                    continue;
                }

                renderer.enabled = false;
            }

            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            EnsureDestroyedGroundMarker();
        }

        private void RestoreIntactOutpostVisuals()
        {
            var marker = transform.Find("DestroyedOutpostMarker");
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }

            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }
        }

        private void EnsureDestroyedGroundMarker()
        {
            if (!isDestroyed || siteKind != BaseSiteKind.Land || worldUnitsPerMile <= 0f)
            {
                return;
            }

            if (transform.Find("DestroyedOutpostMarker") != null)
            {
                return;
            }

            var map = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var worldUnitsPerTic = map != null && map.TicsPerMile > 0f
                ? worldUnitsPerMile / map.TicsPerMile
                : worldUnitsPerMile / 20f;
            var marker = new GameObject("DestroyedOutpostMarker");
            marker.name = "DestroyedOutpostMarker";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var halfSize = worldUnitsPerTic * 0.5f;
            var lineWidth = Mathf.Min(0.05f, worldUnitsPerTic * 0.2f);
            CreateMarkerStroke(marker.transform, new Vector3(0f, 0f, halfSize), Vector3.zero, new Vector3(worldUnitsPerTic, 0.05f, lineWidth));
            CreateMarkerStroke(marker.transform, new Vector3(0f, 0f, -halfSize), Vector3.zero, new Vector3(worldUnitsPerTic, 0.05f, lineWidth));
            CreateMarkerStroke(marker.transform, new Vector3(halfSize, 0f, 0f), Vector3.zero, new Vector3(lineWidth, 0.05f, worldUnitsPerTic));
            CreateMarkerStroke(marker.transform, new Vector3(-halfSize, 0f, 0f), Vector3.zero, new Vector3(lineWidth, 0.05f, worldUnitsPerTic));
            CreateMarkerStroke(
                marker.transform,
                Vector3.zero,
                new Vector3(0f, 45f, 0f),
                new Vector3(worldUnitsPerTic * 1.4143f, 0.06f, lineWidth));
            CreateMarkerStroke(
                marker.transform,
                Vector3.zero,
                new Vector3(0f, -45f, 0f),
                new Vector3(worldUnitsPerTic * 1.4143f, 0.06f, lineWidth));
        }

        private static void CreateMarkerStroke(
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            var stroke = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stroke.name = "BlackMarkerStroke";
            stroke.transform.SetParent(parent, false);
            stroke.transform.localPosition = localPosition;
            stroke.transform.localRotation = Quaternion.Euler(localEulerAngles);
            stroke.transform.localScale = localScale;
            SetMarkerColor(stroke, Color.black);
            Object.Destroy(stroke.GetComponent<Collider>());
        }

        private static void SetMarkerColor(GameObject marker, Color color)
        {
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
