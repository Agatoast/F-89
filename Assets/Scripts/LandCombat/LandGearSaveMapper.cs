using System.Collections.Generic;
using F89.Core;

namespace F89.LandCombat
{
    public static class LandGearSaveMapper
    {
        public static LandRunLoadout ToRuntime(CharacterLoadoutSaveData save)
        {
            if (save == null)
            {
                return new LandRunLoadout();
            }

            var loadout = new LandRunLoadout
            {
                Weapon = ToRuntimeInstance(save.Weapon),
                Core = ToRuntimeInstance(save.Core),
                Boots = ToRuntimeInstance(save.Boots),
                DuffleBag = ToRuntimeInstance(save.DuffleBag),
                Utility1 = ToRuntimeInstance(save.Utility1),
                Utility2 = ToRuntimeInstance(save.Utility2),
                Module1 = ToRuntimeInstance(save.Module1),
                Module2 = ToRuntimeInstance(save.Module2),
                Helmet = ToRuntimeInstance(save.Helmet),
                Shield = ToRuntimeInstance(save.Shield),
                Inventory = new List<LandGearInstance>()
            };

            if (save.Inventory != null)
            {
                for (var i = 0; i < save.Inventory.Length; i++)
                {
                    loadout.Inventory.Add(ToRuntimeInstance(save.Inventory[i]));
                }
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            return loadout;
        }

        public static void ToSave(LandRunLoadout runtime, CharacterLoadoutSaveData save)
        {
            if (runtime == null || save == null)
            {
                return;
            }

            save.Weapon = ToSaveInstance(runtime.Weapon);
            save.Core = ToSaveInstance(runtime.Core);
            save.Boots = ToSaveInstance(runtime.Boots);
            save.DuffleBag = ToSaveInstance(runtime.DuffleBag);
            save.Utility1 = ToSaveInstance(runtime.Utility1);
            save.Utility2 = ToSaveInstance(runtime.Utility2);
            save.Module1 = ToSaveInstance(runtime.Module1);
            save.Module2 = ToSaveInstance(runtime.Module2);
            save.Helmet = ToSaveInstance(runtime.Helmet);
            save.Shield = ToSaveInstance(runtime.Shield);

            LandInventoryRules.EnsureInventoryCapacity(runtime);
            save.Inventory = new CharacterGearInstanceSaveData[runtime.Inventory.Count];
            for (var i = 0; i < runtime.Inventory.Count; i++)
            {
                save.Inventory[i] = ToSaveInstance(runtime.Inventory[i]);
            }
        }

        public static CharacterVaultSaveData CloneVault(CharacterVaultSaveData vault)
        {
            if (vault == null)
            {
                return new CharacterVaultSaveData();
            }

            var clone = new CharacterVaultSaveData { SlotCount = vault.SlotCount };
            if (vault.Items == null)
            {
                clone.Items = System.Array.Empty<CharacterGearInstanceSaveData>();
                return clone;
            }

            clone.Items = new CharacterGearInstanceSaveData[vault.Items.Length];
            for (var i = 0; i < vault.Items.Length; i++)
            {
                clone.Items[i] = ToSaveInstance(ToRuntimeInstance(vault.Items[i]));
            }

            return clone;
        }

        public static LandGearInstance ToRuntimeInstance(CharacterGearInstanceSaveData save)
        {
            if (save == null || string.IsNullOrEmpty(save.DefinitionId))
            {
                return null;
            }

            var affixes = new List<LandRolledAffix>();
            if (save.Affixes != null)
            {
                for (var i = 0; i < save.Affixes.Length; i++)
                {
                    var affix = save.Affixes[i];
                    if (affix == null)
                    {
                        continue;
                    }

                    affixes.Add(new LandRolledAffix
                    {
                        Stat = (LandItemStat)affix.StatId,
                        Value = affix.Value
                    });
                }
            }

            return new LandGearInstance
            {
                DefinitionId = save.DefinitionId,
                Rarity = (LandItemRarity)save.Rarity,
                Affixes = affixes
            };
        }

        public static CharacterGearInstanceSaveData ToSaveInstance(LandGearInstance runtime)
        {
            if (!LandLoadoutSlots.IsValidItem(runtime))
            {
                return null;
            }

            CharacterGearAffixSaveData[] affixes = System.Array.Empty<CharacterGearAffixSaveData>();
            if (runtime.Affixes != null && runtime.Affixes.Count > 0)
            {
                affixes = new CharacterGearAffixSaveData[runtime.Affixes.Count];
                for (var i = 0; i < runtime.Affixes.Count; i++)
                {
                    affixes[i] = new CharacterGearAffixSaveData
                    {
                        StatId = (int)runtime.Affixes[i].Stat,
                        Value = runtime.Affixes[i].Value
                    };
                }
            }

            return new CharacterGearInstanceSaveData
            {
                DefinitionId = runtime.DefinitionId,
                Rarity = (int)runtime.Rarity,
                Affixes = affixes
            };
        }
    }
}
