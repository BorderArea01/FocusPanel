using Microsoft.Win32;
using System;
using System.Reflection;

namespace FocusPanel.Services
{
    public static class AutoStartupService
    {
        private const string AppName = "FocusPanel";

        public static void SetStartup(bool enable)
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (enable)
                    {
                        string location = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        // Use quoting to handle spaces in path
                        key.SetValue(AppName, $"\"{location}\"");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log exception
                System.Diagnostics.Debug.WriteLine($"Error setting startup: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, false))
                {
                    return key.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
