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
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                turn -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                turn += 1f;
            }

            return new AircraftControlInput
            {
                turn = turn,
                throttleHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                airbrakeHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                afterburnerHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                aimScreenPosition = Input.mousePosition,
                hasAimScreenPosition = true,
                firePressed = Input.GetMouseButtonDown(0),
                fireHeld = Input.GetMouseButton(0),
                selectAim9zPressed = Input.GetKeyDown(KeyCode.Alpha5),
                selectAgm88jPressed = Input.GetKeyDown(KeyCode.Alpha4),
                selectGbu12Pressed = Input.GetKeyDown(KeyCode.Alpha3),
                selectAgm114Pressed = Input.GetKeyDown(KeyCode.Alpha2),
                selectGau27aPressed = Input.GetKeyDown(KeyCode.Alpha1),
                flarePressed = Input.GetKeyDown(KeyCode.F)
            };
        }
    }
}
