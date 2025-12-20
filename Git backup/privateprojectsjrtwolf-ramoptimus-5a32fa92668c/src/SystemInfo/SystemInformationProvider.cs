using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using RamOptimizer.Logging;

namespace RamOptimizer.SystemInfo
{
    public class SystemInformationProvider
    {
        private readonly ComprehensiveLogger _logger;

        public SystemInformationProvider(ComprehensiveLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SystemInformation GetSystemInformation()
        {
            _logger.LogInfo("Gathering system information");
            
            var info = new SystemInformation
            {
                Timestamp = DateTime.UtcNow,
                OperatingSystem = GetOperatingSystemInfo(),
                Cpu = GetCpuInfo(),
                Memory = GetMemoryInfo(),
                Storage = GetStorageInfo(),
                Network = GetNetworkInfo(),
                Gpu = GetGpuInfo()
            };

            _logger.LogInfo("System information gathered successfully");
            return info;
        }

        private OperatingSystemInfo GetOperatingSystemInfo()
        {
            try
            {
                var osInfo = new OperatingSystemInfo
                {
                    Name = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.OSArchitecture.ToString(),
                    Version = Environment.OSVersion.Version.ToString()
                };

                // Get additional OS info using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        osInfo.InstallDate = ManagementDateTimeConverter.ToDateTime(obj["InstallDate"].ToString());
                        osInfo.LastBootUpTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                        osInfo.TotalVisibleMemorySizeMB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) / 1024;
                        osInfo.FreePhysicalMemoryMB = Convert.ToUInt64(obj["FreePhysicalMemory"]) / 1024;
                        osInfo.TotalVirtualMemorySizeMB = Convert.ToUInt64(obj["TotalVirtualMemorySize"]) / 1024;
                        osInfo.FreeVirtualMemoryMB = Convert.ToUInt64(obj["FreeVirtualMemory"]) / 1024;
                    }
                }

