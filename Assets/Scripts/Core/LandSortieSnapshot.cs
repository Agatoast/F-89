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
        public bool HasOutpostBunker;
        public string OutpostName;
        /// <summary>
        /// Canonical 1-based flight-map square captured at landing: X is right/east,
        /// Z is up/north, and the southwest square is 1,1.
        /// </summary>
        public bool HasLandingGridCell;
        public int LandingGridCellX;
        public int LandingGridCellZ;
        public Vector3 LandingGridWorldCenter;
        /// <summary>1 means LandingGridCellX/Z use the canonical 1-based southwest-origin map grid.</summary>
        public int GridCoordinateVersion;
        /// <summary>When true, ground exit restores the parked runway deck menu instead of auto takeoff.</summary>
        public bool ReturnToRunwayDeck;

        public static LandSortieSnapshot Empty => new LandSortieSnapshot { ReturnSceneName = GameScenes.FlightTest };
    }
}
