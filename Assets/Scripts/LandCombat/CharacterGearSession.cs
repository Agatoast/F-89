using F89.Core;

namespace F89.LandCombat
{
    public static class CharacterGearSession
    {
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

        public static void Bind(CharacterSaveData save)
        {
            if (save == null)
            {
                ActiveLoadout = new LandRunLoadout();
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            LandDefaultLoadout.ApplyStarterKit(save);
            ActiveLoadout = LandGearSaveMapper.ToRuntime(save.Loadout);
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
