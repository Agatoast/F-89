using System.Collections.Generic;
using System.IO;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPortraitService
    {
        private const string PresetResourceRoot = "SelectionPage/Portraits/";
        private const string CustomPortraitFolderName = "CharacterPortraits";

        private static readonly Dictionary<string, Texture2D> PresetTextures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> CustomTextures = new Dictionary<string, Texture2D>();

        public static Texture2D GetPortraitTexture(CharacterSaveData save)
        {
            if (save == null || string.IsNullOrEmpty(save.PortraitId))
            {
                return null;
            }

            if (save.PortraitId == CharacterPortraitIds.Custom)
            {
                return GetCustomPortraitTexture(save.Id);
            }

            return GetPresetPortraitTexture(save.PortraitId);
        }

        public static Texture2D GetPresetPortraitTexture(string portraitId)
        {
            if (string.IsNullOrEmpty(portraitId))
            {
                return null;
            }

            if (PresetTextures.TryGetValue(portraitId, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(PresetResourceRoot + portraitId);
            PresetTextures[portraitId] = texture;
            return texture;
        }

        public static void ClearPresetCache()
        {
            PresetTextures.Clear();
        }

        public static void SetPortrait(CharacterSaveData save, string portraitId)
        {
            if (save == null || string.IsNullOrEmpty(portraitId))
            {
                return;
            }

            save.PortraitId = portraitId;
            CharacterSaveRepository.WritePortrait(save);
            InvalidateCache(save.Id);
        }

        public static bool TryImportCustomPortrait(CharacterSaveData save, string sourcePath)
        {
            if (save == null || string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return false;
            }

            try
            {
                var bytes = File.ReadAllBytes(sourcePath);
                var tempTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tempTexture.LoadImage(bytes))
                {
                    Object.Destroy(tempTexture);
                    return false;
                }

                Directory.CreateDirectory(GetCustomPortraitDirectory());
                var destinationPath = GetCustomPortraitPath(save.Id);
                File.WriteAllBytes(destinationPath, tempTexture.EncodeToPNG());
                Object.Destroy(tempTexture);

                save.PortraitId = CharacterPortraitIds.Custom;
                CharacterSaveRepository.WritePortrait(save);
                InvalidateCache(save.Id);
                return true;
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"F-89: Failed to import portrait. {exception.Message}");
                return false;
            }
        }

        public static void InvalidateCache(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            if (CustomTextures.TryGetValue(saveId, out var texture) && texture != null)
            {
                Object.Destroy(texture);
            }

            CustomTextures.Remove(saveId);
        }

        public static void DeleteCustomPortrait(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            InvalidateCache(saveId);
            var path = GetCustomPortraitPath(saveId);
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"F-89: Failed to delete custom portrait. {exception.Message}");
            }
        }

        public static void DeleteAllCustomPortraits()
        {
            var directory = GetCustomPortraitDirectory();
            if (!Directory.Exists(directory))
            {
                CustomTextures.Clear();
                return;
            }

            try
            {
                foreach (var path in Directory.GetFiles(directory, "*.png"))
                {
                    File.Delete(path);
                }
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"F-89: Failed to clear custom portraits. {exception.Message}");
            }

            foreach (var texture in CustomTextures.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            CustomTextures.Clear();
        }

        private static Texture2D GetCustomPortraitTexture(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return null;
            }

            if (CustomTextures.TryGetValue(saveId, out var cached) && cached != null)
            {
                return cached;
            }

            var path = GetCustomPortraitPath(saveId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Object.Destroy(texture);
                    return null;
                }

                CustomTextures[saveId] = texture;
                return texture;
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"F-89: Failed to load custom portrait. {exception.Message}");
                return null;
            }
        }

        private static string GetCustomPortraitDirectory()
        {
            return Path.Combine(Application.persistentDataPath, CustomPortraitFolderName);
        }

        private static string GetCustomPortraitPath(string saveId)
        {
            return Path.Combine(GetCustomPortraitDirectory(), saveId + ".png");
        }
    }
}
