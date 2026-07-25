using F89.Core;
using F89.Flight;
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

        private void Update()
        {
            RefreshCurrent();
        }

        private void FixedUpdate()
        {
            RefreshCurrent();
        }

        private void RefreshCurrent()
        {
            if (GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen)
            {
                Current = default;
                return;
            }

            Current = ReadLegacyInput();
        }

        private static AircraftControlInput ReadLegacyInput()
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
                firePressed = GameKeyBindings.WasPressed(GameKeyBindingIds.Fire),
                fireHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Fire),
                selectAim9zPressed = GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAim9z),
                selectAgm88jPressed = GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAgm88j),
                selectGbu12Pressed = GameKeyBindings.WasPressed(GameKeyBindingIds.SelectGbu12),
                selectAgm114Pressed = GameKeyBindings.WasPressed(GameKeyBindingIds.SelectAgm114),
                selectGau27aPressed = GameKeyBindings.WasPressed(GameKeyBindingIds.SelectGau27a),
                flarePressed = GameKeyBindings.WasPressed(GameKeyBindingIds.Flare),
                cycleTargetPressed = GameKeyBindings.WasPressed(GameKeyBindingIds.CycleTarget)
            };
        }
    }
}
