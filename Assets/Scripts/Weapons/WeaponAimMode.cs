namespace F89.Weapons
{
    public enum WeaponAimMode
    {
        /// <summary>GAU-27A — crosshair slides along the nose, fires straight ahead only.</summary>
        ForwardOnly,

        /// <summary>AIM-9z — lock and fire within a forward arc from the aircraft nose.</summary>
        ForwardLock,

        /// <summary>Hellfire, SiAW, GBU-12 — lock any valid target within range in any bearing.</summary>
        OmniLock
    }
}
