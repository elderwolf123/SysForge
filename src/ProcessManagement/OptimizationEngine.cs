using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.ProcessManagement
{
    public class OptimizationEngine
    {
        private readonly IProcessManager _processManager;
        private readonly SafetyEngine _safetyEngine;
        private HashSet<string> _dynamicExclusionList = new HashSet<string>();

        public OptimizationEngine(IProcessManager processManager, SafetyEngine safetyEngine)
        {
            _processManager = processManager;
            _safetyEngine = safetyEngine;
            _dynamicExclusionList = new HashSet<string>();
        }

        public async Task<OptimizationResult> OptimizeForTarget(OptimizationProfile profile)
        {
            // 1. Get all running processes
            var allProcesses = await _processManager.GetRunningProcessesAsync();

            // 2. Filter out critical and excluded processes
            var candidates = _safetyEngine.FilterSafeProcesses(allProcesses)
                .Where(p => !profile.TargetProcessNames.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase))
                .Where(p => !profile.TargetExecutables.Contains(p.ExecutablePath, StringComparer.OrdinalIgnoreCase))
                .Where(p => !_dynamicExclusionList.Contains(p.ProcessName));

            // 3. Sort by memory usage (highest first) and priority
            var sortedCandidates = candidates
                .OrderByDescending(p => p.MemoryUsage)
                .ThenBy(p => p.Priority);

            // 4. Terminate processes with graceful shutdown
            var terminatedProcesses = new List<ProcessInfo>();
            foreach (var process in sortedCandidates)
            {
                if (await _processManager.TerminateProcessAsync(process.ProcessId, process.ProcessName))
                {
                    terminatedProcesses.Add(process);
                }
            }

            return new OptimizationResult
            {
                TerminatedProcesses = terminatedProcesses,
                MemoryFreed = terminatedProcesses.Sum(p => p.MemoryUsage),
                OptimizationTime = DateTime.UtcNow
            };
        }

        public void AddToDynamicExclusionList(string processName)
        {
            _dynamicExclusionList.Add(processName);
        }

        public void RemoveFromDynamicExclusionList(string processName)
        {
            _dynamicExclusionList.Remove(processName);
        }
    }

    public class OptimizationResult
    {
        public List<ProcessInfo> TerminatedProcesses { get; set; }
        public long MemoryFreed { get; set; }
        public DateTime OptimizationTime { get; set; }
    }
}