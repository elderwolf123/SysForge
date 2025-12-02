using System;
using RamOptimizer.Core.Interfaces;
using RamOptimizer.Core.Plugins;

namespace RamOptimizer.Plugins.Asus;

/// <summary>
/// Plugin for ASUS ROG laptops
/// </summary>
public class AsusRogPlugin : HardwarePluginBase
{
    public override string PluginId => "asus.rog";
    public override string PluginName => "ASUS ROG Plugin";
    public override Version PluginVersion => new Version(1, 0, 0);
    public override string[] SupportedManufacturers => new[] { "ASUSTeK COMPUTER INC.", "ASUS" };

    public override bool CanHandle()
    {
        // Check manufacturer
        string manufacturer = GetSystemManufacturer();
        if (!IsManufacturerMatch(manufacturer))
        {
            return false;
        }

        // Check if ATKACPI driver is available
        try
        {
            using var controller = new AsusHardwareController();
            return controller.IsAvailable();
        }
        catch
        {
            return false;
        }
    }

    public override IHardwareController CreateController()
    {
        return new AsusHardwareController();
    }

    public override PluginCapabilities GetCapabilities()
    {
        return PluginCapabilities.CoreControl | 
               PluginCapabilities.BatteryControl | 
               PluginCapabilities.PerformanceControl |
               PluginCapabilities.FanControl; // Assuming fan control is supported via performance modes or similar
    }
}
