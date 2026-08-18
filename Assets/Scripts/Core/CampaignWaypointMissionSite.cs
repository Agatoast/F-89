using UnityEngine;

namespace F89.Core
{
    /// <summary>Tags flight-map units spawned for a campaign waypoint mission (non-outpost).</summary>
    public sealed class CampaignWaypointMissionSite : MonoBehaviour
    {
        [SerializeField] private string siteCode = string.Empty;

        public string SiteCode
        {
            get => siteCode;
            set => siteCode = value?.Trim().ToUpperInvariant() ?? string.Empty;
        }
    }
}
