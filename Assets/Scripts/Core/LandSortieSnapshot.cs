using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Flight-side state preserved while the player is in the land combat module.
    /// Populated before entering ground mode and restored after return.
    /// </summary>
    public struct LandSortieSnapshot
    {
        public bool IsValid;
        public Vector3 AircraftWorldPosition;
        public Quaternion AircraftWorldRotation;
        public float FuelNormalized;
        public float LeftTankGallons;
        public float RightTankGallons;
        public float AfterburnerFuelRemaining;
        public bool HasStoresInventory;
        public int Aim9zRemaining;
        public int Agm88jRemaining;
        public int Gbu12Remaining;
        public int Agm114Remaining;
        public int GauRoundsRemaining;
        public int FlaresRemaining;
        public string ReturnSceneName;

        public static LandSortieSnapshot Empty => new LandSortieSnapshot { ReturnSceneName = GameScenes.FlightTest };
    }
}
