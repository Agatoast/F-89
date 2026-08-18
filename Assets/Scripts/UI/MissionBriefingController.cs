using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class MissionBriefingController : MonoBehaviour
    {
        private bool showBailOutConfirm;

        private void Awake()
        {
            if (CharacterSessionState.ActiveSave != null
                && (CharacterSessionState.ActiveSave.IsKilledInAction
                    || CharacterSessionState.ActiveSave.IsCourtMartialed))
            {
                SceneManager.LoadScene(GameScenes.SelectionPage);
            }
        }

        private void OnGUI()
        {
            MissionBriefingUi.Draw(
                MissionBriefingUi.FooterMode.PreMissionAcceptBailOut,
                ref showBailOutConfirm,
                onAccept: AcceptMission,
                onConfirmBailOut: ConfirmBailOut,
                onContinue: null);
        }

        private static void AcceptMission()
        {
            Time.timeScale = 1f;
            CharacterLoadoutNavState.MarkEnteredFromMissionBrief();
            SceneManager.LoadScene(GameScenes.CharacterLoadout);
        }

        private static void ConfirmBailOut()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }
    }
}
