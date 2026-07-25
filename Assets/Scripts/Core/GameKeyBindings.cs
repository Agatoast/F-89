using UnityEngine;

namespace F89.Core
{
    public static class GameKeyBindings
    {
        private const string PlayerPrefsPrefix = "F89.KeyBinding.";

        private static readonly System.Collections.Generic.Dictionary<string, KeyCode> Keys =
            new System.Collections.Generic.Dictionary<string, KeyCode>();

        private static bool isLoaded;

        public static string ListeningBindingId { get; private set; }

        public static bool IsListening => !string.IsNullOrEmpty(ListeningBindingId);

        public static void Load()
        {
            Keys.Clear();
            foreach (var binding in GameKeyBindingCatalog.Bindings)
            {
                var prefKey = PlayerPrefsPrefix + binding.Id;
                var stored = PlayerPrefs.GetInt(prefKey, (int)binding.DefaultKey);
                Keys[binding.Id] = (KeyCode)stored;
            }

            isLoaded = true;
        }

        public static KeyCode GetKey(string bindingId)
        {
            EnsureLoaded();
            return Keys.TryGetValue(bindingId, out var key) ? key : KeyCode.None;
        }

        public static string GetDisplayLabel(string bindingId)
        {
            return FormatKey(GetKey(bindingId));
        }

        public static void SetKey(string bindingId, KeyCode key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(bindingId) || key == KeyCode.None)
            {
                return;
            }

            foreach (var pair in Keys)
            {
                if (pair.Key != bindingId && pair.Value == key)
                {
                    Keys[pair.Key] = KeyCode.None;
                    PlayerPrefs.SetInt(PlayerPrefsPrefix + pair.Key, (int)KeyCode.None);
                }
            }

            Keys[bindingId] = key;
            PlayerPrefs.SetInt(PlayerPrefsPrefix + bindingId, (int)key);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            Keys.Clear();
            foreach (var binding in GameKeyBindingCatalog.Bindings)
            {
                Keys[binding.Id] = binding.DefaultKey;
                PlayerPrefs.SetInt(PlayerPrefsPrefix + binding.Id, (int)binding.DefaultKey);
            }

            PlayerPrefs.Save();
            isLoaded = true;
        }

        public static void BeginListening(string bindingId)
        {
            ListeningBindingId = bindingId;
        }

        public static void CancelListening()
        {
            ListeningBindingId = null;
        }

        public static bool TryHandleListenEvent(Event currentEvent)
        {
            if (!IsListening || currentEvent == null)
            {
                return false;
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Escape)
                {
                    CancelListening();
                    currentEvent.Use();
                    return true;
                }

                if (currentEvent.keyCode != KeyCode.None)
                {
                    SetKey(ListeningBindingId, currentEvent.keyCode);
                    CancelListening();
                    currentEvent.Use();
                    return true;
                }
            }

            if (currentEvent.type == EventType.MouseDown)
            {
                var mouseKey = MouseButtonToKeyCode(currentEvent.button);
                if (mouseKey != KeyCode.None)
                {
                    SetKey(ListeningBindingId, mouseKey);
                    CancelListening();
                    currentEvent.Use();
                    return true;
                }
            }

            return false;
        }

        public static bool IsHeld(string bindingId)
        {
            var key = GetKey(bindingId);
            if (key == KeyCode.None)
            {
                return false;
            }

            if (IsMouseKey(key))
            {
                return Input.GetMouseButton(MouseKeyToButton(key));
            }

            return Input.GetKey(key);
        }

        public static bool WasPressed(string bindingId)
        {
            var key = GetKey(bindingId);
            if (key == KeyCode.None)
            {
                return false;
            }

            if (IsMouseKey(key))
            {
                return Input.GetMouseButtonDown(MouseKeyToButton(key));
            }

            return Input.GetKeyDown(key);
        }

        public static string FormatKey(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return "Unassigned";
            }

            switch (key)
            {
                case KeyCode.Mouse0: return "Mouse 1";
                case KeyCode.Mouse1: return "Mouse 2";
                case KeyCode.Mouse2: return "Mouse 3";
                case KeyCode.Mouse3: return "Mouse 4";
                case KeyCode.Mouse4: return "Mouse 5";
                case KeyCode.Mouse5: return "Mouse 6";
                case KeyCode.Mouse6: return "Mouse 7";
                case KeyCode.LeftShift: return "Left Shift";
                case KeyCode.RightShift: return "Right Shift";
                case KeyCode.LeftControl: return "Left Ctrl";
                case KeyCode.RightControl: return "Right Ctrl";
                case KeyCode.LeftAlt: return "Left Alt";
                case KeyCode.RightAlt: return "Right Alt";
                case KeyCode.UpArrow: return "Up Arrow";
                case KeyCode.DownArrow: return "Down Arrow";
                case KeyCode.LeftArrow: return "Left Arrow";
                case KeyCode.RightArrow: return "Right Arrow";
                default:
                    var label = key.ToString();
                    if (label.StartsWith("Alpha"))
                    {
                        return label.Substring(5);
                    }

                    return label;
            }
        }

        private static void EnsureLoaded()
        {
            if (!isLoaded)
            {
                Load();
            }
        }

        private static bool IsMouseKey(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        private static int MouseKeyToButton(KeyCode key)
        {
            return (int)(key - KeyCode.Mouse0);
        }

        private static KeyCode MouseButtonToKeyCode(int button)
        {
            if (button < 0 || button > 6)
            {
                return KeyCode.None;
            }

            return KeyCode.Mouse0 + button;
        }
    }
}
