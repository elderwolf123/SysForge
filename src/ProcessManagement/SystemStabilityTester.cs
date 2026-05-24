using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.ProcessManagement
{
    public class SystemStabilityTester : IStabilityTester
    {
        private readonly ILogger<SystemStabilityTester> _logger;

        public event EventHandler<string> StabilityTestCompleted;

        public SystemStabilityTester(ILogger<SystemStabilityTester> logger)
        {
            _logger = logger;
        }

        public bool IsSystemStable()
        {
            return IsSystemStableAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> IsSystemStableAsync()
        {
            try
            {
                return await CheckCpuUsageAsync() &&
                       CheckMemoryUsage() && 
                       CheckDiskUsage() && 
                       CheckNetworkUsage() && 
                       CheckProcessCount() && 
                       CheckSystemServices() && 
                       CheckCriticalProcesses() && 
                       CheckSystemLogs() && 
                       CheckHardwareHealth();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking system stability");
                return false;
            }
        }

        public void LogTestResults(string results)
        {
            // Log the test results
            _logger.LogInformation($"Stability Test Results: {results}");
            StabilityTestCompleted?.Invoke(this, results);
        }

        private async Task<bool> CheckCpuUsageAsync()
        {
            try
            {
                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    cpuCounter.NextValue(); // First call returns 0, so we call it once to initialize
                    await Task.Delay(1000); // Wait for 1 second to get an accurate reading
                    float cpuUsage = cpuCounter.NextValue();
                    _logger.LogInformation($"CPU Usage: {cpuUsage}%");
                    return cpuUsage < 80; // Return true if CPU usage is less than 80%
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking CPU usage");
                return false;
            }
        }

        private bool CheckMemoryUsage()
        {
            try
            {
                using (PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Available MBytes"))
                {
                    float availableMemory = memoryCounter.NextValue();
                    _logger.LogInformation($"Available Memory: {availableMemory} MB");
                    return availableMemory > 2048; // Return true if available memory is more than 2048 MB
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking memory usage");
                return false;
            }
        }

        private bool CheckDiskUsage()
        {
            try
            {
                DriveInfo drive = new DriveInfo("C");
                if (drive.IsReady)
                {
                    float freeSpace = drive.TotalFreeSpace / (1024 * 1024 * 1024); // Free space in GB
                    _logger.LogInformation($"Free Disk Space: {freeSpace} GB");
                    return freeSpace > 10; // Return true if free space is more than 10 GB
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking disk usage");
                return false;
            }
        }

        private bool CheckNetworkUsage()
        {
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in networkInterfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        var stats = ni.GetIPv4Statistics();
                        if (stats.BytesSent > 0 || stats.BytesReceived > 0)
                        {
                            _logger.LogInformation("Network activity detected");
                            return false; // Network activity detected
                        }
                    }
                }
                _logger.LogInformation("No network activity detected");
                return true; // No network activity detected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking network usage");
                return false;
            }
        }

        private bool CheckProcessCount()
        {
            try
            {
                int processCount = Process.GetProcesses().Length;
                _logger.LogInformation($"Process Count: {processCount}");
                return processCount < 200; // Return true if process count is less than 200
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking process count");
                return false;
            }
        }

        private bool CheckSystemServices()
        {
            try
            {
                // Check if critical system services are running
                string[] criticalServices = { "Spooler", "wuauserv", "bits", "Dhcp", "Dnscache", "EventLog", "LanmanServer", "LanmanWorkstation", "RpcSs", "SamSs", "Schedule", "SecurityHealthService", "SENS", "ShellHWDetection", "Spooler", "SystemEventsBroker", "TaskScheduler", "Winmgmt", "wuauserv" };
                foreach (string serviceName in criticalServices)
                {
                    try
                    {
                        ServiceController service = new ServiceController(serviceName);
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            _logger.LogWarning($"Service {serviceName} is not running");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error checking service {serviceName}");
                        // Continue checking other services
                    }
                }
                _logger.LogInformation("All critical services are running");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system services");
                return false;
            }
        }

        private bool CheckCriticalProcesses()
        {
            try
            {
                // Check if critical processes are running
                string[] criticalProcesses = { "explorer.exe", "svchost.exe", "System", "smss.exe", "csrss.exe", "wininit.exe", "winlogon.exe", "services.exe", "lsass.exe", "lsm.exe", "svchost.exe", "cisvc.exe", "LogonUI.exe", "dwm.exe", "explorer.exe", "taskhostw.exe", "ctfmon.exe", "SearchIndexer.exe" };
                foreach (string processName in criticalProcesses)
                {
                    if (!Process.GetProcessesByName(processName).Any())
                    {
                        _logger.LogWarning($"Critical process {processName} is not running");
                        return false;
                    }
                }
                _logger.LogInformation("All critical processes are running");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking critical processes");
                return false;
            }
        }

        private bool CheckSystemLogs()
        {
            try
            {
                using (var eventLog = new EventLog("System"))
                {
                    var entries = eventLog.Entries.Cast<EventLogEntry>()
                        .Where(e => e.EntryType == EventLogEntryType.Error)
                        .Where(e => e.TimeGenerated > DateTime.Now.AddMinutes(-5))
                        .ToList();
            
                    _logger.LogInformation($"Recent system errors: {entries.Count}");
                    return entries.Count == 0; // No recent errors
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system logs");
                return false;
            }
        }

        private bool CheckHardwareHealth()
        {
            try
            {
                var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var wmicPath = System.IO.Path.Combine(systemPath, "wbem", "wmic.exe");

                var processInfo = new ProcessStartInfo
                {
                    FileName = wmicPath,
                    Arguments = "diskdrive get status",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    _logger.LogInformation($"WMIC Output: {output}");

                    if (output.Contains("Error"))
                    {
                        _logger.LogWarning("Hardware issue detected");
                        return false; // Hardware issue detected
                    }
                }
                _logger.LogInformation("No hardware issues detected");
                return true; // No hardware issues detected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing hardware information");
                return false; // Error accessing hardware information
            }
        }
    }
}