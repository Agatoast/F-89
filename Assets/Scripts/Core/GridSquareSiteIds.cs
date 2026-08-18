using UnityEngine;

namespace F89.Core
{
    /// <summary>Site codes for ambient 1×1 MI grid-square enemy spawns (GS = grid square).</summary>
    public static class GridSquareSiteIds
    {
        public const string Prefix = "GS-";

        public static string FormatSiteCode(Vector2Int gridCell) =>
            $"{Prefix}{gridCell.x}-{gridCell.y}";

        public static bool IsGridSquareSiteCode(string siteCode) =>
            !string.IsNullOrWhiteSpace(siteCode)
            && siteCode.Trim().StartsWith(Prefix, System.StringComparison.OrdinalIgnoreCase);

        public static bool TryParseSiteCode(string siteCode, out Vector2Int gridCell)
        {
            gridCell = Vector2Int.zero;
            if (!IsGridSquareSiteCode(siteCode))
            {
                return false;
            }

            var body = siteCode.Trim().Substring(Prefix.Length);
            var separator = body.IndexOf('-');
            if (separator <= 0 || separator >= body.Length - 1)
            {
                return false;
            }

            if (!int.TryParse(body.Substring(0, separator), out var column)
                || !int.TryParse(body.Substring(separator + 1), out var row))
            {
                return false;
            }

            gridCell = new Vector2Int(column, row);
            return gridCell.x > 0 && gridCell.y > 0;
        }
    }
}
