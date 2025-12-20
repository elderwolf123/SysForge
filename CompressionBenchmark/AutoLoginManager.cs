using System;
using Microsoft.Win32;

namespace CompressionBenchmark;

/// <summary>
/// Manages Windows auto-login for unattended testing after crash/reboot.
/// WARNING: This stores password in registry. Use only for testing, disable when done.
/// </summary>
public class AutoLoginManager
{
    private const string WinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    
    /// <summary>
    /// Enable Windows auto-login (for unattended crash recovery)
    /// </summary>
    public static bool EnableAutoLogin(string username, string password, string domain = "")
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key == null)
            {
                Console.WriteLine("   ❌ Could not open Winlogon registry key");
                return false;
            }

            // Save current values for restoration
            var currentAutoLogin = key.GetValue("AutoAdminLogon");
            if (currentAutoLogin?.ToString() != "1")
            {
                key.SetValue("AutoAdminLogon_Backup", currentAutoLogin ?? "0");
            }

            // Enable auto-login
            key.SetValue("AutoAdminLogon", "1", RegistryValueKind.String);
            key.SetValue("DefaultUserName", username, RegistryValueKind.String);
            key.SetValue("DefaultPassword", password, RegistryValueKind.String);
            
            if (!string.IsNullOrEmpty(domain))
            {
                key.SetValue("DefaultDomainName", domain, RegistryValueKind.String);
            }

            Console.WriteLine("   ✓ Auto-login enabled");
            Console.WriteLine("   ⚠️  Security: Password stored in registry (will be removed when done)");
            BenchmarkLogger.LogInfo("Auto-login enabled for unattended testing");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Failed to enable auto-login: {ex.Message}");
            BenchmarkLogger.LogError("Failed to enable auto-login", ex);
            return false;
        }
    }

    /// <summary>
    /// Disable Windows auto-login and clear password
    /// </summary>
    public static void DisableAutoLogin()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key == null)
                return;

            // Restore original value or disable
            var backup = key.GetValue("AutoAdminLogon_Backup");
            if (backup != null)
            {
                key.SetValue("AutoAdminLogon", backup);
                key.DeleteValue("AutoAdminLogon_Backup", false);
            }
            else
            {
                key.SetValue("AutoAdminLogon", "0", RegistryValueKind.String);
            }

            // Clear password
            key.DeleteValue("DefaultPassword", false);
            
            Console.WriteLine("   ✓ Auto-login disabled");
            Console.WriteLine("   ✓ Password cleared from registry");
            BenchmarkLogger.LogInfo("Auto-login disabled, credentials cleared");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Error disabling auto-login: {ex.Message}");
            BenchmarkLogger.LogWarning($"Failed to disable auto-login: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if auto-login is currently enabled
    /// </summary>
    public static bool IsAutoLoginEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey);
            if (key == null)
                return false;

            var value = key.GetValue("AutoAdminLogon");
            return value?.ToString() == "1";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get current username (for auto-fill)
    /// </summary>
    public static string GetCurrentUsername()
    {
        return Environment.UserName;
    }

    /// <summary>
    /// Get current domain (for auto-fill)
    /// </summary>
    public static string GetCurrentDomain()
    {
        return Environment.UserDomainName;
    }
}
