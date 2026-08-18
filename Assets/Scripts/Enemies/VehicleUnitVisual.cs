using F89.Core;
using F89.Flight;
using F89.LandCombat;
using UnityEngine;

namespace F89.Enemies
{
    public static class VehicleUnitVisual
    {
        public static readonly Color HostileUnitColor = new Color(0.92f, 0.15f, 0.1f);
        public static readonly Color FriendlyUnitColor = new Color(0.25f, 0.78f, 0.35f);

        public static void Attach(Transform parent, VehicleUnitDefinition definition, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var footprint = OutpostGroundRules.FootprintWorld(ticSize);
            var isFlier = definition != null && definition.isFlier;
            var isTroop = definition != null && definition.isTroop;

            if (isTroop && TryGetTroopSprite(definition, out var troopSprite))
            {
                var troopSize = footprint * 0.5f;
                AttachSideSpriteVisual(parent, troopSprite, troopSize);
                return;
            }

            if (isFlier
                && definition != null
                && definition.IsHostile
                && string.Equals(
                    definition.abbreviation,
                    UrTdpSpriteSheet.Abbreviation,
                    System.StringComparison.OrdinalIgnoreCase)
                && UrTdpSpriteSheet.TryGetFrames(out var tdpFrames))
            {
                AttachAnimatedTopDownVisual(parent, tdpFrames, footprint, UrTdpSpriteSheet.AnimationFps);
                return;
            }

            if (!isTroop
                && definition != null
                && definition.IsHostile
                && !isFlier
                && string.Equals(
                    definition.abbreviation,
                    UrArwSpriteSheet.Abbreviation,
                    System.StringComparison.OrdinalIgnoreCase)
                && UrArwSpriteSheet.TryGetRotationFrames(out var arwFrames))
            {
                AttachTopDownRotationVisual(parent, arwFrames, footprint);
                return;
            }

            if (!isTroop
                && definition != null
                && definition.IsHostile
                && !isFlier
                && UrTopDownVehicleSpriteSheet.TryGetRotationFrames(definition.abbreviation, out var topDownFrames))
            {
                AttachTopDownRotationVisual(parent, topDownFrames, footprint);
                return;
            }

            if (!isTroop
                && definition != null
                && !definition.IsHostile
                && !isFlier
                && UsTopDownVehicleSpriteSheet.TryGetRotationFrames(definition.abbreviation, out var usTopDownFrames))
            {
                AttachTopDownRotationVisual(parent, usTopDownFrames, footprint);
                return;
            }

            if (!isTroop && TryGetVehicleSprite(definition, out var sprite))
            {
                AttachSideSpriteVisual(parent, sprite, footprint);
                return;
            }

            AttachPrimitiveVisual(parent, definition, footprint, isFlier, isTroop);
        }

        private static bool TryGetTroopSprite(VehicleUnitDefinition definition, out Sprite sprite)
        {
            sprite = null;
            if (definition == null || !definition.isTroop)
            {
                return false;
            }

            if (definition.IsHostile)
            {
                return TryGetIdleClipSprite(LandEnemySpriteSheet.GetClip(LandEnemySpriteSheet.Clip.Idle), out sprite);
            }

            return TryGetIdleClipSprite(LandPlayerSpriteSheet.GetClip(LandPlayerSpriteSheet.Clip.Idle), out sprite);
        }

