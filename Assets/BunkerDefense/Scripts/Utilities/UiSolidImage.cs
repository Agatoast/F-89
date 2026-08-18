using UnityEngine;
using UnityEngine.UI;

namespace SaveAntarctica.BunkerDefense.Utilities
{
    public static class UiSolidImage
    {
        private static Sprite _whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                return _whiteSprite;
            }
        }

        public static void Apply(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
        }
    }
}
