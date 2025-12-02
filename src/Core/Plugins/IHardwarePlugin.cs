using System;
using System.Collections.Generic;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.Core.Plugins
{
    /// <summary>
    /// Interface for hardware controller plugins
    /// Allows extending to support different manufacturers
    /// </summary>
    public interface IHardwarePlugin
    {
        /// <summary>
        /// Unique plugin identifier
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Human-readable plugin name
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        Version PluginVersion { get; }

        /// <summary>
        /// Supported device manufacturers
        /// </summary>
        string[] SupportedManufacturers { get; }

        /// <summary>
        /// Check if this plugin can handle the current system
        /// </summary>
        bool CanHandle();

        /// <summary>
        /// Create a hardware controller instance
        /// </summary>
        IHardwareController CreateController();

        /// <summary>
        /// Get plugin capabilities
        /// </summary>
        PluginCapabilities GetCapabilities();
    }

    /// <summary>
    /// Plugin capabilities flags
    /// </summary>
    [Flags]
    public enum PluginCapabilities
    {
        None = 0,
        CoreControl = 1 << 0,
        BatteryControl = 1 << 1,
        PerformanceControl = 1 << 2,
        FanControl = 1 << 3,
        TemperatureMonitoring = 1 << 4,
        RGBControl = 1 << 5,
        PowerManagement = 1 << 6,
        All = CoreControl | BatteryControl | PerformanceControl | 
              FanControl | TemperatureMonitoring | RGBControl | PowerManagement
    }

    /// <summary>
    /// Plugin registry for managing hardware controller plugins
    /// </summary>
    public interface IPluginRegistry
    {
        /// <summary>
        /// Register a plugin
        /// </summary>
        void RegisterPlugin(IHardwarePlugin plugin);

        /// <summary>
        /// Unregister a plugin
        /// </summary>
        void UnregisterPlugin(string pluginId);

        /// <summary>
        /// Get all registered plugins
        /// </summary>
        IReadOnlyList<IHardwarePlugin> GetAllPlugins();

        /// <summary>
        /// Get plugin by ID
        /// </summary>
        IHardwarePlugin GetPlugin(string pluginId);

        /// <summary>
        /// Find best plugin for current system
        /// </summary>
        IHardwarePlugin FindBestPlugin();

        /// <summary>
        /// Find plugins that support a specific capability
        /// </summary>
        IReadOnlyList<IHardwarePlugin> FindPluginsByCapability(PluginCapabilities capability);
    }

    /// <summary>
    /// Base class for hardware plugins
    /// </summary>
    public abstract class HardwarePluginBase : IHardwarePlugin
    {
        public abstract string PluginId { get; }
        public abstract string PluginName { get; }
        public abstract Version PluginVersion { get; }
        public abstract string[] SupportedManufacturers { get; }

        public abstract bool CanHandle();
        public abstract IHardwareController CreateController();
        public abstract PluginCapabilities GetCapabilities();

        protected bool IsManufacturerMatch(string manufacturer)
        {
            foreach (var supported in SupportedManufacturers)
            {
                if (manufacturer.Contains(supported, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        protected string GetSystemManufacturer()
        {
            try
            {
                // Try WMI first
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Manufacturer FROM Win32_ComputerSystem");
                
                foreach (var obj in searcher.Get())
                {
                    return obj["Manufacturer"]?.ToString() ?? "Unknown";
                }
            }
            catch
            {
                // Fallback to registry
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"HARDWARE\DESCRIPTION\System\BIOS");
                    return key?.GetValue("SystemManufacturer")?.ToString() ?? "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }

            return "Unknown";
        }
    }
}
