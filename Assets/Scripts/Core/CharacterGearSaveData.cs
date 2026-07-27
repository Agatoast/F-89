using System;

namespace F89.Core
{
    [Serializable]
    public class CharacterGearAffixSaveData
    {
        // Maps to F89.LandCombat.LandItemStat (Damage, Range).
        public int StatId;
        public float Value;
    }

    [Serializable]
    public class CharacterGearInstanceSaveData
    {
        public string DefinitionId = string.Empty;
        public int Rarity;
        public CharacterGearAffixSaveData[] Affixes = Array.Empty<CharacterGearAffixSaveData>();
    }

    [Serializable]
    public class CharacterLoadoutSaveData
    {
        public CharacterGearInstanceSaveData Weapon;
        public CharacterGearInstanceSaveData Core;
        public CharacterGearInstanceSaveData Boots;
        public CharacterGearInstanceSaveData DuffleBag;
        public CharacterGearInstanceSaveData Utility1;
        public CharacterGearInstanceSaveData Utility2;
        public CharacterGearInstanceSaveData Module1;
        public CharacterGearInstanceSaveData Module2;
        public CharacterGearInstanceSaveData Helmet;
        public CharacterGearInstanceSaveData Shield;
        public CharacterGearInstanceSaveData[] Inventory = Array.Empty<CharacterGearInstanceSaveData>();
    }

    [Serializable]
    public class CharacterVaultSaveData
    {
        public const int DefaultSlotCount = 30;

        public int SlotCount = DefaultSlotCount;
        public CharacterGearInstanceSaveData[] Items = Array.Empty<CharacterGearInstanceSaveData>();
    }
}
