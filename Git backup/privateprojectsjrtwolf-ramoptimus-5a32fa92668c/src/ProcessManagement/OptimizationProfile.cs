using System.Collections.Generic;

namespace RamOptimizer.ProcessManagement
{
    public class OptimizationProfile
    {
        public List<string> TargetProcessNames { get; set; }
        public List<string> TargetExecutables { get; set; }
    }
}