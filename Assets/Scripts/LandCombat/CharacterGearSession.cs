using F89.Core;

namespace F89.LandCombat
{
    /// <summary>
    /// Single live gear session shared by Character Page, Character Loadout, and land combat.
    /// Equipment (4 boxes), inventory, and footlocker/vault all read and write this state.
    /// </summary>
    public static class CharacterGearSession
    {
        private static string boundSaveId;

        public static LandRunLoadout ActiveLoadout { get; private set; } = new();
        public static LandItemCatalog Catalog { get; } = new();

        public static CharacterSaveData ActiveSave => CharacterSessionState.ActiveSave;

        public static CharacterVaultSaveData ActiveVault
        {
            get
            {
                if (ActiveSave == null)
                {
                    return null;
                }

                CharacterSaveRepository.EnsureGearInitialized(ActiveSave);
                return ActiveSave.Vault;
            }
        }

        public static void Bind(CharacterSaveData save, bool forceReload = false)
        {
            if (save == null)
            {
                ActiveLoadout = new LandRunLoadout();
                boundSaveId = null;
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var clearedLegacy = LandDefaultLoadout.TryClearLegacyAutoEquippedStarters(save);
            if (clearedLegacy)
            {
                CharacterSaveRepository.WriteGear(save);
            }

            if (!forceReload
                && boundSaveId == save.Id
                && ActiveLoadout != null)
            {
                return;
            }

            ActiveLoadout = LandGearSaveMapper.ToRuntime(save.Loadout);
            boundSaveId = save.Id;
        }

        public static void PersistActive()
        {
            var save = ActiveSave;
            if (save == null)
            {
                return;
            }

            LandGearSaveMapper.ToSave(ActiveLoadout, save.Loadout);
            CharacterSaveRepository.WriteGear(save);
        }
    }
}
