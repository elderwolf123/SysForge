using System.Diagnostics;

namespace RamOptimizer.ProcessManagement
{
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public long MemoryUsage { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public string ExecutablePath { get; set; }
        public bool IsCritical { get; set; }
        public System.DateTime StartTime { get; set; }
    }
}