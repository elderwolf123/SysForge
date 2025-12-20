using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RamOptimizer.ProcessManagement
{
    public class GpuOptimizer
    {
        private const string StateFilePath = "gpu_optimizer_state.json";
        private int currentAggressionLevel;
        private List<string> terminatedProcesses;
        private List<string> exclusionList;
        private readonly object _lockObject = new object();

        public GpuOptimizer()
        {
            LoadState();
            InitializeExclusionList();
        }

        private void InitializeExclusionList()
        {
            exclusionList = new List<string>
            {
                "kernel32.dll",
                "ntoskrnl.exe",
                "hal.dll",
                "win32k.sys",
                "csrss.exe",
                "winlogon.exe",
                "services.exe",
                "lsass.exe",
                "smss.exe",
                "wininit.exe",
                "dwm.exe",
                "explorer.exe"
            };
        }

        public async Task OptimizeAsync()
        {
            lock (_lockObject)
            {
                // Implement dynamic clock speed adjustment and power management
                AdjustGpuClockSpeed();
                ManagePower();

                // Implement aggression levels
                ApplyAggressionLevel();

                // Save the current state
                SaveState();
            }
        }

        private void AdjustGpuClockSpeed()
        {
            try
            {
                // Placeholder for GPU clock speed adjustment logic
                // In a real implementation, this would interact with GPU drivers
                Console.WriteLine("Adjusting GPU clock speed...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to adjust GPU clock speed: {ex.Message}");
            }
        }

        private void ManagePower()
        {
            try
            {
                // Placeholder for GPU power management logic
                // In a real implementation, this would interact with GPU drivers
                Console.WriteLine("Managing GPU power...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to manage GPU power: {ex.Message}");
            }
        }

        private void ApplyAggressionLevel()
        {
            try
            {
                Console.WriteLine($"Applying aggression level {currentAggressionLevel}...");
                
                // Define aggression levels and corresponding processes to terminate
                var aggressionLevels = new Dictionary<int, List<string>>
                {
                    { 0, new List<string> { "nvidia-smi.exe" } },
                    { 1, new List<string> { "dxdiag.exe" } },
                    { 2, new List<string> { "directx.exe" } }
                };
            
                if (aggressionLevels.ContainsKey(currentAggressionLevel))
                {
                    foreach (var processName in aggressionLevels[currentAggressionLevel])
                    {
                        if (!exclusionList.Contains(processName))
                        {
                            TerminateProcess(processName);
                        }
                    }
                }
            
                currentAggressionLevel = (currentAggressionLevel + 1) % 3; // Cycle through levels 0-2
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply aggression level: {ex.Message}");
            }
        }

        private void TerminateProcess(string processName)
        {
            try
            {
                Console.WriteLine($"Terminating process: {processName}");
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    // Check if process is in exclusion list
                    if (exclusionList.Contains(process.ProcessName.ToLower()))
                    {
                        Console.WriteLine($"Skipping termination of protected process: {process.ProcessName}");
                        continue;
                    }
                    
                    process.Kill();
                    terminatedProcesses.Add(processName);
                    Console.WriteLine($"Process {processName} (ID: {process.Id}) terminated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to terminate process {processName}: {ex.Message}");
            }
        }

        private void LoadState()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    string stateJson = File.ReadAllText(StateFilePath);
                    var state = JsonConvert.DeserializeObject<OptimizerState>(stateJson);
                    currentAggressionLevel = state.CurrentAggressionLevel;
                    terminatedProcesses = state.TerminatedProcesses ?? new List<string>();
                    Console.WriteLine("Loading optimizer state...");
                }
                else
                {
                    currentAggressionLevel = 0;
                    terminatedProcesses = new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load optimizer state: {ex.Message}");
                currentAggressionLevel = 0;
                terminatedProcesses = new List<string>();
            }
        }

        private void SaveState()
        {
            try
            {
                var state = new OptimizerState
                {
                    CurrentAggressionLevel = currentAggressionLevel,
                    TerminatedProcesses = terminatedProcesses
                };
                string stateJson = JsonConvert.SerializeObject(state, Formatting.Indented);
                File.WriteAllText(StateFilePath, stateJson);
                Console.WriteLine("Saving optimizer state...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save optimizer state: {ex.Message}");
            }
        }

        public void RecoverTerminatedProcesses()
        {
            try
            {
                foreach (var processName in terminatedProcesses)
                {
                    try
                    {
                        // Attempt to restart the process
                        Process.Start(processName);
                        Console.WriteLine($"Recovered process: {processName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to recover process {processName}: {ex.Message}");
                    }
                }
                
                // Clear the terminated processes list
                terminatedProcesses.Clear();
                SaveState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to recover terminated processes: {ex.Message}");
            }
        }

        private class OptimizerState
        {
            public int CurrentAggressionLevel { get; set; }
            public List<string> TerminatedProcesses { get; set; }
        }
    }
}