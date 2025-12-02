using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.ServiceTesting;

/// <summary>
/// Multi-layer recovery system for service testing
/// </summary>
public class RecoverySystem
{
    private const string RegistryPath = @"SOFTWARE\RamOptimizer\ServiceTest";
    private readonly ILogger? _logger;
    private readonly Stack<string> _rollbackQueue = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RecoverySystem(ILogger? logger = null)
    {
        _logger = logger;
    }

    #region Level 1: Service Restart

    /// <summary>
    /// Attempt to restart a service
    /// </summary>
    public async Task<bool> RestartService(string serviceName)
    {
        try
        {
            _logger?.LogInformation($"Attempting to restart service: {serviceName}");
            
            using var service = new ServiceController(serviceName);
            
            // Check current status
            service.Refresh();
            var timeout = TimeSpan.FromSeconds(30);

            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }

            _logger?.LogInformation($"Successfully restarted service: {serviceName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to restart service {serviceName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stop a service safely
    /// </summary>
    public async Task<bool> StopService(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            service.Refresh();

            if (service.Status == ServiceControllerStatus.Running)
            {
                // Add to rollback queue before stopping
                await _lock.WaitAsync();
                try
                {
                    _rollbackQueue.Push(serviceName);
                }
                finally
                {
                    _lock.Release();
                }

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                
                _logger?.LogInformation($"Successfully stopped service: {serviceName}");
                return true;
            }

            return true; // Already stopped
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to stop service {serviceName}: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Level 2: Rollback Queue

    /// <summary>
    /// Process rollback queue - restart all stopped services in reverse order (LIFO)
    /// </summary>
    public async Task<int> ProcessRollbackQueue()
    {
        int successCount = 0;
        
        await _lock.WaitAsync();
        try
        {
            _logger?.LogInformation($"Processing rollback queue: {_rollbackQueue.Count} services");

            while (_rollbackQueue.Count > 0)
            {
                var serviceName = _rollbackQueue.Pop();
                
                // Release lock temporarily to allow async restart
                // Note: This is safe because we popped the item already
                _lock.Release();
                try 
                {
                    if (await RestartService(serviceName))
                    {
                        successCount++;
                    }
                    
                    // Small delay between restarts
                    await Task.Delay(500);
                }
                finally
                {
                    await _lock.WaitAsync();
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        _logger?.LogInformation($"Rollback complete: {successCount} services restarted");
        return successCount;
    }

    /// <summary>
    /// Clear the rollback queue
    /// </summary>
    public void ClearRollbackQueue()
    {
        _lock.Wait();
        try
        {
            _rollbackQueue.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Level 3: System Restore Point

    /// <summary>
    /// Create a system restore point
    /// </summary>
    public async Task<bool> CreateRestorePoint(string description)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger?.LogInformation($"Creating system restore point: {description}");

                // Use WMI to create restore point
                var managementScope = new ManagementScope("\\\\localhost\\root\\default");
                var managementPath = new ManagementPath("SystemRestore");
                var options = new ObjectGetOptions();
                using var managementClass = new ManagementClass(managementScope, managementPath, options);

                var parameters = managementClass.GetMethodParameters("CreateRestorePoint");
                parameters["Description"] = description;
                parameters["RestorePointType"] = 12; // MODIFY_SETTINGS
                parameters["EventType"] = 100; // BEGIN_SYSTEM_CHANGE

                var result = managementClass.InvokeMethod("CreateRestorePoint", parameters, null);
                
                _logger?.LogInformation("System restore point created successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create restore point: {ex.Message}");
                return false;
            }
        });
    }

    #endregion

    #region Level 4: Auto-Reboot

    /// <summary>
    /// Schedule a system reboot with optional delay
    /// </summary>
    public void ScheduleReboot(int delaySeconds = 10)
    {
        try
        {
            _logger?.LogWarning($"Scheduling system reboot in {delaySeconds} seconds");

            // Use shutdown command
            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = $"/r /t {delaySeconds} /c \"RamOptimizer Service Test Recovery\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to schedule reboot: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancel a pending reboot
    /// </summary>
    public void CancelReboot()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/a",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            
            _logger?.LogInformation("Cancelled pending reboot");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to cancel reboot: {ex.Message}");
        }
    }

    #endregion

    #region Critical Failure Handling

    /// <summary>
    /// Handle critical failure - save state and initiate recovery
    /// </summary>
    public async Task HandleCriticalFailure(string serviceName, Exception ex)
    {
        _logger?.LogCritical($"CRITICAL FAILURE testing service {serviceName}: {ex.Message}");

        // 1. Try to restart the problematic service
        var restarted = await RestartService(serviceName);
        
        if (!restarted)
        {
            // 2. Process rollback queue
            await ProcessRollbackQueue();
            
            // 3. Wait to see if system stabilizes
            await Task.Delay(5000);
            
            // 4. If still problematic, schedule reboot
            _logger?.LogCritical("System unstable - scheduling automatic reboot");
            ScheduleReboot(10);
        }
    }

    #endregion

    #region Recovery State Management

    /// <summary>
    /// Save recovery state to registry
    /// </summary>
    public void SaveRecoveryState(string serviceName, bool crashed)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(RegistryPath);
            if (key != null)
            {
                key.SetValue("LastService", serviceName);
                key.SetValue("Crashed", crashed ? 1 : 0);
                key.SetValue("Timestamp", DateTime.Now.ToString("O"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to save recovery state: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if there's a pending recovery
    /// </summary>
    public bool CheckPendingRecovery(out string? lastService, out bool crashed)
    {
        lastService = null;
        crashed = false;

        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryPath);
            if (key != null)
            {
                lastService = key.GetValue("LastService") as string;
                crashed = ((int?)key.GetValue("Crashed")) == 1;
                
                return !string.IsNullOrEmpty(lastService);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to check recovery state: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Clear recovery state
    /// </summary>
    public void ClearRecoveryState()
    {
        try
        {
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(RegistryPath, false);
        }
        catch
        {
            // Ignore errors
        }
    }

    #endregion
}
