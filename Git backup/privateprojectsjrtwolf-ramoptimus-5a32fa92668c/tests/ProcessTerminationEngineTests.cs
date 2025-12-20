using System;
using System.Collections.Generic;
using Xunit;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.Tests
{
    public class ProcessTerminationEngineTests
    {
        [Fact]
        public void TestDynamicExclusionListPersistence()
        {
            // Arrange
            var terminationStrategy = new UltraAggressiveTerminationStrategy();
            var engine = new ProcessTerminationEngine(terminationStrategy);
            string testProcessName = "test_process";

            // Act
            engine.AddToDynamicExclusionList(testProcessName);
            engine.TerminateLevel(1); // This will save the state

            // Reset the engine to simulate a new instance
            engine = new ProcessTerminationEngine(terminationStrategy);

            // Assert
            var exclusionList = engine.GetDynamicExclusionList();
            Assert.Contains(testProcessName.ToLower(), exclusionList);
        }
    }
}