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

        /// <summary>When true, ground exit restores the parked friendly runway menu instead of immediate takeoff.</summary>

        public bool ReturnToRunwayDeck;

        /// <summary>When true, the player landed in open terrain (not CV or outpost).</summary>

        public bool IsOpenFieldLanding;

        /// <summary>When true, flight restore starts VTOL takeoff from the saved landing coordinates.</summary>

        public bool RestoreWithImmediateTakeoff;

        /// <summary>Tactical-map mile coordinate captured at touchdown.</summary>

        public bool HasLandingMiles;

        public float LandingMileX;

        public float LandingMileY;

        public float LandingRotationY;

        /// <summary>Non-outpost waypoint secondary landing (WP-NN). Empty for outpost or open-field landings.</summary>
        public string WaypointSiteCode;

        /// <summary>When true, restore in-flight airspeed after mid-air refuel return.</summary>
        public bool HasInFlightSpeed;

        public float InFlightSpeedMph;

        public bool InFlightAutopilotActive;



        public static LandSortieSnapshot Empty => new LandSortieSnapshot { ReturnSceneName = GameScenes.FlightTest };

    }

}

