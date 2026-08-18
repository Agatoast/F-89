using System.Collections.Generic;
using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Loads MA refuel art from Resources/MidAirRefuel/.
    /// Ground scroll: antarcticaground (single seamless tile) or ground_01, ground_02, … filmstrip.
    /// </summary>
    public static class MidAirRefuelArtCatalog
    {
        public const string GroundFolder = "MidAirRefuel/Ground";
        public static readonly string[] GroundScrollPaths =
        {
            "MidAirRefuel/Ground/antarcticaground",
            "MidAirRefuel/Ground/antarctica_ground",
        };
        public const string F89SpritePath = "MidAirRefuel/f89";
        public const string ReceiverSpritePath = "MidAirRefuel/receiver_plane";
        public static readonly string[] BoomWingSpritePaths =
        {
            "MidAirRefuel/bigboom",
            "MidAirRefuel/boom2",
            "MidAirRefuel/boom_wing",
        };
        public const string BoomSpritePath = "MidAirRefuel/boom";

        private static Texture2D[] groundFrames;
        private static Texture2D f89Texture;
        private static Texture2D receiverTexture;
        private static Texture2D boomWingTexture;
        private static Texture2D boomTexture;
        private static bool loaded;

        public static Texture2D F89Texture
        {
            get
            {
                EnsureLoaded();
                return f89Texture;
            }
        }

        public static Texture2D ReceiverTexture
        {
            get
            {
                EnsureLoaded();
                return receiverTexture;
            }
        }

        public static Texture2D BoomWingTexture
        {
            get
            {
                EnsureLoaded();
                return boomWingTexture;
            }
        }

        public static Texture2D BoomTexture
        {
            get
            {
                EnsureLoaded();
                return boomTexture;
            }
        }

        public static Texture2D[] GroundFrames
        {
            get
            {
                EnsureLoaded();
                return groundFrames;
            }
        }

        public static bool HasGroundFrames
        {
            get
            {
                EnsureLoaded();
                return groundFrames != null && groundFrames.Length > 0;
            }
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            groundFrames = LoadGroundFrames();
            f89Texture = Resources.Load<Texture2D>(F89SpritePath);
            receiverTexture = Resources.Load<Texture2D>(ReceiverSpritePath);
            boomWingTexture = LoadFirstTexture(BoomWingSpritePaths);
            boomTexture = Resources.Load<Texture2D>(BoomSpritePath);
            loaded = true;

            if (groundFrames.Length == 0)
            {
                Debug.LogWarning(
                    $"F-89 MA Refuel: No ground scroll in Resources/{GroundFolder}. "
                    + "Add antarcticaground.png (seamless vertical tile) or ground_01.png, ground_02.png, …");
            }

            if (f89Texture == null)
            {
                Debug.LogWarning($"F-89 MA Refuel: Missing tanker sprite at Resources/{F89SpritePath}.");
            }

            if (receiverTexture == null)
            {
                Debug.LogWarning($"F-89 MA Refuel: Missing receiver at Resources/{ReceiverSpritePath}.");
            }
        }

        private static Texture2D[] LoadGroundFrames()
        {
            foreach (var path in GroundScrollPaths)
            {
                var scroll = Resources.Load<Texture2D>(path);
                if (scroll != null)
                {
                    return new[] { scroll };
                }
            }

            var numbered = new List<Texture2D>();
            for (var index = 1; index <= 64; index++)
            {
                var path = $"{GroundFolder}/ground_{index:D2}";
                var texture = Resources.Load<Texture2D>(path);
                if (texture == null)
                {
                    break;
                }

                numbered.Add(texture);
            }

            if (numbered.Count > 0)
            {
                return numbered.ToArray();
            }

            var loose = Resources.LoadAll<Texture2D>(GroundFolder);
            if (loose == null || loose.Length == 0)
            {
                return System.Array.Empty<Texture2D>();
            }

            System.Array.Sort(loose, (a, b) => string.CompareOrdinal(a.name, b.name));
            return loose;
        }

        private static Texture2D LoadFirstTexture(string[] paths)
        {
            foreach (var path in paths)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }
    }
}
