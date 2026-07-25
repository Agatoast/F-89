using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class MilitaryMedalService
    {
        private const string MedalResourceRoot = "SelectionPage/Medals/";

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static Texture2D GetMedalTexture(CharacterSaveData save)
        {
            if (save == null)
            {
                return GetMedalTexture(MilitaryMedalIds.DefaultForNewCharacter);
            }

            return GetMedalTexture(MilitaryMedalCatalog.NormalizeAwardId(save.HighestAward));
        }

        public static Texture2D GetMedalTexture(string medalId)
        {
            var normalizedId = MilitaryMedalCatalog.NormalizeAwardId(medalId);
            if (Textures.TryGetValue(normalizedId, out var cached) && cached != null)
            {
                return cached;
            }

            if (!MilitaryMedalCatalog.TryGetDefinition(normalizedId, out var definition))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(MedalResourceRoot + definition.ResourceName);
            Textures[normalizedId] = texture;
            return texture;
        }

        public static void ClearCache()
        {
            Textures.Clear();
        }
    }
}
