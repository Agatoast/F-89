using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandPlayerController : MonoBehaviour
    {
        private LandPlayerMotor motor;
        private LandPlayerCombat combat;
        private LandPlayerHealth health;

        private void Awake()
        {
            motor = GetComponent<LandPlayerMotor>();
            combat = GetComponent<LandPlayerCombat>();
            health = GetComponent<LandPlayerHealth>();
        }

        private void Update()
        {
            LandLootBagUi.HandleCloseHotkey();

            if (GamePauseController.IsPaused
                || LandBossEncounter.IsIntroActive
                || LandLandedPlane.IsTakeOffPromptPending
                || (health != null && health.IsUnconscious))
            {
                motor.SetMoveInput(Vector2.zero);
                combat.SetFireHeld(false);
                return;
            }

            var move = new Vector2(
                Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f,
                Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
            move.x += Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
            move.y -= Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
            motor.SetMoveInput(move);

            var worldMouse = Camera.main != null
                ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
                : (Vector2)transform.position + Vector2.right;
            motor.SetAimDirection(worldMouse);

            LandCorpseLootInput.UpdateHover(transform.position);
            LandLandedPlane.HandleTakeOffClick();

            if (GameKeyBindings.WasPressed(GameKeyBindingIds.AutoFireToggle))
            {
                AutoFireState.Toggle();
            }

            var manualFire = Input.GetMouseButton(0);
            var blockAllFire = LandLandedPlane.IsTakeOffPromptPending
                                || LandLandedPlane.IsWorldPointerOverPlane()
                                || LandLootBagUi.IsPointerOverPanel()
                                || (LandLootBagSession.IsOpen && !LandLootBagSession.IsPinned);
            var blockManualFire = blockAllFire
                                  || LandCombatHud.IsPointerOverHud()
                                  || CharacterPageGearUi.IsDraggingGear;

            if (blockManualFire)
            {
                manualFire = false;
            }

            combat.SetFireHeld(!blockAllFire && (manualFire || AutoFireState.Enabled));
        }
    }
}
