namespace F89.Core
{
    public static class CharacterSessionState
    {
        private static CharacterSaveData activeSave;

        public static CharacterSaveData ActiveSave
        {
            get => activeSave;
            set
            {
                activeSave = value;
                BunkerDefenseIntegration.OnActiveSaveChanged(value);
            }
        }
    }
}
