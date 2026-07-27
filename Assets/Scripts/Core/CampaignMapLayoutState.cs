using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace F89.Core
{
    [Serializable]
    public sealed class CampaignMapMarkerRecord
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Label = "Mission";
        public float XMiles;
        public float ZMiles;
        public int GridCellX;
        public int GridCellZ;
    }

    [Serializable]
    public sealed class CampaignMapLayoutData
    {
        public CampaignMapMarkerRecord[] Markers = Array.Empty<CampaignMapMarkerRecord>();
        public string[] HiddenBaseNames = Array.Empty<string>();
    }

    /// <summary>Dev/campaign layout markers on the tactical map (blue squares).</summary>
    public static class CampaignMapLayoutState
    {
        private const string SaveFileName = "CampaignMapLayout.json";

        private static readonly List<CampaignMapMarkerRecord> markers = new();
        private static readonly List<string> hiddenBaseNames = new();
        private static bool isLoaded;

        public static IReadOnlyList<CampaignMapMarkerRecord> Markers
        {
            get
            {
                EnsureLoaded();
                return markers;
            }
        }

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            isLoaded = true;
            markers.Clear();
            hiddenBaseNames.Clear();

            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var loaded = JsonUtility.FromJson<CampaignMapLayoutData>(File.ReadAllText(SavePath));
                if (loaded?.Markers != null)
                {
                    markers.AddRange(loaded.Markers);
                }

                if (loaded?.HiddenBaseNames != null)
                {
                    hiddenBaseNames.AddRange(loaded.HiddenBaseNames);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"F-89: Failed to load campaign map layout — {exception.Message}");
            }

            RefreshMarkerGridCells();
        }

        private static void RefreshMarkerGridCells()
        {
            for (var i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var miles = new Vector2(marker.XMiles, marker.ZMiles);
                if (CampaignMapCoordinates.TryMilesToGridCell(miles, out var gridCell))
                {
                    marker.GridCellX = gridCell.x;
                    marker.GridCellZ = gridCell.y;
                }
            }
        }

        public static bool IsBaseHidden(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return false;
            }

            EnsureLoaded();
            for (var i = 0; i < hiddenBaseNames.Count; i++)
            {
                if (string.Equals(hiddenBaseNames[i], baseName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static CampaignMapMarkerRecord AddMarker(Vector2 miles, string label)
        {
            EnsureLoaded();
            CampaignMapCoordinates.TryMilesToGridCell(miles, out var gridCell);
            var marker = new CampaignMapMarkerRecord
            {
                Label = string.IsNullOrWhiteSpace(label) ? $"Mission {markers.Count + 1:00}" : label.Trim(),
                XMiles = miles.x,
                ZMiles = miles.y,
                GridCellX = gridCell.x,
                GridCellZ = gridCell.y
            };
            markers.Add(marker);
            Save();
            Debug.Log(
                $"F-89 Campaign: Added marker '{marker.Label}' at {CampaignMapCoordinates.FormatCoordinateSummary(miles, gridCell)}. Saved to {SavePath}");
            return marker;
        }

        public static Vector3 GetMarkerWorldPosition(CampaignMapMarkerRecord marker)
        {
            if (marker == null)
            {
                return Vector3.zero;
            }

            return CampaignMapCoordinates.MilesToWorld(new Vector2(marker.XMiles, marker.ZMiles));
        }

        public static bool RemoveMarker(string markerId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(markerId))
            {
                return false;
            }

            var removed = markers.RemoveAll(marker => marker != null && marker.Id == markerId);
            if (removed <= 0)
            {
                return false;
            }

            Save();
            return true;
        }

        public static bool HideBase(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName) || IsBaseHidden(baseName))
            {
                return false;
            }

            EnsureLoaded();
            hiddenBaseNames.Add(baseName.Trim());
            Save();
            Debug.Log($"F-89 Campaign: Hidden base '{baseName}'. Saved to {SavePath}");
            return true;
        }

        public static bool TryGetMarker(string markerId, out CampaignMapMarkerRecord marker)
        {
            marker = null;
            EnsureLoaded();
            if (string.IsNullOrEmpty(markerId))
            {
                return false;
            }

            for (var i = 0; i < markers.Count; i++)
            {
                var candidate = markers[i];
                if (candidate != null && candidate.Id == markerId)
                {
                    marker = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void Save()
        {
            try
            {
                var payload = new CampaignMapLayoutData
                {
                    Markers = markers.ToArray(),
                    HiddenBaseNames = hiddenBaseNames.ToArray()
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(payload, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"F-89: Failed to save campaign map layout — {exception.Message}");
            }
        }
    }
}
