using Microsoft.VisualStudio.TestTools.UnitTesting;
using RamOptimizer.ProcessManagement;
using RamOptimizer.ProcessManagement.Tests;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using src.ProcessManagement;

namespace RamOptimizer.Tests
{
    [TestClass]
    public class SystemStabilityTests
    {
        private readonly FileCompressionSystem _fileCompressionSystem;
        private readonly CpuOptimizer _cpuOptimizer;
        private readonly GpuOptimizer _gpuOptimizer;

        public SystemStabilityTests()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _fileCompressionSystem = new FileCompressionSystem();
            _cpuOptimizer = new CpuOptimizer();
            _gpuOptimizer = new GpuOptimizer();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize any necessary components before each test
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up any resources after each test
        }

        [TestMethod]
        public void TestFileCompressionStability()
        {
            // Arrange
            string filePath = "path/to/file.txt";
            string operationName = "File compression";

            // Act & Assert
            CompressFileAndAssertStability(filePath, operationName);
        }

        [TestMethod]
        public void TestCpuOptimizationStability()
        {
            // Arrange
            IOptimizer optimizer = _cpuOptimizer;
            string operationName = "CPU optimization";

            // Act & Assert
            OptimizeAndAssertStability(optimizer, operationName);
        }

        [TestMethod]
        public void TestGpuOptimizationStability()
        {
            // Arrange
            IOptimizer optimizer = _gpuOptimizer;
            string operationName = "GPU optimization";

            // Act & Assert
            OptimizeAndAssertStability(optimizer, operationName);
        }

        private void AssertStability(bool isSuccessful, string operationName)
        {
            Assert.IsTrue(isSuccessful, $"{operationName} was not successful.");
        }

        private void CompressFileAndAssertStability(string filePath, string operationName)
        {
            _fileCompressionSystem.CompressFile(filePath);
            AssertStability(_fileCompressionSystem.IsCompressionSuccessful(), operationName);
        }

        private void OptimizeAndAssertStability(IOptimizer optimizer, string operationName)
        {
            optimizer.Optimize();
            AssertStability(optimizer.IsOptimizationSuccessful(), operationName);
        }
    }
}