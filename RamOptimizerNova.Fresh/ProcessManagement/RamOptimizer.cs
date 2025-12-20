using System;
using System.Collections.Generic;
using RamOptimizer.ProcessManagement;
using Microsoft.Extensions.Logging;

namespace RamOptimizer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Ram Optimizer started.");
            // Initialize components
            var criticalProcesses = new List<string> { "system", "csrss", "winlogon", "services", "lsass", "smss" };
            var dynamicExclusionList = new List<string> { "explorer" };
            var processManager = new ProcessManager(criticalProcesses, dynamicExclusionList);
            var safetyEngine = new SafetyEngine();
            
            // Create a simple logger
            var logger = new LoggerFactory().CreateLogger<ProcessTerminationEngine>();
            
            var processTerminationEngine = new ProcessTerminationEngine(processManager, safetyEngine, logger);
            
            // Start optimization process - terminate level 1 processes
            var cancellationToken = new System.Threading.CancellationToken();
            var result = processTerminationEngine.TerminateLevelAsync(1, cancellationToken).Result;
            
            Console.WriteLine($"Terminated {result.TerminatedProcesses.Count} processes, freed {result.MemoryFreed} bytes");
        }
    }
}