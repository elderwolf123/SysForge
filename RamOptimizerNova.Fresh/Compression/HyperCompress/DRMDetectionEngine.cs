using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Detects DRM (Digital Rights Management) systems in game directories
/// to assess compression safety and risk levels.
/// </summary>
public class DRMDetectionEngine
{
    /// <summary>
    /// Known DRM indicators with their file patterns and risk assessments
    /// </summary>
    private static readonly Dictionary<string, DRMMarker> _drmIndicators = new()
    {
        // Steam DRM
        ["steam_api.dll"] = new DRMMarker { Name = "Steam", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Low, Description = "File verification, relatively tolerant of file system changes" },
        ["steam_api64.dll"] = new DRMMarker { Name = "Steam", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Low, Description = "64-bit Steam API, same as steam_api.dll" },
        ["steam_appid.txt"] = new DRMMarker { Name = "Steam", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Low, Description = "Steam App ID marker" },

        // Epic Games Store DRM
        ["EOSSDK-Win64-Shipping.dll"] = new DRMMarker { Name = "Epic Online Services", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Medium, Description = "Epic Games Online Services SDK" },
        ["EOSSDK-Win32-Shipping.dll"] = new DRMMarker { Name = "Epic Online Services", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Medium, Description = "32-bit EOS SDK" },

        // Origin/EA DRM
        ["Origin.dll"] = new DRMMarker { Name = "Origin/EA", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Medium, Description = "EA Origin/Access launcher integration" },
        ["EALauncher.exe"] = new DRMMarker { Name = "Origin/EA", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Medium, Description = "EA Launcher DRM wrapper" },

        // Ubisoft Uplay DRM
        ["uplay_r1.dll"] = new DRMMarker { Name = "Uplay DRM", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "Ubisoft's anti-cheat/driving overlay" },
        ["uplay_r1_loader.dll"] = new DRMMarker { Name = "Uplay DRM", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "Ubisoft DRM launching system" },

        // Microsoft Store/Game Pass DRM
        ["microsoftgamemonitor.exe"] = new DRMMarker { Name = "Microsoft Game Monitor", Type = DRMType.PlatformDRM, RiskLevel = DrmRiskLevel.Medium, Description = "Microsoft Game Monitor for accessibility" },

        // Battle.net/Blizzard DRM
        ["battle.net.dll"] = new DRMMarker { Name = "Battle.net", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Low, Description = "Blizzard Battle.net launcher" },

        // GOG Galaxy DRM (minimal)
        ["Galaxy.dll"] = new DRMMarker { Name = "GOG Galaxy", Type = DRMType.OnlineDRM, RiskLevel = DrmRiskLevel.Minimal, Description = "GOG Galaxy launcher, very non-invasive" },

        // Denuvo Anti-Tamper
        ["denuvo"] = new DRMMarker { Name = "Denuvo Anti-Tamper", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "Denuvo protection system - extremely sensitive" },

        // Arxan Anti-Tamper
        ["arxan"] = new DRMMarker { Name = "Arxan Anti-Tamper", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "Arxan protection system - file integrity critical" },

        // VMProtect/Other Packagers
        ["vmprotect"] = new DRMMarker { Name = "VMProtect", Type = DRMType.PackagedExe, RiskLevel = DrmRiskLevel.High, Description = "VMProtect executable packer - compression may be restricted" },

        // Unity DRM/Content Protection
        ["UnityPlayer.dll"] = new DRMMarker { Name = "Unity Engine", Type = DRMType.GameEngine, RiskLevel = DrmRiskLevel.Low, Description = "Unity engine, generally compression-safe" },

        // Unreal Engine DRM
        ["UnrealEngine"] = new DRMMarker { Name = "Unreal Engine", Type = DRMType.GameEngine, RiskLevel = DrmRiskLevel.Low, Description = "Unreal Engine, generally compression-safe" },

        // Older/legacy DRM systems (potentially bypassable)
        ["secdrv.sys"] = new DRMMarker { Name = "SecuROM", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "SecuROM - legacy but discontinued, may have bypass methods" },
        ["sfc_os.dll"] = new DRMMarker { Name = "SecuROM SFC", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "SecuROM SafeCast - legacy, bypasses available online" },
        ["gp.sys"] = new DRMMarker { Name = "SecuROM GameShield", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "SecuROM GameShield - legacy, known bypass patterns" },
        ["secdrv64.sys"] = new DRMMarker { Name = "SecuROM 64-bit", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "SecuROM 64-bit version - legacy, outdated" },

        ["drvmgt.dll"] = new DRMMarker { Name = "SafeDisc", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "Macrovision SafeDisc - very old, bypass widely available" },
        ["00000001.@"] = new DRMMarker { Name = "SafeDisc ClonyXXL", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "SafeDisc - old, predictable bypass vectors" },
        ["clony.sys"] = new DRMMarker { Name = "SafeDisc", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "SafeDisc driver - legacy, discontinued" },

        ["protect.dll"] = new DRMMarker { Name = "Tages", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "Tages - rare now, very invasive, may crash systems" },
        ["tages.exe"] = new DRMMarker { Name = "Tages", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "Tages protection system - outdated but dangerous" },

        ["starforce_device.sys"] = new DRMMarker { Name = "StarForce", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "StarForce - known for system instability, bypassed by emulators" },
        ["sfdrv.sys"] = new DRMMarker { Name = "StarForce", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.VeryHigh, Description = "StarForce driver - legacy, emulators bypass this" },

        ["laserlok.sys"] = new DRMMarker { Name = "LaserLock", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "LaserLock - DVD/BD protection, often bypassed by virtual drives" },
        ["laserlok.dll"] = new DRMMarker { Name = "LaserLock", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "LaserLock protection system - old, known bypass methods" },

        ["CPI.dll"] = new DRMMarker { Name = "Copy Protection International", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "CPI - failed protection scheme, easily bypassed" },
        ["cpi.dll"] = new DRMMarker { Name = "CPI", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "Copy Protection International - legacy, bypass trivial" },

        ["_armadillo_.sys"] = new DRMMarker { Name = "Armadillo", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "Armadillo - specific game protection, bypass programs exist" },

        ["shieldev.dll"] = new DRMMarker { Name = "ExeShield", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.High, Description = "ExeShield - legacy executable protection, predictable bypass" },

        ["pcguard.dll"] = new DRMMarker { Name = "PC Guard", Type = DRMType.PackagedExe, RiskLevel = DrmRiskLevel.Medium, Description = "PC Guard - old executable compressor/drm, known unpackers available" },

        ["telock.dll"] = new DRMMarker { Name = "PELock", Type = DRMType.PackagedExe, RiskLevel = DrmRiskLevel.Medium, Description = "PELock - PE compression/drm, unpackers exist but uncommon" },

        // CD/DVD protection markers
        ["cd-dvd.sys"] = new DRMMarker { Name = "CD/DVD Protection", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Low, Description = "Generic CD/DVD check system - often no-exec, bypass via virtual drive" },
        ["copykiller.dll"] = new DRMMarker { Name = "CopyKiller", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "CopyKiller - old audio CD protection, obsolete" },

        // Generic old protections
        ["padus.sys"] = new DRMMarker { Name = "Padus", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "Padus DiscJuggler - legacy CD protection, virtual drives bypass" },
        ["alcohol.sys"] = new DRMMarker { Name = "Alcohol Soft", Type = DRMType.InvasiveDRM, RiskLevel = DrmRiskLevel.Medium, Description = "Alcohol 120% - burning software protection, bypass by emulators" }
    };

