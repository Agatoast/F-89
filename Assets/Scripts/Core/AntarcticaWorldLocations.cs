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
        public static readonly Vector2 DefaultCarrierPositionMiles = new Vector2(-1223f, 670f);

        private static Vector2 runtimeCarrierPositionMiles = DefaultCarrierPositionMiles;

        public static Vector2 CarrierPositionMiles => runtimeCarrierPositionMiles;

        public static void SetCarrierPositionMiles(Vector2 miles)
        {
            runtimeCarrierPositionMiles = miles;
        }
    }
}
