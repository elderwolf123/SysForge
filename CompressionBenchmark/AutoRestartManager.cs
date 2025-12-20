using System;
using System.Diagnostics;
using System.IO;

namespace CompressionBenchmark;

/// <summary>
/// Manages Windows Task Scheduler integration for auto-restart on crash/reboot.
/// </summary>
public class AutoRestartManager
{
    private const string TaskName = "CompressionBenchmark_AutoResume";
    private static readonly string ExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

    /// <summary>
    /// Register a Windows scheduled task to auto-start on boot
    /// </summary>
    public static bool RegisterAutoRestart()
    {
        if (string.IsNullOrEmpty(ExePath))
        {
            Console.WriteLine("   ❌ Could not determine executable path");
            return false;
        }

        try
        {
            // Create scheduled task using schtasks command
            var arguments = $@"/Create /F /TN ""{TaskName}"" " +
                           $@"/TR ""\""{ExePath}\"""" " +
                           $@"/SC ONSTART " +
                           $@"/RU ""{Environment.UserDomainName}\{Environment.UserName}"" " +
                           $@"/RL HIGHEST " +
                           $@"/IT";

            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("   ❌ Failed to start schtasks");
                return false;
            }

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("   ✓ Auto-restart on boot enabled");
                Console.WriteLine($"   📍 Task registered: {TaskName}");
                Console.WriteLine("   💡 Will auto-resume after crash/reboot");
                BenchmarkLogger.LogInfo($"Auto-restart task registered: {TaskName}");
                return true;
            }
            else
            {
                var error = process.StandardError.ReadToEnd();
                Console.WriteLine($"   ⚠️  Failed to register task: {error}");
                BenchmarkLogger.LogError($"Failed to register auto-restart task: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error registering auto-restart: {ex.Message}");
            BenchmarkLogger.LogError("Failed to register auto-restart", ex);
            return false;
        }
    }

    /// <summary>
    /// Unregister the auto-restart task
    /// </summary>
    public static void UnregisterAutoRestart()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $@"/Delete /TN ""{TaskName}"" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return;

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("   ✓ Auto-restart disabled");
                BenchmarkLogger.LogInfo("Auto-restart task unregistered");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Error unregistering: {ex.Message}");
            BenchmarkLogger.LogWarning($"Failed to unregister auto-restart: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if auto-restart is currently enabled
    /// </summary>
    public static bool IsAutoRestartEnabled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $@"/Query /TN ""{TaskName}""",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a flag file to indicate we're in auto-resume mode
    /// </summary>
    public static void SetAutoResumeFlag()
    {
        try
        {
            File.WriteAllText("auto_resume.flag", DateTime.Now.ToString());
            BenchmarkLogger.LogInfo("Auto-resume flag set");
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to set auto-resume flag: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if we're in auto-resume mode
    /// </summary>
    public static bool IsAutoResuming()
    {
        return File.Exists("auto_resume.flag");
    }

    /// <summary>
    /// Clear auto-resume flag
    /// </summary>
    public static void ClearAutoResumeFlag()
    {
        try
        {
            if (File.Exists("auto_resume.flag"))
            {
                File.Delete("auto_resume.flag");
                BenchmarkLogger.LogInfo("Auto-resume flag cleared");
            }
        }
        catch (Exception ex)
        {
            BenchmarkLogger.LogWarning($"Failed to clear auto-resume flag: {ex.Message}");
        }
    }
}
