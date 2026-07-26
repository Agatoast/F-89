using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class MilitaryRibbonService
    {
        private const string RibbonResourceRoot = "CharacterPage/Ribbons/";

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static Texture2D GetRibbonTexture(string ribbonId)
        {
            if (string.IsNullOrWhiteSpace(ribbonId))
            {
                return null;
            }

            if (Textures.TryGetValue(ribbonId, out var cached) && cached != null)
            {
                return cached;
            }

            if (!MilitaryRibbonCatalog.TryGetDefinition(ribbonId, out var definition))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(RibbonResourceRoot + definition.ResourceName);
            Textures[ribbonId] = texture;
            return texture;
        }

        public static void ClearCache()
        {
            Textures.Clear();
        }
    }
}
