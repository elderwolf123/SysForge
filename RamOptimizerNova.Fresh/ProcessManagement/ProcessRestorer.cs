using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.ProcessManagement
{
    public class ProcessRestorer
    {
        private readonly IProcessManager _processManager;
        private readonly ILogger _logger;

        public ProcessRestorer(IProcessManager processManager, ILogger logger)
        {
            _processManager = processManager;
            _logger = logger;
        }

        public async Task<RestoreResult> RestoreProcesses(List<ProcessInfo> terminatedProcesses)
        {
            var restored = new List<ProcessInfo>();
            var failed = new List<ProcessInfo>();

            foreach (var process in terminatedProcesses)
            {
                try
                {
                    if (await _processManager.RestoreProcessAsync(process.ExecutablePath))
                    {
                        restored.Add(process);
                    }
                    else
                    {
                        failed.Add(process);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to restore {process.ProcessName}: {ex.Message}");
                    failed.Add(process);
                }
            }

            return new RestoreResult { Restored = restored, Failed = failed };
        }
    }

    public class RestoreResult
    {
        public List<ProcessInfo> Restored { get; set; }
        public List<ProcessInfo> Failed { get; set; }
    }
}