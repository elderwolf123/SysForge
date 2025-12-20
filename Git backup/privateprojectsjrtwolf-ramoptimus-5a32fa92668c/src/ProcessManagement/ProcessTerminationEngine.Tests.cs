using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.ProcessManagement.Tests
{
    public class ProcessTerminationEngineTests
    {
        private Mock<IProcessManager> _mockProcessManager;
        private Mock<SafetyEngine> _mockSafetyEngine;
        private ProcessTerminationEngine _processTerminationEngine;
        private Mock<ILogger<ProcessTerminationEngine>> _mockLogger;

        public ProcessTerminationEngineTests()
        {
            _mockProcessManager = new Mock<IProcessManager>();
            _mockSafetyEngine = new Mock<SafetyEngine>();
            _mockLogger = new Mock<ILogger<ProcessTerminationEngine>>();
            _processTerminationEngine = new ProcessTerminationEngine(_mockProcessManager.Object, _mockSafetyEngine.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task TerminateProcessesAsync_TerminatesNonCriticalProcesses()
        {
            // Arrange
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal },
                new ProcessInfo { ProcessId = 3, ProcessName = "System", MemoryUsage = 3000000, Priority = ProcessPriorityClass.High },
                new ProcessInfo { ProcessId = 4, ProcessName = "Idle", MemoryUsage = 4000000, Priority = ProcessPriorityClass.Idle }
            };

            _mockSafetyEngine.Setup(m => m.IsExcluded(It.IsAny<string>())).Returns(false);
            _mockSafetyEngine.Setup(m => m.IsExcluded("System")).Returns(true);
            _mockSafetyEngine.Setup(m => m.IsExcluded("Idle")).Returns(true);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "TestProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "TestProcess2")).ReturnsAsync(true);

            // Act
            var result = await _processTerminationEngine.TerminateLevelAsync(1, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TerminatedProcesses.Count);
            Assert.Equal(3000000, result.MemoryFreed);
            Assert.True(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.True(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        }

        [Fact]
        public async Task TerminateProcessesAsync_DoesNotTerminateCriticalProcesses()
        {
            // Arrange
            _mockSafetyEngine.Setup(m => m.IsExcluded(It.IsAny<string>())).Returns(true);

            // Act
            var result = await _processTerminationEngine.TerminateLevelAsync(1, CancellationToken.None);

            // Assert
            Assert.Equal(0, result.TerminatedProcesses.Count);
            Assert.Equal(0, result.MemoryFreed);
        }
    }
}