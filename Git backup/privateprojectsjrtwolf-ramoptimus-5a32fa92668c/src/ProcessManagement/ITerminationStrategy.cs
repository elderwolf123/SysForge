using System.Collections.Generic;

namespace RamOptimizer.ProcessManagement
{
    public interface ITerminationStrategy
    {
        List<string> GetProcessesForLevel(int level);
    }
}