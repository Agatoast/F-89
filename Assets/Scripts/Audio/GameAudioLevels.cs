namespace F89.Audio
{
    /// <summary>
    /// Per-channel reference volumes. Settings percents map linearly;
    /// 50% equals the comfortable reference established for music / lock tones.
    /// </summary>
    public static class GameAudioLevels
    {
        /// <summary>Theme bed at Music 50%.</summary>
        public const float MusicVolumeAtFiftyPercent = F89.Core.GameMusic.VolumeAtFiftyPercent;

        /// <summary>Missile lock / tone at Sound FX 50% (half prior loudness).</summary>
        public const float MissileLockVolumeAtFiftyPercent = 0.175f;

        /// <summary>Sustained target-lock tone is half the lock/beep channel level.</summary>
        public const float MissileTargetLockToneScale = 0.5f;

        public static float MissileTargetingSfxVolume =>
            CurrentSfxVolume * MissileTargetLockToneScale;

        public static float CurrentMusicVolume => F89.Core.GameMusic.CurrentLinearVolume;

        public static float CurrentSfxVolume =>
            (F89.Core.GameSettings.SfxVolumePercent / 100f) * (MissileLockVolumeAtFiftyPercent * 2f);

        /// <summary>Legacy name kept for missile lock setup (equals 50% SFX).</summary>
        public const float MissileLockVolumeAtFive = MissileLockVolumeAtFiftyPercent;
    }
}