    /// <summary>
    /// Additional patterns for old protection systems
    /// </summary>
    private static readonly HashSet<string> _legacyDrmPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "sriupdate",    // SecuROM updater
        "gamemanagerservice",  // StarForce
        "protection",   // Generic
        "antisec",      // Generic
        "nodvd",        // Common bypass marker
        "crack"         // Though these shouldn't be in legit installs
    };

    /// <summary>
    /// File patterns that suggest anti-cheat systems
    /// </summary>
    private static readonly HashSet<string> _antiCheatFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "EasyAntiCheat_EOS_Setup.exe",
        "EasyAntiCheat_Setup.exe",
        "BattlEye_Setup.exe",
        "PunkBuster_Setup.exe",
        "Faceit_Setup.exe",
        "vgk.sys", // Vanguard kernel
        "Faceit.sys"
    };

    /// <summary>
    /// Scans a game directory for DRM markers and returns comprehensive analysis
    /// </summary>
    public DRMAnalysisResult AnalyzeForDRM(string gamePath)
    {
        if (!Directory.Exists(gamePath))
            throw new DirectoryNotFoundException($"Game directory not found: {gamePath}");

        Console.WriteLine($"Scanning {gamePath} for DRM indicators...");

        var result = new DRMAnalysisResult
        {
            GamePath = gamePath,
            DetectedMarkers = new List<DRMMarker>(),
            FoundIndicators = new Dictionary<string, DRMMarker>(),
            RiskLevel = DrmRiskLevel.None
        };

        // Get all files in the directory (recursive)
        var allFiles = Directory.GetFiles(gamePath, "*.*", SearchOption.AllDirectories);

        // Check each file against known DRM markers
        var foundMarkers = new HashSet<string>();

        foreach (var filePath in allFiles)
        {
            var fileName = Path.GetFileName(filePath);

            // Exact file name match
            if (_drmIndicators.ContainsKey(fileName))
            {
                var marker = _drmIndicators[fileName];
                result.DetectedMarkers.Add(marker);
                result.FoundIndicators[fileName] = marker;

                if (!foundMarkers.Contains(marker.Name))
                {
                    Console.WriteLine($"  ⚠️  Detected: {marker.Name} ({marker.Type}) - {marker.Description}");
                    foundMarkers.Add(marker.Name);
                }
            }

            // Check for partial matches in filename (like "denuvo" in filename)
            foreach (var key in _drmIndicators.Keys.Where(k => k.Length < 10)) // Only short patterns
            {
                if (fileName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var marker = _drmIndicators[key];
                    if (!result.FoundIndicators.ContainsKey(key))
                    {
                        result.DetectedMarkers.Add(marker);
                        result.FoundIndicators[key] = marker;

                        Console.WriteLine($"  ⚠️  Potential: {marker.Name} (partial match: {fileName})");
                    }
                }
            }

            // Anti-cheat detection
            if (_antiCheatFiles.Contains(fileName))
            {
                result.HasAntiCheat = true;
                result.AntiCheatFiles.Add(fileName);
                Console.WriteLine($"  🛡️   Anti-Cheat: {fileName}");
            }

            // Check for game launchers that might verify game files
            if (IsGameLauncher(fileName))
            {
                result.HasVerifiedLauncher = true;
                result.Launchers.Add(fileName);
                Console.WriteLine($"  🚀  Launcher: {fileName} (may verify game files)");
            }
        }

        // Calculate overall risk level
        result.RiskLevel = CalculateRiskLevel(result);

        // Safety recommendations
        result.Recommendations = GetSafetyRecommendations(result);

        Console.WriteLine($"\nRisk Assessment: {result.RiskLevel}");
        Console.WriteLine($"Overall Safety: {GetSafetyRating(result)}");

        return result;
    }

    /// <summary>
    /// Checks if this game appears DRM-free for compression
    /// </summary>
    public bool IsCompressionSafe(string gamePath)
    {
        var analysis = AnalyzeForDRM(gamePath);
        return analysis.RiskLevel <= DrmRiskLevel.Low;
    }

    private DrmRiskLevel CalculateRiskLevel(DRMAnalysisResult result)
    {
        if (!result.DetectedMarkers.Any())
            return DrmRiskLevel.None;

        // Highest risk marker determines overall risk
        var maxRisk = result.DetectedMarkers.Max(m => m.RiskLevel);

        // Anti-cheat presence increases risk
        if (result.HasAntiCheat)
        {
            maxRisk = (DrmRiskLevel)Math.Max((int)maxRisk, (int)DrmRiskLevel.High);
        }

        // Verified launcher increases risk
        if (result.HasVerifiedLauncher)
        {
            maxRisk = (DrmRiskLevel)Math.Max((int)maxRisk, (int)DrmRiskLevel.Medium);
        }

        return maxRisk;
    }

    private List<string> GetSafetyRecommendations(DRMAnalysisResult result)
    {
        var recommendations = new List<string>();

        if (result.RiskLevel == DrmRiskLevel.None)
        {
            recommendations.Add("🟢 No DRM detected - consider selective compression safe");
            recommendations.Add("🐢 Use folder-level VFS mounting for maximum safety");
        }
        else if (result.RiskLevel == DrmRiskLevel.Low)
        {
            recommendations.Add("🟡 Low-risk DRM - selective compression may be viable");
            recommendations.Add("🔒 Test with file-level reparse points before folder mounting");
            recommendations.Add("🧪 Perform small-scale testing first");
        }
        else if (result.RiskLevel == DrmRiskLevel.Medium)
        {
            recommendations.Add("🟠 Medium-risk DRM - extreme caution required");
            recommendations.Add("🚫 Avoid compression of protected executables/assets");
            recommendations.Add("🛡️ Only compress loose user/createable content");
        }
        else
        {
            recommendations.Add("🔴 High/very high-risk DRM - DO NOT compress");
            recommendations.Add("⚠️  Significant chance of game breakage");
            recommendations.Add("🔄 Consider alternative storage solutions");
        }

        if (result.HasAntiCheat)
        {
            recommendations.Add("🎮 Anti-cheat detected - compression may trigger false positives");
            recommendations.Add("🛑 Avoid any file system interception techniques");
        }

        return recommendations;
    }

    private string GetSafetyRating(DRMAnalysisResult result)
    {
        return result.RiskLevel switch
        {
            DrmRiskLevel.None => "Safe",
            DrmRiskLevel.Minimal => "Very Low Risk",
            DrmRiskLevel.Low => "Low Risk",
            DrmRiskLevel.Medium => "Medium Risk",
            DrmRiskLevel.High => "High Risk",
            DrmRiskLevel.VeryHigh => "Very High Risk - Strongly Advised Against",
            _ => "Unknown"
        };
    }

    private bool IsGameLauncher(string fileName)
    {
        var launchers = new[]
        {
            "steam.exe",
            "epicgameslauncher.exe",
            "ealauncher.exe",
            "origin.exe",
            "battlenet.exe",
            "goglauncher.exe",
            "uplay.exe"
        };

        return launchers.Contains(fileName.ToLowerInvariant());
    }
}

