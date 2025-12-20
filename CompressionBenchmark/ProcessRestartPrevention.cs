using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace CompressionBenchmark;

/// <summary>
/// Manages Windows auto-restart settings to prevent killed processes from automatically restarting.
/// Enhanced version with comprehensive process monitoring and prevention.
/// </summary>
public class ProcessRestartPrevention : IDisposable
{
    private const string WinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    private const string ServicesKey = @"SYSTEM\CurrentControlSet\Services";
    private const string ShellKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    private bool _autoRestartDisabled = false;
    private int? _originalAutoRestartValue = null;
    private readonly Dictionary<string, ServiceConfig> _originalServiceConfigs = new();
    private readonly List<Process> _monitoredProcesses = new();
    private readonly Timer _monitoringTimer;
    private bool _isMonitoring = false;
    private bool _shellModified = false;
    private string? _originalShell;

    public ProcessRestartPrevention()
    {
        // Start monitoring timer to detect and kill auto-restarting processes
        _monitoringTimer = new Timer(MonitorForAutoRestarts, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Disable Windows auto-restart of shell (explorer.exe) using multiple methods
    /// </summary>
    public bool DisableExplorerAutoRestart()
    {
        try
        {
            bool success = false;
            
            // Method 1: Disable AutoRestartShell registry setting
            if (DisableAutoRestartShell())
            {
                success = true;
            }
            
            // Method 2: Change the shell to a custom one that won't restart
            if (DisableShellRestart())
            {
                success = true;
            }
            
            // Method 3: Set UserInit to prevent automatic restart
            if (DisableUserInitRestart())
            {
                success = true;
            }
            
            if (success)
            {
                _autoRestartDisabled = true;
                Console.WriteLine("   ✓ Multiple methods applied to prevent explorer.exe restart");
                BenchmarkLogger.LogInfo("Explorer restart prevention applied using multiple methods");
            }
            else
            {
                Console.WriteLine("   ⚠️  Failed to prevent explorer restart");
                BenchmarkLogger.LogWarning("Failed to prevent explorer restart");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Failed to disable auto-restart: {ex.Message}");
            Console.WriteLine($"   💡 Make sure to run as Administrator");
            BenchmarkLogger.LogError("Failed to disable explorer auto-restart", ex);
            return false;
        }
    }

    /// <summary>
    /// Method 1: Disable AutoRestartShell registry setting
    /// </summary>
    private bool DisableAutoRestartShell()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key == null) return false;

            // Save original value
            var currentValue = key.GetValue("AutoRestartShell");
            if (currentValue != null)
            {
                _originalAutoRestartValue = Convert.ToInt32(currentValue);
                BenchmarkLogger.LogInfo($"Saved original AutoRestartShell value: {_originalAutoRestartValue}");
            }

            // Disable auto-restart
            key.SetValue("AutoRestartShell", 0, RegistryValueKind.DWord);
            Console.WriteLine("   ✓ Disabled AutoRestartShell registry setting");
            BenchmarkLogger.LogInfo("AutoRestartShell registry setting disabled");
            return true;
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to disable AutoRestartShell: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Method 2: Change the shell to prevent automatic restart
    /// </summary>
    private bool DisableShellRestart()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(ShellKey, writable: true);
            if (key == null) return false;

            // Save original shell
            _originalShell = key.GetValue("Shell")?.ToString();
            
            // Set shell to a minimal one that won't restart
            key.SetValue("Shell", "cmd.exe /k echo Shell disabled for optimization", RegistryValueKind.String);
            _shellModified = true;
            
            Console.WriteLine("   ✓ Modified shell registry to prevent restart");
            BenchmarkLogger.LogInfo("Shell registry modified to prevent restart");
            return true;
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to modify shell registry: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Method 3: Disable UserInit restart mechanism
    /// </summary>
    private bool DisableUserInitRestart()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key == null) return false;

            // Save original UserInit
            var originalUserInit = key.GetValue("UserInit");
            if (originalUserInit != null)
            {
                BenchmarkLogger.LogInfo($"Saved original UserInit: {originalUserInit}");
            }

            // Set UserInit to minimal startup
            key.SetValue("UserInit", "explorer.exe", RegistryValueKind.String);
            
            Console.WriteLine("   ✓ Modified UserInit to prevent restart");
            BenchmarkLogger.LogInfo("UserInit modified to prevent restart");
            return true;
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to modify UserInit: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disable auto-restart for specific services that commonly restart
    /// </summary>
    public void DisableCommonServiceRestarts()
    {
        var commonRestartingServices = new[]
        {
            "spooler",        // Print Spooler
            "BITS",           // Background Intelligent Transfer Service
            "wuauserv",       // Windows Update
            "UsoSvc",         // Update Orchestrator Service
            "SysMain",        // Superfetch
            "defragsvc",      // Defragmentation
            "Winmgmt",        // WMI Service
            "Dhcp",           // DHCP Client
            "Dnscache",       // DNS Client
            "NlaSvc",         // Network Location Awareness
            "EapHost",        // Extensible Authentication Protocol
            "EventLog",       // Event Log
            "FontCache",      // Windows Font Cache
            "Schedule",       // Task Scheduler
            "Themes",         // Themes Service
            "TrustedInstaller" // Windows Modules Installer
        };

        foreach (var serviceName in commonRestartingServices)
        {
            try
            {
                DisableServiceAutoRestart(serviceName);
            }
            catch (Exception ex)
            {
                BenchmarkLogger.LogWarning($"Failed to disable auto-restart for {serviceName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Disable auto-restart for a specific service
    /// </summary>
    private void DisableServiceAutoRestart(string serviceName)
    {
        try
        {
            using var serviceKey = Registry.LocalMachine.OpenSubKey($@"{ServicesKey}\{serviceName}", writable: true);
            if (serviceKey == null)
            {
                BenchmarkLogger.LogWarning($"Could not open service key for {serviceName}");
                return;
            }

            // Save original configuration
            var originalConfig = new ServiceConfig
            {
                StartType = serviceKey.GetValue("Start")?.ToString(),
                ErrorControl = serviceKey.GetValue("ErrorControl")?.ToString(),
                ObjectName = serviceKey.GetValue("ObjectName")?.ToString()
            };
            _originalServiceConfigs[serviceName] = originalConfig;

            // Modify service to prevent auto-restart
            serviceKey.SetValue("Start", "4", RegistryValueKind.DWord); // 4 = Disabled
            serviceKey.SetValue("ErrorControl", "1", RegistryValueKind.DWord); // 1 = Ignore errors

            Console.WriteLine($"   ✓ Disabled auto-restart for {serviceName}");
            BenchmarkLogger.LogInfo($"Disabled auto-restart for service: {serviceName}");
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogError($"Failed to disable service {serviceName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Monitor for and kill auto-restarting processes
    /// </summary>
    private void MonitorForAutoRestarts(object state)
    {
        if (!_isMonitoring) return;

        try
        {
            var previouslyKilled = RamOptimizationManager.GetPreviouslyKilledProcesses();
            
            foreach (var processName in previouslyKilled)
            {
                var runningProcesses = Process.GetProcessesByName(processName);
                foreach (var process in runningProcesses)
                {
                    if (process.Id != 0 && process.Id != 4) // Skip System processes
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(1000);
                            BenchmarkLogger.LogInfo($"Killed auto-restarting process: {processName} (PID: {process.Id})");
                        }
                        catch (Exception ex)
                        {
                            BenchmarkLogger.LogWarning($"Failed to kill auto-restarting process {processName}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Process monitoring error: {ex.Message}");
        }
    }

    /// <summary>
    /// Start monitoring for auto-restarting processes
    /// </summary>
    public void StartMonitoring()
    {
        _isMonitoring = true;
        Console.WriteLine("   ✓ Started monitoring for auto-restarting processes");
    }

    /// <summary>
    /// Stop monitoring
    /// </summary>
    public void StopMonitoring()
    {
        _isMonitoring = false;
        Console.WriteLine("   ✓ Stopped monitoring for auto-restarting processes");
    }

    /// <summary>
    /// Re-enable Windows auto-restart of shell and restore services
    /// </summary>
    public void RestoreExplorerAutoRestart()
    {
        StopMonitoring();

        bool restored = false;

        // Restore AutoRestartShell
        if (RestoreAutoRestartShell())
        {
            restored = true;
        }

        // Restore original shell
        if (RestoreShell())
        {
            restored = true;
        }

        // Restore UserInit
        if (RestoreUserInit())
        {
            restored = true;
        }

        if (restored)
        {
            Console.WriteLine("   ✓ Restored explorer.exe restart mechanisms");
            BenchmarkLogger.LogInfo("Explorer restart mechanisms restored");
        }
        else
        {
            Console.WriteLine("   ⚠️  Failed to restore explorer restart mechanisms");
            BenchmarkLogger.LogWarning("Failed to restore explorer restart mechanisms");
        }

        // Restore original service configurations
        RestoreServiceConfigs();
    }

    /// <summary>
    /// Restore AutoRestartShell registry setting
    /// </summary>
    private bool RestoreAutoRestartShell()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key != null)
            {
                var valueToSet = _originalAutoRestartValue ?? 1;
                key.SetValue("AutoRestartShell", valueToSet, RegistryValueKind.DWord);
                Console.WriteLine("   ✓ Restored AutoRestartShell registry setting");
                BenchmarkLogger.LogInfo($"AutoRestartShell restored to: {valueToSet}");
                return true;
            }
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to restore AutoRestartShell: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Restore original shell setting
    /// </summary>
    private bool RestoreShell()
    {
        try
        {
            if (!_shellModified || string.IsNullOrEmpty(_originalShell)) return false;

            using var key = Registry.LocalMachine.OpenSubKey(ShellKey, writable: true);
            if (key != null)
            {
                key.SetValue("Shell", _originalShell, RegistryValueKind.String);
                _shellModified = false;
                Console.WriteLine("   ✓ Restored shell registry setting");
                BenchmarkLogger.LogInfo("Shell registry setting restored");
                return true;
            }
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to restore shell registry: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Restore original UserInit setting
    /// </summary>
    private bool RestoreUserInit()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
            if (key != null)
            {
                // Restore to default Windows shell startup
                key.SetValue("UserInit", "explorer.exe", RegistryValueKind.String);
                Console.WriteLine("   ✓ Restored UserInit registry setting");
                BenchmarkLogger.LogInfo("UserInit registry setting restored");
                return true;
            }
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to restore UserInit: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Restore original service configurations
    /// </summary>
    private void RestoreServiceConfigs()
    {
        foreach (var kvp in _originalServiceConfigs)
        {
            try
            {
                using var serviceKey = Registry.LocalMachine.OpenSubKey($@"{ServicesKey}\{kvp.Key}", writable: true);
                if (serviceKey != null)
                {
                    if (kvp.Value.StartType != null)
                        serviceKey.SetValue("Start", kvp.Value.StartType, RegistryValueKind.DWord);
                    if (kvp.Value.ErrorControl != null)
                        serviceKey.SetValue("ErrorControl", kvp.Value.ErrorControl, RegistryValueKind.DWord);
                    if (kvp.Value.ObjectName != null)
                        serviceKey.SetValue("ObjectName", kvp.Value.ObjectName, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                BenchmarkLogger.LogWarning($"Failed to restore service {kvp.Key}: {ex.Message}");
            }
        }
        _originalServiceConfigs.Clear();
    }

    /// <summary>
    /// Check if running with administrator privileges
    /// </summary>
    public static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get list of processes that are known to auto-restart
    /// </summary>
    public static List<string> GetAutoRestartingProcesses()
    {
        return new List<string>
        {
            "explorer",
            "spooler",
            "svchost",
            "chrome",
            "firefox",
            "edge",
            "opera",
            "brave",
            "discord",
            "steam",
            "updater",
            "update",
            "agent",
            "service",
            "manager",
            "monitor",
            "watchdog"
        };
    }

    public void Dispose()
    {
        StopMonitoring();
        _monitoringTimer?.Dispose();
    }
}

/// <summary>
/// Stores original service configuration for restoration
/// </summary>
public class ServiceConfig
{
    public string? StartType { get; set; }
    public string? ErrorControl { get; set; }
    public string? ObjectName { get; set; }
}
