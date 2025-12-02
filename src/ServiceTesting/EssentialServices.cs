namespace RamOptimizer.ServiceTesting;

/// <summary>
/// Essential Windows services that should NEVER be tested
/// </summary>
public static class EssentialServices
{
    private static readonly HashSet<string> _essential = new(StringComparer.OrdinalIgnoreCase)
    {
        // Critical system processes
        "csrss",              // Client/Server Runtime Subsystem
        "services",           // Service Control Manager
        "lsass",              // Local Security Authority Subsystem
        "winlogon",           // Windows Logon Process
        "smss",               // Session Manager Subsystem
        "wininit",            // Windows Start-Up Application
        "System",             // System Process
        
        // Critical for stability and security
        "DcomLaunch",         // DCOM Server Process Launcher
        "RpcSs",              // Remote Procedure Call (RPC)
        "RpcEptMapper",       // RPC Endpoint Mapper
        "EventLog",           // Windows Event Log
        "PlugPlay",           // Plug and Play
        "Power",              // Power
        "ProfSvc",            // User Profile Service
        "SamSs",              // Security Accounts Manager
        "Schedule",           // Task Scheduler
        "SENS",               // System Event Notification Service
        "Themes",             // Themes (required for UI)
        "UserManager",        // User Manager
        "Winmgmt",            // Windows Management Instrumentation
        
        // Core networking
        "Dhcp",               // DHCP Client
        "Dnscache",           // DNS Client
        "LanmanServer",       // Server
        "LanmanWorkstation",  // Workstation
        "NlaSvc",             // Network Location Awareness
        "Tcpip",              // TCP/IP Protocol Driver
        "nsi",                // Network Store Interface Service
        
        // Storage and file system
        "disk",               // Disk Driver
        "NTFS",               // NTFS File System Driver
        "volmgr",             // Volume Manager
        "volsnap",            // Volume Shadow Copy
        "partmgr",            // Partition Manager
        "mpssvc",             // Windows Defender Firewall
        
        // Display and graphics
        "DWM",                // Desktop Window Manager
        "UxSms",               // Desktop Window Manager Session Manager
        
        // Input devices
        "kbdclass",           // Keyboard Class Driver
        "mouclass",           // Mouse Class Driver
        "i8042prt",           // PS/2 Keyboard and Mouse Driver
        
        // Critical Windows services
        "WinDefend",          // Windows Defender Antivirus Service
        "wscsvc",             // Security Center
        "wuauserv",           // Windows Update (can cause issues if stopped)
        "BITS",               // Background Intelligent Transfer Service
        "CryptSvc",           // Cryptographic Services
        "TrustedInstaller",   // Windows Modules Installer
        
        // Session and logon
        "Netlogon",           // Net Logon
        "gpsvc",              // Group Policy Client
        
        // Audio (if testing on desktop)
        "AudioSrv",           // Windows Audio
        "Audiosrv",           // Windows Audio (alt name)
        
        // Boot critical
        "BFE",                // Base Filtering Engine
        "MpsSvc",             // Windows Firewall
    };

    public static bool IsEssential(string serviceName)
    {
        return _essential.Contains(serviceName);
    }

    public static int Count => _essential.Count;

    public static HashSet<string> GetAll()
    {
        return new HashSet<string>(_essential, StringComparer.OrdinalIgnoreCase);
    }
}