/// <summary>
/// Result of DRM analysis for a game directory
/// </summary>
public class DRMAnalysisResult
{
    public string GamePath { get; set; } = "";
    public List<DRMMarker> DetectedMarkers { get; set; } = new();
    public Dictionary<string, DRMMarker> FoundIndicators { get; set; } = new();
    public DrmRiskLevel RiskLevel { get; set; }
    public bool HasAntiCheat { get; set; }
    public bool HasVerifiedLauncher { get; set; }
    public List<string> AntiCheatFiles { get; set; } = new();
    public List<string> Launchers { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Information about a detected DRM marker
/// </summary>
public class DRMMarker
{
    public string Name { get; set; } = "";
    public DRMType Type { get; set; }
    public DrmRiskLevel RiskLevel { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Types of DRM systems
/// </summary>
public enum DRMType
{
    OnlineDRM,      // Online verification (Steam, Epic, etc.)
    InvasiveDRM,    // Anti-cheat or tamper protection
    PlatformDRM,    // Store/platform restrictions
    PackagedExe,    // Executable packers/obfuscators
    GameEngine      // Engine-level protections
}

/// <summary>
/// Risk level for compression compatibility
/// </summary>
public enum DrmRiskLevel
{
    None = 0,       // No DRM detected
    Minimal = 1,    // Essentially none (GOG Galaxy)
    Low = 2,        // Tolerant of changes (basic Steam)
    Medium = 3,     // May have issues (Epic, Origin)
    High = 4,       // Likely problematic (Uplay, anti-cheat)
    VeryHigh = 5    // Almost certain to break (Denuvo, Arxan)
}
