using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RamOptimizer.ProcessManagement
{
    /// <summary>
    /// Manages I/O priority for processes to optimize disk performance
    /// Uses Windows NtSetInformationProcess API
    /// </summary>
    public class IOPriorityManager
    {
        // Windows API constants
        private const int ProcessIoPriority = 0x21;

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref int processInformation,
            int processInformationLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref int processInformation,
            int processInformationLength,
            out int returnLength);

        /// <summary>
        /// Set I/O priority for a process by ID
        /// </summary>
        public static bool SetProcessIOPriority(int processId, IOPriority priority)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return SetProcessIOPriority(process, priority);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set I/O priority for a process
        /// </summary>
        public static bool SetProcessIOPriority(Process process, IOPriority priority)
        {
            try
            {
                int priorityValue = (int)priority;
                int result = NtSetInformationProcess(
                    process.Handle,
                    ProcessIoPriority,
                    ref priorityValue,
                    sizeof(int));

                return result == 0; // 0 = STATUS_SUCCESS
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set I/O priority for all processes with a given name
        /// </summary>
        public static int SetProcessIOPriorityByName(string processName, IOPriority priority)
        {
            int successCount = 0;
            
            // Remove .exe if provided
            processName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    if (SetProcessIOPriority(process, priority))
                        successCount++;
                    
                    process.Dispose();
                }
            }
            catch
            {
                // Ignore errors
            }

            return successCount;
        }

        /// <summary>
        /// Get current I/O priority for a process
        /// </summary>
        public static IOPriority GetProcessIOPriority(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return GetProcessIOPriority(process);
            }
            catch
            {
                return IOPriority.Normal;
            }
        }

        /// <summary>
        /// Get current I/O priority for a process
        /// </summary>
        public static IOPriority GetProcessIOPriority(Process process)
        {
            try
            {
                int priorityValue = 0;
                int result = NtQueryInformationProcess(
                    process.Handle,
                    ProcessIoPriority,
                    ref priorityValue,
                    sizeof(int),
                    out _);

                if (result == 0)
                    return (IOPriority)priorityValue;
            }
            catch
            {
                // Ignore errors
            }

            return IOPriority.Normal;
        }

        /// <summary>
        /// Set high I/O priority for current process (for file transfers)
        /// </summary>
        public static bool SetCurrentProcessHighPriority()
        {
            return SetProcessIOPriority(Process.GetCurrentProcess(), IOPriority.High);
        }

        /// <summary>
        /// Restore normal I/O priority for current process
        /// </summary>
        public static bool RestoreCurrentProcessPriority()
        {
            return SetProcessIOPriority(Process.GetCurrentProcess(), IOPriority.Normal);
        }
    }

    /// <summary>
    /// Windows I/O Priority levels
    /// </summary>
    public enum IOPriority
    {
        /// <summary>
        /// Minimal I/O impact - lowest priority
        /// </summary>
        VeryLow = 0,

        /// <summary>
        /// Background tasks - low priority
        /// </summary>
        Low = 1,

        /// <summary>
        /// Default priority for most processes
        /// </summary>
        Normal = 2,

        /// <summary>
        /// High priority - file transfers, user-selected processes
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical priority - reserved for system operations
        /// Use with caution
        /// </summary>
        Critical = 4
    }
}
