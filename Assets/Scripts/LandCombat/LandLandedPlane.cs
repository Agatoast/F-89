using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Static parked F-89 at ground-battle entry. Minimap reads <see cref="WorldPosition"/>.</summary>
    public sealed class LandLandedPlane : MonoBehaviour
    {
        private const string ResourcePath = "LandCombat/Plane/plane_landed";
        private const float TargetWorldWidth = 14f;

        public static LandLandedPlane Instance { get; private set; }
        public static bool IsTakeOffPromptPending { get; private set; }

        public Vector3 WorldPosition => transform.position;

        public static void CancelTakeOffPrompt() => IsTakeOffPromptPending = false;

        public static bool IsWorldPointerOverPlane()
        {
            var plane = Instance;
            if (plane == null)
            {
                return false;
            }

            var camera = Camera.main;
            if (camera == null || !plane.TryGetComponent<SpriteRenderer>(out var renderer) || renderer.sprite == null)
            {
                return false;
            }

            var world = camera.ScreenToWorldPoint(Input.mousePosition);
            var point = new Vector3(world.x, world.y, renderer.bounds.center.z);
            return renderer.bounds.Contains(point);
        }

        public static void HandleTakeOffClick()
        {
            if (IsTakeOffPromptPending
                || GamePauseController.IsPaused
                || CharacterPageGearUi.IsDraggingGear
                || CharacterPageGearUi.IsDeleteConfirmPending
                || LandLootBagSession.IsOpen
                || LandCombatHud.IsPointerOverHud())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && IsWorldPointerOverPlane())
            {
                IsTakeOffPromptPending = true;
            }
        }

        public static LandLandedPlane SpawnAt(Vector3 worldPosition)
        {
            var existing = Object.FindAnyObjectByType<LandLandedPlane>();
            if (existing != null)
            {
                Instance = existing;
                existing.transform.position = worldPosition;
                if (existing.GetComponent<SpriteRenderer>() == null
                    || existing.GetComponent<SpriteRenderer>().sprite == null)
                {
                    existing.BuildVisual();
                }

                existing.EnsureShelterZone();
                return existing;
            }

            var go = new GameObject("LandedPlane");
            var plane = go.AddComponent<LandLandedPlane>();
            plane.BuildVisual();
            go.transform.position = worldPosition;
            plane.EnsureShelterZone();
            Instance = plane;
            return plane;
        }

        public static LandLandedPlane SpawnLeftOf(Vector3 playerWorldPosition)
        {
            return SpawnAt(playerWorldPosition
                + new Vector3(-LandGameConstants.LandedPlaneOffsetWorldUnits, 0f, 0f));
        }

        private void OnEnable() => Instance = this;

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BuildVisual()
        {
            // Prefer the imported Sprite asset (correct alpha). Fall back to Texture2D.
            var sprite = Resources.Load<Sprite>(ResourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(ResourcePath);
                if (texture == null)
                {
                    Debug.LogWarning($"F-89 Land: Missing landed plane art at Resources/{ResourcePath}.");
                    return;
                }

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit: texture.width / TargetWorldWidth,
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect);
            }

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            renderer.color = Color.white;

            // Match imported PPU to target world width when using the Sprite asset.
            if (sprite.texture != null && sprite.pixelsPerUnit > 0.01f)
            {
                var worldWidth = sprite.rect.width / sprite.pixelsPerUnit;
                if (worldWidth > 0.01f)
                {
                    var scale = TargetWorldWidth / worldWidth;
                    transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private void EnsureShelterZone()
        {
            var shelter = GetComponent<LandShelterZone>();
            if (shelter == null)
            {
                shelter = gameObject.AddComponent<LandShelterZone>();
            }

            var size = new Vector2(TargetWorldWidth * 0.85f, TargetWorldWidth * 0.45f);
            if (TryGetComponent<SpriteRenderer>(out var renderer) && renderer.sprite != null)
            {
                var b = renderer.bounds;
                size = new Vector2(Mathf.Max(2f, b.size.x * 0.9f), Mathf.Max(2f, b.size.y * 0.9f));
            }

            shelter.Configure(LandShelterZone.ShelterKind.Plane, size);
        }
    }
}
