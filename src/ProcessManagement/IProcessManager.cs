using System.Collections.Generic;
using System.Threading.Tasks;

namespace RamOptimizer.ProcessManagement
{
    public interface IProcessManager
    {
        Task<List<ProcessInfo>> GetRunningProcessesAsync();
        Task<bool> TerminateProcessAsync(int processId, string processName);
        Task<bool> RestoreProcessAsync(string executablePath);
    }
}