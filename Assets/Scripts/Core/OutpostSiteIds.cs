using System;
using System.Text;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Stable speakable outpost IDs (OP-01, OP-SOUTH, STN-PALMER) for design/dev talk.
    /// Display names stay on <see cref="AntarcticaBase.BaseName"/>; codes identify the place.
    /// </summary>
    public static class OutpostSiteIds
    {
        public const string CarrierCode = "CV-MVB";

        public static string FromLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return string.Empty;
            }

            var normalized = CampaignMapLayoutState.NormalizeSiteName(label);
            if (string.Equals(normalized, AntarcticaWorldLocations.CarrierName, StringComparison.OrdinalIgnoreCase))
            {
                return CarrierCode;
            }

            if (normalized.StartsWith("Outpost ", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = normalized.Substring("Outpost ".Length).Trim();
                return "OP-" + ToSlug(suffix);
            }

            return "STN-" + ToSlug(StripStationSuffix(normalized));
        }

        public static string FormatIdentity(CampaignMapMarkerRecord site)
        {
            if (site == null)
            {
                return string.Empty;
            }

            var code = string.IsNullOrWhiteSpace(site.SiteCode)
                ? FromLabel(site.Label)
                : site.SiteCode;
            var miles = new Vector2(site.XMiles, site.ZMiles);
            var grid = new Vector2Int(site.GridCellX, site.GridCellZ);
            return FormatIdentity(code, site.Label, miles, grid);
        }

        public static string FormatIdentity(
            string siteCode,
            string label,
            Vector2 miles,
            Vector2Int gridCell)
        {
            var code = string.IsNullOrWhiteSpace(siteCode) ? FromLabel(label) : siteCode.Trim().ToUpperInvariant();
            var name = string.IsNullOrWhiteSpace(label) ? code : label.Trim();
            var milesLabel = CampaignMapCoordinates.FormatMilesLabel(miles);
            var gridLabel = CampaignMapCoordinates.FormatGridCellLabel(gridCell);
            return string.IsNullOrEmpty(gridLabel)
                ? $"{code}  {name}  {milesLabel}"
                : $"{code}  {name}  {milesLabel}  {gridLabel}";
        }

        private static string StripStationSuffix(string label)
        {
            var trimmed = label.Trim();
            if (trimmed.EndsWith(" Station", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - " Station".Length).Trim();
            }

            if (trimmed.EndsWith(" Research", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - " Research".Length).Trim();
            }

            if (trimmed.EndsWith(" Base", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - " Base".Length).Trim();
            }

            return trimmed;
        }

        private static string ToSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UNKNOWN";
            }

            var builder = new StringBuilder(value.Length);
            var lastWasDash = false;
            foreach (var ch in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasDash = false;
                    continue;
                }

                if (ch == ' ' || ch == '-' || ch == '_' || ch == '/')
                {
                    if (!lastWasDash && builder.Length > 0)
                    {
                        builder.Append('-');
                        lastWasDash = true;
                    }
                }
            }

            while (builder.Length > 0 && builder[builder.Length - 1] == '-')
            {
                builder.Length--;
            }

            return builder.Length > 0 ? builder.ToString() : "UNKNOWN";
        }
    }
}
