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
                return null;
            }

            var medalId = MilitaryMedalCatalog.GetHighestMedalIdFromEarnedRibbons(save.EarnedRibbonIds);
            return GetMedalTexture(medalId);
        }

        public static Texture2D GetMedalTextureByPrecedence(int precedence)
        {
            if (!MilitaryMedalCatalog.TryGetDefinitionByPrecedence(precedence, out var definition))
            {
                return null;
            }

            return GetMedalTexture(definition.Id);
        }

        public static Texture2D GetMedalTexture(string medalId)
        {
            if (string.IsNullOrEmpty(medalId) || medalId == MilitaryMedalIds.None)
            {
                return null;
            }

            if (Textures.TryGetValue(medalId, out var cached) && cached != null)
            {
                return cached;
            }

            if (!MilitaryMedalCatalog.TryGetDefinition(medalId, out var definition))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(MedalResourceRoot + definition.ResourceName);
            Textures[medalId] = texture;
            return texture;
        }

        public static void ClearCache()
        {
            Textures.Clear();
        }
    }
}
