using System;
using System.Collections.Generic;

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
