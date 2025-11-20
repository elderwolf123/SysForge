using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using RamOptimizer.Logging;

namespace RamOptimizer.Testing
{
    public class SystemStabilityTester
    {
        private readonly ComprehensiveLogger _logger;
        private readonly List<StabilityTestResult> _testResults;

        public event EventHandler<StabilityTestProgressEventArgs> TestProgressUpdated;
        public event EventHandler<StabilityTestResult> TestCompleted;

        public SystemStabilityTester(ComprehensiveLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _testResults = new List<StabilityTestResult>();
        }

        public async Task<StabilityTestResult> RunFullStabilityTestAsync()
        {
            _logger.LogInfo("Starting full system stability test");
            
            var result = new StabilityTestResult
            {
                TestName = "Full System Stability Test",
                StartTime = DateTime.UtcNow,
                Checks = new List<StabilityCheckResult>()
            };

            try
            {
                // Run all stability checks
                var checks = new List<Func<Task<StabilityCheckResult>>>
                {
                    CheckCpuStabilityAsync,
                    CheckMemoryStabilityAsync,
                    CheckDiskStabilityAsync,
                    CheckNetworkStabilityAsync,
                    CheckProcessStabilityAsync,
                    CheckServiceStabilityAsync,
                    CheckHardwareHealthAsync,
                    CheckSystemLogsAsync
                };

                for (int i = 0; i < checks.Count; i++)
                {
                    var check = checks[i];
                    var checkResult = await check();
                    result.Checks.Add(checkResult);
                    
                    // Report progress
                    var progress = (int)(((i + 1) / (double)checks.Count) * 100);
                    TestProgressUpdated?.Invoke(this, new StabilityTestProgressEventArgs
                    {
                        TestName = result.TestName,
                        ProgressPercentage = progress,
                        CurrentCheck = checkResult.CheckName
                    });
                }

                // Calculate overall result
                result.EndTime = DateTime.UtcNow;
                result.OverallStatus = result.Checks.All(c => c.Status == CheckStatus.Pass) 
                    ? StabilityStatus.Stable 
                    : StabilityStatus.Unstable;
                
                result.TotalChecks = result.Checks.Count;
                result.PassedChecks = result.Checks.Count(c => c.Status == CheckStatus.Pass);
                result.FailedChecks = result.Checks.Count(c => c.Status == CheckStatus.Fail);
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogInfo($"Full stability test completed. Status: {result.OverallStatus}");
                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.OverallStatus = StabilityStatus.Error;
                result.ErrorMessage = ex.Message;
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogError($"Full stability test failed: {ex.Message}");
                return result;
            }
        }

        public async Task<StabilityCheckResult> CheckCpuStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "CPU Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    // Take multiple samples
                    var samples = new List<float>();
                    for (int i = 0; i < 10; i++)
                    {
                        cpuCounter.NextValue();
                        await Task.Delay(100);
                        samples.Add(cpuCounter.NextValue());
                    }

                    var averageUsage = samples.Average();
                    var maxUsage = samples.Max();

                    result.Details = $"Average CPU Usage: {averageUsage:F2}%, Max CPU Usage: {maxUsage:F2}%";

