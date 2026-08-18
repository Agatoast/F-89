using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Core
{
    public sealed class BunkerDefenseHostResumeBridge : IBunkerDefenseHostResume
    {
        public static BunkerDefenseHostResumeBridge Instance { get; } = new();

        public void ResumeToLastLanding(string lastLandedBaseId, string hostUiRequest)
        {
            var save = CharacterSessionState.ActiveSave;
            if (save != null && !string.IsNullOrWhiteSpace(lastLandedBaseId))
            {
                MissionLaunchOrigin.PersistLaunchOutpost(save, lastLandedBaseId);
            }

            if (string.Equals(
                    hostUiRequest,
                    GameConstants.HostMissionCharacterPageUi,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadScene(GameScenes.CharacterPage);
            }
        }
    }
}
