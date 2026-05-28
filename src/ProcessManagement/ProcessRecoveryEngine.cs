using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using RamOptimizer.ProcessManagement;
using System.ServiceProcess;

namespace RamOptimizer.ProcessManagement
{
    public class ProcessRecoveryEngine
    {
        private Dictionary<string, List<RecoveryStrategy>> recoveryStrategies;

        public ProcessRecoveryEngine()
        {
            recoveryStrategies = new Dictionary<string, List<RecoveryStrategy>>
            {
                { "explorer.exe", new List<RecoveryStrategy> { new RestartProcessStrategy() } },
                { "svchost.exe", new List<RecoveryStrategy> { new RestartServiceStrategy("Spooler") } },
                { "wuauserv", new List<RecoveryStrategy> { new RestartServiceStrategy("wuauserv") } },
                { "bits", new List<RecoveryStrategy> { new RestartServiceStrategy("bits") } },
                // Add more recovery strategies as needed
            };
        }

        public async Task RecoverProcessAsync(string processName, CancellationToken cancellationToken)
        {
            if (recoveryStrategies.TryGetValue(processName.ToLower(), out var strategies))
            {
                foreach (var strategy in strategies)
                {
                    await strategy.RecoverAsync(cancellationToken);
                }
            }
            else
            {
                Console.WriteLine($"No recovery strategy found for process: {processName}");
            }
        }
    }

    public abstract class RecoveryStrategy
    {
        public abstract Task RecoverAsync(CancellationToken cancellationToken);
    }

    public class RestartProcessStrategy : RecoveryStrategy
    {
        public override async Task RecoverAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Security: Use absolute path for explorer to prevent path hijacking
                var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                Process.Start(new ProcessStartInfo(explorerPath) { UseShellExecute = false, CreateNoWindow = true });
                Console.WriteLine("Process restarted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restart process: {ex.Message}");
            }
        }
    }

    public class RestartServiceStrategy : RecoveryStrategy
    {
        private string serviceName;

        public RestartServiceStrategy(string serviceName)
        {
            this.serviceName = serviceName;
        }

        public override async Task RecoverAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (ServiceController service = new ServiceController(serviceName))
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)), cancellationToken);
                    Console.WriteLine($"Service {serviceName} restarted successfully.");
                }
                else
                {
                    Console.WriteLine($"Service {serviceName} is already running.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restart service {serviceName}: {ex.Message}");
            }
        }
    }
}