using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RamOptimizer.ProcessManagement
{
    public class ProcessTerminationEngine
    {
        private readonly string _logFilePath = "process_termination_log.txt";
        private readonly string _exclusionListFilePath = "dynamic_exclusion_list.json";
        private readonly ILogger<ProcessTerminationEngine> _logger;
        private readonly IProcessManager _processManager;
        private readonly SafetyEngine _safetyEngine;
        private HashSet<string> _dynamicExclusionList;

        public ProcessTerminationEngine(IProcessManager processManager, SafetyEngine safetyEngine, ILogger<ProcessTerminationEngine> logger)
        {
            _processManager = processManager;
            _safetyEngine = safetyEngine;
            _logger = logger;
            _dynamicExclusionList = LoadDynamicExclusionList();
        }

        public void AddToDynamicExclusionList(string processName)
        {
            _dynamicExclusionList.Add(processName);
            SaveDynamicExclusionList();
        }

        public void RemoveFromDynamicExclusionList(string processName)
        {
            _dynamicExclusionList.Remove(processName);
            SaveDynamicExclusionList();
        }

        private HashSet<string> LoadDynamicExclusionList()
        {
            if (File.Exists(_exclusionListFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_exclusionListFilePath);
                    return new HashSet<string>(JsonConvert.DeserializeObject<HashSet<string>>(json) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load dynamic exclusion list.");
                }
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveDynamicExclusionList()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_dynamicExclusionList);
                File.WriteAllText(_exclusionListFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save dynamic exclusion list.");
            }
        }

        public async Task<TerminationResult> TerminateLevelAsync(int level, CancellationToken cancellationToken)
        {
            var ultraAggressiveStrategy = new UltraAggressiveTerminationStrategy();
            var processesToTerminate = ultraAggressiveStrategy.GetProcessesForLevel(level);
            
            var result = new TerminationResult
            {
                TerminatedProcesses = new List<ProcessInfo>(),
                MemoryFreed = 0,
                TerminationTime = DateTime.UtcNow
            };
            
            foreach (var processName in processesToTerminate)
            {
                if (_dynamicExclusionList.Contains(processName) || _safetyEngine.IsExcluded(processName))
                {
                    _logger.LogInformation($"Process {processName} is in the exclusion list and will not be terminated.");
                    continue;
                }
                
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        var processInfo = new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            MemoryUsage = process.WorkingSet64,
                            Priority = process.PriorityClass,
                            ExecutablePath = process.MainModule?.FileName ?? string.Empty
                        };
                        
                        if (await _processManager.TerminateProcessAsync(process.Id, process.ProcessName))
                        {
                            result.TerminatedProcesses.Add(processInfo);
                            result.MemoryFreed += processInfo.MemoryUsage;
                            _logger.LogInformation($"Process {processName} (ID: {process.Id}) terminated.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to terminate process {processName}.");
                    }
                }
            }
            
            return result;
        }

        public async Task<bool> RecoverProcessAsync(string processName, CancellationToken cancellationToken)
        {
            try
            {
                // This is a simplified recovery mechanism
                // In a real implementation, we would need to store the executable path before termination
                return await _processManager.RestoreProcessAsync(processName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to recover process {processName}.");
                return false;
            }
        }
    }
    
    public class TerminationResult
    {
        public List<ProcessInfo> TerminatedProcesses { get; set; }
        public long MemoryFreed { get; set; }
        public DateTime TerminationTime { get; set; }
    }
}
