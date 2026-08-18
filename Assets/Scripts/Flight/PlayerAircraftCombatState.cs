namespace F89.Flight
{
    /// <summary>
    /// When the player is landed, parked, or in deck/approach menus, enemy air weapons
    /// must not connect and onboard weapons must stay silent.
    /// </summary>
    public static class PlayerAircraftCombatState
    {
        public static bool IsAirborneForEnemyEngagement(AircraftController aircraft = null)
        {
            aircraft ??= AircraftController.Player;
            if (aircraft == null)
            {
                return false;
            }

            if (AircraftLandingController.IsLandingActive
                || AircraftLandingController.IsLandingComplete
                || AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsParkedAtRunway
                || AircraftLandingController.IsRunwayDeckMenuVisible
                || AircraftLandingController.IsCarrierApproachPromptVisible
                || aircraft.IsLandingLocked)
            {
                return false;
            }

            return true;
        }

        public static bool CanOperateWeapons(AircraftController aircraft = null) =>
            IsAirborneForEnemyEngagement(aircraft);
    }
}
