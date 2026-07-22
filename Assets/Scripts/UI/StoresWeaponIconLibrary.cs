using System.Collections.Generic;
using UnityEngine;

namespace F89.UI
{
    public static class StoresWeaponIconLibrary
    {
        public const float IconDisplayScale = 0.5f;
        private const string ReferenceIconName = "agm114";

        // Overall length relative to AGM-114 Hellfire (~64 in).
        private static readonly Dictionary<string, float> LengthRelativeToAgm114 =
            new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["agm114"] = 1f,
                ["gbu12"] = 129f / 64f,
                ["agm88j"] = 164f / 64f * 0.9f,
                ["aim9z"] = 119f / 64f
            };

        private static Texture2D referenceTexture;

        public static bool TryGetIcon(string iconName, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(iconName))
            {
                return false;
            }

            texture = Resources.Load<Texture2D>($"Stores/Icons/{iconName}");
            return texture != null;
        }

        public static Rect FitIconRect(Rect bounds, Texture2D texture, string iconName, float maxFill = 0.92f)
        {
            if (texture == null || texture.height <= 0)
            {
                return bounds;
            }

            var reference = GetReferenceTexture() ?? texture;
            var refHeight = bounds.height * maxFill * IconDisplayScale;
            var refWidth = refHeight * (reference.width / (float)reference.height);

            var lengthRatio = LengthRelativeToAgm114.TryGetValue(iconName, out var ratio)
                ? ratio
                : texture.width / (float)reference.width;
            var width = refWidth * lengthRatio;
            var height = width / (texture.width / (float)texture.height);

            return new Rect(
                bounds.xMin,
                bounds.yMin + (bounds.height - height) * 0.5f,
                width,
                height);
        }

        private static Texture2D GetReferenceTexture()
        {
            if (referenceTexture != null)
            {
                return referenceTexture;
            }

            TryGetIcon(ReferenceIconName, out referenceTexture);
            return referenceTexture;
        }
    }
}
