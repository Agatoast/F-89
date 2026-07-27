using UnityEngine;

namespace F89.LandCombat
{
    public static class LandPlaceholderArt
    {
        private static Sprite pixelCircle;
        private static Sprite pixelOblongBullet;
        private static Sprite playerPixelOblongBullet;
        private static Sprite pixelSquare;

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

        /// <summary>1×1 white square sprite (tint/scale in callers).</summary>
        public static Sprite GetPixelSquare()
        {
            if (pixelSquare != null)
            {
                return pixelSquare;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            pixelSquare = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return pixelSquare;
        }

        /// <summary>5×9 white oblong (5 tall × 9 long along +X). Point-filtered for crisp pixels.</summary>
        public static Sprite GetPixelOblongBullet()
        {
            const int width = 9;
            const int height = 5;
            const float pixelsPerUnit = 48f;

            if (pixelOblongBullet != null
                && pixelOblongBullet.texture != null
                && pixelOblongBullet.texture.width == width
                && pixelOblongBullet.texture.height == height)
            {
                return pixelOblongBullet;
            }

            pixelOblongBullet = CreateOblongBulletSprite(width, height, pixelsPerUnit, outlined: false);
            return pixelOblongBullet;
        }

        /// <summary>Player tracer: yellow-tintable white core with fixed black outer pixels.</summary>
        public static Sprite GetPlayerPixelOblongBullet()
        {
            const int width = 9;
            const int height = 5;
            const float pixelsPerUnit = 48f;

            if (playerPixelOblongBullet != null
                && playerPixelOblongBullet.texture != null
                && playerPixelOblongBullet.texture.width == width
                && playerPixelOblongBullet.texture.height == height)
            {
                return playerPixelOblongBullet;
            }

            playerPixelOblongBullet = CreateOblongBulletSprite(width, height, pixelsPerUnit, outlined: true);
            return playerPixelOblongBullet;
        }

        private static Sprite CreateOblongBulletSprite(int width, int height, float pixelsPerUnit, bool outlined)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var isEdge = outlined
                                 && (x == 0 || y == 0 || x == width - 1 || y == height - 1);
                    texture.SetPixel(x, y, isEdge ? Color.black : Color.white);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }
    }
}
