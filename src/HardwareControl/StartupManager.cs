using Microsoft.Win32;
using System;

namespace RamOptimizer.HardwareControl
{
    /// <summary>
    /// Manages application startup with Windows
    /// </summary>
    public class StartupManager
    {
        private const string AppName = "RAMOptimizerPro";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Check if application is set to run on startup
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enable application to run on Windows startup
        /// </summary>
        public static void EnableStartup()
        {
            try
            {
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                // For .NET 8.0, we need to use the .exe path, not the .dll
                appPath = appPath.Replace(".dll", ".exe");

                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.SetValue(AppName, $"\"{appPath}\"");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enable startup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disable application from running on Windows startup
        /// </summary>
        public static void DisableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to disable startup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Toggle startup state
        /// </summary>
        public static void ToggleStartup()
        {
            if (IsStartupEnabled())
                DisableStartup();
            else
                EnableStartup();
        }
    }
}
