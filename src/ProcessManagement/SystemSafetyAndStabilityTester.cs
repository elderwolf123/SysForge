using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ProcessManagement
{
    public class SystemSafetyAndStabilityTester
    {
        public void RunSafetyChecks()
        {
            Console.WriteLine("Running safety checks...");

            CheckCpuUsage();
            CheckMemoryUsage();
            CheckDiskSpace();
            CheckProcessHealth();
        }

        private void CheckCpuUsage()
        {
            double cpuUsage = GetCpuUsage();
            if (cpuUsage > 80)
            {
                Console.WriteLine($"High CPU usage detected: {cpuUsage}%");
            }
            else
            {
                Console.WriteLine($"CPU usage is normal: {cpuUsage}%");
            }
        }

        private void CheckMemoryUsage()
        {
            double memoryUsage = GetMemoryUsage();
            if (memoryUsage > 80)
            {
                Console.WriteLine($"High memory usage detected: {memoryUsage}%");
            }
            else
            {
                Console.WriteLine($"Memory usage is normal: {memoryUsage}%");
            }
        }

        private void CheckDiskSpace()
        {
            double diskSpace = GetDiskSpace();
            if (diskSpace < 10)
            {
                Console.WriteLine($"Low disk space detected: {diskSpace}%");
            }
            else
            {
                Console.WriteLine($"Disk space is sufficient: {diskSpace}%");
            }
        }

        private void CheckProcessHealth()
        {
            var processes = Process.GetProcesses();
            if (processes.Any(p => p.HasExited))
            {
                Console.WriteLine("Some processes have exited unexpectedly.");
            }
            else
            {
                Console.WriteLine("All processes are running normally.");
            }
        }

        private double GetCpuUsage()
        {
            // Placeholder for CPU usage logic
            // For example, use PerformanceCounter to get CPU usage
            // Placeholder logic for demonstration purposes
            return 45; // Example CPU usage
        }

        private double GetMemoryUsage()
        {
            // Placeholder for memory usage logic
            // For example, use PerformanceCounter to get memory usage
            // Placeholder logic for demonstration purposes
            return 55; // Example memory usage
        }

        private double GetDiskSpace()
        {
            // Placeholder for disk space logic
            // For example, use DriveInfo to get disk space
            // Placeholder logic for demonstration purposes
            return 20; // Example disk space
        }

        public void RunStabilityTests()
        {
            Console.WriteLine("Running stability tests...");

            SimulateHighLoad();
            SimulateLowMemory();
            SimulateDiskFull();
        }

        private void SimulateHighLoad()
        {
            Console.WriteLine("Simulating high CPU load...");
            // Placeholder for high load simulation logic
        }

        private void SimulateLowMemory()
        {
            Console.WriteLine("Simulating low memory...");
            // Placeholder for low memory simulation logic
        }

        private void SimulateDiskFull()
        {
            Console.WriteLine("Simulating disk full...");
            // Placeholder for disk full simulation logic
        }

        public void LogTestResults()
        {
            Console.WriteLine("Logging test results...");

            // Placeholder for logging test results
            // For example, write results to a file or database
        }
    }
}