using System;
using System.Runtime.InteropServices;

namespace FocusPanel.Helpers;

public static class DesktopHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static void ToggleDesktopIcons(bool show)
    {
        IntPtr hWnd = GetDesktopListViewHandle();
        if (hWnd != IntPtr.Zero)
        {
            // IMPORTANT: Hiding SHELLDLL_DefView directly is safer than hiding the ListView
            // But hiding ListView (SysListView32) is standard for "Show Desktop Icons" toggle.
            // Let's stick to hiding the ListView.
            
            // Wait, hiding the parent (SHELLDLL_DefView) might be better?
            // Windows "Show Desktop Icons" context menu actually hides the SysListView32.
            
            ShowWindow(hWnd, show ? SW_SHOW : SW_HIDE);
        }
    }

    public static bool IsDesktopIconsVisible()
    {
        IntPtr hWnd = GetDesktopListViewHandle();
        if (hWnd != IntPtr.Zero)
        {
            return IsWindowVisible(hWnd);
        }
        return true;
    }

    private static IntPtr GetDesktopListViewHandle()
    {
        // 1. Try finding Progman -> SHELLDLL_DefView
        IntPtr progman = FindWindow("Progman", "Program Manager");
        IntPtr shellDllDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

        if (shellDllDefView == IntPtr.Zero)
        {
            // 2. Try finding WorkerW -> SHELLDLL_DefView
            IntPtr workerW = IntPtr.Zero;
            int retryCount = 0;
            const int MAX_RETRIES = 20; // Prevent infinite loop in any case

            while (retryCount < MAX_RETRIES)
            {
                workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                if (workerW == IntPtr.Zero) break;

                shellDllDefView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDllDefView != IntPtr.Zero) break;
                
                retryCount++;
            }
        }

        if (shellDllDefView != IntPtr.Zero)
        {
            return FindWindowEx(shellDllDefView, IntPtr.Zero, "SysListView32", "FolderView");
        }

        return IntPtr.Zero;
    }
}