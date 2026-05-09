using System;
using System.Collections.Generic;

namespace RamOptimizer.ProcessManagement
{
    public static class SecurityConfig
    {
        public static readonly HashSet<string> AllowedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nvidia-smi.exe",
            "dxdiag.exe",
            "directx.exe"
        };
    }
}