        private static bool TryGetIdleClipSprite(Sprite[] clip, out Sprite sprite)
        {
            sprite = null;
            if (clip == null || clip.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < clip.Length; i++)
            {
                if (clip[i] != null)
                {
                    sprite = clip[i];
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetVehicleSprite(VehicleUnitDefinition definition, out Sprite sprite)
        {
            sprite = null;
            if (definition == null || string.IsNullOrWhiteSpace(definition.abbreviation))
            {
                return false;
            }

            if (definition.IsHostile)
            {
                if (UrVehicleSpriteSheet.TryGetSideSprite(definition.abbreviation, out sprite))
                {
                    return true;
                }

                return UrVehicleSpriteSheet.TryGetSideSprite("FW", out sprite);
            }

            if (UsVehicleSpriteSheet.TryGetSideSprite(definition.abbreviation, out sprite))
            {
                return true;
            }

            // US AH-64 and other fliers without dedicated art reuse the UR attack-helicopter side view.
            return UrVehicleSpriteSheet.TryGetSideSprite("AH", out sprite);
        }

        private static void AttachSideSpriteVisual(Transform parent, Sprite sprite, float footprint)
        {
            var visualObject = new GameObject("VehicleVisual");
            visualObject.transform.SetParent(parent, false);
            // Side art faces left; lay flat and yaw so nose aligns with parent +Z.
            visualObject.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            visualObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var renderer = CreateSpriteRenderer(visualObject, sprite);
            ScaleSpriteToFootprint(visualObject.transform, sprite, footprint);
        }

        private static void AttachTopDownRotationVisual(Transform parent, Sprite[] frames, float footprint)
        {
            var first = frames[0];
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    first = frames[i];
                    break;
                }
            }

            if (first == null)
            {
                return;
            }

            var visualObject = new GameObject("VehicleVisual");
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
            visualObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var renderer = CreateSpriteRenderer(visualObject, first);
            ScaleSpriteToFootprint(visualObject.transform, first, footprint);

            var rotationSprite = visualObject.AddComponent<VehicleTopDownRotationSprite>();
            rotationSprite.Configure(parent, renderer, frames);
        }

        private static void AttachAnimatedTopDownVisual(
            Transform parent,
            Sprite[] frames,
            float footprint,
            float animationFps)
        {
            var first = frames[0];
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    first = frames[i];
                    break;
                }
            }

            if (first == null)
            {
                return;
            }

            var visualObject = new GameObject("VehicleVisual");
            visualObject.transform.SetParent(parent, false);
            // Top-down saucer art — lay flat on the map plane.
            visualObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visualObject.transform.localPosition = Vector3.zero;

            var renderer = CreateSpriteRenderer(visualObject, first);
            ScaleSpriteToFootprint(visualObject.transform, first, footprint);

            var animator = visualObject.AddComponent<VehicleSpriteAnimator>();
            animator.Configure(renderer, frames, animationFps);
        }

        private static SpriteRenderer CreateSpriteRenderer(GameObject visualObject, Sprite sprite)
        {
            var renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 4;
            var material = F89RenderMaterials.CreateSpriteMaterial();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            return renderer;
        }

        private static void ScaleSpriteToFootprint(Transform visual, Sprite sprite, float footprint)
        {
            if (sprite == null)
            {
                return;
            }

            var bounds = sprite.bounds.size;
            var reference = Mathf.Max(bounds.x, bounds.y);
            if (reference <= 0.0001f)
            {
                return;
            }

            var scale = footprint / reference;
            visual.localScale = new Vector3(scale, scale, scale);
        }

        private static void AttachPrimitiveVisual(
            Transform parent,
            VehicleUnitDefinition definition,
            float footprint,
            bool isFlier,
            bool isTroop)
        {
            GameObject visualObject;
            if (isTroop)
            {
                var troopSize = footprint * 0.5f;
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualObject.name = "TroopVisual";
                visualObject.transform.SetParent(parent, false);
                visualObject.transform.localPosition = new Vector3(0f, troopSize * 0.5f, 0f);
                visualObject.transform.localScale = Vector3.one * troopSize;
            }
            else
            {
                var height = isFlier ? footprint * 0.45f : footprint * 0.35f;
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = "VehicleVisual";
                visualObject.transform.SetParent(parent, false);
                visualObject.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                visualObject.transform.localScale = new Vector3(footprint, height, footprint);
            }

            var collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = F89RenderMaterials.CreateUnlit(GetUnitColor(definition));
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        public static Color GetUnitColor(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return HostileUnitColor;
            }

            return definition.IsHostile ? HostileUnitColor : FriendlyUnitColor;
        }
    }
}
