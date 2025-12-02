using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.Core.Plugins
{
    /// <summary>
    /// Plugin registry implementation
    /// Manages all hardware controller plugins
    /// </summary>
    public class PluginRegistry : IPluginRegistry
    {
        private readonly Dictionary<string, IHardwarePlugin> _plugins = new();
        private readonly ILogger<PluginRegistry> _logger;

        public PluginRegistry(ILogger<PluginRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterPlugin(IHardwarePlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            if (_plugins.ContainsKey(plugin.PluginId))
            {
                _logger.LogWarning("Plugin {PluginId} already registered, replacing", plugin.PluginId);
            }

            _plugins[plugin.PluginId] = plugin;
            _logger.LogInformation("Registered plugin: {PluginName} v{Version} ({PluginId})",
                plugin.PluginName, plugin.PluginVersion, plugin.PluginId);
        }

        public void UnregisterPlugin(string pluginId)
        {
            if (_plugins.Remove(pluginId))
            {
                _logger.LogInformation("Unregistered plugin: {PluginId}", pluginId);
            }
            else
            {
                _logger.LogWarning("Plugin {PluginId} not found for unregistration", pluginId);
            }
        }

        public IReadOnlyList<IHardwarePlugin> GetAllPlugins()
        {
            return _plugins.Values.ToList().AsReadOnly();
        }

        public IHardwarePlugin GetPlugin(string pluginId)
        {
            return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }

        public IHardwarePlugin FindBestPlugin()
        {
            _logger.LogInformation("Searching for best plugin for current system...");

            // First, find all plugins that can handle this system
            var compatiblePlugins = _plugins.Values
                .Where(p => p.CanHandle())
                .ToList();

            if (!compatiblePlugins.Any())
            {
                _logger.LogWarning("No compatible plugins found for this system");
                return null;
            }

            _logger.LogInformation("Found {Count} compatible plugin(s)", compatiblePlugins.Count);

            // Prioritize by capabilities (more capabilities = better)
            var bestPlugin = compatiblePlugins
                .OrderByDescending(p => CountCapabilities(p.GetCapabilities()))
                .ThenByDescending(p => p.PluginVersion)
                .First();

            _logger.LogInformation("Best plugin: {PluginName} v{Version}", 
                bestPlugin.PluginName, bestPlugin.PluginVersion);

            return bestPlugin;
        }

        public IReadOnlyList<IHardwarePlugin> FindPluginsByCapability(PluginCapabilities capability)
        {
            return _plugins.Values
                .Where(p => (p.GetCapabilities() & capability) == capability)
                .ToList()
                .AsReadOnly();
        }

        private int CountCapabilities(PluginCapabilities capabilities)
        {
            int count = 0;
            foreach (PluginCapabilities value in Enum.GetValues(typeof(PluginCapabilities)))
            {
                if (value != PluginCapabilities.None && 
                    value != PluginCapabilities.All &&
                    (capabilities & value) == value)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
