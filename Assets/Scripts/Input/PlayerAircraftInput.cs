using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Controls
{
    public struct AircraftControlInput
    {
        public float turn;
        public bool throttleHeld;
        public bool airbrakeHeld;
        public bool afterburnerHeld;
        public Vector2 aimScreenPosition;
        public bool hasAimScreenPosition;
        public bool firePressed;
        public bool fireHeld;
        public bool selectAim9zPressed;
        public bool selectAgm88jPressed;
        public bool selectGbu12Pressed;
        public bool selectAgm114Pressed;
        public bool selectGau27aPressed;
        public bool flarePressed;
        public bool cycleTargetPressed;
    }

    [DefaultExecutionOrder(-100)]
    public class PlayerAircraftInput : MonoBehaviour
    {
        public AircraftControlInput Current { get; private set; }

        private bool firePressed;
        private bool selectAim9zPressed;
        private bool selectAgm88jPressed;
        private bool selectGbu12Pressed;
        private bool selectAgm114Pressed;
        private bool selectGau27aPressed;
        private bool flarePressed;
        private bool cycleTargetPressed;

        private void Update()
        {
            if (IsInputBlocked())
            {
                Current = default;
                ClearPressedInputs();
                return;
            }

            firePressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.Fire);
            if (GameKeyBindings.WasPressed(GameKeyBindingIds.AutoFireToggle))
            {
                AutoFireState.Toggle();
            }

            selectAim9zPressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAim9z);
            selectAgm88jPressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAgm88j);
            selectGbu12Pressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.SelectGbu12);
            selectAgm114Pressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAgm114);
            selectGau27aPressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.SelectGau27a);
            flarePressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.Flare);
            cycleTargetPressed |= GameKeyBindings.WasPressed(GameKeyBindingIds.CycleTarget);
            Current = BuildCurrentInput();
        }

        private void FixedUpdate()
        {
            if (IsInputBlocked())
            {
                Current = default;
                return;
            }

            Current = BuildCurrentInput();
        }

        private void LateUpdate()
        {
            ClearPressedInputs();
        }

        private static bool IsInputBlocked() =>
            GamePauseController.IsPaused || AntarcticaMapOverlay.IsOpen;

        private void ClearPressedInputs()
        {
            firePressed = false;
            selectAim9zPressed = false;
            selectAgm88jPressed = false;
            selectGbu12Pressed = false;
            selectAgm114Pressed = false;
            selectGau27aPressed = false;
            flarePressed = false;
            cycleTargetPressed = false;
        }

        private AircraftControlInput BuildCurrentInput()
        {
            var turn = 0f;
            if (GameKeyBindings.IsHeld(GameKeyBindingIds.TurnLeft))
            {
                turn -= 1f;
            }

            if (GameKeyBindings.IsHeld(GameKeyBindingIds.TurnRight))
            {
                turn += 1f;
            }

            return new AircraftControlInput
            {
                turn = turn,
                throttleHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Throttle),
                airbrakeHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Airbrake),
                afterburnerHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Afterburner),
                aimScreenPosition = Input.mousePosition,
                hasAimScreenPosition = true,
                firePressed = firePressed,
                fireHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Fire) || AutoFireState.Enabled,
                selectAim9zPressed = selectAim9zPressed,
                selectAgm88jPressed = selectAgm88jPressed,
                selectGbu12Pressed = selectGbu12Pressed,
                selectAgm114Pressed = selectAgm114Pressed,
                selectGau27aPressed = selectGau27aPressed,
                flarePressed = flarePressed,
                cycleTargetPressed = cycleTargetPressed
            };
        }
    }
}
