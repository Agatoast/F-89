using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace F89.UI
{
    public static class PortraitFilePicker
    {
        public static bool TryPickPortraitFile(out string path)
        {
#if UNITY_EDITOR
            path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
                "Select Portrait",
                string.Empty,
                new[] { "Image Files", "png,jpg,jpeg,bmp" });
            return !string.IsNullOrEmpty(path);
#elif UNITY_STANDALONE_WIN
            return TryPickPortraitFileWindows(out path);
#else
            path = null;
            Debug.LogWarning("F-89: Portrait browse is not supported on this platform yet.");
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int OfnExplorer = 0x00080000;
        private const int OfnFileMustExist = 0x00001000;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnHideReadOnly = 0x00000200;
        private const int OfnNoChangeDir = 0x00000008;

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileNameW(ref OpenFileName openFileName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public IntPtr filter;
            public IntPtr customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public IntPtr file;
            public int maxFile;
            public IntPtr fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reserved0;
            public IntPtr reserved1;
            public int flagsEx;
        }

        private static bool TryPickPortraitFileWindows(out string path)
        {
            path = null;

            // Exclusive fullscreen often blocks or hides the native dialog.
            var restoreFullScreen = Screen.fullScreen;
            if (restoreFullScreen)
            {
                Screen.fullScreen = false;
            }

            const int maxPath = 260;
            var fileBuffer = Marshal.AllocHGlobal(maxPath * sizeof(char));
            var filterBuffer = IntPtr.Zero;
            try
            {
                for (var i = 0; i < maxPath * sizeof(char); i++)
                {
                    Marshal.WriteByte(fileBuffer, i, 0);
                }

                // Must be a double-null-terminated multi-string. A C# string field would
                // truncate at the first embedded '\0' when marshaled.
                filterBuffer = AllocateFilterBuffer(
                    "Image Files\0*.png;*.jpg;*.jpeg;*.bmp\0All Files\0*.*\0\0");

                var openFileName = new OpenFileName
                {
                    structSize = Marshal.SizeOf(typeof(OpenFileName)),
                    dlgOwner = GetActiveWindow(),
                    filter = filterBuffer,
                    file = fileBuffer,
                    maxFile = maxPath,
                    filterIndex = 1,
                    flags = OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnHideReadOnly | OfnNoChangeDir,
                    title = "Select Portrait",
                    defExt = "png"
                };

                if (!GetOpenFileNameW(ref openFileName))
                {
                    return false;
                }

                path = Marshal.PtrToStringUni(fileBuffer);
                return !string.IsNullOrEmpty(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"F-89: Portrait browse failed. {exception.Message}");
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
                if (filterBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(filterBuffer);
                }

                if (restoreFullScreen)
                {
                    Screen.fullScreen = true;
                }
            }
        }

        private static IntPtr AllocateFilterBuffer(string filter)
        {
            var bytes = Encoding.Unicode.GetBytes(filter);
            var buffer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            return buffer;
        }
#endif
    }
}
