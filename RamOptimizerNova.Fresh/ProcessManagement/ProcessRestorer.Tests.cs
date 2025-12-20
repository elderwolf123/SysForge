using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.ProcessManagement.Tests
{
    public class ProcessRestorerTests
    {
        private Mock<IProcessManager> _mockProcessManager;
        private Mock<ILogger> _mockLogger;
        private ProcessRestorer _processRestorer;

        public ProcessRestorerTests()
        {
            _mockProcessManager = new Mock<IProcessManager>();
            _mockLogger = new Mock<ILogger>();
            _processRestorer = new ProcessRestorer(_mockProcessManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RestoreProcesses_RestoresAllProcesses()
        {
            // Arrange
            var terminatedProcesses = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.RestoreProcessAsync("C:\\Path\\To\\TestProcess1.exe")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.RestoreProcessAsync("C:\\Path\\To\\TestProcess2.exe")).ReturnsAsync(true);

            // Act
            var result = await _processRestorer.RestoreProcesses(terminatedProcesses);

            // Assert
            Assert.Equal(2, result.Restored.Count);
            Assert.Equal(0, result.Failed.Count);
            Assert.True(result.Restored.Any(p => p.ProcessName == "TestProcess1"));
            Assert.True(result.Restored.Any(p => p.ProcessName == "TestProcess2"));
        }

        // Other test methods...
    }
}
