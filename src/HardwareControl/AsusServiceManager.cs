using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Linq;

namespace RamOptimizer.HardwareControl
{
    /// <summary>
    /// Manages ASUS services that may conflict with hardware control
    /// </summary>
    public class AsusServiceManager
    {
        // ASUS services that can override battery limit and other settings
        private static readonly string[] ConflictingServices = new[]
        {
            "ASUSOptimization",           // ASUS Optimization Service
            "ASUSSystemAnalysis",         // ASUS System Analysis
            "ASUSSystemDiagnosis",        // ASUS System Diagnosis
            "ASUSSoftwareManager",        // ASUS Software Manager
            "ArmouryCrateControlInterface" // Armoury Crate Control
        };

        /// <summary>
        /// Check if any conflicting ASUS services are running
        /// </summary>
        public static bool AreAsusServicesRunning()
        {
            try
            {
                var services = ServiceController.GetServices();
                return ConflictingServices.Any(serviceName =>
                {
                    var service = services.FirstOrDefault(s => 
                        s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                    return service != null && service.Status == ServiceControllerStatus.Running;
                });
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get status of all ASUS services
        /// </summary>
        public static string GetServicesStatus()
        {
            try
            {
                var services = ServiceController.GetServices();
                var status = new System.Text.StringBuilder();
                
                foreach (var serviceName in ConflictingServices)
                {
                    var service = services.FirstOrDefault(s => 
                        s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                    
                    if (service != null)
                    {
                        status.AppendLine($"{serviceName}: {service.Status}");
                    }
                }

                return status.Length > 0 ? status.ToString() : "No ASUS services found";
            }
            catch (Exception ex)
            {
                return $"Error checking services: {ex.Message}";
            }
        }

        /// <summary>
        /// Stop all conflicting ASUS services
        /// Requires administrator privileges
        /// </summary>
        public static void StopAsusServices()
        {
            if (!IsAdministrator())
                throw new UnauthorizedAccessException("Administrator privileges required to stop services");

            try
            {
                var services = ServiceController.GetServices();
                
                foreach (var serviceName in ConflictingServices)
                {
                    var service = services.FirstOrDefault(s => 
                        s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                    
                    if (service != null && service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to stop ASUS services: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disable ASUS services from auto-starting
        /// Requires administrator privileges
        /// </summary>
        public static void DisableAsusServices()
        {
            if (!IsAdministrator())
                throw new UnauthorizedAccessException("Administrator privileges required to disable services");

            try
            {
                foreach (var serviceName in ConflictingServices)
                {
                    // Use sc.exe to disable services
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"config {serviceName} start= disabled",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to disable ASUS services: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if running as administrator
        /// </summary>
        private static bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
