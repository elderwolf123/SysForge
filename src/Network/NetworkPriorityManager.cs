using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RamOptimizer.Network
{
    /// <summary>
    /// Manages network QoS and bandwidth prioritization for Windows processes
    /// </summary>
    public class NetworkPriorityManager
    {
        private const uint QOS_MAX_SERVICE_TYPE = 255;
        private const uint SERVICETYPE_GUARANTEED = 3;
        private const uint SERVICETYPE_CONTROLLEDLOAD = 2;
        private const uint SERVICETYPE_BESTEFFORT = 1;

        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto)]
        private static extern uint GetTcpTable(IntPtr pTcpTable, ref uint pdwSize, bool bOrder);

        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetTcpEntry(IntPtr pTcprow);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(string filename, uint accessMode, uint shareMode,
            IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static readonly Dictionary<string, NetworkOptimization> _activeOptimizations =
            new Dictionary<string, NetworkOptimization>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Set high network priority for a specific process
        /// </summary>
        public static async Task<bool> SetHighNetworkPriorityAsync(string processName, double bandwidthPercentage = 85)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""))
                    .Where(p => !p.HasExited).ToArray();

                if (processes.Length == 0)
                    return false;

                foreach (var process in processes)
                {
                    try
                    {
                        // Set process priority (if not already high)
                        if (process.PriorityClass != ProcessPriorityClass.High &&
                            process.PriorityClass != ProcessPriorityClass.RealTime)
                        {
                            process.PriorityClass = ProcessPriorityClass.AboveNormal;
                        }

                        // Throttle competing processes to redistributed bandwidth
                        await ThrottleCompetingProcessesAsync(processName);

                        // Apply QoS rules
                        await ApplyQosRulesAsync(process.Id, bandwidthPercentage);

                        // Track optimization
                        _activeOptimizations[processName] = new NetworkOptimization
                        {
                            ProcessName = processName,
                            BandwidthPercentage = bandwidthPercentage,
                            ProcessId = process.Id,
                            StartTime = DateTime.Now
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to optimize process {process.Id}: {ex.Message}");
                    }
                }

                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set network priority for {processName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Throttle background and competing processes to make room for priority application
        /// </summary>
        private static async Task ThrottleCompetingProcessesAsync(string priorityProcessName)
        {
            var backgroundProcesses = new[]
            {
                // Windows Update
                "svchost (Windows Update service)",
                "TiWorker", // TrustedInstaller worker
                "wuauclt", // Windows Update client
                // Steam
                "Steam", "steamwebhelper", "steamcmd",
                // Cloud services
                "OneDrive", "Dropbox", "GoogleDrive",
                // Background sync
                "groove", // OneNote sync
                "outlook", // Outlook sync
                "teams", // Teams background
                // System services that can be throttled
                "mscorsvw", // .NET runtime optimization
            };

            foreach (var processName in backgroundProcesses)
            {
                try
                {
                    await SetLowNetworkPriorityAsync(processName, 10); // Max 10% bandwidth
                }
                catch
                {
                    // Continue if process not found or can't be throttled
                }
            }

            // Throttle other user applications (except priority one)
            var userProcesses = new[] { "chrome", "firefox", "edge", "spotify", "discord" };
            foreach (var processName in userProcesses)
            {
                if (!processName.Contains(priorityProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await SetLowNetworkPriorityAsync(processName, 5); // Max 5% bandwidth
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Set low network priority for background processes
        /// </summary>
        public static async Task<bool> SetLowNetworkPriorityAsync(string processName, double bandwidthPercentage = 10)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""))
                    .Where(p => !p.HasExited).ToArray();

                foreach (var process in processes)
                {
                    try
                    {
                        // Lower process priority if too high
                        if (process.PriorityClass == ProcessPriorityClass.AboveNormal ||
                            process.PriorityClass == ProcessPriorityClass.High)
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal;
                        }

                        // Limit network priority
                        await ApplyQosRulesAsync(process.Id, bandwidthPercentage);
                    }
                    catch { }
                }

                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Apply QoS (Quality of Service) rules using Windows QoS API
        /// </summary>
        private static async Task ApplyQosRulesAsync(int processId, double bandwidthPercentage)
        {
            // Note: Full QoS implementation would require external libraries or Windows API calls
            // For now, we'll use process priority adjustments and basic throttling

            try
            {
                // Run netsh commands to setup QoS rules
                var ruleName = $"RamOptimizer_{processId}";
                var bandwidthLimit = Math.Max(1, (int)(100 / (100.0 / bandwidthPercentage))); // Convert to limit

                // Use built-in Windows QoS features via command line
                await RunNetshCommand($"qos delete rule name=\"{ruleName}\"") ; // Remove existing
                
                if (bandwidthPercentage > 10) // Only limit if significantly above minimum
                {
                    await RunNetshCommand(
                        $"qos add rule name=\"{ruleName}\" " +
                        $"destination=any protocol=any " +
                        $"direction=outbound " +
                        $"throttlerateinbps={(int)(bandwidthPercentage * 1000)} " +
                        $"appid={processId}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QoS rule application failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute netsh command for network configuration
        /// </summary>
        private static async Task RunNetshCommand(string command)
        {
            await Task.Run(() =>
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh.exe",
                            Arguments = command,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(2000); // 2 second timeout
                }
                catch
                {
                    // Ignore netsh failures
                }
            });
        }

        /// <summary>
        /// Remove all network optimizations
        /// </summary>
        public static async Task RemoveAllOptimizationsAsync()
        {
            try
            {
                // Remove all QoS rules created by this application
                await RunNetshCommand("qos delete rule name=\"RamOptimizer_*\"");

                // Reset process priorities
                foreach (var optimization in _activeOptimizations.Values.ToList())
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(
                            optimization.ProcessName.Replace(".exe", ""))
                            .Where(p => p.Id == optimization.ProcessId);

                        foreach (var process in processes)
                        {
                            try
                            {
                                if (process.PriorityClass == ProcessPriorityClass.AboveNormal)
                                {
                                    process.PriorityClass = ProcessPriorityClass.Normal;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                _activeOptimizations.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove network optimizations: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor if priority process is still running and adjust accordingly
        /// </summary>
        public static async Task MonitorPriorityProcessAsync(string processName, Action onProcessEnded)
        {
            await Task.Run(async () =>
            {
                while (_activeOptimizations.ContainsKey(processName))
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName.Replace(".exe", ""))
                            .Where(p =>
                            {
                                _activeOptimizations.TryGetValue(processName, out var opt);
                                return opt == null || p.Id == opt.ProcessId;
                            });

                        if (!processes.Any())
                        {
                            // Priority process ended, remove optimizations
                            await RemoveAllOptimizationsAsync();
                            onProcessEnded?.Invoke();
                            break;
                        }
                    }
                    catch
                    {
                        // Log error but continue monitoring
                    }

                    await Task.Delay(2000); // Check every 2 seconds
                }
            });
        }

        /// <summary>
        /// Get current network optimization status
        /// </summary>
        public static Dictionary<string, NetworkOptimization> GetActiveOptimizations()
        {
            return new Dictionary<string, NetworkOptimization>(_activeOptimizations);
        }

        /// <summary>
        /// Check if a process currently has network optimization
        /// </summary>
        public static bool IsProcessOptimized(string processName)
        {
            return _activeOptimizations.ContainsKey(processName.Replace(".exe", ""));
        }

        /// <summary>
        /// Get network statistics for monitoring
        /// </summary>
        public static NetworkStats GetNetworkStats()
        {
            return new NetworkStats
            {
                ActiveConnections = 0, // Would need IPHelper API to get actual count
                DownloadSpeedMbps = 0, // Would need Performance Counters
                UploadSpeedMbps = 0,
                NetworkLoadPercentage = 0
            };
        }
    }

    /// <summary>
    /// Represents active network optimization for a process
    /// </summary>
    public class NetworkOptimization
    {
        public string ProcessName { get; set; }
        public double BandwidthPercentage { get; set; }
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public string Icon => GetIconForProcess();
        public string Status => $"{BandwidthPercentage:F0}% bandwidth priority since {StartTime:HH:mm:ss}";

        private string GetIconForProcess()
        {
            return ProcessName.ToLower() switch
            {
                var name when name.Contains("steam") => "🎮",
                var name when name.Contains("chrome") || name.Contains("firefox") || name.Contains("edge") => "🌐",
                var name when name.Contains("discord") => "💬",
                var name when name.Contains("spotify") => "🎵",
                _ => "⚡"
            };
        }
    }

    /// <summary>
    /// Simple network statistics container
    /// </summary>
    public class NetworkStats
    {
        public int ActiveConnections { get; set; }
        public double DownloadSpeedMbps { get; set; }
        public double UploadSpeedMbps { get; set; }
        public double NetworkLoadPercentage { get; set; }
    }
}
