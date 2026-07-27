namespace F89.LandCombat
{
    public static class LandGameConstants
    {
        /// <summary>Default max Hit Points for a new character.</summary>
        public const int DefaultMaxHitPoints = 100;

        /// <summary>Legacy alias for HUD / older call sites.</summary>
        public const float PlayerMaxHealth = DefaultMaxHitPoints;

        /// <summary>Inherent character Damage Resistance before gear.</summary>
        public const int InherentDamageResistance = 0;

        /// <summary>Move rating range (relative ground speed).</summary>
        public const int MoveMin = 3;
        public const int MoveMax = 15;
        public const int DefaultMove = 3;

        /// <summary>
        /// World units/sec at Move rating 3. Higher Move scales linearly: speed = this * (Move / 3).
        /// Tunable once Move-3 feel is established in playtests.
        /// </summary>
        public const float MoveWorldUnitsPerSecondAtRating3 = 6f;

        /// <summary>Legacy fixed speed; prefer <see cref="LandPlayerAttributes.MoveSpeedWorldUnits"/>.</summary>
        public const float PlayerMoveSpeed = MoveWorldUnitsPerSecondAtRating3 * (DefaultMove / (float)MoveMin);

        /// <summary>
        /// Land-combat range units only (not flight / map miles).
        /// 10 = distance from character to left or right screen edge in GroundAttack.
        /// </summary>
        public const float LandUnitsToScreenEdge = 10f;

        /// <summary>Legacy alias for <see cref="LandUnitsToScreenEdge"/>.</summary>
        public const float WeaponRangeFullScreen = LandUnitsToScreenEdge;

        public const float WorldUnitsPerTile = 1f;
        public const float WorldUnitsPerMile = 20f;
        public const float ArenaSizeMiles = 1f;
        public const float ArenaSizeWorldUnits = 20f;
        public const float ArenaHalfSizeWorldUnits = 10f;

        /// <summary>Infinite ground quad follows the player; sized large enough to fill any camera view.</summary>
        public const float InfiniteTerrainWorldSize = 240f;

        /// <summary>Continuous outdoor exposure before freezing (2 hours).</summary>
        public const float OutdoorExposureLimitSeconds = 2f * 60f * 60f;

        /// <summary>Minimap zoom: 2× radius = 4× visible area; widget size unchanged.</summary>
        public const float MinimapViewHalfSizeMultiplier = 2f;

        /// <summary>World X offset of parked F-89 left of the player at ground-battle start.</summary>
        public const float LandedPlaneOffsetWorldUnits = 9f;

        /// <summary>Nearest enemy spawn distance from the landed plane (land-combat range units).</summary>
        public const float EnemySpawnMinRangeLandUnits = 50f;
        public const float EnemySpawnMaxRangeLandUnits = 100f;

        /// <summary>Bunker entrance distance from the plane along the same objective bearing.</summary>
        public const float BunkerEntranceMinRangeLandUnits = 55f;
        public const float BunkerEntranceMaxRangeLandUnits = 110f;

        /// <summary>Show bunker direction on the minimap when within this land-unit distance.</summary>
        public const float BunkerMinimapIndicatorMaxRangeLandUnits = 200f;

        /// <summary>World size of the bunker entrance pad (black square).</summary>
        public const float BunkerEntranceWorldSize = 2.2f;

        public const int ProjectilePoolSize = 256;
        public const float DefaultProjectileLifetime = 4f;
        public const int PackSlotCount = 12;
        public const int DuffleBagSlotCount = 12;
        public const int MainSlotCount = PackSlotCount + DuffleBagSlotCount;
        public const int VaultBaseSlotCount = 30;
        public const int VaultMaxSlotCount = 50;
        public const int VaultGridColumns = 10;
        public const int InventoryGridColumns = 4;
        public const int InventoryGridRows = 3;

        public const int BandageSlotCapacity = 5;
        public const int BandageHealAmount = 10;
        public const int GrenadeSlotCapacity = 4;
        public const float GrenadeDamage = 50f;
        /// <summary>Blast radius in land-combat units.</summary>
        public const float GrenadeBlastRadiusLandUnits = 3f;
        public const float GrenadeThrowSpeed = 14f;
    }
}
