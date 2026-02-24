using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FocusPanel.Helpers;

public static class IconHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0; // 32x32
    private const uint SHGFI_SMALLICON = 0x1; // 16x16
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint SHGFI_LINKOVERLAY = 0x000008000; // Add link overlay

    public static ImageSource GetIcon(string path, bool large = true)
    {
        // Try to resolve shortcut target to get clean icon without overlay
        if (System.IO.Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            string target = ResolveShortcut(path);
            if (!string.IsNullOrEmpty(target) && (System.IO.File.Exists(target) || System.IO.Directory.Exists(target)))
            {
                path = target;
            }
        }

        var shinfo = new SHFILEINFO();
        uint flags = SHGFI_ICON | (large ? SHGFI_LARGEICON : SHGFI_SMALLICON);

        // Use file attributes if file doesn't exist (for extension lookup)
        // Or if we resolved a target but it's not accessible? No, SHGetFileInfo handles paths fine.
        if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
        {
            flags |= SHGFI_USEFILEATTRIBUTES;
        }

        SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

        if (shinfo.hIcon == IntPtr.Zero) return null;

        var icon = Imaging.CreateBitmapSourceFromHIcon(
            shinfo.hIcon,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        icon.Freeze(); // Make it cross-thread accessible

        // Cleanup
        DestroyIcon(shinfo.hIcon);

        return icon;
    }

    private static string ResolveShortcut(string shortcutPath)
    {
        // Simple WScript resolution using dynamic to avoid reference
        try
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            return shortcut.TargetPath;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
