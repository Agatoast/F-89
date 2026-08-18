using System;
using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class LoadoutWeaponIconLibrary
    {
        public const float DefaultHardpointMountSizeMultiplier = 2f;

        public static float GetHardpointMountSizeMultiplier(AircraftLoadoutWeapon weapon)
        {
            return weapon switch
            {
                AircraftLoadoutWeapon.Agm88j => 3.6f,
                AircraftLoadoutWeapon.Aim9z => 1f,
                _ => DefaultHardpointMountSizeMultiplier
            };
        }

        /// <summary>Extra black mask below mounted icons (mockup pixels) to hide wing art bleed-through.</summary>
        public static float GetHardpointMaskBottomPaddingPx(AircraftLoadoutWeapon weapon)
        {
            return weapon == AircraftLoadoutWeapon.Aim9z ? 10f : 0f;
        }

        private static readonly Dictionary<AircraftLoadoutWeapon, string> VerticalResourcePaths =
            new Dictionary<AircraftLoadoutWeapon, string>
            {
                [AircraftLoadoutWeapon.Agm88j] = "Loadout/Weapons/agm88j_vertical",
                [AircraftLoadoutWeapon.Gbu12] = "Loadout/Weapons/gbu12_vertical",
                [AircraftLoadoutWeapon.Agm114] = "Loadout/Weapons/agm114_vertical",
                [AircraftLoadoutWeapon.Aim9z] = "Loadout/Weapons/aim9z_vertical",
            };

        private static readonly Dictionary<string, AircraftLoadoutWeapon> LabelToWeapon =
            new Dictionary<string, AircraftLoadoutWeapon>(StringComparer.OrdinalIgnoreCase)
            {
                ["AGM-88J"] = AircraftLoadoutWeapon.Agm88j,
                ["GBU-12"] = AircraftLoadoutWeapon.Gbu12,
                ["AGM-114"] = AircraftLoadoutWeapon.Agm114,
                ["AIM-9Z"] = AircraftLoadoutWeapon.Aim9z,
            };

        private const string Gau27aVerticalResourcePath = "Loadout/Weapons/gau27a_vertical";

        private static Texture2D gau27aVerticalTexture;
        private static readonly Dictionary<AircraftLoadoutWeapon, Texture2D> VerticalIconCache =
            new Dictionary<AircraftLoadoutWeapon, Texture2D>();

        public static bool TryGetWeaponForLabel(string label, out AircraftLoadoutWeapon weapon)
        {
            return LabelToWeapon.TryGetValue(label, out weapon);
        }

        public static bool TryGetVerticalIcon(AircraftLoadoutWeapon weapon, out Texture2D texture)
        {
            texture = null;
            if (weapon == AircraftLoadoutWeapon.None)
            {
                return false;
            }

            if (VerticalIconCache.TryGetValue(weapon, out texture) && texture != null)
            {
                return true;
            }

            if (!VerticalResourcePaths.TryGetValue(weapon, out var path))
            {
                return false;
            }

            texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                VerticalIconCache[weapon] = texture;
            }

            return texture != null;
        }

        public static bool TryGetGau27aVerticalIcon(out Texture2D texture)
        {
            if (gau27aVerticalTexture == null)
            {
                gau27aVerticalTexture = Resources.Load<Texture2D>(Gau27aVerticalResourcePath);
            }

            texture = gau27aVerticalTexture;
            return texture != null;
        }

        public static Rect ExpandBounds(Rect bounds, float multiplier)
        {
            if (multiplier <= 0f)
            {
                return bounds;
            }

            var width = bounds.width * multiplier;
            var height = bounds.height * multiplier;
            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }

        public static Rect FitPreservingAspect(Rect bounds, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return bounds;
            }

            var scale = Mathf.Min(bounds.width / texture.width, bounds.height / texture.height);
            var width = texture.width * scale;
            var height = texture.height * scale;

            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }

        private static Rect PadRect(Rect rect, float padding)
        {
            return new Rect(
                rect.x - padding,
                rect.y - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
        }

        public static Rect GetMountedDrawRect(Rect bounds, AircraftLoadoutWeapon weapon, float sizeMultiplier = 1f)
        {
            if (!TryGetVerticalIcon(weapon, out var texture))
            {
                return bounds;
            }

            return GetMountedDrawRect(bounds, texture, sizeMultiplier);
        }

        public static Rect GetMountedDrawRect(Rect bounds, Texture2D texture, float sizeMultiplier)
        {
            var drawRect = FitPreservingAspect(bounds, texture);
            if (!Mathf.Approximately(sizeMultiplier, 1f))
            {
                drawRect = ExpandBounds(drawRect, sizeMultiplier);
            }

            return drawRect;
        }

        public static Rect GetHardpointMaskRect(Rect hardpointRect, Rect iconRect)
        {
            var xMin = Mathf.Min(hardpointRect.xMin, iconRect.xMin);
            var yMin = Mathf.Min(hardpointRect.yMin, iconRect.yMin);
            var xMax = Mathf.Max(hardpointRect.xMax, iconRect.xMax);
            var yMax = Mathf.Max(hardpointRect.yMax, iconRect.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static Color GetHardpointMaskColor(AircraftLoadoutWeapon weapon)
        {
            return weapon == AircraftLoadoutWeapon.Agm114
                ? new Color(0.38f, 0.38f, 0.40f)
                : Color.black;
        }

        public static void DrawHardpointMount(Rect hardpointRect, AircraftLoadoutWeapon weapon, float maskVerticalPadding = 0f)
        {
            if (weapon == AircraftLoadoutWeapon.None)
            {
                return;
            }

            if (!TryGetVerticalIcon(weapon, out var texture))
            {
                return;
            }

            var sizeMultiplier = GetHardpointMountSizeMultiplier(weapon);
            var iconRect = GetMountedDrawRect(hardpointRect, texture, sizeMultiplier);
            var paddedHardpoint = PadRect(hardpointRect, 1f);
            if (maskVerticalPadding > 0f)
            {
                paddedHardpoint.y -= maskVerticalPadding;
                paddedHardpoint.height += maskVerticalPadding * 2f;
            }

            var maskRect = GetHardpointMaskRect(paddedHardpoint, iconRect);
            var bottomPaddingPx = GetHardpointMaskBottomPaddingPx(weapon);
            if (bottomPaddingPx > 0f)
            {
                var scaleY = hardpointRect.height / AircraftLoadoutLayout.HardpointHeightPx;
                maskRect.height += bottomPaddingPx * scaleY;
            }

            GUI.color = GetHardpointMaskColor(weapon);
            GUI.DrawTexture(maskRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, texture, ScaleMode.StretchToFill, true);
        }

        public static void DrawWeaponIcon(Rect bounds, AircraftLoadoutWeapon weapon, float sizeMultiplier = 1f)
        {
            if (weapon == AircraftLoadoutWeapon.None)
            {
                return;
            }

            if (!TryGetVerticalIcon(weapon, out var texture))
            {
                return;
            }

            var drawRect = GetMountedDrawRect(bounds, texture, sizeMultiplier);

            GUI.color = Color.white;
            GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }
    }
}
