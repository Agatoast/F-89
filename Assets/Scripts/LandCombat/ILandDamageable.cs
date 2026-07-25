namespace F89.LandCombat
{
    public interface ILandDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(float amount);
    }
}
