using F89.Core;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public class GroundAttackScreenController : MonoBehaviour
    {
        private const float DownedExitDelaySeconds = 2.5f;
        private bool exitingAfterDowned;
        private float downedExitAt;

        private void Start()
        {
            LandCombatConsumables.ResetForMission();
            LandGroundCrosshair.Apply();
        }

        private void OnDestroy()
        {
            LandGroundCrosshair.Clear();
        }

        private void OnDisable()
        {
            LandGroundCrosshair.Clear();
        }

        private void Update()
        {
            LandCombatHud.HandleHotkeys();
            TryScheduleDownedExit();
        }

        private void TryScheduleDownedExit()
        {
            if (exitingAfterDowned)
            {
                if (Time.unscaledTime >= downedExitAt)
                {
                    exitingAfterDowned = false;
                    LandGroundMissionExit.Leave();
                }

                return;
            }

            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            if (health == null || !health.IsUnconscious)
            {
                return;
            }

            exitingAfterDowned = true;
            downedExitAt = Time.unscaledTime + DownedExitDelaySeconds;
            Debug.Log($"F-89 Land: Downed ({health.DownedOutcome}) — leaving ground in {DownedExitDelaySeconds:0.0}s.");
        }

        private void OnGUI()
        {
            DrawOpenFieldInfantryNotice();
            LandCombatHud.Draw(LandGroundMissionExit.Leave);
            LandLootBagUi.Draw();
            DrawRunwayDeckOrTakeOffDialog();
        }

        private static void DrawOpenFieldInfantryNotice()
        {
            if (!OpenFieldLandingNotice.IsPending)
            {
                return;
            }

            if (OkMessageDialog.Draw("Enemy infantry in the open") == OkMessageDialog.Result.Confirmed)
            {
                OpenFieldLandingNotice.Clear();
            }
        }

        private static void DrawRunwayDeckOrTakeOffDialog()
        {
            if (!LandLandedPlane.IsTakeOffPromptPending)
            {
                return;
            }

            GroundRunwayDeckMenu.Draw();
        }
    }
}
