using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Starter / R&amp;D Loadout tray gear. New characters begin with one of each
    /// starter item equipped (Helmet, Vest, Weapon, Boots).
    /// </summary>
    public static class LandDefaultLoadout
    {
        private const string LegacyAutoEquipClearedPrefsPrefix = "F89_ClearedLegacyAutoEquip_";

        /// <summary>
        /// Equips the four starter R&amp;D Loadout items into empty equipment slots.
        /// </summary>
        public static void EquipStarterEquipment(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var loadout = LandGearSaveMapper.ToRuntime(save.Loadout);

            if (LandUsGearCatalog.TryGetByDefinitionId(LandUsGearCatalog.BasicHelmetId, out var helmet))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Helmet, FromGear(helmet));
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(LandUsGearCatalog.BasicVestId, out var vest))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Core, FromGear(vest));
            }

            if (LandUsWeaponCatalog.TryGetByDefinitionId(
                    LandUsWeaponCatalog.BasicLoadoutDefinitionId,
                    out var weapon))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Weapon, FromWeapon(weapon));
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(LandUsGearCatalog.BasicBootsId, out var boots))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Boots, FromGear(boots));
            }

            LandGearSaveMapper.ToSave(loadout, save.Loadout);
            SuppressLegacyStarterClear(save.Id);
        }

        /// <summary>
        /// One-shot cleanup for legacy saves that incorrectly auto-equipped starters
        /// before tray-only behavior. New characters that receive
        /// <see cref="EquipStarterEquipment"/> skip this via prefs.
        /// </summary>
        public static bool TryClearLegacyAutoEquippedStarters(CharacterSaveData save)
        {
            if (save == null || string.IsNullOrEmpty(save.Id))
            {
                return false;
            }

            var prefsKey = LegacyAutoEquipClearedPrefsPrefix + save.Id;
            if (PlayerPrefs.GetInt(prefsKey, 0) == 1)
            {
                return false;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            save.Loadout.Helmet = null;
            save.Loadout.Core = null;
            save.Loadout.Weapon = null;
            save.Loadout.Boots = null;
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void SuppressLegacyStarterClear(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            PlayerPrefs.SetInt(LegacyAutoEquipClearedPrefsPrefix + saveId, 1);
            PlayerPrefs.Save();
        }

        public static void ClearPrefsForSave(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            PlayerPrefs.DeleteKey(LegacyAutoEquipClearedPrefsPrefix + saveId);
            PlayerPrefs.Save();
        }

        private static LandGearInstance FromGear(LandUsGearCatalog.Entry entry) =>
            new()
            {
                DefinitionId = entry.DefinitionId,
                Rarity = entry.Rarity
            };

        private static LandGearInstance FromWeapon(LandUsWeaponCatalog.Entry entry) =>
            new()
            {
                DefinitionId = entry.DefinitionId,
                Rarity = entry.Rarity
            };
    }
}
