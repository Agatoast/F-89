using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class PilotRankInsigniaService
    {
        private const string RankResourceRoot = "CharacterPage/Ranks/";

        private static readonly Dictionary<int, Texture2D> Textures = new();

        public static Texture2D GetInsigniaTexture(string rank) =>
            GetInsigniaTexture(PilotCareerRanks.GetRankIndex(rank));

        public static Texture2D GetInsigniaTexture(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= PilotCareerRanks.RankCount)
            {
                return null;
            }

            if (Textures.TryGetValue(rankIndex, out var cached) && cached != null)
            {
                return cached;
            }

            var resourceName = PilotCareerRanks.GetRankInsigniaResourceName(rankIndex);
            var texture = Resources.Load<Texture2D>(RankResourceRoot + resourceName);
            Textures[rankIndex] = texture;
            return texture;
        }

        public static void ClearCache()
        {
            Textures.Clear();
        }
    }
}
