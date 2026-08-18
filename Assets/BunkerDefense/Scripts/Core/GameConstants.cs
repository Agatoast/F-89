namespace SaveAntarctica.BunkerDefense.Core
{
    public static class GameConstants
    {
        public const int DefaultHangarHp = 100;
        public const int GrenadeHangarDamage = 18;
        public const int SoldierReachHangarDamage = 25;
        public const int SoldierHitPoints = 3;
        public const int RusherHitPoints = 2;
        public const int VehicleHitPoints = 20;
        public const int VehicleMissileHangarDamage = GrenadeHangarDamage * 5;

        public const float MachineGunRoundsPerSecond = 14f;
        public const float MachineGunSpread = 0.16f;
        public const float MachineGunHitRadius = 0.32f;
        public const float MachineGunRange = 22f;
        public const float HeatPerShot = 0.020625f;
        public const float HeatCoolPerSecond = 0.38f;
        public const float OverheatLockUntil = 0.22f;
        public const string MachineGunFireSoundResource = "Bunker/GAU-27sound";
        public const string ClaxonSoundResource = "Bunker/claxon";
        public const float BriefingClaxonVolume = 0.41f;
        public const string GrenadeExplosionSoundResource = "Bunker/grenade_explosion";
        public const string RocketExplosionSoundResource = "Bunker/rocket_explosion";
        public const string LightningDischargeSoundResource = "Bunker/lightning_discharge";
        public const float LightningDischargeVolume = 0.88f;
        public const string BoardClearGrenadeResource = "Bunker/grenade";

        public const int FlyingSaucerHitPoints = VehicleHitPoints * 2;
        public const float FlyingSaucerScale = 0.52f;
        public const float FlyingSaucerMinSpeed = 1.05f;
        public const float FlyingSaucerMaxSpeed = 1.55f;
        public const float FlyingSaucerCrashFallSpeed = 2.35f;
        public const float FlyingSaucerExplosionScale = 0.55f;
        public const float FlyingSaucerLightningMinInterval = 2f;
        public const float FlyingSaucerLightningMaxInterval = 3f;
        public const int FlyingSaucerLightningHangarDamage = 1;
        public const float SkyBandHeightFraction = 0.25f;

        public const int DefaultPlaneHitPoints = 3;
        public const int MinPlaneHitPoints = 1;
        public const int DefenseSuccessHitDelta = 1;
        public const int DefenseFailureHitDelta = -1;

        public const string EnemyTag = "Enemy";
        public const string HostMissionCharacterPageUi = "MissionCharacterPage";
        public const string VictoryHeadline = "Hangar Defense Successful";
        public const string DefeatHeadline = "HANGAR DESTROYED";
    }
}
