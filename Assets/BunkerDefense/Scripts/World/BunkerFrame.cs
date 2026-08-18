using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    /// <summary>Full-view sandbag embrasure: painted field behind, live units in the slit, bags in front.</summary>
    public static class BunkerFrame
    {
        public static void Build(Camera camera)
        {
            var source = LoadTexture();
            if (source == null)
            {
                Debug.LogError($"F-89 Bunker Defense: missing frame at Resources/{BattlefieldLayout.FrameResourcePath}.");
                return;
            }

            FitSpriteToCamera(CreateSpriteObject("BunkerField", source, "Background", 0), camera);
            FitSpriteToCamera(CreateSpriteObject("BunkerSandbags", PunchOpening(source), "Projectiles & FX", 100), camera);
        }

        private static Texture2D LoadTexture()
        {
            var texture = Resources.Load<Texture2D>(BattlefieldLayout.FrameResourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Bilinear;
            }

            return texture;
        }

        private static GameObject CreateSpriteObject(string name, Texture2D texture, string sortingLayer, int order)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;

            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return go;
        }

        private static void FitSpriteToCamera(GameObject go, Camera camera)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            var bounds = renderer.sprite.bounds.size;
            var height = camera.orthographicSize * 2f;
            var width = height * camera.aspect;
            go.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 1f);
            if (bounds.x > 0.001f && bounds.y > 0.001f)
            {
                go.transform.localScale = new Vector3(width / bounds.x, height / bounds.y, 1f);
            }
        }

        private static Texture2D PunchOpening(Texture2D source)
        {
            source = MakeReadable(source);
            var width = source.width;
            var height = source.height;
            var overlay = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = source.GetPixels32();
            var opening = BattlefieldLayout.OpeningViewport;
            const float feather = 0.018f;

            for (var y = 0; y < height; y++)
            {
                var vy = y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var vx = x / (float)(width - 1);
                    var insideX = Mathf.InverseLerp(opening.xMin, opening.xMax, vx);
                    var insideY = Mathf.InverseLerp(opening.yMin, opening.yMax, vy);
                    var edge = Mathf.Min(
                        Mathf.Min(insideX, 1f - insideX),
                        Mathf.Min(insideY, 1f - insideY));
                    var hole = Mathf.SmoothStep(0f, 1f, edge / feather);
                    var i = y * width + x;
                    var pixel = pixels[i];
                    pixel.a = (byte)(pixel.a * (1f - hole));
                    pixels[i] = pixel;
                }
            }

            overlay.SetPixels32(pixels);
            overlay.Apply(false, false);
            return overlay;
        }

        private static Texture2D MakeReadable(Texture2D source)
        {
            try
            {
                _ = source.GetPixels32();
                return source;
            }
            catch (UnityException)
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
        }
    }
}
