using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.ProcessManagement.Tests
{
    public class SystemStabilityTesterTests
    {
        private Mock<IProcessManager> _mockProcessManager;
        private Mock<ILogger<SystemStabilityTester>> _mockLogger;
        private SystemStabilityTester _systemStabilityTester;
      
        public SystemStabilityTesterTests()
        {
            _mockProcessManager = new Mock<IProcessManager>();
            _mockLogger = new Mock<ILogger<SystemStabilityTester>>();
            _systemStabilityTester = new SystemStabilityTester(_mockLogger.Object);
        }

        private void SetupMockProcesses(List<ProcessInfo> processes)
        {
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
        }

        [Fact]
        public async Task TestSystemStability_ReturnsTrueForStableSystem()
        {
            // Arrange
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal }
            };

            SetupMockProcesses(processes);

            // Act
            var result = await _systemStabilityTester.IsSystemStableAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestSystemStability_ReturnsFalseForUnstableSystem()
        {
            // Arrange
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal },
                new ProcessInfo { ProcessId = 3, ProcessName = "HighMemoryProcess", MemoryUsage = 5000000000, Priority = ProcessPriorityClass.Normal }
            };

            SetupMockProcesses(processes);

            // Act
            var result = await _systemStabilityTester.IsSystemStableAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestSystemStability_LogsErrorOnException()
        {
            // Arrange
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _systemStabilityTester.IsSystemStableAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(m => m.LogError(It.IsAny<Exception>(), "Error occurred while checking system stability"), Times.Once);
        }
    }
}