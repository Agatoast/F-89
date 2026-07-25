using F89.Flight;
using F89.Weapons;

namespace F89.UI
{
    public sealed class ShortRangeRadarOverlay : PlaneRadarOverlay
    {
        public void ConfigureShortRange(
            AircraftController aircraftController,
            MissileLockController lockController,
            PlayerWeaponController weapons)
        {
            Configure(aircraftController, lockController, weapons, RadarScopeKind.ShortRange);
        }
    }
}
