using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using RamOptimizer.ProcessManagement;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace RamOptimizer.Tests
{
    [TestFixture]
    public class ProcessTerminationEngineTests
    {
        private ProcessTerminationEngine _engine;
        private Mock<IOptimizationStateManager> _mockOptimizationStateManager;

        [SetUp]
        public void Setup()
        {
            InitializeMocks();
            InitializeEngine();
        }

        private void InitializeMocks()
        {
            _mockOptimizationStateManager = new Mock<IOptimizationStateManager>();
        }

        private void InitializeEngine()
        {
            _engine = new ProcessTerminationEngine(NullLogger<ProcessTerminationEngine>.Instance, _mockOptimizationStateManager.Object);
        }

        [Test]
        public void AddToDynamicExclusionList_ShouldAddProcess()
        {
            // Arrange
            var processName = "testProcess";
            
            // Act
            _engine.AddToDynamicExclusionList(processName);
            
            // Assert
            AssertExclusionListContains(processName);
        }

        private void AssertExclusionListContains(string processName)
        {
            Assert.IsTrue(_engine._dynamicExclusionList.Contains(processName.ToLower()));
        }

        [Test]
        public void RemoveFromDynamicExclusionList_ShouldRemoveProcess()
        {
            // Arrange
            var processName = "testProcess";
            _engine.AddToDynamicExclusionList(processName);
            
            // Act
            _engine.RemoveFromDynamicExclusionList(processName);
            
            // Assert
            AssertExclusionListDoesNotContain(processName);
        }

        private void AssertExclusionListDoesNotContain(string processName)
        {
            Assert.IsFalse(_engine._dynamicExclusionList.Contains(processName.ToLower()));
        }

        [Test]
        public void LoadDynamicExclusionList_ShouldLoadFromFile()
        {
            // Arrange
            var exclusionListFilePath = "dynamic_exclusion_list.json";
            var expectedList = new HashSet<string> { "process1", "process2" };
            WriteExclusionListToFile(exclusionListFilePath, expectedList);
            
            // Act
            var engine = new ProcessTerminationEngine(NullLogger<ProcessTerminationEngine>.Instance, _mockOptimizationStateManager.Object);
            
            // Assert
            Assert.AreEqual(expectedList, engine._dynamicExclusionList);
            
            // Cleanup
            File.Delete(exclusionListFilePath);
        }

        private void WriteExclusionListToFile(string filePath, HashSet<string> exclusionList)
        {
            var json = JsonConvert.SerializeObject(exclusionList);
            File.WriteAllText(filePath, json);
        }

        [Test]
        public void SaveDynamicExclusionList_ShouldSaveToFile()
        {
            // Arrange
            var exclusionListFilePath = "dynamic_exclusion_list.json";
            var processName = "testProcess";
            _engine.AddToDynamicExclusionList(processName);
            
            // Act
            _engine.SaveDynamicExclusionList();
            
            // Assert
            var savedList = ReadExclusionListFromFile(exclusionListFilePath);
            Assert.AreEqual(_engine._dynamicExclusionList, savedList);
            
            // Cleanup
            File.Delete(exclusionListFilePath);
        }

        private HashSet<string> ReadExclusionListFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<HashSet<string>>(json);
        }

        [Test]
        public void TerminateProcess_ShouldTerminateNonCriticalProcess()
        {
            // Arrange
            var processName = "notepad";
            var process = CreateAndStartProcess(processName);
            
            // Act
            _engine.TerminateProcess(processName);
            
            // Assert
            Assert.IsTrue(process.HasExited);
            
            // Cleanup
            process.Dispose();
        }

        private Process CreateAndStartProcess(string processName)
        {
            var process = new Process();
            process.StartInfo.FileName = $"{processName}.exe";
            process.Start();
            return process;
        }

        [Test]
        public void TerminateProcess_ShouldNotTerminateCriticalProcess()
        {
            // Arrange
            var processName = "notepad";
            _engine.AddToDynamicExclusionList(processName);
            var process = CreateAndStartProcess(processName);
            
            // Act
            _engine.TerminateProcess(processName);
            
            // Assert
            Assert.IsFalse(process.HasExited);
            
            // Cleanup
            process.Kill();
            process.Dispose();
        }

        [Test]
        public void RecoveryMechanism_ShouldBeTriggered()
        {
            // Arrange
            var processName = "notepad";
            var process = CreateAndStartProcess(processName);
            
            // Act
            _engine.TerminateProcess(processName);
            
            // Assert
            Assert.IsTrue(process.HasExited);
            
            // Simulate recovery mechanism
            _engine.RecoverProcess(processName);
            
            // Assert
            // Since we are simulating the recovery, we need to check if a new process is started
            // This can be done by checking if there is a new process with the same name
            var processes = Process.GetProcessesByName(processName);
            Assert.IsTrue(processes.Length > 0);
            
            // Cleanup
            foreach (var p in processes)
            {
                p.Kill();
                p.Dispose();
            }
        }

        // Add more test cases as needed
    }
}