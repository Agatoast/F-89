using System;
using System.Collections.Generic;
using F89.Enemies;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Per-character persistence for destructible flight-map outposts and their units.</summary>
    public static class AntarcticaOutpostState
    {
        public static bool IsDestroyed(string outpostName)
        {
            return Contains(CharacterSessionState.ActiveSave?.DestroyedOutpostNames, outpostName);
        }

        public static bool IsTargetDestroyed(string outpostName, string targetLabel)
        {
            return Contains(
                CharacterSessionState.ActiveSave?.DestroyedWorldTargetIds,
                GetTargetId(outpostName, targetLabel));
        }

        public static void MarkDestroyed(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null || Contains(save.DestroyedOutpostNames, outpostName))
            {
                return;
            }

            save.DestroyedOutpostNames = Add(save.DestroyedOutpostNames, outpostName);
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static void MarkTargetDestroyed(string outpostName, string targetLabel)
        {
            var targetId = GetTargetId(outpostName, targetLabel);
            if (string.IsNullOrEmpty(targetId))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null || Contains(save.DestroyedWorldTargetIds, targetId))
            {
                return;
            }

            save.DestroyedWorldTargetIds = Add(save.DestroyedWorldTargetIds, targetId);
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static bool IsTargetDestroyedForBase(AntarcticaBase baseSite, string targetLabel)
        {
            if (baseSite == null || string.IsNullOrWhiteSpace(targetLabel))
            {
                return false;
            }

            if (IsTargetDestroyed(baseSite.BaseName, targetLabel))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(baseSite.SiteCode)
                && IsTargetDestroyed(baseSite.SiteCode, targetLabel);
        }

        public static void MarkTargetDestroyedForBase(AntarcticaBase baseSite, string targetLabel)
        {
            if (baseSite == null || string.IsNullOrWhiteSpace(targetLabel))
            {
                return;
            }

            MarkTargetDestroyed(baseSite.BaseName, targetLabel);
            if (!string.IsNullOrWhiteSpace(baseSite.SiteCode))
            {
                MarkTargetDestroyed(baseSite.SiteCode, targetLabel);
            }
        }

        /// <summary>Clears one persisted destroy flag so mission assets can respawn.</summary>
        public static void ClearTargetDestroyed(string outpostName, string targetLabel)
        {
            RemoveTargetId(GetTargetId(outpostName, targetLabel));
        }

        /// <summary>Removes every persisted destroy entry keyed under this outpost or site code.</summary>
        public static void ClearAllSiteTargets(string outpostNameOrSiteCode)
        {
            if (string.IsNullOrWhiteSpace(outpostNameOrSiteCode))
            {
                return;
            }

            var prefix = $"{outpostNameOrSiteCode.Trim()}::";
            RemoveTargetIdsMatching(id =>
                id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Clears whole-outpost destroyed flag when restoring a mission site.</summary>
        public static void ClearOutpostDestroyed(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save?.DestroyedOutpostNames == null)
            {
                return;
            }

            var trimmed = outpostName.Trim();
            var changed = false;
            var kept = new List<string>();
            for (var i = 0; i < save.DestroyedOutpostNames.Length; i++)
            {
                var value = save.DestroyedOutpostNames[i];
                if (string.Equals(value, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    changed = true;
                    continue;
                }

                kept.Add(value);
            }

            if (!changed)
            {
                return;
            }

            save.DestroyedOutpostNames = kept.ToArray();
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static void MarkMissionStructuresClearedForBase(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return;
            }

            if (!IsTargetDestroyedForBase(baseSite, OutpostPrimaryObjective.RunwayTowerLabel))
            {
                MarkTargetDestroyedForBase(baseSite, OutpostPrimaryObjective.RunwayTowerLabel);
            }

            if (!IsTargetDestroyedForBase(baseSite, OutpostPrimaryObjective.BunkerBuildingLabel))
            {
                MarkTargetDestroyedForBase(baseSite, OutpostPrimaryObjective.BunkerBuildingLabel);
            }
        }

        public static bool IsFriendlyOccupied(string outpostNameOrSiteCode) =>
            IsFriendlyOccupied(CharacterSessionState.ActiveSave, outpostNameOrSiteCode);

        public static bool IsFriendlyOccupied(CharacterSaveData save, string outpostNameOrSiteCode)
        {
            if (string.IsNullOrWhiteSpace(outpostNameOrSiteCode))
            {
                return false;
            }

            var names = save?.FriendlyOccupiedOutpostNames;
            if (ContainsIgnoreCase(names, outpostNameOrSiteCode))
            {
                return true;
            }

            if (!CampaignMapLayoutState.TryGetSite(outpostNameOrSiteCode, out var site) || site == null)
            {
                return false;
            }

            return ContainsIgnoreCase(names, site.SiteCode)
                || ContainsIgnoreCase(names, CampaignMapLayoutState.NormalizeSiteName(site.Label));
        }

        /// <summary>True when the bunker boss was defeated or the flight-map bunker building was destroyed.</summary>
        public static bool IsBunkerCleared(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (IsBossDefeatedAtOutpost(outpostName))
            {
                return true;
            }

            return IsTargetDestroyed(outpostName, OutpostPrimaryObjective.BunkerBuildingLabel);
        }

        /// <summary>Persisted friendly occupation — set only after END MISSION with air objectives complete.</summary>
        public static bool IsFriendlyBase(string outpostName) => IsFriendlyOccupied(outpostName);

        public static bool IsFriendlyBaseSite(AntarcticaBase baseSite)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land || baseSite.IsDestroyed)
            {
                return false;
            }

            if (baseSite.Control == BaseControl.Friendly)
            {
                return true;
            }

            return IsFriendlyOccupied(baseSite.BaseName)
                || (!string.IsNullOrWhiteSpace(baseSite.SiteCode)
                    && IsFriendlyOccupied(baseSite.SiteCode));
        }

        /// <summary>Hostile while the bunker boss is still alive.</summary>
        public static bool IsEnemyOutpost(string outpostName) =>
            !string.IsNullOrWhiteSpace(outpostName)
            && !IsFriendlyOccupied(outpostName)
            && !IsBunkerCleared(outpostName);

        /// <summary>Boss cleared but not yet friendly — white on map until END MISSION.</summary>
        public static bool IsNeutralOutpost(string outpostName) =>
            !string.IsNullOrWhiteSpace(outpostName)
            && !IsFriendlyOccupied(outpostName)
            && IsBunkerCleared(outpostName);

        /// <summary>
        /// Marks the current catalog mission outpost friendly after END MISSION when air
        /// objectives are complete. Boss clear is not required. Follows mission SiteCode.
        /// </summary>
        public static void TryApplyMissionCompleteOccupation(string landedOutpostName)
        {
            _ = landedOutpostName;
            CampaignMissionObjectiveState.TryApplyVictoryOccupation(CharacterSessionState.ActiveSave);
        }

        public static void MarkFriendlyOccupied(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            var baseName = outpostName.Trim();
            var siteCode = string.Empty;
            if (CampaignMapLayoutState.TryGetSite(baseName, out var site) && site != null)
            {
                baseName = CampaignMapLayoutState.NormalizeSiteName(site.Label);
                siteCode = site.SiteCode;
            }

            var changed = false;
            if (!ContainsIgnoreCase(save.FriendlyOccupiedOutpostNames, baseName))
            {
                save.FriendlyOccupiedOutpostNames = Add(save.FriendlyOccupiedOutpostNames, baseName);
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(siteCode)
                && !ContainsIgnoreCase(save.FriendlyOccupiedOutpostNames, siteCode))
            {
                save.FriendlyOccupiedOutpostNames = Add(save.FriendlyOccupiedOutpostNames, siteCode);
                changed = true;
            }

            if (!changed && IsFriendlyOccupied(baseName))
            {
                ApplyFriendlyControlToBase(baseName);
                return;
            }

            OutpostFlightPlatoonState.MarkPlatoonCleared(baseName);
            if (!string.IsNullOrWhiteSpace(siteCode))
            {
                OutpostFlightPlatoonState.MarkPlatoonCleared(siteCode);
            }

            CharacterSaveRepository.WriteWorldProgress(save);
            ApplyFriendlyControlToBase(baseName);
        }

        public static void ApplyFriendlyControlToAllBases()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save?.FriendlyOccupiedOutpostNames == null)
            {
                return;
            }

            for (var i = 0; i < save.FriendlyOccupiedOutpostNames.Length; i++)
            {
                ApplyFriendlyControlToBase(save.FriendlyOccupiedOutpostNames[i]);
            }
        }

        private static void ApplyFriendlyControlToBase(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var bases = UnityEngine.Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null
                    || baseSite.SiteKind != BaseSiteKind.Land
                    || !MatchesBaseKey(baseSite, outpostName))
                {
                    continue;
                }

                baseSite.Capture();
                OutpostFlightPlatoonState.MarkPlatoonCleared(baseSite.BaseName);
                if (!string.IsNullOrWhiteSpace(baseSite.SiteCode))
                {
                    OutpostFlightPlatoonState.MarkPlatoonCleared(baseSite.SiteCode);
                }

                var platoon = baseSite.transform.Find("VehiclePlatoon");
                if (platoon != null)
                {
                    UnityEngine.Object.Destroy(platoon.gameObject);
                }

                OutpostVehicleSpawner.EnsureEmptyPlatoonMarker(baseSite);
                OutpostRunwayVisual.ApplyFriendlyBaseCleanup(baseSite);
                return;
            }
        }

        private static bool MatchesBaseKey(AntarcticaBase baseSite, string key)
        {
            if (baseSite == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (string.Equals(baseSite.BaseName, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseSite.SiteCode, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!CampaignMapLayoutState.TryGetSite(key, out var site) || site == null)
            {
                return false;
            }

            return string.Equals(
                       baseSite.BaseName,
                       CampaignMapLayoutState.NormalizeSiteName(site.Label),
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseSite.SiteCode, site.SiteCode, StringComparison.OrdinalIgnoreCase);
        }

        public static void ResetAllDestroyedOutposts()
        {
            CampaignWorldReset.ResetAllOutpostsAndBunkers();
        }

        public static string GetTargetId(string outpostName, string targetLabel)
        {
            if (string.IsNullOrWhiteSpace(outpostName) || string.IsNullOrWhiteSpace(targetLabel))
            {
                return string.Empty;
            }

            return $"{outpostName.Trim()}::{targetLabel.Trim()}";
        }

        private static bool Contains(string[] values, string value) =>
            ContainsIgnoreCase(values, value);

        private static bool ContainsIgnoreCase(string[] values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBossDefeatedAtOutpost(string outpostName)
        {
            var save = CharacterSessionState.ActiveSave;
            if (save?.BossMissionOutpostNames == null)
            {
                return false;
            }

            for (var i = 0; i < save.BossMissionOutpostNames.Length; i++)
            {
                if (!string.Equals(save.BossMissionOutpostNames[i], outpostName, StringComparison.Ordinal))
                {
                    continue;
                }

                return (save.DefeatedBossMask & (1 << i)) != 0;
            }

            return false;
        }

        private static string[] Add(string[] values, string value)
        {
            var list = values == null ? new List<string>() : new List<string>(values);
            list.Add(value);
            return list.ToArray();
        }

        private static void RemoveTargetId(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return;
            }

            RemoveTargetIdsMatching(id => string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase));
        }

        private static void RemoveTargetIdsMatching(System.Func<string, bool> predicate)
        {
            if (predicate == null)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save?.DestroyedWorldTargetIds == null || save.DestroyedWorldTargetIds.Length == 0)
            {
                return;
            }

            var kept = new List<string>();
            var changed = false;
            for (var i = 0; i < save.DestroyedWorldTargetIds.Length; i++)
            {
                var value = save.DestroyedWorldTargetIds[i];
                if (predicate(value))
                {
                    changed = true;
                    continue;
                }

                kept.Add(value);
            }

            if (!changed)
            {
                return;
            }

            save.DestroyedWorldTargetIds = kept.ToArray();
            CharacterSaveRepository.WriteWorldProgress(save);
        }
    }
}
