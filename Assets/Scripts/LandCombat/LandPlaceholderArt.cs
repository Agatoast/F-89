using UnityEngine;

namespace F89.LandCombat
{
    public static class LandPlaceholderArt
    {
        private static Sprite pixelCircle;

        public static Sprite GetPixelCircle()
        {
            if (pixelCircle != null)
            {
                return pixelCircle;
            }

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = (size - 1) * 0.5f;
            var radius = size * 0.42f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            pixelCircle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return pixelCircle;
        }
    }
}
