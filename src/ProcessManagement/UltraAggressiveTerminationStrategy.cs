using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RamOptimizer.ProcessManagement
{
    public class UltraAggressiveTerminationStrategy : ITerminationStrategy
    {
        private static readonly Dictionary<int, List<string>> AggressionLevels = new()
        {
            [1] = new() { // User Applications Only
                "chrome.exe", "firefox.exe", "notepad.exe", "calculator.exe",
                "mspaint.exe", "wordpad.exe", "games.exe"
            },
            
            [2] = new() { // Microsoft Office & Productivity
                "winword.exe", "excel.exe", "powerpnt.exe", "outlook.exe",
                "teams.exe", "skype.exe", "zoom.exe"
            },
            
            [3] = new() { // Background Services & Updaters
                "updater.exe", "crashreporter.exe", "feedback.exe",
                "telemetry.exe", "cortana.exe", "searchui.exe"
            },
            
            [4] = new() { // Windows Optional Services
                "spoolsv.exe",          // Print Spooler
                "fax.exe",              // Fax Service
                "tabtip.exe",           // Touch Keyboard
                "mobsync.exe",          // Microsoft Synchronization Manager
                "wuauclt.exe",          // Windows Update
            },
            
            [5] = new() { // Windows Shell Components
                "shellexperiencehost.exe", // Start Menu, Action Center
                "startmenuexperiencehost.exe", // Start Menu Process
                "runtimebroker.exe",     // Windows Runtime Broker
                "applicationframehost.exe", // UWP App Frame
            },
            
            [6] = new() { // System Background Processes
                "backgroundtaskhost.exe", // Background Task Host
                "taskhostw.exe",         // Task Host Windows
                "dllhost.exe",           // COM Surrogate
                "rundll32.exe",          // Run DLL Process
            },
            
            [7] = new() { // Critical System Services (EXTREME RISK)
                "browser_broker.exe",    // Edge Browser Broker
                "smartscreen.exe",       // Windows Defender SmartScreen
                "securityhealthsystray.exe", // Security Health Systray
            }
        };

        public List<string> GetProcessesForLevel(int level)
        {
            if (AggressionLevels.TryGetValue(level, out var processes))
            {
                return processes;
            }
            throw new ArgumentOutOfRangeException(nameof(level), "Aggression level must be between 1 and 7.");
        }
    }
}