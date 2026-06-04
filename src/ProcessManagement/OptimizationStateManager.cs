using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.ProcessManagement
{
    public class OptimizationStateManager
    {
        private readonly string _stateFilePath = "optimization_state.json";
        private readonly ILogger<OptimizationStateManager> _logger;

        public OptimizationStateManager(ILogger<OptimizationStateManager> logger)
        {
            _logger = logger;

            if (!File.Exists(_stateFilePath))
            {
                try
                {
                    File.Create(_stateFilePath).Close();
                    SaveDynamicExclusionList(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create optimization state file.");
                }
            }
        }

        public HashSet<string> GetDynamicExclusionList()
        {
            try
            {
                var json = File.ReadAllText(_stateFilePath);
                return new HashSet<string>(JsonConvert.DeserializeObject<HashSet<string>>(json) ?? System.Linq.Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read dynamic exclusion list.");
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public void SaveDynamicExclusionList(HashSet<string> dynamicExclusionList)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dynamicExclusionList);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save dynamic exclusion list.");
            }
        }
    }
}