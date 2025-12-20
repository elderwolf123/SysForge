using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tests
{
    // Minimal stub implementations used by CompressionView_TestHandlers.cs

    public static class TestDataGenerator
    {
        public static string CreateDummyGameFolder(string rootPath, string folderName, int sizeMB = 100)
        {
            // Create a temporary folder and return its path (no actual files needed for compilation)
            var path = Path.Combine(rootPath, folderName);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public class Tier2AutomatedTests
    {
        public class TestResult
        {
            public int TotalTests { get; set; } = 0;
            public int PassedTests { get; set; } = 0;
            public int FailedTests { get; set; } = 0;
            public double SuccessRate => TotalTests == 0 ? 0 : (double)PassedTests / TotalTests;
            public Dictionary<string, bool> Results { get; set; } = new();
        }

        // Simulated async test runner returning a default successful result
        public Task<TestResult> RunAllTestsAsync()
        {
            var result = new TestResult
            {
                TotalTests = 1,
                PassedTests = 1,
                FailedTests = 0,
                Results = new Dictionary<string, bool> { { "DummyTest", true } }
            };
            return Task.FromResult(result);
        }
    }
}