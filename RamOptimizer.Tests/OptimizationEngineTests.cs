using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.Tests
{
    public class OptimizationEngineTests
    {
        [Fact]
        public async Task OptimizeForTarget_ExcludesCriticalProcesses()
        {
            // Arrange
            var (mockProcessManager, mockSafetyEngine, engine) = SetupOptimizationEngine(new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "CriticalProcess", MemoryUsage = 100, Priority = 1 },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess", MemoryUsage = 200, Priority = 2 }
            });
            engine.AddToDynamicExclusionList("CriticalProcess");
 
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonCriticalProcess" },
                TargetExecutables = new List<string> { "NonCriticalProcess.exe" }
            };
 
            // Act
            var result = await engine.OptimizeForTarget(profile);
 
            // Assert
            AssertOptimizationResult(result, new List<string> { "NonCriticalProcess" }, 200);
        }

        [Fact]
        public async Task OptimizeForTarget_TerminatesNonCriticalProcesses()
        {
            // Arrange
            var (mockProcessManager, mockSafetyEngine, engine) = SetupOptimizationEngine(new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "NonCriticalProcess1", MemoryUsage = 100, Priority = 1 },
                new ProcessInfo { ProcessId = 2, ProcessName = "NonCriticalProcess2", MemoryUsage = 200, Priority = 2 }
            });
 
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonCriticalProcess1" },
                TargetExecutables = new List<string> { "NonCriticalProcess2.exe" }
            };
 
            // Act
            var result = await engine.OptimizeForTarget(profile);
 
            // Assert
            AssertOptimizationResult(result, new List<string> { "NonCriticalProcess1", "NonCriticalProcess2" }, 300);
        }

        [Fact]
        public async Task OptimizeForTarget_RecoveryMechanismTriggered()
        {
            // Arrange
            var (mockProcessManager, mockSafetyEngine, engine) = SetupOptimizationEngine(new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "NonCriticalProcess", MemoryUsage = 100, Priority = 1 }
            });
 
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonCriticalProcess" },
                TargetExecutables = new List<string> { "NonCriticalProcess.exe" }
            };
 
            mockProcessManager.Setup(m => m.TerminateProcessAsync(1, "NonCriticalProcess")).ReturnsAsync(false);
 
            // Act
            var result = await engine.OptimizeForTarget(profile);
 
            // Assert
            Assert.Empty(result.TerminatedProcesses);
            // Additional assertions to verify recovery mechanism can be added here
        }

        [Fact]
        public async Task OptimizeForTarget_DynamicExclusionListPersistsAcrossCycles()
        {
            // Arrange
            var (mockProcessManager, mockSafetyEngine, engine) = SetupOptimizationEngine(new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" }
            });
 
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
 
            // Act
            var result = await engine.OptimizeForTarget(profile);
 
            // Assert
            AssertOptimizationResult(result, new List<string> { "TestProcess1", "TestProcess2" }, 3000000);
 
            // Add a process to the dynamic exclusion list
            engine.AddToDynamicExclusionList("TestProcess1");
 
            // Re-run optimization
            var secondResult = await engine.OptimizeForTarget(profile);
 
            // Assert that TestProcess1 is not terminated in the second run
            AssertOptimizationResult(secondResult, new List<string> { "TestProcess2" }, 2000000);
 
            // Re-run optimization again
            var thirdResult = await engine.OptimizeForTarget(profile);
 
            // Assert that TestProcess1 is not terminated in the third run
            AssertOptimizationResult(thirdResult, new List<string> { "TestProcess2" }, 2000000);
        }
    
        [Fact]
        public async Task OptimizeForTarget_DynamicExclusionListWithMultipleProcessesPersistsAcrossCycles()
        {
            // Arrange
            var (mockProcessManager, mockSafetyEngine, engine) = SetupOptimizationEngine(new List<ProcessInfo>
            {
                new ProcessInfo { ProcessId = 1, ProcessName = "TestProcess1", MemoryUsage = 1000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess1.exe" },
                new ProcessInfo { ProcessId = 2, ProcessName = "TestProcess2", MemoryUsage = 2000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess2.exe" },
                new ProcessInfo { ProcessId = 3, ProcessName = "TestProcess3", MemoryUsage = 3000000, Priority = ProcessPriorityClass.Normal, ExecutablePath = "C:\\Path\\To\\TestProcess3.exe" }
            });
 
            var profile = new OptimizationProfile
            {
                TargetProcessNames = new List<string> { "NonExistentProcess" },
                TargetExecutables = new List<string> { "NonExistentExecutable" }
            };
 
            // Act
            var result = await engine.OptimizeForTarget(profile);
 
            // Assert
            AssertOptimizationResult(result, new List<string> { "TestProcess1", "TestProcess2", "TestProcess3" }, 6000000);
 
            // Add multiple processes to the dynamic exclusion list
            engine.AddToDynamicExclusionList("TestProcess1");
            engine.AddToDynamicExclusionList("TestProcess2");
 
            // Re-run optimization
            var secondResult = await engine.OptimizeForTarget(profile);
 
            // Assert that TestProcess1 and TestProcess2 are not terminated in the second run
            AssertOptimizationResult(secondResult, new List<string> { "TestProcess3" }, 3000000);
 
            // Re-run optimization again
            var thirdResult = await engine.OptimizeForTarget(profile);
 
            // Assert that TestProcess1 and TestProcess2 are not terminated in the third run
            AssertOptimizationResult(thirdResult, new List<string> { "TestProcess3" }, 3000000);
        }
    }

}