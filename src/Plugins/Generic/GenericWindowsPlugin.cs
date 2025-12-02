using System;
using RamOptimizer.Core.Interfaces;
using RamOptimizer.Core.Plugins;

namespace RamOptimizer.Plugins.Generic;

/// <summary>
/// Generic Windows plugin for systems without manufacturer-specific drivers
/// Provides read-only monitoring via WMI
/// </summary>
public class GenericWindowsPlugin : HardwarePluginBase
{
    public override string PluginId => "generic.windows";
    public override string PluginName => "Generic Windows Plugin";
    public override Version PluginVersion => new Version(1, 0, 0);
    public override string[] SupportedManufacturers => Array.Empty<string>(); // Supports all manufacturers

    public override bool CanHandle()
    {
        // This is a fallback plugin - it can handle any Windows system
        // But it should be lower priority than manufacturer-specific plugins
        try
        {
            using var controller = new GenericWindowsController();
            return controller.IsAvailable();
        }
        catch
        {
            return false;
        }
    }

    public override IHardwareController CreateController()
    {
        return new GenericWindowsController();
    }

    public override PluginCapabilities GetCapabilities()
    {
        // Read-only monitoring capabilities
        return PluginCapabilities.TemperatureMonitoring; // Limited capabilities
    }
}
