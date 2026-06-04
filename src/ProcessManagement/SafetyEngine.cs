using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizer.ProcessManagement
{
    public class SafetyEngine
    {
        private HashSet<string> exclusionList;
        private readonly HashSet<string> _criticalProcesses;

        public SafetyEngine()
        {
            exclusionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _criticalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "system", "idle", "csrss.exe", "smss.exe", "wininit.exe",
                "winlogon.exe", "services.exe", "lsass.exe", "lsm.exe",
                "explorer.exe", "dwm.exe", "taskhost.exe", "taskhostw.exe",
                "svchost.exe", "spoolsv.exe", "audiodg.exe", "fontdrvhost.exe",
                "ctfmon.exe", "conhost.exe", "dllhost.exe", "sihost.exe",
                "runtimebroker.exe", "taskeng.exe", "wlanext.exe", "wudfhost.exe",
                "searchindexer.exe", "searchprotocolhost.exe", "searchfilterhost.exe",
                "dashost.exe", "wmpnetwk.exe", "microsoftedge.exe", "microsoftedgecp.exe",
                "backgroundtaskhost.exe", "shellexperiencehost.exe", "windowsinternal.composableshell.experiences.textinput.inputapp.exe"
            };
        }

        public async Task AddToExclusionListAsync(string processName, CancellationToken cancellationToken)
        {
            exclusionList.Add(processName);
            LogAction($"Added {processName} to exclusion list.");
        }

        public async Task RemoveFromExclusionListAsync(string processName, CancellationToken cancellationToken)
        {
            if (exclusionList.Remove(processName))
            {
                LogAction($"Removed {processName} from exclusion list.");
            }
            else
            {
                LogAction($"Process {processName} not found in exclusion list.");
            }
        }

        public bool IsExcluded(string processName)
        {
            return exclusionList.Contains(processName);
        }

        public List<ProcessInfo> FilterSafeProcesses(List<ProcessInfo> processes)
        {
            return processes.Where(p => !_criticalProcesses.Contains(p.ProcessName) &&
                                         !exclusionList.Contains(p.ProcessName))
                           .ToList();
        }

        public bool IsCriticalProcess(string processName)
        {
            return _criticalProcesses.Contains(processName);
        }

        private void LogAction(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter("safety_engine_log.txt", true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log action: {ex.Message}");
            }
        }
    }
}