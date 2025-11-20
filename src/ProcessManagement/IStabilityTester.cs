using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RamOptimizer.ProcessManagement
{
    public interface IStabilityTester
    {
        bool IsSystemStable();
        Task<bool> IsSystemStableAsync();
        void LogTestResults(string results);
        event EventHandler<string> StabilityTestCompleted;
    }
}