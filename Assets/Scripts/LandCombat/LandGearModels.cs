using System;
using System.Collections.Generic;

namespace F89.LandCombat
{
    [Serializable]
    public sealed class LandRolledAffix
    {
        public int StatId;
        public float Value;
    }

    [Serializable]
    public sealed class LandGearInstance
    {
        public string DefinitionId = string.Empty;
        public LandItemRarity Rarity = LandItemRarity.White;
        public List<LandRolledAffix> Affixes = new();
    }

    [Serializable]
    public sealed class LandRunLoadout
    {
        public LandGearInstance Weapon;
        public LandGearInstance Core;
        public LandGearInstance Boots;
        public LandGearInstance DuffleBag;
        public LandGearInstance Utility1;
        public LandGearInstance Utility2;
        public LandGearInstance Module1;
        public LandGearInstance Module2;
        public LandGearInstance Helmet;
        public LandGearInstance Shield;
        public List<LandGearInstance> Inventory = new();
    }
}
