using System;
using System.Runtime.InteropServices;
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
        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetOpenFileName(ref OpenFileName openFileName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
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
            const int maxPath = 260;
            var fileBuffer = Marshal.AllocHGlobal(maxPath * 2);
            try
            {
                for (var i = 0; i < maxPath * 2; i++)
                {
                    Marshal.WriteByte(fileBuffer, i, 0);
                }

                var openFileName = new OpenFileName
                {
                    structSize = Marshal.SizeOf(typeof(OpenFileName)),
                    filter = "Image Files\0*.png;*.jpg;*.jpeg;*.bmp\0All Files\0*.*\0\0",
                    file = fileBuffer,
                    maxFile = maxPath,
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008,
                    title = "Select Portrait"
                };

                if (!GetOpenFileName(ref openFileName))
                {
                    path = null;
                    return false;
                }

                path = Marshal.PtrToStringAuto(fileBuffer);
                return !string.IsNullOrEmpty(path);
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
            }
        }
#endif
    }
}
