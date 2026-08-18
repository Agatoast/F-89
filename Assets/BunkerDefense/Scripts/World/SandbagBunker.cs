using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public sealed class SandbagBunker : MonoBehaviour
    {
        private const float RecoilKick = 0.06f;
        private const float RecoilRecover = 14f;
        private const float AimSmooth = 16f;
        private const float VisibleGunFraction = 0.52f;
        private const float LowerByPixels = 200f;

        private float _aimOffsetX;
        private float _targetOffsetX;
        private float _recoilY;
        private SpriteRenderer _gunRenderer;

        public Transform Muzzle { get; private set; }
        public Transform[] Muzzles { get; private set; }
        public Transform Barrel { get; private set; }
        public Vector2 MountPosition { get; private set; }

        public static SandbagBunker Create()
        {
            var root = new GameObject("SandbagBunker");
            var bunker = root.AddComponent<SandbagBunker>();
            bunker.BuildGun();
            bunker.MountPosition = bunker.transform.position;
            bunker.ApplyPose();
            return bunker;
        }

        public void AimAt(Vector2 crosshairWorld)
        {
            var opening = BattlefieldLayout.OpeningWorld;
            if (opening.width < 0.01f)
            {
                return;
            }

            var targetX = Mathf.Clamp(crosshairWorld.x, opening.xMin, opening.xMax);
            _targetOffsetX = targetX - MountPosition.x;
        }

        public void Kick()
        {
            _recoilY = RecoilKick;
        }

        private void LateUpdate()
        {
            _recoilY = Mathf.MoveTowards(_recoilY, 0f, RecoilRecover * Time.deltaTime);
            _aimOffsetX = Mathf.Lerp(_aimOffsetX, _targetOffsetX, AimSmooth * Time.deltaTime);
            ApplyPose();
        }

        private void BuildGun()
        {
            var slideGo = new GameObject("GunSlide");
            slideGo.transform.SetParent(transform, false);
            slideGo.transform.localPosition = Vector3.zero;
            Barrel = slideGo.transform;

            var sprite = GunArt.CreateSightSprite();
            var barrelGo = new GameObject("Minigun");
            barrelGo.transform.SetParent(slideGo.transform, false);
            barrelGo.transform.localRotation = Quaternion.identity;

            _gunRenderer = barrelGo.AddComponent<SpriteRenderer>();
            _gunRenderer.sprite = sprite != null
                ? sprite
                : SpriteFactory.Solid(new Color(0.12f, 0.12f, 0.13f, 1f), 56, 8);
            _gunRenderer.sortingLayerName = "Projectiles & FX";
            _gunRenderer.sortingOrder = 110;

            if (sprite != null)
            {
                var camera = Camera.main;
                var viewHeight = camera != null ? camera.orthographicSize * 2f : 10.3f;
                var bounds = sprite.bounds.size;
                if (bounds.y > 0.001f)
                {
                    var scale = (viewHeight * 0.82f) / bounds.y;
                    barrelGo.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            LowerMountToShowTopHalf();

            Muzzles = BuildMuzzleCluster(barrelGo.transform, sprite);
            Muzzle = Muzzles[Muzzles.Length / 2];
        }

        private static Transform[] BuildMuzzleCluster(Transform barrel, Sprite sprite)
        {
            const int barrelCount = 6;
            if (sprite == null)
            {
                var fallback = new GameObject("Muzzle").transform;
                fallback.SetParent(barrel, false);
                fallback.localPosition = new Vector3(0f, 0.4f, 0f);
                return new[] { fallback };
            }

            var bounds = sprite.bounds;
            var top = bounds.max.y;
            var left = bounds.min.x * 0.82f;
            var right = bounds.max.x * 0.82f;
            var muzzles = new Transform[barrelCount];
            for (var i = 0; i < barrelCount; i++)
            {
                var t = barrelCount == 1 ? 0.5f : i / (float)(barrelCount - 1);
                var muzzleGo = new GameObject($"Muzzle{i}");
                muzzleGo.transform.SetParent(barrel, false);
                muzzleGo.transform.localPosition = new Vector3(Mathf.Lerp(left, right, t), top, 0f);
                muzzles[i] = muzzleGo.transform;
            }

            return muzzles;
        }

        private void LowerMountToShowTopHalf()
        {
            var camera = Camera.main;
            var opening = BattlefieldLayout.OpeningWorld;
            if (camera == null || _gunRenderer == null || _gunRenderer.sprite == null)
            {
                MountPosition = BattlefieldLayout.GunMountWorld;
                transform.position = MountPosition;
                return;
            }

            transform.position = new Vector3(opening.center.x, BattlefieldLayout.GunMountWorld.y, 0f);

            var screenBottom = camera.transform.position.y - camera.orthographicSize;
            var hiddenBelowScreen = _gunRenderer.bounds.size.y * (1f - VisibleGunFraction);
            var targetBottom = screenBottom - hiddenBelowScreen;
            var deltaY = targetBottom - _gunRenderer.bounds.min.y;
            transform.position += new Vector3(0f, deltaY, 0f);
            transform.position -= new Vector3(0f, PixelsToWorldY(camera, LowerByPixels), 0f);
            MountPosition = transform.position;
        }

        private static float PixelsToWorldY(Camera camera, float pixels)
        {
            if (camera == null || camera.pixelHeight <= 0)
            {
                return pixels * 0.01f;
            }

            var worldHeight = camera.orthographicSize * 2f;
            return pixels / camera.pixelHeight * worldHeight;
        }

        private void ApplyPose()
        {
            if (Barrel == null)
            {
                return;
            }

            Barrel.localRotation = Quaternion.identity;
            Barrel.localPosition = new Vector3(_aimOffsetX, -_recoilY, 0f);
        }
    }
}
