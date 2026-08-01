using F89.LandCombat;

namespace F89.Core
{
    /// <summary>
    /// Resolves where a fresh sortie may start: carrier deck or a friendly occupied runway.
    /// </summary>
    public static class MissionLaunchOrigin
    {
        public static bool TryResolveFromSave(CharacterSaveData save, out string friendlyOutpostName)
        {
            friendlyOutpostName = string.Empty;
            if (save == null || string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName))
            {
                return false;
            }

            var outpostName = save.MissionLaunchOutpostName.Trim();
            if (!AntarcticaOutpostState.IsFriendlyBase(outpostName))
            {
                return false;
            }

            friendlyOutpostName = outpostName;
            return true;
        }

        public static void PrepareFreshSortieLaunch(CharacterSaveData save)
        {
            LandMissionHandoffState.Clear();

            if (TryResolveFromSave(save, out var outpostName))
            {
                FlightMissionLaunchState.BeginOutpostLaunch(outpostName, vtolTakeoff: true);
                return;
            }

            LandBossMissionAssignment.ClearMissionLaunchOutpost(save);
            FlightMissionLaunchState.BeginCarrierLaunch();
        }
    }
}
