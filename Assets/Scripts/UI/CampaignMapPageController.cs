using F89.Core;
using F89.Testing;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Full-screen campaign map from the main menu (no sortie launch required).
    /// </summary>
    public sealed class CampaignMapPageController : MonoBehaviour
    {
        private AntarcticaMapOverlay mapOverlay;

        public static bool IsActive { get; private set; }

        public static void Open()
        {
            if (IsActive)
            {
                Object.FindAnyObjectByType<AntarcticaMapOverlay>()?.OpenMenuPreview();
                return;
            }

            var root = new GameObject("CampaignMapPage");
            root.AddComponent<CampaignMapPageController>();
        }

        public static void CloseActive()
        {
            var page = Object.FindAnyObjectByType<CampaignMapPageController>();
            if (page == null)
            {
                return;
            }

            Object.Destroy(page.gameObject);
        }

        private void Awake()
        {
            IsActive = true;
            CampaignMapLayoutState.EnsureLoaded();
            CampaignWaypointLayoutState.EnsureLoaded();
            AntarcticaBaseSpawner.SpawnIfNeeded();

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var mapObject = new GameObject("AntarcticaMapOverlay");
            mapObject.transform.SetParent(transform, false);
            mapOverlay = mapObject.AddComponent<AntarcticaMapOverlay>();
            mapOverlay.ConfigureForMenuPreview(worldMap);
            mapOverlay.OpenMenuPreview();
        }

        private void OnDestroy()
        {
            mapOverlay?.CloseMenuPreview();
            mapOverlay = null;
            IsActive = false;
        }
    }
}