                return osInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get OS information: {ex.Message}");
                return new OperatingSystemInfo
                {
                    Name = Environment.OSVersion.ToString(),
                    Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86"
                };
            }
        }

        private CpuInfo GetCpuInfo()
        {
            try
            {
                var cpuInfo = new CpuInfo();

                // Get CPU info using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        cpuInfo.Name = obj["Name"]?.ToString() ?? "Unknown";
                        cpuInfo.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                        cpuInfo.NumberOfCores = Convert.ToUInt32(obj["NumberOfCores"]);
                        cpuInfo.NumberOfLogicalProcessors = Convert.ToUInt32(obj["NumberOfLogicalProcessors"]);
                        cpuInfo.MaxClockSpeedMHz = Convert.ToUInt32(obj["MaxClockSpeed"]);
                        cpuInfo.CurrentClockSpeedMHz = Convert.ToUInt32(obj["CurrentClockSpeed"]);
                        cpuInfo.L2CacheSizeKB = Convert.ToUInt32(obj["L2CacheSize"]);
                        cpuInfo.L3CacheSizeKB = Convert.ToUInt32(obj["L3CacheSize"]);
                    }
                }

                return cpuInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get CPU information: {ex.Message}");
                return new CpuInfo
                {
                    Name = "Unknown CPU",
                    NumberOfCores = (uint)Environment.ProcessorCount,
                    NumberOfLogicalProcessors = (uint)Environment.ProcessorCount
                };
            }
        }

        private MemoryInfo GetMemoryInfo()
        {
            try
            {
                var memoryInfo = new MemoryInfo();

                // Get memory info using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    ulong totalCapacity = 0;
                    var memoryModules = new List<MemoryModuleInfo>();

                    foreach (var obj in searcher.Get())
                    {
                        var module = new MemoryModuleInfo
                        {
                            CapacityMB = Convert.ToUInt64(obj["Capacity"]) / (1024 * 1024),
                            SpeedMHz = Convert.ToUInt32(obj["Speed"]),
                            Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                            PartNumber = obj["PartNumber"]?.ToString() ?? "Unknown"
                        };

                        memoryModules.Add(module);
                        totalCapacity += module.CapacityMB;
                    }

                    memoryInfo.Modules = memoryModules;
                    memoryInfo.TotalPhysicalMemoryMB = totalCapacity;
                }

                // Get current memory usage
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        memoryInfo.FreePhysicalMemoryMB = Convert.ToUInt64(obj["FreePhysicalMemory"]) / 1024;
                        memoryInfo.TotalVirtualMemoryMB = Convert.ToUInt64(obj["TotalVirtualMemorySize"]) / 1024;
                        memoryInfo.FreeVirtualMemoryMB = Convert.ToUInt64(obj["FreeVirtualMemory"]) / 1024;
                    }
                }

                return memoryInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get memory information: {ex.Message}");
                return new MemoryInfo
                {
                    TotalPhysicalMemoryMB = GetTotalPhysicalMemoryFallback()
                };
            }
        }

        private ulong GetTotalPhysicalMemoryFallback()
        {
            try
            {
                var gcMemory = GC.GetGCMemoryInfo();
                return (ulong)gcMemory.TotalAvailableMemoryBytes / (1024 * 1024);
            }
            catch
            {
                return (ulong)Environment.WorkingSet / (1024 * 1024);
            }
        }

        private List<StorageInfo> GetStorageInfo()
        {
            try
            {
                var storageInfo = new List<StorageInfo>();

                // Get drive info
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    var driveInfo = new StorageInfo
                    {
                        DriveLetter = drive.Name,
                        DriveType = drive.DriveType.ToString(),
                        FileSystem = drive.DriveFormat,
                        TotalSizeGB = drive.TotalSize / (1024L * 1024 * 1024),
                        FreeSpaceGB = drive.TotalFreeSpace / (1024L * 1024 * 1024),
                        AvailableSpaceGB = drive.AvailableFreeSpace / (1024L * 1024 * 1024),
                        VolumeLabel = drive.VolumeLabel
                    };

                    storageInfo.Add(driveInfo);
                }

                // Get additional storage info using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var model = obj["Model"]?.ToString() ?? "Unknown";
                        var deviceId = obj["DeviceID"]?.ToString() ?? "";

                        // Find matching drive in our list
                        var matchingDrive = storageInfo.FirstOrDefault(d => d.DriveLetter.StartsWith(deviceId.Substring(deviceId.Length - 1)));
                        if (matchingDrive != null)
                        {
                            matchingDrive.Model = model;
                            matchingDrive.InterfaceType = obj["InterfaceType"]?.ToString() ?? "Unknown";
                        }
                    }
                }

                return storageInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get storage information: {ex.Message}");
                return new List<StorageInfo>();
            }
        }

        private NetworkInfo GetNetworkInfo()
        {
            try
            {
                var networkInfo = new NetworkInfo
                {
                    Adapters = new List<NetworkAdapterInfo>()
                };

                // Get network adapter info
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var adapter in adapters)
                {
                    var adapterInfo = new NetworkAdapterInfo
                    {
                        Name = adapter.Name,
                        Description = adapter.Description,
                        Type = adapter.NetworkInterfaceType.ToString(),
                        SpeedMbps = (ulong)(adapter.Speed / 1000000),
                        OperationalStatus = adapter.OperationalStatus.ToString(),
                        MacAddress = adapter.GetPhysicalAddress().ToString(),
                        IpAddresses = new List<string>()
                    };

                    // Get IP addresses
                    var ipProperties = adapter.GetIPProperties();
                    foreach (var ip in ipProperties.UnicastAddresses)
                    {
                        adapterInfo.IpAddresses.Add(ip.Address.ToString());
                    }

                    networkInfo.Adapters.Add(adapterInfo);
                }

                return networkInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get network information: {ex.Message}");
                return new NetworkInfo();
            }
        }

        private List<GpuInfo> GetGpuInfo()
        {
            try
            {
                var gpuInfo = new List<GpuInfo>();

                // Get GPU info using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var gpu = new GpuInfo
                        {
                            Name = obj["Name"]?.ToString() ?? "Unknown GPU",
                            AdapterCompatibility = obj["AdapterCompatibility"]?.ToString() ?? "Unknown",
                            AdapterRAMMB = Convert.ToUInt64(obj["AdapterRAM"]) / (1024 * 1024),
                            DriverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown"
                        };

                        gpuInfo.Add(gpu);
                    }
                }

                return gpuInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get GPU information: {ex.Message}");
                return new List<GpuInfo>();
            }
        }

        public string GenerateSystemReport()
        {
            try
            {
                var info = GetSystemInformation();
                var report = $"System Information Report\n";
                report += $"========================\n";
                report += $"Generated: {info.Timestamp:yyyy-MM-dd HH:mm:ss}\n\n";

                // OS Information
                report += $"Operating System:\n";
                report += $"  Name: {info.OperatingSystem.Name}\n";
                report += $"  Architecture: {info.OperatingSystem.Architecture}\n";
                report += $"  Version: {info.OperatingSystem.Version}\n";
                report += $"  Installed: {info.OperatingSystem.InstallDate:yyyy-MM-dd}\n";
                report += $"  Last Boot: {info.OperatingSystem.LastBootUpTime:yyyy-MM-dd HH:mm:ss}\n";
                report += $"  Total Memory: {info.OperatingSystem.TotalVisibleMemorySizeMB} MB\n";
                report += $"  Free Memory: {info.OperatingSystem.FreePhysicalMemoryMB} MB\n\n";

                // CPU Information
                report += $"CPU Information:\n";
                report += $"  Name: {info.Cpu.Name}\n";
                report += $"  Manufacturer: {info.Cpu.Manufacturer}\n";
                report += $"  Cores: {info.Cpu.NumberOfCores}\n";
                report += $"  Logical Processors: {info.Cpu.NumberOfLogicalProcessors}\n";
                report += $"  Max Clock Speed: {info.Cpu.MaxClockSpeedMHz} MHz\n";
                report += $"  Current Clock Speed: {info.Cpu.CurrentClockSpeedMHz} MHz\n";
                report += $"  L2 Cache: {info.Cpu.L2CacheSizeKB} KB\n";
                report += $"  L3 Cache: {info.Cpu.L3CacheSizeKB} KB\n\n";

                // Memory Information
                report += $"Memory Information:\n";
                report += $"  Total Physical Memory: {info.Memory.TotalPhysicalMemoryMB} MB\n";
                report += $"  Free Physical Memory: {info.Memory.FreePhysicalMemoryMB} MB\n";
                report += $"  Total Virtual Memory: {info.Memory.TotalVirtualMemoryMB} MB\n";
                report += $"  Free Virtual Memory: {info.Memory.FreeVirtualMemoryMB} MB\n";
                report += $"  Memory Modules: {info.Memory.Modules.Count}\n";
                foreach (var module in info.Memory.Modules)
                {
                    report += $"    - {module.Manufacturer} {module.PartNumber} ({module.CapacityMB} MB @ {module.SpeedMHz} MHz)\n";
                }
                report += "\n";

                // Storage Information
                report += $"Storage Information:\n";
                foreach (var drive in info.Storage)
                {
                    report += $"  Drive {drive.DriveLetter} ({drive.VolumeLabel}):\n";
                    report += $"    Type: {drive.DriveType}\n";
                    report += $"    File System: {drive.FileSystem}\n";
                    report += $"    Model: {drive.Model}\n";
                    report += $"    Interface: {drive.InterfaceType}\n";
                    report += $"    Total Size: {drive.TotalSizeGB} GB\n";
                    report += $"    Free Space: {drive.FreeSpaceGB} GB\n";
                    report += $"    Available Space: {drive.AvailableSpaceGB} GB\n";
                }
                report += "\n";

                // Network Information
                report += $"Network Information:\n";
                foreach (var adapter in info.Network.Adapters)
                {
                    report += $"  Adapter: {adapter.Name}\n";
                    report += $"    Description: {adapter.Description}\n";
                    report += $"    Type: {adapter.Type}\n";
                    report += $"    Speed: {adapter.SpeedMbps} Mbps\n";
                    report += $"    Status: {adapter.OperationalStatus}\n";
                    report += $"    MAC Address: {adapter.MacAddress}\n";
                    report += $"    IP Addresses: {string.Join(", ", adapter.IpAddresses)}\n";
                }
                report += "\n";

                // GPU Information
                report += $"GPU Information:\n";
                foreach (var gpu in info.Gpu)
                {
                    report += $"  GPU: {gpu.Name}\n";
                    report += $"    Compatibility: {gpu.AdapterCompatibility}\n";
                    report += $"    Memory: {gpu.AdapterRAMMB} MB\n";
                    report += $"    Driver Version: {gpu.DriverVersion}\n";
                }

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to generate system report: {ex.Message}");
                return $"Failed to generate system report: {ex.Message}";
            }
        }

        public async System.Threading.Tasks.Task SaveSystemReportAsync(string filePath)
        {
            try
            {
                var report = GenerateSystemReport();
                await File.WriteAllTextAsync(filePath, report);
                _logger.LogInfo($"System report saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save system report: {ex.Message}");
                throw;
            }
        }
    }

    public class SystemInformation
    {
        public DateTime Timestamp { get; set; }
        public OperatingSystemInfo OperatingSystem { get; set; }
        public CpuInfo Cpu { get; set; }
        public MemoryInfo Memory { get; set; }
        public List<StorageInfo> Storage { get; set; }
        public NetworkInfo Network { get; set; }
        public List<GpuInfo> Gpu { get; set; }
    }

    public class OperatingSystemInfo
    {
        public string Name { get; set; }
        public string Architecture { get; set; }
        public string Version { get; set; }
        public DateTime InstallDate { get; set; }
        public DateTime LastBootUpTime { get; set; }
        public ulong TotalVisibleMemorySizeMB { get; set; }
        public ulong FreePhysicalMemoryMB { get; set; }
        public ulong TotalVirtualMemorySizeMB { get; set; }
        public ulong FreeVirtualMemoryMB { get; set; }
    }

    public class CpuInfo
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public uint NumberOfCores { get; set; }
        public uint NumberOfLogicalProcessors { get; set; }
        public uint MaxClockSpeedMHz { get; set; }
        public uint CurrentClockSpeedMHz { get; set; }
        public uint L2CacheSizeKB { get; set; }
        public uint L3CacheSizeKB { get; set; }
    }

    public class MemoryInfo
    {
        public ulong TotalPhysicalMemoryMB { get; set; }
        public ulong FreePhysicalMemoryMB { get; set; }
        public ulong TotalVirtualMemoryMB { get; set; }
        public ulong FreeVirtualMemoryMB { get; set; }
        public List<MemoryModuleInfo> Modules { get; set; } = new List<MemoryModuleInfo>();
    }

    public class MemoryModuleInfo
    {
        public ulong CapacityMB { get; set; }
        public uint SpeedMHz { get; set; }
        public string Manufacturer { get; set; }
        public string PartNumber { get; set; }
    }

    public class StorageInfo
    {
        public string DriveLetter { get; set; }
        public string DriveType { get; set; }
        public string FileSystem { get; set; }
        public string VolumeLabel { get; set; }
        public long TotalSizeGB { get; set; }
        public long FreeSpaceGB { get; set; }
        public long AvailableSpaceGB { get; set; }
        public string Model { get; set; }
        public string InterfaceType { get; set; }
    }

    public class NetworkInfo
    {
        public List<NetworkAdapterInfo> Adapters { get; set; } = new List<NetworkAdapterInfo>();
    }

    public class NetworkAdapterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public ulong SpeedMbps { get; set; }
        public string OperationalStatus { get; set; }
        public string MacAddress { get; set; }
        public List<string> IpAddresses { get; set; } = new List<string>();
    }

    public class GpuInfo
    {
        public string Name { get; set; }
        public string AdapterCompatibility { get; set; }
        public ulong AdapterRAMMB { get; set; }
        public string DriverVersion { get; set; }
    }
}