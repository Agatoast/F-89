using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Fixed world-map locations for the full campaign. The carrier seed is resolved to visible
    /// ocean on the satellite map at spawn and cached here for HUD/map readout.
    /// </summary>
    public static class AntarcticaWorldLocations
    {
        public const string CarrierName = "USS Martin Van Buren";

        /// Fixed map seed for the carrier — resolved to visible ocean on the satellite map at spawn.
        /// Map display up/north is -Z miles (see AntarcticaMapOverlay map georef).
        // +X east/right, -Y map-up/north (mile +Y draws toward screen bottom).
        public static readonly Vector2 DefaultCarrierPositionMiles = new Vector2(-1022.5f, 569.5f);

        private static Vector2 runtimeCarrierPositionMiles = DefaultCarrierPositionMiles;

        public static Vector2 CarrierPositionMiles => runtimeCarrierPositionMiles;

        public static void SetCarrierPositionMiles(Vector2 miles)
        {
            runtimeCarrierPositionMiles = miles;
        }
    }
}
