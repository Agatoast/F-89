using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Character personal attributes for ground combat: HP, Move, and DR.
    /// Inherent DR is 0. Equipped helmet/vest/boots add DR; boots set Move while worn.
    /// </summary>
    public sealed class LandPlayerAttributes : MonoBehaviour
    {
        private int maxHitPoints = LandGameConstants.DefaultMaxHitPoints;
        private int baseMove = LandGameConstants.DefaultMove;
        private int inherentDamageResistance;
        private int gearDamageResistance;
        private int bootsMoveOverride;

        public int MaxHitPoints => maxHitPoints;

        /// <summary>Effective Move: boots override when worn, else character base Move.</summary>
        public int Move => bootsMoveOverride > 0
            ? bootsMoveOverride
            : baseMove;

        public int InherentDamageResistance => inherentDamageResistance;
        public int GearDamageResistance => gearDamageResistance;
        public int EffectiveDamageResistance => Mathf.Max(0, inherentDamageResistance + gearDamageResistance);

        public float MoveSpeedWorldUnits =>
            LandGameConstants.MoveWorldUnitsPerSecondAtRating3
            * (Move / (float)LandGameConstants.MoveMin);

        public void ConfigureFromSave(CharacterSaveData save)
        {
            if (save == null)
            {
                ApplyDefaults();
                return;
            }

            maxHitPoints = Mathf.Max(1, save.MaxHitPoints);
            baseMove = Mathf.Clamp(save.Move, LandGameConstants.MoveMin, LandGameConstants.MoveMax);
            inherentDamageResistance = Mathf.Max(0, save.DamageResistance);
            gearDamageResistance = 0;
            bootsMoveOverride = 0;
        }

        public void ApplyDefaults()
        {
            maxHitPoints = LandGameConstants.DefaultMaxHitPoints;
            baseMove = LandGameConstants.DefaultMove;
            inherentDamageResistance = LandGameConstants.InherentDamageResistance;
            gearDamageResistance = 0;
            bootsMoveOverride = 0;
        }

        /// <summary>Applies DR from helmet/vest/boots and Move override from boots.</summary>
        public void ApplyEquippedGear(LandRunLoadout loadout, LandItemCatalog catalog)
        {
            gearDamageResistance = 0;
            bootsMoveOverride = 0;
            if (loadout == null || catalog == null)
            {
                return;
            }

            AddGearContribution(loadout.Helmet, catalog);
            AddGearContribution(loadout.Core, catalog);
            AddGearContribution(loadout.Boots, catalog);
        }

        /// <summary>Legacy helper — prefer <see cref="ApplyEquippedGear"/>.</summary>
        public void SetBonusDamageResistance(int bonus)
        {
            gearDamageResistance = Mathf.Max(0, bonus);
        }

        private void AddGearContribution(LandGearInstance item, LandItemCatalog catalog)
        {
            if (!catalog.TryGetGearCombatStats(item, out var slot, out var damageResistance, out var move))
            {
                return;
            }

            gearDamageResistance += damageResistance;
            if (slot == LandEquipmentSlot.Boots && move > 0)
            {
                bootsMoveOverride = Mathf.Clamp(move, LandGameConstants.MoveMin, LandGameConstants.MoveMax);
            }
        }

        private void Awake()
        {
            ConfigureFromSave(CharacterSessionState.ActiveSave);
            ApplyEquippedGear(CharacterGearSession.ActiveLoadout, CharacterGearSession.Catalog);
        }
    }
}
