using F89.Flight;

using UnityEngine;



namespace F89.Core

{

    /// <summary>

    /// Flat destroyed-tank marker on the flight map — secondary landing pad for waypoint missions

    /// (same interaction model as the outpost bunker surface pad: land in its 1 MI grid cell).

    /// Sized to match <see cref="OutpostSurfaceBunkerPad"/> footprint (1 tac).

    /// </summary>

    public sealed class CampaignWaypointSecondaryPad : MonoBehaviour

    {

        private const string PadRootName = "CampaignWaypointSecondaryPads";

        private const string PadObjectPrefix = "WaypointSecondaryPad_";

        private const string WreckVisualName = "WreckVisual";

        private const string ResourcePath = "Vehicles/destroyed_tank";

        private const byte BlackKeyThreshold = 24;



        private static Sprite wreckSprite;

        private static bool loadAttempted;



        public static void EnsureAt(Vector3 groundPosition, string siteCode = null)

        {

            groundPosition = TacticalMapPadPlacement.ResolveInteriorWorld(
                groundPosition,
                siteCode);
            groundPosition.y = 0f;

            var root = GetOrCreateRoot();

            var padName = string.IsNullOrWhiteSpace(siteCode)

                ? PadObjectPrefix + "Unknown"

                : PadObjectPrefix + siteCode.Trim().ToUpperInvariant();

            var existing = root.transform.Find(padName);

            if (existing != null)

            {

                existing.position = groundPosition + Vector3.up * 0.02f;

                existing.rotation = Quaternion.Euler(0f, GetStableYaw(siteCode, groundPosition), 0f);

                EnsurePadVisuals(existing, groundPosition);

                return;

            }



            var padObject = new GameObject(padName);

            padObject.transform.SetParent(root.transform, true);

            padObject.transform.position = groundPosition + Vector3.up * 0.02f;

            padObject.transform.rotation = Quaternion.Euler(0f, GetStableYaw(siteCode, groundPosition), 0f);

            padObject.AddComponent<CampaignWaypointSecondaryPad>();

            EnsurePadVisuals(padObject.transform, groundPosition);

        }



        private static void EnsurePadVisuals(Transform padObject, Vector3 groundPosition)

        {

            if (padObject == null)

            {

                return;

            }



            EnsureBlackPad(padObject, groundPosition);

            EnsureWreckVisual(padObject);

        }



        private static void EnsureBlackPad(Transform padObject, Vector3 groundPosition)

        {

            var footprint = ResolveBunkerPadFootprintWorld();

            OutpostSurfaceBunkerPad.EnsureAt(padObject, groundPosition, footprint);

        }



        private static void EnsureWreckVisual(Transform padObject)

        {

            if (padObject == null)

            {

                return;

            }



            if (!TryGetWreckSprite(out var sprite))

            {

                Debug.LogWarning($"F-89: Missing destroyed tank sprite at Resources/{ResourcePath}.");

                return;

            }



            var existingVisual = padObject.Find(WreckVisualName);

            if (existingVisual != null)

            {

                ApplyBunkerPadScale(existingVisual, sprite);

                return;

            }



            CreateWreckVisual(padObject, sprite);

        }



        private static void CreateWreckVisual(Transform padObject, Sprite sprite)

        {

            var rendererObject = new GameObject(WreckVisualName);

            rendererObject.transform.SetParent(padObject, false);

            // Flat on the ice like the bunker surface pad (top-down wreck art).

            rendererObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);



            var renderer = rendererObject.AddComponent<SpriteRenderer>();

            renderer.sprite = sprite;

            renderer.color = Color.white;

            renderer.sortingOrder = 2;



            ApplyBunkerPadScale(rendererObject.transform, sprite);

        }



        private static void ApplyBunkerPadScale(Transform visual, Sprite sprite)

        {

            if (visual == null || sprite == null)

            {

                return;

            }



            var footprint = ResolveBunkerPadFootprintWorld();

            var maxAxis = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);

            var scale = maxAxis > 0.001f ? footprint / maxAxis : 1f;

            visual.localScale = Vector3.one * scale;

        }



        /// <summary>Same world footprint as <see cref="OutpostSurfaceBunkerPad"/>.</summary>

        private static float ResolveBunkerPadFootprintWorld()

        {

            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;

            return OutpostGroundRules.FootprintWorld(ticSize);

        }



        private static bool TryGetWreckSprite(out Sprite sprite)

        {

            if (!loadAttempted)

            {

                loadAttempted = true;

                wreckSprite = LoadKeyedSprite();

            }



            sprite = wreckSprite;

            return sprite != null;

        }



        private static Sprite LoadKeyedSprite()

        {

            var source = Resources.Load<Texture2D>(ResourcePath);

            if (source == null)

            {

                return null;

            }



            var width = source.width;

            var height = source.height;

            Color32[] pixels;

            try

            {

                pixels = source.GetPixels32();

            }

            catch (UnityException)

            {

                Debug.LogError(

                    $"F-89: Destroyed tank texture is not readable. Enable Read/Write on Resources/{ResourcePath}.");

                return null;

            }



            var minX = width;

            var minY = height;

            var maxX = -1;

            var maxY = -1;

            for (var y = 0; y < height; y++)

            {

                for (var x = 0; x < width; x++)

                {

                    var pixel = pixels[(y * width) + x];

                    if (pixel.r <= BlackKeyThreshold

                        && pixel.g <= BlackKeyThreshold

                        && pixel.b <= BlackKeyThreshold)

                    {

                        pixel.a = 0;

                        pixels[(y * width) + x] = pixel;

                        continue;

                    }



                    if (pixel.a <= 0)

                    {

                        continue;

                    }



                    if (x < minX)

                    {

                        minX = x;

                    }



                    if (y < minY)

                    {

                        minY = y;

                    }



                    if (x > maxX)

                    {

                        maxX = x;

                    }



                    if (y > maxY)

                    {

                        maxY = y;

                    }

                }

            }



            if (maxX < minX || maxY < minY)

            {

                return null;

            }



            var keyed = new Texture2D(width, height, TextureFormat.RGBA32, false)

            {

                name = "DestroyedTank_Keyed",

                filterMode = FilterMode.Bilinear,

                wrapMode = TextureWrapMode.Clamp,

                hideFlags = HideFlags.HideAndDontSave

            };

            keyed.SetPixels32(pixels);

            keyed.Apply(false, true);



            var contentWidth = maxX - minX + 1;

            var contentHeight = maxY - minY + 1;

            var footprint = ResolveBunkerPadFootprintWorld();

            var pixelsPerUnit = Mathf.Max(contentWidth, contentHeight) / footprint;

            var cropRect = new Rect(minX, minY, contentWidth, contentHeight);



            return Sprite.Create(

                keyed,

                cropRect,

                new Vector2(0.5f, 0.5f),

                pixelsPerUnit,

                0,

                SpriteMeshType.FullRect);

        }



        private static GameObject GetOrCreateRoot()

        {

            var existing = GameObject.Find(PadRootName);

            if (existing != null)

            {

                return existing;

            }



            return new GameObject(PadRootName);

        }



        private static float GetStableYaw(string siteCode, Vector3 groundPosition)

        {

            var seed = (siteCode ?? string.Empty).GetHashCode() ^ groundPosition.GetHashCode();

            return Mathf.Abs(seed) % 360;

        }

    }

}


