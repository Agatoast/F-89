namespace F89.Weapons
{
    public enum TargetUnitClass
    {
        Standard,
        Infantry,
        FlareDecoy,
        PlayerAircraft,
        GroundVehicle,
        Building,
        /// <summary>Helicopter / aerial vehicle (e.g. UR TDP, US AH-64). Uses air combat and air damage rules.</summary>
        Flier
    }
}
