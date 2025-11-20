using System;
using System.Threading.Tasks;
using RamOptimizer.Logging;
using RamOptimizer.Monitoring;
using RamOptimizer.ProcessManagement;
using RamOptimizer.SystemTray;

namespace RamOptimizer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("RAM Optimizer Starting...");

            // Initialize logger
            using var logger = new ComprehensiveLogger("logs/ram_optimizer.log", LogLevel.Debug);
            logger.LogInfo("Application started");

            // Initialize system tray manager
            using var systemTray = new SimpleSystemTrayManager();
            systemTray.OptimizationRequested += async (sender, e) => await HandleOptimizationRequest(logger);
            systemTray.ExitRequested += (sender, e) => HandleExitRequest();
            systemTray.UpdateStatus("Running");

            // Initialize performance monitor
            using var performanceMonitor = new RealTimePerformanceMonitor(2000); // Update every 2 seconds
            performanceMonitor.PerformanceMetricsUpdated += (sender, e) => 
            {
                logger.LogDebug($"CPU: {e.CpuUsage:F2}%, Memory: {e.AvailableMemoryMB:F2}MB, Disk: {e.DiskUsage:F2}%");
                systemTray.UpdateStatus($"CPU: {e.CpuUsage:F2}%");
            };
            performanceMonitor.StartMonitoring();

            // Initialize CPU usage pattern monitor
            using var cpuPatternMonitor = new CpuUsagePatternMonitor(1000, 60); // 1 second interval, 60 samples
            cpuPatternMonitor.CpuUsagePatternUpdated += (sender, e) => 
            {
                logger.LogInfo($"CPU Pattern: {e.Pattern}, Trend: {e.Trend}, Avg: {e.AverageUsage:F2}%");
            };
            cpuPatternMonitor.StartMonitoring();

            // Initialize GPU optimizer
            var gpuOptimizer = new GpuResourceOptimizer();

            // Initialize system stability tester
            var stabilityTester = new SystemStabilityTester(new ComprehensiveLoggerAdapter<SystemStabilityTester>(logger));

            // Initialize process manager
            var criticalProcesses = new List<string> { "system", "idle", "csrss.exe", "smss.exe", "wininit.exe", "winlogon.exe", "services.exe", "lsass.exe", "lsm.exe", "explorer.exe", "dwm.exe", "taskhost.exe", "taskhostw.exe", "svchost.exe", "spoolsv.exe", "audiodg.exe", "fontdrvhost.exe", "ctfmon.exe", "conhost.exe", "dllhost.exe", "sihost.exe", "runtimebroker.exe", "taskeng.exe", "wlanext.exe", "wudfhost.exe", "searchindexer.exe", "searchprotocolhost.exe", "searchfilterhost.exe", "dashost.exe", "wmpnetwk.exe", "microsoftedge.exe", "microsoftedgecp.exe", "backgroundtaskhost.exe", "shellexperiencehost.exe", "windowsinternal.composableshell.experiences.textinput.inputapp.exe" };
            var dynamicExclusionList = new List<string>();
            var processManager = new ProcessManager(criticalProcesses, dynamicExclusionList);

            // Initialize safety engine
            var safetyEngine = new SafetyEngine();

            // Initialize logger for process termination engine
            var terminationEngineLogger = new ComprehensiveLoggerAdapter<ProcessTerminationEngine>(logger);

            // Initialize process termination engine
            var terminationEngine = new ProcessTerminationEngine(processManager, safetyEngine, terminationEngineLogger);

            logger.LogInfo("All components initialized successfully");

            // Show initial notification
            systemTray.ShowNotification("RAM Optimizer", "Application started successfully");

            // Run for 30 seconds for demonstration
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                
                // Check system stability every 5 seconds
                if (i % 5 == 0)
                {
                    var isStable = stabilityTester.IsSystemStable();
                    logger.LogInfo($"System stability check: {(isStable ? "Stable" : "Unstable")}");
                }

                // Perform GPU optimization every 10 seconds
                if (i % 10 == 0)
                {
                    await gpuOptimizer.OptimizeGpuResourcesAsync();
                    logger.LogInfo("GPU resources optimized");
                }

                // Terminate some processes every 15 seconds
                if (i % 15 == 0)
                {
                    terminationEngine.TerminateProcess("notepad");
                    logger.LogInfo("Process termination executed");
                }

                Console.WriteLine($"Running... ({i + 1}/30 seconds)");
            }

            // Clean up
            logger.LogInfo("Application shutting down");
            systemTray.ShowNotification("RAM Optimizer", "Application shutting down");
        }

        static async Task HandleOptimizationRequest(ComprehensiveLogger logger)
        {
            logger.LogInfo("Optimization requested from system tray");
            // In a real implementation, this would trigger the optimization process
            await Task.Delay(100); // Simulate work
            logger.LogInfo("Optimization completed");
        }

        static void HandleExitRequest()
        {
            Console.WriteLine("Exit requested from system tray");
            Environment.Exit(0);
        }
    }
}