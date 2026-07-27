using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>World-space points on character/enemy sprites (sprites are center-pivoted).</summary>
    public static class LandSpriteAnchor
    {
        /// <summary>Visual center of the sprite, or transform position if no sprite.</summary>
        public static Vector2 GetVisualCenter(Component host)
        {
            if (host == null)
            {
                return Vector2.zero;
            }

            if (!host.TryGetComponent<SpriteRenderer>(out var renderer) || renderer.sprite == null)
            {
                return host.transform.position;
            }

            return renderer.bounds.center;
        }

        /// <summary>Muzzle point: sprite center, nudged forward along aim so shots clear the body.</summary>
        public static Vector2 GetMuzzle(Component host, Vector2 aimDirection)
        {
            var origin = GetVisualCenter(host);
            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                return origin;
            }

            return origin + aimDirection.normalized * 0.2f;
        }
    }
}
