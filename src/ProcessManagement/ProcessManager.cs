using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.ProcessManagement
{
    public class ProcessManager : IProcessManager
    {
        private readonly HashSet<string> _criticalProcesses;
        private readonly HashSet<string> _dynamicExclusionList;

        public ProcessManager(IEnumerable<string> criticalProcesses, IEnumerable<string> dynamicExclusionList)
        {
            _criticalProcesses = new HashSet<string>(criticalProcesses, StringComparer.OrdinalIgnoreCase);
            _dynamicExclusionList = new HashSet<string>(dynamicExclusionList, StringComparer.OrdinalIgnoreCase);
        }

        public event EventHandler<ProcessEventArgs> ProcessTerminated;
        public event EventHandler<ProcessEventArgs> ProcessRestored;

        public async Task<List<ProcessInfo>> GetRunningProcessesAsync()
        {
            var processes = Process.GetProcesses();
            var processInfos = new List<ProcessInfo>(processes.Length);

            foreach (var process in processes)
            {
                try
                {
                    processInfos.Add(new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        ExecutablePath = process.MainModule?.FileName ?? string.Empty,
                        MemoryUsage = process.WorkingSet64,
                        Priority = process.PriorityClass
                    });
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Failed to get process info for {process.ProcessName}: {ex.Message}");
                }
            }

            return processInfos;
        }

        public async Task<bool> TerminateProcessAsync(int processId, string processName)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                if (_criticalProcesses.Contains(process.ProcessName) || _dynamicExclusionList.Contains(process.ProcessName))
                {
                    Console.WriteLine($"Process {process.ProcessName} (ID: {processId}) is in the exclusion list and will not be terminated.");
                    return false;
                }

                process.Kill();
                process.WaitForExit();
                LogAction($"Terminated process: {process.ProcessName} (ID: {processId})");
                ProcessTerminated?.Invoke(this, new ProcessEventArgs(process.ProcessName, processId));
                return true;
            }
            catch (Exception ex)
            {
                LogAction($"Failed to terminate process with ID {processId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreProcessAsync(string executablePath)
        {
            try
            {
                var psi = new ProcessStartInfo(executablePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                LogAction($"Restored process: {executablePath}");
                ProcessRestored?.Invoke(this, new ProcessEventArgs(Path.GetFileNameWithoutExtension(executablePath), 0));
                return true;
            }
            catch (Exception ex)
            {
                LogAction($"Failed to restore process {executablePath}: {ex.Message}");
                return false;
            }
        }

        private void LogAction(string message)
        {
            // Implement logging as needed
            Console.WriteLine(message);
        }
    }

    public class ProcessEventArgs : EventArgs
    {
        public string ProcessName { get; }
        public int ProcessId { get; }

        public ProcessEventArgs(string processName, int processId)
        {
            ProcessName = processName;
            ProcessId = processId;
        }
    }
}