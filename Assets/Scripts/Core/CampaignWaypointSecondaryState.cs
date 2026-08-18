using System;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Secondary landing pads for waypoint missions — revealed when the first hostile ground vehicle
    /// is destroyed (wreck pad). Landing still requires primary air objectives complete.
    /// </summary>
    public static class CampaignWaypointSecondaryState
    {
        public static bool IsSecondaryRevealed(string siteCode)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return false;
            }

            return TryGetReveal(siteCode, out _);
        }

        public static bool IsPrimaryAirComplete(string siteCode)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return false;
            }

            return CampaignWaypointPlatoonState.IsPrimaryAirObjectivesComplete(siteCode);
        }

        public static bool CanLandForSecondary(string siteCode)
        {
            return IsSecondaryRevealed(siteCode) && IsPrimaryAirComplete(siteCode);
        }

        public static void ClearReveal(string siteCode, bool destroyScenePad = false)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save?.WaypointSecondaryReveals == null || save.WaypointSecondaryReveals.Length == 0)
            {
                if (destroyScenePad)
                {
                    DestroySecondaryPadInScene(siteCode);
                }

                return;
            }

            var normalized = siteCode.Trim().ToUpperInvariant();
            var kept = new System.Collections.Generic.List<WaypointSecondaryRevealSaveData>();
            var removed = false;
            for (var i = 0; i < save.WaypointSecondaryReveals.Length; i++)
            {
                var reveal = save.WaypointSecondaryReveals[i];
                if (reveal != null
                    && string.Equals(reveal.SiteCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }

                kept.Add(reveal);
            }

            if (removed)
            {
                save.WaypointSecondaryReveals = kept.ToArray();
                CharacterSaveRepository.WriteWorldProgress(save);
            }

            if (destroyScenePad)
            {
                DestroySecondaryPadInScene(siteCode);
            }
        }

        private static void DestroySecondaryPadInScene(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            var padName = "WaypointSecondaryPad_" + siteCode.Trim().ToUpperInvariant();
            var pads = UnityEngine.Object.FindObjectsByType<CampaignWaypointSecondaryPad>(FindObjectsSortMode.None);
            for (var i = 0; i < pads.Length; i++)
            {
                var padRoot = pads[i];
                if (padRoot == null)
                {
                    continue;
                }

                var pad = padRoot.transform.Find(padName);
                if (pad != null)
                {
                    UnityEngine.Object.Destroy(pad.gameObject);
                }
            }
        }

        public static bool TryGetPadWorldPosition(string siteCode, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (!TryGetReveal(siteCode, out var reveal))
            {
                return false;
            }

            worldPosition = new Vector3(reveal.PadWorldX, 0f, reveal.PadWorldZ);
            return true;
        }

        public static void TryRevealSecondary(string siteCode, Vector3 worldPosition, CampaignWaypointMissionSite site = null)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode)
                || !CampaignWaypointPlatoonState.ShouldShowSecondaryPad(siteCode, site))
            {
                return;
            }

            worldPosition = ResolveLayoutPadPosition(siteCode, worldPosition);
            if (IsSecondaryRevealed(siteCode))
            {
                UpdateSavedPadPosition(siteCode, worldPosition);
                CampaignWaypointSecondaryPad.EnsureAt(worldPosition, siteCode);
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            var reveal = new WaypointSecondaryRevealSaveData
            {
                SiteCode = siteCode.Trim().ToUpperInvariant(),
                PadWorldX = worldPosition.x,
                PadWorldZ = worldPosition.z
            };

            var reveals = save.WaypointSecondaryReveals ?? Array.Empty<WaypointSecondaryRevealSaveData>();
            Array.Resize(ref reveals, reveals.Length + 1);
            reveals[reveals.Length - 1] = reveal;
            save.WaypointSecondaryReveals = reveals;
            CharacterSaveRepository.WriteWorldProgress(save);

            CampaignWaypointSecondaryPad.EnsureAt(worldPosition, siteCode);
            Debug.Log($"F-89: Waypoint secondary pad revealed at {siteCode} ({worldPosition.x:0}, {worldPosition.z:0}).");
        }

        public static void RestorePadsInScene()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save?.WaypointSecondaryReveals == null)
            {
                return;
            }

            for (var i = 0; i < save.WaypointSecondaryReveals.Length; i++)
            {
                var reveal = save.WaypointSecondaryReveals[i];
                if (reveal == null || string.IsNullOrWhiteSpace(reveal.SiteCode))
                {
                    continue;
                }

                var position = ResolveLayoutPadPosition(
                    reveal.SiteCode,
                    new Vector3(reveal.PadWorldX, 0f, reveal.PadWorldZ));
                UpdateSavedPadPosition(reveal.SiteCode, position);
                CampaignWaypointSecondaryPad.EnsureAt(position, reveal.SiteCode);
            }
        }

        private static Vector3 ResolveLayoutPadPosition(string siteCode, Vector3 fallbackWorldPosition)
        {
            fallbackWorldPosition.y = 0f;
            if (CampaignWaypointLayoutState.TryGetWorldPosition(siteCode, out var layoutWorld))
            {
                return TacticalMapPadPlacement.ResolveInteriorWorld(layoutWorld, siteCode);
            }

            return TacticalMapPadPlacement.ResolveInteriorWorld(fallbackWorldPosition, siteCode);
        }

        private static void UpdateSavedPadPosition(string siteCode, Vector3 worldPosition)
        {
            worldPosition.y = 0f;
            var save = CharacterSessionState.ActiveSave;
            if (save?.WaypointSecondaryReveals == null || string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            var normalized = siteCode.Trim().ToUpperInvariant();
            var changed = false;
            for (var i = 0; i < save.WaypointSecondaryReveals.Length; i++)
            {
                var reveal = save.WaypointSecondaryReveals[i];
                if (reveal == null
                    || !string.Equals(reveal.SiteCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Mathf.Approximately(reveal.PadWorldX, worldPosition.x)
                    && Mathf.Approximately(reveal.PadWorldZ, worldPosition.z))
                {
                    return;
                }

                reveal.PadWorldX = worldPosition.x;
                reveal.PadWorldZ = worldPosition.z;
                changed = true;
                break;
            }

            if (changed)
            {
                CharacterSaveRepository.WriteWorldProgress(save);
            }
        }

        private static bool TryGetReveal(string siteCode, out WaypointSecondaryRevealSaveData reveal)
        {
            reveal = null;
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return false;
            }

            var normalized = siteCode.Trim().ToUpperInvariant();
            var save = CharacterSessionState.ActiveSave;
            if (save?.WaypointSecondaryReveals == null)
            {
                return false;
            }

            for (var i = 0; i < save.WaypointSecondaryReveals.Length; i++)
            {
                var candidate = save.WaypointSecondaryReveals[i];
                if (candidate != null
                    && string.Equals(candidate.SiteCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    reveal = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// After primary WP air objectives are complete, hide ambient/bunker-style radar contacts
        /// on the secondary pad cell and the cleared primary site cell.
        /// </summary>
        public static bool ShouldSuppressRadarContact(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            worldPosition.y = 0f;
            if (worldMap == null || ticSizeWorldUnits <= 0f || !GamePlayModeState.IsCampaign)
            {
                return false;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null
                || !CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var activeSiteCode)
                || !CampaignWaypointSiteIds.IsWaypointSiteCode(activeSiteCode)
                || !CampaignWaypointPlatoonState.IsPrimaryAirObjectivesComplete(activeSiteCode))
            {
                return false;
            }

            if (!CampaignMapCoordinates.TryWorldToGridCell(
                    worldPosition,
                    worldMap,
                    ticSizeWorldUnits,
                    out var contactCell))
            {
                return false;
            }

            if (TryGetPadWorldPosition(activeSiteCode, out var padPosition)
                && CampaignMapCoordinates.TryWorldToGridCell(
                    padPosition,
                    worldMap,
                    ticSizeWorldUnits,
                    out var padCell)
                && padCell == contactCell)
            {
                return true;
            }

            if (CampaignWaypointLayoutState.TryGetWorldPosition(activeSiteCode, out var primaryWorld)
                && CampaignMapCoordinates.TryWorldToGridCell(
                    primaryWorld,
                    worldMap,
                    ticSizeWorldUnits,
                    out var primaryCell)
                && primaryCell == contactCell)
            {
                return true;
            }

            return false;
        }
    }
}
