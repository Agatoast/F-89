using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Basic Loadout tray items are drag sources only — they must not auto-fill equipped slots.
    /// </summary>
    public static class LandDefaultLoadout
    {
        private const string LegacyAutoEquipClearedPrefsPrefix = "F89_ClearedLegacyAutoEquip_";

        /// <summary>
        /// One-shot cleanup: remove starter items that were incorrectly auto-equipped into
        /// Helmet / Vest / Weapon / Boots. Tray tiles stay available on Character Loadout.
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

        public static void ClearPrefsForSave(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            PlayerPrefs.DeleteKey(LegacyAutoEquipClearedPrefsPrefix + saveId);
            PlayerPrefs.Save();
        }
    }
}