                    // Check for stability
                    if (maxUsage < 95 && averageUsage < 80)
                    {
                        result.Status = CheckStatus.Pass;
                        result.Message = "CPU usage within normal limits";
                    }
                    else if (maxUsage < 98)
                    {
                        result.Status = CheckStatus.Warning;
                        result.Message = "CPU usage is high but not critical";
                    }
                    else
                    {
                        result.Status = CheckStatus.Fail;
                        result.Message = "CPU usage is critically high";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check CPU stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckMemoryStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Memory Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                using (var memoryCounter = new PerformanceCounter("Memory", "Available MBytes"))
                {
                    var availableMemory = memoryCounter.NextValue();
                    await Task.Delay(100);
                    availableMemory = memoryCounter.NextValue(); // Second read for accuracy

                    result.Details = $"Available Memory: {availableMemory:F2} MB";

                    // Get total physical memory
                    var totalMemory = GetTotalPhysicalMemoryMB();
                    var usedMemoryPercentage = ((totalMemory - availableMemory) / totalMemory) * 100;

                    result.Details += $", Total Memory: {totalMemory:F2} MB, Usage: {usedMemoryPercentage:F2}%";

                    // Check for stability
                    if (availableMemory > 1024) // More than 1 GB available
                    {
                        result.Status = CheckStatus.Pass;
                        result.Message = "Sufficient memory available";
                    }
                    else if (availableMemory > 512) // More than 512 MB available
                    {
                        result.Status = CheckStatus.Warning;
                        result.Message = "Memory is getting low";
                    }
                    else
                    {
                        result.Status = CheckStatus.Fail;
                        result.Message = "Critically low memory";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check memory stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckDiskStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Disk Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
                var issues = new List<string>();

                foreach (var drive in drives)
                {
                    try
                    {
                        var freeSpacePercentage = (double)drive.TotalFreeSpace / drive.TotalSize * 100;
                        var freeSpaceGB = drive.TotalFreeSpace / (1024 * 1024 * 1024.0);

                        result.Details += $"Drive {drive.Name}: {freeSpacePercentage:F2}% free ({freeSpaceGB:F2} GB), ";

                        // Check for issues
                        if (freeSpacePercentage < 5)
                        {
                            issues.Add($"Drive {drive.Name} critically low on space");
                        }
                        else if (freeSpacePercentage < 10)
                        {
                            issues.Add($"Drive {drive.Name} low on space");
                        }

                        // Check for disk errors (simplified check)
                        if (drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
                        {
                            // In a real implementation, you would check the NTFS health
                            // This is a placeholder
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"Failed to check drive {drive.Name}: {ex.Message}");
                    }
                }

                if (issues.Count == 0)
                {
                    result.Status = CheckStatus.Pass;
                    result.Message = "All drives are healthy";
                }
                else if (issues.Count < drives.Count())
                {
                    result.Status = CheckStatus.Warning;
                    result.Message = "Some drives have issues: " + string.Join(", ", issues);
                }
                else
                {
                    result.Status = CheckStatus.Fail;
                    result.Message = "Multiple drive issues detected: " + string.Join(", ", issues);
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check disk stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckNetworkStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Network Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up);

                var activeInterfaces = 0;
                var issues = new List<string>();

                foreach (var ni in networkInterfaces)
                {
                    var stats = ni.GetIPv4Statistics();
                    if (stats.BytesSent > 0 || stats.BytesReceived > 0)
                    {
                        activeInterfaces++;
                        result.Details += $"Interface {ni.Name} is active, ";
                    }

                    // Check for errors
                    if (stats.IncomingPacketsDiscarded > 0 || stats.OutgoingPacketsDiscarded > 0)
                    {
                        issues.Add($"Interface {ni.Name} has discarded packets");
                    }
                }

                if (activeInterfaces > 0)
                {
                    result.Status = CheckStatus.Pass;
                    result.Message = $"Network is active with {activeInterfaces} interfaces";
                }
                else
                {
                    result.Status = CheckStatus.Warning;
                    result.Message = "No active network interfaces detected";
                }

                if (issues.Count > 0)
                {
                    result.Message += ". Issues: " + string.Join(", ", issues);
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check network stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckProcessStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Process Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var processes = Process.GetProcesses();
                var processCount = processes.Length;
                var criticalProcesses = new[] { "System", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "svchost" };
                var missingCritical = new List<string>();

                result.Details = $"Total processes: {processCount}, ";

                // Check for critical processes
                foreach (var critical in criticalProcesses)
                {
                    if (!processes.Any(p => p.ProcessName.Equals(critical, StringComparison.OrdinalIgnoreCase)))
                    {
                        missingCritical.Add(critical);
                    }
                }

                if (missingCritical.Count == 0)
                {
                    result.Status = CheckStatus.Pass;
                    result.Message = "All critical processes are running";
                }
                else
                {
                    result.Status = CheckStatus.Fail;
                    result.Message = "Missing critical processes: " + string.Join(", ", missingCritical);
                }

                // Check for excessive process count
                if (processCount > 300)
                {
                    result.Status = result.Status == CheckStatus.Pass ? CheckStatus.Warning : result.Status;
                    result.Message += ". Excessive number of processes";
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check process stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckServiceStabilityAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Service Stability Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var criticalServices = new[] { "Spooler", "wuauserv", "bits", "Dhcp", "Dnscache", "EventLog", "LanmanServer", "LanmanWorkstation" };
                var stoppedServices = new List<string>();

                result.Details = "";

                foreach (var serviceName in criticalServices)
                {
                    try
                    {
                        using (var service = new ServiceController(serviceName))
                        {
                            if (service.Status != ServiceControllerStatus.Running)
                            {
                                stoppedServices.Add(serviceName);
                                result.Details += $"Service {serviceName} is {service.Status}, ";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Details += $"Failed to check service {serviceName}: {ex.Message}, ";
                    }
                }

                if (stoppedServices.Count == 0)
                {
                    result.Status = CheckStatus.Pass;
                    result.Message = "All critical services are running";
                }
                else
                {
                    result.Status = CheckStatus.Fail;
                    result.Message = "Stopped critical services: " + string.Join(", ", stoppedServices);
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check service stability: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckHardwareHealthAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "Hardware Health Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                result.Details = "";

                // Check system uptime
                var uptime = TimeSpan.FromTicks(Environment.TickCount * TimeSpan.TicksPerMillisecond);
                result.Details += $"System Uptime: {uptime.Days} days {uptime.Hours} hours, ";

                // Check for hardware errors in event log (simplified)
                try
                {
                    using (var eventLog = new EventLog("System"))
                    {
                        var hardwareErrors = eventLog.Entries.Cast<EventLogEntry>()
                            .Where(e => e.EntryType == EventLogEntryType.Error)
                            .Where(e => e.TimeGenerated > DateTime.Now.AddDays(-1))
                            .Where(e => e.Message.Contains("hardware", StringComparison.OrdinalIgnoreCase) ||
                                       e.Message.Contains("disk", StringComparison.OrdinalIgnoreCase) ||
                                       e.Message.Contains("memory", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (hardwareErrors.Count == 0)
                        {
                            result.Details += "No recent hardware errors, ";
                        }
                        else
                        {
                            result.Details += $"{hardwareErrors.Count} recent hardware errors, ";
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Details += $"Failed to check event log: {ex.Message}, ";
                }

                result.Status = CheckStatus.Pass;
                result.Message = "Hardware health check completed";
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check hardware health: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<StabilityCheckResult> CheckSystemLogsAsync()
        {
            var result = new StabilityCheckResult
            {
                CheckName = "System Logs Check",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var errorCount = 0;
                var warningCount = 0;

                // Check application log
                try
                {
                    using (var eventLog = new EventLog("Application"))
                    {
                        var recentErrors = eventLog.Entries.Cast<EventLogEntry>()
                            .Where(e => e.TimeGenerated > DateTime.Now.AddHours(-1))
                            .ToList();

                        errorCount += recentErrors.Count(e => e.EntryType == EventLogEntryType.Error);
                        warningCount += recentErrors.Count(e => e.EntryType == EventLogEntryType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    result.Details += $"Failed to check application log: {ex.Message}, ";
                }

                // Check system log
                try
                {
                    using (var eventLog = new EventLog("System"))
                    {
                        var recentErrors = eventLog.Entries.Cast<EventLogEntry>()
                            .Where(e => e.TimeGenerated > DateTime.Now.AddHours(-1))
                            .ToList();

                        errorCount += recentErrors.Count(e => e.EntryType == EventLogEntryType.Error);
                        warningCount += recentErrors.Count(e => e.EntryType == EventLogEntryType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    result.Details += $"Failed to check system log: {ex.Message}, ";
                }

                result.Details = $"Recent errors: {errorCount}, Recent warnings: {warningCount}";

                if (errorCount == 0)
                {
                    result.Status = CheckStatus.Pass;
                    result.Message = "No recent system errors";
                }
                else if (errorCount < 5)
                {
                    result.Status = CheckStatus.Warning;
                    result.Message = $"Few recent errors ({errorCount}) and warnings ({warningCount})";
                }
                else
                {
                    result.Status = CheckStatus.Fail;
                    result.Message = $"Multiple recent errors ({errorCount}) and warnings ({warningCount})";
                }
            }
            catch (Exception ex)
            {
                result.Status = CheckStatus.Error;
                result.Message = $"Failed to check system logs: {ex.Message}";
                _logger.LogError(result.Message);
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        private double GetTotalPhysicalMemoryMB()
        {
            try
            {
                var gcMemory = GC.GetGCMemoryInfo();
                return gcMemory.TotalAvailableMemoryBytes / (1024.0 * 1024.0);
            }
            catch
            {
                // Fallback method
                return Environment.WorkingSet / (1024.0 * 1024.0);
            }
        }

        public List<StabilityTestResult> GetTestResults()
        {
            return new List<StabilityTestResult>(_testResults);
        }

        public void ClearTestResults()
        {
            _testResults.Clear();
            _logger.LogInfo("Stability test results cleared");
        }

        public async Task ExportTestResultsAsync(string filePath)
        {
            try
            {
                var content = "System Stability Test Results\n";
                content += "============================\n\n";

                foreach (var result in _testResults)
                {
                    content += $"Test: {result.TestName}\n";
                    content += $"Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}\n";
                    content += $"End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}\n";
                    content += $"Duration: {result.EndTime - result.StartTime}\n";
                    content += $"Overall Status: {result.OverallStatus}\n";
                    content += $"Total Checks: {result.TotalChecks}\n";
                    content += $"Passed: {result.PassedChecks}\n";
                    content += $"Failed: {result.FailedChecks}\n";

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        content += $"Error: {result.ErrorMessage}\n";
                    }

                    content += "\nDetailed Checks:\n";
                    foreach (var check in result.Checks)
                    {
                        content += $"  {check.CheckName}: {check.Status}\n";
                        content += $"    Message: {check.Message}\n";
                        content += $"    Details: {check.Details}\n";
                        content += $"    Duration: {check.EndTime - check.StartTime}\n\n";
                    }

                    content += "\n";
                }

                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInfo($"Stability test results exported to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to export stability test results: {ex.Message}");
                throw;
            }
        }
    }

    public class StabilityTestResult
    {
        public string TestName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public StabilityStatus OverallStatus { get; set; }
        public string ErrorMessage { get; set; }
        public List<StabilityCheckResult> Checks { get; set; }
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
    }

    public class StabilityCheckResult
    {
        public string CheckName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public CheckStatus Status { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }

    public class StabilityTestProgressEventArgs : EventArgs
    {
        public string TestName { get; set; }
        public int ProgressPercentage { get; set; }
        public string CurrentCheck { get; set; }
    }

    public enum StabilityStatus
    {
        Unknown,
        Stable,
        Unstable,
        Error
    }

    public enum CheckStatus
    {
        NotRun,
        Pass,
        Warning,
        Fail,
        Error
    }
}