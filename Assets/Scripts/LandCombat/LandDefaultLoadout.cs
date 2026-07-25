using F89.Core;

namespace F89.LandCombat
{
    public static class LandDefaultLoadout
    {
        public static void ApplyStarterKit(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            if (LandLoadoutSlots.IsValidItem(LandGearSaveMapper.ToRuntimeInstance(save.Loadout.Weapon)))
            {
                return;
            }

            save.Loadout.Weapon = new CharacterGearInstanceSaveData
            {
                DefinitionId = "Blaster",
                Rarity = (int)LandItemRarity.White
            };
        }
    }
}
