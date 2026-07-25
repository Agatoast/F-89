using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class AircraftLoadoutLayout
    {
        public const float HardpointWidthPx = 12f;
        public const float HardpointHeightPx = 102f;
        public const float HardpointMaskVerticalPaddingPx = 2f;
        public const float MinHardpointHitWidthPx = 36f;
        public const float MinHardpointHitHeightPx = 120f;
        public const float HardpointHitSlopPx = 32f;
        public const float GunBoxTopPx = 40f;
        public const float GunBoxLeftPx = 436f;
        public const float GunBoxWidthPx = 174f;
        public const float GunBoxHeightPx = 42f;
        public const float GunBoxBottomPx = GunBoxTopPx + GunBoxHeightPx - 1f;
        public const float GunCounterLeftPx = GunBoxLeftPx + 20f;
        public const float GunCounterTopPx = GunBoxTopPx + 13f;
        public const float GunCounterWidthPx = 120f;
        public const float GunCounterHeightPx = 28f;
        public const float Hardpoint19HitTopBelowGunBoxPx = 25f;
        public const float Hardpoint19HitMinTopPx = GunBoxBottomPx + Hardpoint19HitTopBelowGunBoxPx;

        public struct GunArrowHitLayout
        {
            public float XPx;
            public float YPx;
            public float WidthPx;
            public float HeightPx;
            public int RoundDelta;
        }

        public const float GunArrowHitPaddingPx = 3f;

        // Upper triangles increase rounds; lower triangles decrease rounds.
        public static readonly GunArrowHitLayout[] GunArrowHits =
        {
            new GunArrowHitLayout { XPx = 478f, YPx = 43f, WidthPx = 34f, HeightPx = 11f, RoundDelta = 1 },
            new GunArrowHitLayout { XPx = 478f, YPx = 60f, WidthPx = 34f, HeightPx = 16f, RoundDelta = -1 },
            new GunArrowHitLayout { XPx = 540f, YPx = 43f, WidthPx = 30f, HeightPx = 11f, RoundDelta = 1 },
            new GunArrowHitLayout { XPx = 540f, YPx = 60f, WidthPx = 30f, HeightPx = 16f, RoundDelta = -1 }
        };

        public struct HardpointPairLayout
        {
            public int LeftNumber;
            public int RightNumber;
            public Vector2 LeftCenterPx;
            public Vector2 RightCenterPx;
        }

        public struct WeaponTraySlot
        {
            public string Label;
            public AircraftLoadoutWeapon Weapon;
            public bool ShowGunRounds;
            public float PickXPx;
            public float PickYPx;
            public float PickWidthPx;
            public float PickHeightPx;
            public float CounterXPx;
            public float CounterYPx;
            public float CounterWidthPx;
            public float CounterHeightPx;
        }

        // Pick areas traced from user-marked boxes on plane_loadout weapon tray.
        public static readonly WeaponTraySlot[] WeaponTraySlots =
        {
            new WeaponTraySlot
            {
                Label = "AGM-88J",
                Weapon = AircraftLoadoutWeapon.Agm88j,
                PickXPx = 0f,
                PickYPx = 472f,
                PickWidthPx = 68f,
                PickHeightPx = 136f,
                CounterXPx = 0f,
                CounterYPx = 628f,
                CounterWidthPx = 68f,
                CounterHeightPx = 54f
            },
            new WeaponTraySlot
            {
                Label = "GBU-12",
                Weapon = AircraftLoadoutWeapon.Gbu12,
                PickXPx = 81f,
                PickYPx = 472f,
                PickWidthPx = 68f,
                PickHeightPx = 135f,
                CounterXPx = 81f,
                CounterYPx = 629f,
                CounterWidthPx = 68f,
                CounterHeightPx = 54f
            },
            new WeaponTraySlot
            {
                Label = "AGM-114",
                Weapon = AircraftLoadoutWeapon.Agm114,
                PickXPx = 166f,
                PickYPx = 471f,
                PickWidthPx = 63f,
                PickHeightPx = 137f,
                CounterXPx = 163f,
                CounterYPx = 629f,
                CounterWidthPx = 68f,
                CounterHeightPx = 54f
            },
            new WeaponTraySlot
            {
                Label = "AIM-9Z",
                Weapon = AircraftLoadoutWeapon.Aim9z,
                PickXPx = 250f,
                PickYPx = 472f,
                PickWidthPx = 58f,
                PickHeightPx = 134f,
                CounterXPx = 245f,
                CounterYPx = 629f,
                CounterWidthPx = 68f,
                CounterHeightPx = 54f
            },
            new WeaponTraySlot
            {
                Label = "GAU-27A",
                ShowGunRounds = true,
                PickXPx = 320f,
                PickYPx = 472f,
                PickWidthPx = 60f,
                PickHeightPx = 134f,
                CounterXPx = 321f,
                CounterYPx = 627f,
                CounterWidthPx = 68f,
                CounterHeightPx = 54f
            }
        };

        // Coordinates measured from numbered labels on plane_loadout mockup.
        public static readonly HardpointPairLayout[] LinkedHardpointPairs =
        {
            new HardpointPairLayout { LeftNumber = 1, RightNumber = 2, LeftCenterPx = new Vector2(109f, 281.5f), RightCenterPx = new Vector2(911.5f, 294.5f) },
            new HardpointPairLayout { LeftNumber = 3, RightNumber = 4, LeftCenterPx = new Vector2(160f, 280.5f), RightCenterPx = new Vector2(863f, 292f) },
            new HardpointPairLayout { LeftNumber = 5, RightNumber = 6, LeftCenterPx = new Vector2(215f, 263f), RightCenterPx = new Vector2(817f, 267f) },
            new HardpointPairLayout { LeftNumber = 7, RightNumber = 8, LeftCenterPx = new Vector2(266f, 242f), RightCenterPx = new Vector2(769f, 247.5f) },
            new HardpointPairLayout { LeftNumber = 9, RightNumber = 10, LeftCenterPx = new Vector2(312f, 227f), RightCenterPx = new Vector2(729f, 230.5f) },
            new HardpointPairLayout { LeftNumber = 11, RightNumber = 12, LeftCenterPx = new Vector2(354.5f, 207.5f), RightCenterPx = new Vector2(686.5f, 211f) },
            new HardpointPairLayout { LeftNumber = 13, RightNumber = 14, LeftCenterPx = new Vector2(397.5f, 207f), RightCenterPx = new Vector2(642f, 209.5f) },
            new HardpointPairLayout { LeftNumber = 15, RightNumber = 16, LeftCenterPx = new Vector2(440.5f, 125.5f), RightCenterPx = new Vector2(604f, 128.5f) },
            new HardpointPairLayout { LeftNumber = 17, RightNumber = 18, LeftCenterPx = new Vector2(464f, 343.5f), RightCenterPx = new Vector2(567f, 344.5f) },
            new HardpointPairLayout { LeftNumber = 19, RightNumber = 20, LeftCenterPx = new Vector2(519.5f, 190.5f), RightCenterPx = new Vector2(516.5f, 453.5f) }
        };

        public static readonly Vector2 LeftWingTipCenterPx = new Vector2(41f, 262.5f);
        public static readonly Vector2 RightWingTipCenterPx = new Vector2(984.5f, 285.5f);

        public static int LinkedHardpointPairCount => LinkedHardpointPairs.Length;

        public static Rect MockupPixelTopLeftRect(float xPx, float yPx, float widthPx, float heightPx, Rect mockupRect, int textureWidth, int textureHeight)
        {
            var scaleX = mockupRect.width / textureWidth;
            var scaleY = mockupRect.height / textureHeight;
            return new Rect(
                mockupRect.x + xPx * scaleX,
                mockupRect.y + yPx * scaleY,
                widthPx * scaleX,
                heightPx * scaleY);
        }

        public static Rect MockupPixelCenterRect(float centerXPx, float centerYPx, float widthPx, float heightPx, Rect mockupRect, int textureWidth, int textureHeight)
        {
            return MockupPixelTopLeftRect(
                centerXPx - widthPx * 0.5f,
                centerYPx - heightPx * 0.5f,
                widthPx,
                heightPx,
                mockupRect,
                textureWidth,
                textureHeight);
        }

        public static Rect WeaponPickRect(WeaponTraySlot slot, Rect mockupRect, int textureWidth, int textureHeight)
        {
            return MockupPixelTopLeftRect(slot.PickXPx, slot.PickYPx, slot.PickWidthPx, slot.PickHeightPx, mockupRect, textureWidth, textureHeight);
        }

        public static Rect WeaponCounterRect(WeaponTraySlot slot, Rect mockupRect, int textureWidth, int textureHeight)
        {
            return MockupPixelTopLeftRect(
                slot.CounterXPx,
                slot.CounterYPx,
                slot.CounterWidthPx,
                slot.CounterHeightPx,
                mockupRect,
                textureWidth,
                textureHeight);
        }

        public static Rect GunCounterRect(Rect mockupRect, int textureWidth, int textureHeight)
        {
            return MockupPixelTopLeftRect(
                GunCounterLeftPx,
                GunCounterTopPx,
                GunCounterWidthPx,
                GunCounterHeightPx,
                mockupRect,
                textureWidth,
                textureHeight);
        }

        public static Rect GunArrowHitRect(GunArrowHitLayout hit, Rect mockupRect, int textureWidth, int textureHeight)
        {
            var padding = GunArrowHitPaddingPx;
            var topPadding = hit.RoundDelta > 0 ? 2f : 0f;
            var bottomPadding = hit.RoundDelta < 0 ? 2f : 0f;
            return MockupPixelTopLeftRect(
                hit.XPx - padding,
                hit.YPx - topPadding,
                hit.WidthPx + padding * 2f,
                hit.HeightPx + topPadding + bottomPadding,
                mockupRect,
                textureWidth,
                textureHeight);
        }

        public static Rect HardpointRect(Vector2 centerPx, Rect mockupRect, int textureWidth, int textureHeight)
        {
            return MockupPixelCenterRect(centerPx.x, centerPx.y, HardpointWidthPx, HardpointHeightPx, mockupRect, textureWidth, textureHeight);
        }

        public static Rect HardpointHitRect(Vector2 centerPx, Rect mockupRect, int textureWidth, int textureHeight)
        {
            return HardpointHitRect(centerPx, mockupRect, textureWidth, textureHeight, extraBottomPx: 0f, hardpointNumber: 0);
        }

        public static Rect HardpointHitRect(
            Vector2 centerPx,
            Rect mockupRect,
            int textureWidth,
            int textureHeight,
            float extraBottomPx,
            int hardpointNumber = 0)
        {
            var rect = HardpointRect(centerPx, mockupRect, textureWidth, textureHeight);
            var minWidth = MinHardpointHitWidthPx / textureWidth * mockupRect.width;
            if (rect.width < minWidth)
            {
                var expand = (minWidth - rect.width) * 0.5f;
                rect.x -= expand;
                rect.width = minWidth;
            }

            var minHeight = MinHardpointHitHeightPx / textureHeight * mockupRect.height;
            if (rect.height < minHeight)
            {
                var expand = (minHeight - rect.height) * 0.5f;
                rect.y -= expand;
                rect.height = minHeight;
            }

            var extraTopPx = GetHardpointHitExtraTopPx(hardpointNumber);
            if (extraTopPx > 0f)
            {
                var extraTop = extraTopPx / textureHeight * mockupRect.height;
                rect.y -= extraTop;
                rect.height += extraTop;
            }

            if (extraBottomPx > 0f)
            {
                rect.height += extraBottomPx / textureHeight * mockupRect.height;
            }

            var minTopPx = GetHardpointHitMinTopPx(hardpointNumber);
            if (minTopPx > 0f)
            {
                var minTopScreen = mockupRect.y + minTopPx / textureHeight * mockupRect.height;
                if (rect.y < minTopScreen)
                {
                    var trim = minTopScreen - rect.y;
                    rect.y = minTopScreen;
                    rect.height = Mathf.Max(0f, rect.height - trim);
                }
            }

            return rect;
        }

        public static float GetHardpointHitMinTopPx(int hardpointNumber)
        {
            return hardpointNumber switch
            {
                19 => Hardpoint19HitMinTopPx,
                _ => 0f
            };
        }

        public static float GetHardpointHitMinTopScreenY(float minTopPx, Rect mockupRect, int textureHeight)
        {
            return mockupRect.y + minTopPx / textureHeight * mockupRect.height;
        }

        public static float GetHardpointHitExtraTopPx(int hardpointNumber)
        {
            return hardpointNumber switch
            {
                17 or 18 => 12f,
                19 => 0f,
                20 => 8f,
                _ => 0f
            };
        }

        public static float GetHardpointHitExtraBottomPx(int hardpointNumber)
        {
            return hardpointNumber switch
            {
                17 or 18 => 12f,
                19 => 8f,
                20 => 72f,
                _ => 0f
            };
        }

        public static bool IsFuselageHardpoint(int hardpointNumber) => hardpointNumber is >= 17 and <= 20;

        public static bool MatchesFuselageHardpointBand(int hardpointNumber, float mockupYPx)
        {
            return hardpointNumber switch
            {
                19 => mockupYPx is >= Hardpoint19HitMinTopPx and < 285f,
                17 or 18 => mockupYPx is >= 265f and <= 395f,
                20 => mockupYPx > 375f,
                _ => true
            };
        }

        public static float GetHardpointHitSlopPx(int hardpointNumber)
        {
            return hardpointNumber is >= 17 and <= 20 ? HardpointHitSlopPx : HardpointHitSlopPx * 0.5f;
        }

        public static Rect HardpointRectAtScreenPoint(Vector2 screenPoint, int textureWidth, int textureHeight, Rect mockupRect)
        {
            var width = HardpointWidthPx / textureWidth * mockupRect.width;
            var height = HardpointHeightPx / textureHeight * mockupRect.height;
            return new Rect(screenPoint.x - width * 0.5f, screenPoint.y - height * 0.5f, width, height);
        }

        public static HardpointPairLayout GetLinkedHardpointPair(int pairIndex) => LinkedHardpointPairs[pairIndex];
    }
}
