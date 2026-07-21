using F89.Controls;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class WeaponReticleHud : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private PlayerAircraftInput inputSource;

        private Texture2D gauReticleTexture;
        private Texture2D gauGunSquareTexture;
        private Texture2D siawCircleTexture;
        private Texture2D hellfireSquareTexture;
        private Texture2D gbuTriangleTexture;
        private Texture2D aimCircleTexture;

        private const float Aim9zCircleDiameter = 101f;
        private const float GauGunSquareSize = 51f;
        private const float SiawCircleDiameter = 51f;
        private const float HellfireSquareSize = 101f;
        private const float GbuTriangleSideLength = 100f;
        private const int GauGunSquareSegmentPixels = 5;
        private const float WeaponLabelSpacing = 5f;
        private const int WeaponLabelFontSize = 11;

        private GUIStyle weaponLabelStyle;
        private bool gameplayCursorHidden;

        public void Configure(PlayerWeaponController weapons, PlayerAircraftInput input)
        {
            weaponController = weapons;
            inputSource = input;
            EnsureTextures();
        }

        private void Update()
        {
            var shouldHideCursor = !GamePauseController.IsPaused && !AntarcticaMapOverlay.IsOpen;
            if (shouldHideCursor)
            {
                if (!gameplayCursorHidden)
                {
                    Cursor.visible = false;
                    gameplayCursorHidden = true;
                }

                return;
            }

            if (gameplayCursorHidden)
            {
                Cursor.visible = true;
                gameplayCursorHidden = false;
            }
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || weaponController == null
                || inputSource == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureTextures();
            var hudColor = FlightHudColorPalette.Current;

            if (weaponController.ActiveWeapon == SelectedWeapon.None)
            {
                DrawGauReticleAtScreenPosition(inputSource.Current.aimScreenPosition, hudColor);
                return;
            }

            if (weaponController.ActiveWeapon == SelectedWeapon.Gau27a)
            {
                DrawGauReticle(hudColor);
                return;
            }

            var lockController = weaponController.LockController;
            if (lockController == null || !lockController.ReticleVisible)
            {
                return;
            }

            var reticleColor = hudColor;
            if (lockController.TargetOutOfRange)
            {
                reticleColor = WithAlpha(hudColor, 0.45f);
            }

            if (weaponController.ActiveWeapon == SelectedWeapon.Aim9z)
            {
                DrawAim9zReticle(inputSource.Current.aimScreenPosition, reticleColor);
                return;
            }

            if (weaponController.ActiveWeapon == SelectedWeapon.Agm88jSiaw)
            {
                DrawSiawReticle(inputSource.Current.aimScreenPosition, reticleColor);
                return;
            }

            if (weaponController.ActiveWeapon == SelectedWeapon.Agm114Hellfire)
            {
                DrawHellfireReticle(inputSource.Current.aimScreenPosition, reticleColor);
                return;
            }

            if (weaponController.ActiveWeapon == SelectedWeapon.Gbu12Paveway)
            {
                DrawGbuReticle(inputSource.Current.aimScreenPosition, reticleColor);
            }
        }

        private void DrawHellfireReticle(Vector2 screenBottomLeft, Color color)
        {
            DrawGauReticleAtScreenPosition(screenBottomLeft, color);
            DrawOverlayCenteredAtScreenPosition(screenBottomLeft, color, hellfireSquareTexture);
            DrawWeaponLabelBelowReticle(
                screenBottomLeft,
                HellfireSquareSize * 0.5f,
                "AGM-114 HELLFIRE",
                color);
        }

        private void DrawGbuReticle(Vector2 screenBottomLeft, Color color)
        {
            DrawGauReticleAtScreenPosition(screenBottomLeft, color);
            DrawOverlayCenteredAtScreenPosition(screenBottomLeft, color, gbuTriangleTexture);
            DrawWeaponLabelBelowReticle(
                screenBottomLeft,
                GbuTriangleBottomExtent,
                "GBU-12 PAVEWAY",
                color);
        }

        private void DrawAim9zReticle(Vector2 screenBottomLeft, Color color)
        {
            var circleHalf = Aim9zCircleDiameter * 0.5f;
            var guiY = Screen.height - screenBottomLeft.y;
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(screenBottomLeft.x - circleHalf, guiY - circleHalf, Aim9zCircleDiameter, Aim9zCircleDiameter),
                aimCircleTexture);
            GUI.color = previous;
            DrawGauReticleAtScreenPosition(screenBottomLeft, color);
            DrawWeaponLabelBelowReticle(
                screenBottomLeft,
                circleHalf,
                "AIM-9Z",
                color);
        }

        private void DrawSiawReticle(Vector2 screenBottomLeft, Color color)
        {
            DrawGauReticleAtScreenPosition(screenBottomLeft, color);
            DrawDottedCircleAtScreenPosition(screenBottomLeft, color, siawCircleTexture, SiawCircleDiameter);
            DrawWeaponLabelBelowReticle(
                screenBottomLeft,
                SiawCircleDiameter * 0.5f,
                "AGM-88J SiAW",
                color);
        }

        private static float GbuTriangleBottomExtent =>
            GbuTriangleSideLength * Mathf.Sqrt(3f) / 3f;

        private void DrawDottedCircleAtScreenPosition(
            Vector2 screenBottomLeft,
            Color hudColor,
            Texture2D texture,
            float diameter)
        {
            if (texture == null)
            {
                return;
            }

            var half = diameter * 0.5f;
            var guiX = screenBottomLeft.x - half;
            var guiY = Screen.height - screenBottomLeft.y - half;

            var previous = GUI.color;
            GUI.color = hudColor;
            GUI.DrawTexture(new Rect(guiX, guiY, diameter, diameter), texture);
            GUI.color = previous;
        }

        private void DrawOverlayCenteredAtScreenPosition(
            Vector2 screenBottomLeft,
            Color hudColor,
            Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            var guiX = screenBottomLeft.x - texture.width * 0.5f;
            var guiY = Screen.height - screenBottomLeft.y - texture.height * 0.5f;

            var previous = GUI.color;
            GUI.color = hudColor;
            GUI.DrawTexture(new Rect(guiX, guiY, texture.width, texture.height), texture);
            GUI.color = previous;
        }

        private void DrawGauReticle(Color hudColor)
        {
            var gun = weaponController.Gau27aGun;
            if (gun == null)
            {
                return;
            }

            var screen = gun.CrosshairScreenPoint;
            if (screen.z < 0f)
            {
                return;
            }

            DrawGauReticleAtScreenPosition(new Vector2(screen.x, screen.y), hudColor);
            DrawGauGunSquareAtScreenPosition(new Vector2(screen.x, screen.y), hudColor);
        }

        private void DrawGauGunSquareAtScreenPosition(Vector2 screenBottomLeft, Color hudColor)
        {
            if (gauGunSquareTexture == null)
            {
                return;
            }

            var half = GauGunSquareSize * 0.5f;
            var guiX = screenBottomLeft.x - half;
            var guiY = Screen.height - screenBottomLeft.y - half;

            var previous = GUI.color;
            GUI.color = hudColor;
            GUI.DrawTexture(new Rect(guiX, guiY, GauGunSquareSize, GauGunSquareSize), gauGunSquareTexture);
            GUI.color = previous;
        }

        private void DrawGauReticleAtScreenPosition(Vector2 screenBottomLeft, Color hudColor)
        {
            if (gauReticleTexture == null)
            {
                return;
            }

            var size = gauReticleTexture.width;
            var hotspot = size / 2;
            var guiX = screenBottomLeft.x - hotspot;
            var guiY = Screen.height - screenBottomLeft.y - hotspot;

            var previous = GUI.color;
            GUI.color = hudColor;
            GUI.DrawTexture(new Rect(guiX, guiY, size, size), gauReticleTexture);
            GUI.color = previous;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void DrawWeaponLabelBelowReticle(
            Vector2 screenBottomLeft,
            float extentBelowCenter,
            string label,
            Color color)
        {
            EnsureLabelStyle();

            var guiCenterX = screenBottomLeft.x;
            var guiCenterY = Screen.height - screenBottomLeft.y;
            var labelY = guiCenterY + extentBelowCenter + WeaponLabelSpacing;
            var content = new GUIContent(label);
            var size = weaponLabelStyle.CalcSize(content);
            var rect = new Rect(guiCenterX - size.x * 0.5f, labelY, size.x, size.y);

            var previous = GUI.color;
            GUI.color = color;
            GUI.Label(rect, content, weaponLabelStyle);
            GUI.color = previous;
        }

        private void EnsureLabelStyle()
        {
            if (weaponLabelStyle != null)
            {
                return;
            }

            weaponLabelStyle = HudStyleFactory.CreateLabel(
                WeaponLabelFontSize,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                Color.white);
        }

        private void EnsureTextures()
        {
            if (gauReticleTexture == null)
            {
                gauReticleTexture = CreateGauCrosshairTexture();
            }

            if (gauGunSquareTexture == null)
            {
                gauGunSquareTexture = CreateDottedSquareTexture(
                    Mathf.RoundToInt(GauGunSquareSize),
                    GauGunSquareSegmentPixels);
            }

            if (siawCircleTexture == null)
            {
                siawCircleTexture = CreateDottedCircleTexture(
                    Mathf.RoundToInt(SiawCircleDiameter),
                    GauGunSquareSegmentPixels);
            }

            if (hellfireSquareTexture == null)
            {
                hellfireSquareTexture = CreateDottedSquareTexture(
                    Mathf.RoundToInt(HellfireSquareSize),
                    GauGunSquareSegmentPixels);
            }

            if (gbuTriangleTexture == null)
            {
                gbuTriangleTexture = CreateEquilateralTriangleOutlineTexture(
                    Mathf.RoundToInt(GbuTriangleSideLength));
            }

            if (aimCircleTexture == null)
            {
                aimCircleTexture = CreateAimCircleTexture(Mathf.RoundToInt(Aim9zCircleDiameter));
            }
        }

        private static Texture2D CreateEquilateralTriangleOutlineTexture(int sideLength)
        {
            var height = sideLength * Mathf.Sqrt(3f) * 0.5f;
            const int padding = 3;
            var width = sideLength + padding * 2;
            var heightPixels = Mathf.CeilToInt(height * 4f / 3f) + padding * 2;

            var texture = new Texture2D(width, heightPixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[width * heightPixels];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var centerX = (width - 1) * 0.5f;
            var centerY = (heightPixels - 1) * 0.5f;
            var thirdHeight = height / 3f;
            var bottomTip = new Vector2(centerX, centerY + thirdHeight * 2f);
            var topLeft = new Vector2(centerX - sideLength * 0.5f, centerY - thirdHeight);
            var topRight = new Vector2(centerX + sideLength * 0.5f, centerY - thirdHeight);

            for (var y = 0; y < heightPixels; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = new Vector2(x, y);
                    var edgeDistance = Mathf.Min(
                        DistanceToSegment(point, bottomTip, topLeft),
                        DistanceToSegment(point, topLeft, topRight),
                        DistanceToSegment(point, topRight, bottomTip));
                    if (edgeDistance <= 0.75f)
                    {
                        pixels[y * width + x] = Color.white;
                    }
                }
            }

            PlotVertex(pixels, width, heightPixels, bottomTip);
            PlotVertex(pixels, width, heightPixels, topLeft);
            PlotVertex(pixels, width, heightPixels, topRight);

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void PlotVertex(Color[] pixels, int width, int height, Vector2 vertex)
        {
            var x = Mathf.RoundToInt(vertex.x);
            var y = Mathf.RoundToInt(vertex.y);
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            pixels[y * width + x] = Color.white;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return Vector2.Distance(point, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
            var closest = a + ab * t;
            return Vector2.Distance(point, closest);
        }

        private static Texture2D CreateAimCircleTexture(int diameter)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[diameter * diameter];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(distance - radius) <= 0.75f)
                    {
                        pixels[y * diameter + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateDottedCircleTexture(int diameter, int segmentPixels)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[diameter * diameter];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;
            var dashPeriod = segmentPixels * 2;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(distance - radius) > 0.6f)
                    {
                        continue;
                    }

                    var angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    var arcLength = angle * radius;
                    if (!IsDottedSegment(Mathf.FloorToInt(arcLength), segmentPixels, dashPeriod))
                    {
                        continue;
                    }

                    pixels[y * diameter + x] = Color.white;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateDottedSquareTexture(int size, int segmentPixels)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var dashPeriod = segmentPixels * 2;
            var last = size - 1;

            for (var x = 0; x < size; x++)
            {
                if (!IsDottedSegment(x, segmentPixels, dashPeriod))
                {
                    continue;
                }

                pixels[x] = Color.white;
                pixels[last * size + x] = Color.white;
            }

            for (var y = 0; y < size; y++)
            {
                if (!IsDottedSegment(y, segmentPixels, dashPeriod))
                {
                    continue;
                }

                pixels[y * size] = Color.white;
                pixels[y * size + last] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static bool IsDottedSegment(int position, int segmentPixels, int dashPeriod)
        {
            return position % dashPeriod < segmentPixels;
        }

        private static Texture2D CreateGauCrosshairTexture()
        {
            const int gapPixels = 3;
            const int linePixels = 10;
            const int centerPixels = 1;
            var radius = centerPixels + gapPixels + linePixels;
            var size = radius * 2 + centerPixels;
            var center = size / 2;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            SetPixel(pixels, size, center, center, Color.white);

            var lineStart = center + centerPixels + gapPixels;
            var lineEnd = lineStart + linePixels - 1;

            for (var x = lineStart; x <= lineEnd; x++)
            {
                SetPixel(pixels, size, x, center, Color.white);
            }

            for (var x = center - centerPixels - gapPixels - linePixels + 1; x <= center - centerPixels - gapPixels; x++)
            {
                SetPixel(pixels, size, x, center, Color.white);
            }

            for (var y = lineStart; y <= lineEnd; y++)
            {
                SetPixel(pixels, size, center, y, Color.white);
            }

            for (var y = center - centerPixels - gapPixels - linePixels + 1; y <= center - centerPixels - gapPixels; y++)
            {
                SetPixel(pixels, size, center, y, Color.white);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void SetPixel(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
            {
                return;
            }

            pixels[y * size + x] = color;
        }
    }
}
