using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public static class HangarVisual
    {
        private const string SpriteResourcePath = "Structures/Hangar";

        public static GameObject Create(Vector2 worldCenter, Vector2 worldSize)
        {
            var go = new GameObject("Hangar");
            go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite();
            renderer.sortingLayerName = "Units";
            renderer.sortingOrder = 8;
            renderer.color = renderer.sprite == null
                ? new Color(0.55f, 0.58f, 0.52f, 1f)
                : Color.white;

            if (renderer.sprite == null)
            {
                renderer.sprite = SpriteFactory.Solid(new Color(0.42f, 0.46f, 0.4f, 1f), 64, 40);
            }

            var bounds = renderer.sprite.bounds.size;
            if (bounds.x > 0.001f && bounds.y > 0.001f)
            {
                go.transform.localScale = new Vector3(worldSize.x / bounds.x, worldSize.y / bounds.y, 1f);
            }

            return go;
        }

        private static Sprite LoadSprite()
        {
            var sprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(SpriteResourcePath);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
