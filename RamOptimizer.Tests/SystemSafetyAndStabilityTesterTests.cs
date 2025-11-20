using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProcessManagement;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RamOptimizer.Tests
{
    [TestClass]
    public class SystemSafetyAndStabilityTesterTests
    {
        private OptimizationEngine _optimizationEngine;
        private Mock<IProcessManager> _mockProcessManager;
        private Mock<SafetyEngine> _mockSafetyEngine;

        [TestInitialize]
        public void Setup()
        {
            _mockProcessManager = new Mock<IProcessManager>();
            _mockSafetyEngine = new Mock<SafetyEngine>();
            _optimizationEngine = new OptimizationEngine(_mockProcessManager.Object, _mockSafetyEngine.Object);
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesProcessesBasedOnProfile()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" }
            };
        
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "TestProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "TestProcess2")).ReturnsAsync(true);
        
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
        
            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(3000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        }
        
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateExcludedProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" }
            };
        
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "TestProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "TestProcess2")).ReturnsAsync(true);
        
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
        
            // Add a process to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("TestProcess1");
        
            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert
            Assert.AreEqual(1, result.TerminatedProcesses.Count);
            Assert.AreEqual(2000000, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        }
        
        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListPersistsAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" }
            };
        
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "TestProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "TestProcess2")).ReturnsAsync(true);
        
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
        
            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(3000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        
            // Add a process to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("TestProcess1");
        
            // Re-run optimization
            var secondResult = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert that TestProcess1 is not terminated in the second run
            Assert.IsFalse(secondResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(secondResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        
            // Re-run optimization again
            var thirdResult = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert that TestProcess1 is not terminated in the third run
            Assert.IsFalse(thirdResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(thirdResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
        }
        
        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListWithMultipleProcessesPersistsAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" },
                new ProcessInfo { ProcessId = 3, ProcessName = "TestProcess3", MemoryUsage = 3000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess3.exe" }
            };
        
            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
            _mockSafetyEngine.Setup(m => m.IsSystemStable()).Returns(true);
        
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "TestProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "TestProcess2")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "TestProcess3")).ReturnsAsync(true);
        
            _mockProcessManager.Setup(m => m.RestoreProcessAsync("C:\\Path\\To\\TestProcess1.exe")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.RestoreProcessAsync("C:\\Path\\To\\TestProcess2.exe")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.RestoreProcessAsync("C:\\Path\\To\\TestProcess3.exe")).ReturnsAsync(true);
        
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
        
            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert
            Assert.AreEqual(3, result.TerminatedProcesses.Count);
            Assert.AreEqual(6000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess3"));
        
            // Add multiple processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("TestProcess1");
            _optimizationEngine.AddToDynamicExclusionList("TestProcess2");
        
            // Re-run optimization
            var secondResult = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert that TestProcess1 and TestProcess2 are not terminated in the second run
            Assert.IsFalse(secondResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsFalse(secondResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
            Assert.IsTrue(secondResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess3"));
        
            // Re-run optimization again
            var thirdResult = await _optimizationEngine.OptimizeForTarget(profile);
        
            // Assert that TestProcess1 and TestProcess2 are not terminated in the third run
            Assert.IsFalse(thirdResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess1"));
            Assert.IsFalse(thirdResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess2"));
            Assert.IsTrue(thirdResult.TerminatedProcesses.Any(p => p.ProcessName == "TestProcess3"));
                [TestMethod]
                async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
                {
                    // Arrange: Setting up a list of processes for testing
                    var processes = new List<ProcessInfo>
                    {
                        new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                        new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
                    };
        
                    _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
                    _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);
        
                    var profile = new OptimizationProfile
                    {
                        TargetProcessNames = new List<string> { "NonExistentProcess" },
                        TargetExecutables = new List<string> { "NonExistentExecutable" }
                    };
        
                    // Add critical processes to the dynamic exclusion list
                    _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
                    _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");
        
                    // Act
                    var result = await _optimizationEngine.OptimizeForTarget(profile);
        
                    // Assert
                    Assert.AreEqual(0, result.TerminatedProcesses.Count);
                    Assert.AreEqual(0, result.MemoryFreed);
                    Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
                    Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
                }
        
                [TestMethod]
                async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
                {
                    // Arrange: Setting up a list of processes for testing
                    var processes = new List<ProcessInfo>
                    {
                        new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                        new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
                    };
        
                    _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
                    _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);
        
                    var profile = new OptimizationProfile
                    {
                        TargetProcessNames = new List<string> { "NonCriticalProcess1", "NonCriticalProcess2" },
                        TargetExecutables = new List<string> { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
                    };
        
                    // Act
                    var result = await _optimizationEngine.OptimizeForTarget(profile);
        
                    // Assert
                    Assert.AreEqual(2, result.TerminatedProcesses.Count);
                    Assert.AreEqual(4000000, result.MemoryFreed);
                    Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
                    Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
                }
        
                [TestMethod]
                async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
                {
                    // Arrange: Setting up a list of processes for testing
                    var processes = new List<ProcessInfo>
                    {
                        new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
                    };
        
                    _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
                    _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
                    _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny<ProcessInfo>())).Returns(false);
        
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
                    _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
        
                    var profile = new OptimizationProfile
                    {
                        TargetProcessNames = new List<string> { "CriticalProcess1" },
                        TargetExecutables = new List<string> { "CriticalProcess1.exe" }
                    };
        
                    // Add critical processes to the dynamic exclusion list
                    _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
        
                    // Act
                    var result = await _optimizationEngine.OptimizeForTarget(profile);
        
                    // Assert
                    Assert.AreEqual(0, result.TerminatedProcesses.Count);
                    Assert.AreEqual(0, result.MemoryFreed);
                    Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
                }
        
                [TestMethod]
                async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
                {
                    // Arrange: Setting up a list of processes for testing
                    var processes = new List<ProcessInfo>
                    {
                        new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                        new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
                    };
        
                    _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                        .ReturnsAsync(processes)
                        .ReturnsAsync(processes);
        
                    _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny<List<ProcessInfo>>())).Returns(processes);
        
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
                    _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);
        
                    var profile = new OptimizationProfile
                    {
                        TargetProcessNames = new List<string> { "CriticalProcess1", "NonCriticalProcess1" },
                        TargetExecutables = new List<string> { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
                    };
        
                    // Add critical processes to the dynamic exclusion list
                    _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
        
                    // Act
                    var result1 = await _optimizationEngine.OptimizeForTarget(profile);
                    var result2 = await _optimizationEngine.OptimizeForTarget(profile);
        
                    // Assert
                    Assert.AreEqual(0, result1.TerminatedProcesses.Count);
                    Assert.AreEqual(0, result1.MemoryFreed);
                    Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
                    Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        
                    Assert.AreEqual(0, result2.TerminatedProcesses.Count);
                    Assert.AreEqual(0, result2.MemoryFreed);
                    Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
                    Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
                }
        }

        [TestMethod]
        public void TestRunStabilityTests()
        {
            // Placeholder for stability tests test
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                _tester.RunStabilityTests();
                string output = writer.ToString();
                Assert.IsTrue(output.Contains("Running stability tests..."));
                // Add more assertions to verify stability tests
            }
        }

        [TestMethod]
        public void TestLogTestResults()
        {
            // Placeholder for logging test results test
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                _tester.LogTestResults();
                string output = writer.ToString();
                Assert.IsTrue(output.Contains("Logging test results..."));
                // Add more assertions to verify logging
            }
        }
    }
}
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m => m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m => m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m => m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m => m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m => m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p => p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p => p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "CriticalProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "CriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonExistentProcess" },
                TargetExecutables = new List&lt;string&gt; { "NonExistentExecutable" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess2");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 4, ProcessName = "NonCriticalProcess2", MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess2.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, "NonCriticalProcess1")).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, "NonCriticalProcess2")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "NonCriticalProcess1", "NonCriticalProcess2" },
                TargetExecutables = new List&lt;string&gt; { "NonCriticalProcess1.exe", "NonCriticalProcess2.exe" }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess2"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, "CriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = "C:\\Path\\To\\CriticalProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess1", MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\NonCriticalProcess1.exe" }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, "CriticalProcess1")).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, "NonCriticalProcess1")).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { "CriticalProcess1", "NonCriticalProcess1" },
                TargetExecutables = new List&lt;string&gt; { "CriticalProcess1.exe", "NonCriticalProcess1.exe" }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList("CriticalProcess1");

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "CriticalProcess1"));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == "NonCriticalProcess1"));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 2, ProcessName = &quot;CriticalProcess2&quot;, MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess2.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, &quot;CriticalProcess2&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;NonExistentProcess&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;NonExistentExecutable&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess2&quot;);

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess2&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = &quot;NonCriticalProcess1&quot;, MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 4, ProcessName = &quot;NonCriticalProcess2&quot;, MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess2.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, &quot;NonCriticalProcess1&quot;)).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, &quot;NonCriticalProcess2&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;NonCriticalProcess1&quot;, &quot;NonCriticalProcess2&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;NonCriticalProcess1.exe&quot;, &quot;NonCriticalProcess2.exe&quot; }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess2&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;CriticalProcess1&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;CriticalProcess1.exe&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 2, ProcessName = &quot;NonCriticalProcess1&quot;, MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess1.exe&quot; }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, &quot;NonCriticalProcess1&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;CriticalProcess1&quot;, &quot;NonCriticalProcess1&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;CriticalProcess1.exe&quot;, &quot;NonCriticalProcess1.exe&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));
        }
        [TestMethod]
        public async Task OptimizeForTarget_DoesNotTerminateCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 2, ProcessName = &quot;CriticalProcess2&quot;, MemoryUsage = 2000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess2.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, &quot;CriticalProcess2&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;NonExistentProcess&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;NonExistentExecutable&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess2&quot;);

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsFalse(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess2&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 3, ProcessName = &quot;NonCriticalProcess1&quot;, MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 4, ProcessName = &quot;NonCriticalProcess2&quot;, MemoryUsage = 2500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess2.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(3, &quot;NonCriticalProcess1&quot;)).ReturnsAsync(true);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(4, &quot;NonCriticalProcess2&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;NonCriticalProcess1&quot;, &quot;NonCriticalProcess2&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;NonCriticalProcess1.exe&quot;, &quot;NonCriticalProcess2.exe&quot; }
            };

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(2, result.TerminatedProcesses.Count);
            Assert.AreEqual(4000000, result.MemoryFreed);
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));
            Assert.IsTrue(result.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess2&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_RecoveryMechanismTriggeredForUnstableCriticalProcess()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; }
            };

            _mockProcessManager.Setup(m =&gt; m.GetRunningProcessesAsync()).ReturnsAsync(processes);
            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockSafetyEngine.Setup(m =&gt; m.IsProcessStable(It.IsAny&lt;ProcessInfo&gt;())).Returns(false);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.RestartProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;CriticalProcess1&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;CriticalProcess1.exe&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);

            // Act
            var result = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result.TerminatedProcesses.Count);
            Assert.AreEqual(0, result.MemoryFreed);
            Assert.IsTrue(result.RecoveredProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
        }

        [TestMethod]
        public async Task OptimizeForTarget_DynamicExclusionListRespectedAcrossCycles()
        {
            // Arrange: Setting up a list of processes for testing
            var processes = new List&lt;ProcessInfo&gt;()
            {
                new ProcessInfo { ProcessId = 1, ProcessName = &quot;CriticalProcess1&quot;, MemoryUsage = 1000000, Priority = ProcessPriorityClass.High, ExecutablePath = &quot;C:\\Path\\To\\CriticalProcess1.exe&quot; },
                new ProcessInfo { ProcessId = 2, ProcessName = &quot;NonCriticalProcess1&quot;, MemoryUsage = 1500000, Priority = ProcessPriorityClass.Normal, ExecutablePath = &quot;C:\\Path\\To\\NonCriticalProcess1.exe&quot; }
            };

            _mockProcessManager.SetupSequence(m =&gt; m.GetRunningProcessesAsync())
                .ReturnsAsync(processes)
                .ReturnsAsync(processes);

            _mockSafetyEngine.Setup(m =&gt; m.FilterSafeProcesses(It.IsAny&lt;List&lt;ProcessInfo&gt;&gt;())).Returns(processes);

            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(1, &quot;CriticalProcess1&quot;)).ReturnsAsync(false);
            _mockProcessManager.Setup(m =&gt; m.TerminateProcessAsync(2, &quot;NonCriticalProcess1&quot;)).ReturnsAsync(true);

            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List&lt;string&gt; { &quot;CriticalProcess1&quot;, &quot;NonCriticalProcess1&quot; },
                TargetExecutables = new List&lt;string&gt; { &quot;CriticalProcess1.exe&quot;, &quot;NonCriticalProcess1.exe&quot; }
            };

            // Add critical processes to the dynamic exclusion list
            _optimizationEngine.AddToDynamicExclusionList(&quot;CriticalProcess1&quot;);

            // Act
            var result1 = await _optimizationEngine.OptimizeForTarget(profile);
            var result2 = await _optimizationEngine.OptimizeForTarget(profile);

            // Assert
            Assert.AreEqual(0, result1.TerminatedProcesses.Count);
            Assert.AreEqual(0, result1.MemoryFreed);
            Assert.IsFalse(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsTrue(result1.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));

            Assert.AreEqual(0, result2.TerminatedProcesses.Count);
            Assert.AreEqual(0, result2.MemoryFreed);
            Assert.IsFalse(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;CriticalProcess1&quot;));
            Assert.IsTrue(result2.TerminatedProcesses.Any(p =&gt; p.ProcessName == &quot;NonCriticalProcess1&quot;));
        }