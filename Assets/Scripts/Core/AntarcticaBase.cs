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
            isDestroyed = false;
            positionMiles = miles;
            ApplyWorldPosition(worldUnitsPerMile);
        }

        public void SetPositionMiles(Vector2 miles, float worldUnitsPerMile)
        {
            positionMiles = miles;
            ApplyWorldPosition(worldUnitsPerMile);
        }

        public void ApplyWorldPosition(float worldUnitsPerMile)
        {
            transform.position = WorldMapConfig.MileOffsetToWorld(positionMiles, worldUnitsPerMile);
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
        }
    }
}
