using System;
using System.Collections.Generic;
using F89.Enemies;
using F89.LandCombat;
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

        public static bool IsFriendlyOccupied(string outpostName) =>
            Contains(CharacterSessionState.ActiveSave?.FriendlyOccupiedOutpostNames, outpostName);

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
        /// Marks the mission outpost friendly after air objectives are destroyed and the pilot
        /// ends the mission from the carrier deck or an outpost runway.
        /// </summary>
        public static void TryApplyMissionCompleteOccupation(string landedOutpostName)
        {
            if (!GamePlayModeState.IsCampaign)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null || !LandBossMissionAssignment.IsPrimaryMissionComplete(save))
            {
                return;
            }

            var outpostName = !string.IsNullOrWhiteSpace(landedOutpostName)
                ? landedOutpostName
                : save.AssignedBossOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            if (!OutpostPrimaryObjective.AreAirObjectivesDestroyed(outpostName))
            {
                return;
            }

            MarkFriendlyOccupied(outpostName);
        }

        public static void MarkFriendlyOccupied(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null || Contains(save.FriendlyOccupiedOutpostNames, outpostName))
            {
                return;
            }

            save.FriendlyOccupiedOutpostNames = Add(save.FriendlyOccupiedOutpostNames, outpostName);
            CharacterSaveRepository.WriteWorldProgress(save);
            ApplyFriendlyControlToBase(outpostName);
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
                    || !string.Equals(baseSite.BaseName, outpostName, StringComparison.Ordinal))
                {
                    continue;
                }

                baseSite.Capture();
                OutpostFlightPlatoonState.MarkPlatoonCleared(outpostName);
                var platoon = baseSite.transform.Find("VehiclePlatoon");
                if (platoon != null)
                {
                    UnityEngine.Object.Destroy(platoon.gameObject);
                }

                OutpostVehicleSpawner.EnsureEmptyPlatoonMarker(baseSite);
                return;
            }
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

        private static bool Contains(string[] values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
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
    }
}
