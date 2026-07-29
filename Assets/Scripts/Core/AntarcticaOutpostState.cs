using System;
using System.Collections.Generic;
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

        public static void ResetAllDestroyedOutposts()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                Debug.LogWarning("F-89: Cannot reset destroyed outposts — no active character save.");
                return;
            }

            var destroyedCount = save.DestroyedOutpostNames?.Length ?? 0;
            var targetCount = save.DestroyedWorldTargetIds?.Length ?? 0;
            if (destroyedCount == 0 && targetCount == 0)
            {
                Debug.Log("F-89: No destroyed outposts to reset.");
                return;
            }

            save.DestroyedOutpostNames = Array.Empty<string>();
            save.DestroyedWorldTargetIds = Array.Empty<string>();
            save.UrVehicleKillsByLevel = new int[UrKillCredit.LevelCount];
            save.UrTroopKillsByLevel = new int[UrKillCredit.LevelCount];
            save.EnemyVehiclesKilled = 0;
            save.EnemyTroopsKilled = 0;
            CharacterSaveRepository.WriteWorldProgress(save);
            Debug.Log($"F-89: Reset {destroyedCount} destroyed outpost(s) and {targetCount} destroyed map target(s).");
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

        private static string[] Add(string[] values, string value)
        {
            var list = values == null ? new List<string>() : new List<string>(values);
            list.Add(value);
            return list.ToArray();
        }
    }
}
