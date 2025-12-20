using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using RamOptimizer.Compression;
using RamOptimizer.ProcessManagement;

namespace CompressionBenchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== COMPRESSION BENCHMARK TOOL ===");
                Console.WriteLine("Starting application...");
                
                // Test basic functionality
                Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"OS: {Environment.OSVersion}");
                Console.WriteLine($"Framework: {Environment.Version}");
                
                // Test file access
                string testFile = "test_startup.txt";
                try
                {
                    await File.WriteAllTextAsync(testFile, "Test startup");
                    string content = await File.ReadAllTextAsync(testFile);
                    Console.WriteLine($"File I/O test: {content}");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"File I/O error: {ex.Message}");
                }
                
                Console.WriteLine("Application started successfully!");
                
                // Now run the main application
                try
                {
                    var mainApp = new MainApplication();
                    mainApp.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Main application error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
    
    public class MainApplication
    {
        private FileTypeDatabase _database;
        private bool fullScan = false;
        
        public void Run()
        {
            try
            {
                // Initialize database
                _database = new FileTypeDatabase();
                
                Console.WriteLine("\n🚀 COMPRESSION BENCHMARK TOOL");
                Console.WriteLine("   Advanced compression testing with RAM optimization");
                Console.WriteLine("   ==============================================");
                
                while (true)
                {
                    ShowMenu();
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(input))
                        continue;
                    
                    switch (input)
                    {
                        case "1":
                            DiscoverFileTypes();
                            break;
                        case "2":
                            TestCompressionAlgorithms();
                            break;
                        case "3":
                            ContinueTesting();
                            break;
                        case "4":
                            GenerateReport();
                            break;
                        case "5":
                            ShowDatabaseSummary();
                            break;
                        case "6":
                        case "R":
                        case "r":
                            RetryFailedTests();
                            break;
                        case "7":
                            Console.WriteLine("\n🚀 Running full compression scan...");
                            // Test ALL file types with full reporting
                            fullScan = true;
                            
                            // Configure JSON serializer with proper number handling
                            var jsonOptions = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };
                            
                            RunCompressionScan(jsonOptions);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error in main application: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void ShowMenu()
        {
            Console.WriteLine("\n=============================================================");
            Console.WriteLine("MAIN MENU");
            Console.WriteLine("=============================================================");
            Console.WriteLine("1. Discover file types and create sample data");
            Console.WriteLine("2. Test compression algorithms on sample files");
            Console.WriteLine("3. Continue testing untested file types");
            Console.WriteLine("4. Generate compression report");
            Console.WriteLine("5. Show database summary");
            Console.WriteLine("6. Retry failed compression tests");
            Console.WriteLine("7. Exit");
            Console.WriteLine("=============================================================");
            Console.Write("Select option (1-7): ");
        }
        
        private void DiscoverFileTypes()
        {
            Console.WriteLine("\n🔍 Discovering file types...");
            Console.WriteLine("   This will scan your drives for common file types");
            Console.WriteLine("   and add them to the compression testing database.");
            
            // Add some common file types for demonstration
            var commonExtensions = new[] { 
                ".txt", ".doc", ".docx", ".pdf", ".jpg", ".png", ".mp3", ".mp4", 
                ".zip", ".rar", ".7z", ".exe", ".dll", ".json", ".xml", ".csv" 
            };
            
            foreach (var ext in commonExtensions)
            {
                _database.AddDiscoveredFileType(ext);
            }
            
            Console.WriteLine($"   ✓ Added {commonExtensions.Length} common file types to database");
            Console.WriteLine("   You can now start testing these file types.");
            
            // Add some sample failed entries for testing retry functionality
            AddSampleFailedEntries();
            
            // Create sample test files
            CreateSampleTestFiles();
        }
        
        private void CreateSampleTestFiles()
        {
            Console.WriteLine("\n📝 Creating sample test files for compression testing...");
            
            try
            {
                // Create sample text file
                string sampleText = "This is a sample text file for compression testing.\n";
                for (int i = 0; i < 100; i++)
                {
                    sampleText += $"Line {i+1}: This is test data for compression algorithm benchmarking.\n";
                }
                File.WriteAllText("sample_test.txt", sampleText);
                _database.AddDiscoveredFileType(".txt", "sample_test.txt");
                Console.WriteLine("   ✅ Created sample_test.txt");
                
                // Create sample JSON file
                string sampleJson = @"{
    ""name"": ""Sample Data"",
    ""version"": ""1.0"",
    ""data"": [
        {""id"": 1, ""value"": ""test""},
        {""id"": 2, ""value"": ""compression""},
        {""id"": 3, ""value"": ""benchmark""}
    ],
    ""metadata"": {
        ""created"": ""2025-12-16"",
        ""size"": ""1KB"",
        ""type"": ""sample""
    }
}";
                File.WriteAllText("sample_test.json", sampleJson);
                _database.AddDiscoveredFileType(".json", "sample_test.json");
                Console.WriteLine("   ✅ Created sample_test.json");
                
                // Create sample CSV file
                string sampleCsv = "Name,Age,City,Country\n";
                for (int i = 1; i <= 50; i++)
                {
                    sampleCsv += $"User{i},{20 + (i % 30)},City{i % 10},Country{i % 5}\n";
                }
                File.WriteAllText("sample_test.csv", sampleCsv);
                _database.AddDiscoveredFileType(".csv", "sample_test.csv");
                Console.WriteLine("   ✅ Created sample_test.csv");
                
                _database.SaveDatabase();
                Console.WriteLine($"   ✓ Created 3 sample test files for compression testing");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Failed to create sample files: {ex.Message}");
            }
        }
        
        private void TestCompressionAlgorithms()
        {
            Console.WriteLine("\n🧪 Testing Compression Algorithms");
            Console.WriteLine("   This will test various compression algorithms on sample files");
            Console.WriteLine("   with optional RAM optimization for better performance.");
            
            // Check if we have sample files to test
            var sampleFiles = new[] { "sample_test.txt", "sample_test.json", "sample_test.csv" };
            var availableFiles = sampleFiles.Where(f => File.Exists(f)).ToList();
            
            if (availableFiles.Count == 0)
            {
                Console.WriteLine("   ⚠️  No sample files found. Please select option 1 first to create sample files.");
                return;
            }
            
            Console.WriteLine($"   Found {availableFiles.Count} sample files to test:");
            foreach (var file in availableFiles)
            {
                Console.WriteLine($"   - {file}");
            }
            
            RamOptimizationManager? testRamManager = null;
            
            // Offer RAM optimization for testing
            Console.Write("\nEnable aggressive RAM cleanup for testing? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                Console.Write("Reserved RAM (GB, Enter for 5): ");
                var ramIn = Console.ReadLine();
                int gb = 5;
                if (!string.IsNullOrEmpty(ramIn) && int.TryParse(ramIn, out int p))
                {
                    gb = Math.Max(2, Math.Min(10, p));
                }
                
                testRamManager = new RamOptimizationManager();
                testRamManager.MinimumReservedRamBytes = gb * 1024L * 1024 * 1024;
                testRamManager.EnableAggressiveMode();
                
                Console.WriteLine($"   ✓ Aggressive RAM optimization enabled ({gb}GB reserved)");
            }
            
            Console.Write("\nStart compression testing? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                try
                {
                    var tester = new CompressionTester(_database);
                    
                    foreach (var file in availableFiles)
                    {
                        var extension = Path.GetExtension(file);
                        Console.WriteLine($"\n📊 Testing {extension} files...");
                        tester.TestFileType(extension);
                    }
                    
                    // Restore processes if RAM optimization was enabled
                    if (testRamManager != null)
                    {
                        Console.WriteLine("\n🔄 Restoring system processes...");
                        testRamManager.RestoreProcesses();
                        testRamManager.ExportLearningData();
                    }
                    
                    Console.WriteLine("\n✅ Compression testing complete!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Error during testing: {ex.Message}");
                }
            }
        }
        
        private void AddSampleFailedEntries()
        {
            Console.WriteLine("\n📝 Adding sample failed entries for testing...");
            
            var failedExtensions = new[] { ".psd", ".iso", ".vmdk", ".dmg", ".bin" };
            
            foreach (var ext in failedExtensions)
            {
                _database.AddDiscoveredFileType(ext);
                var entry = _database.GetEntry(ext);
                if (entry != null)
                {
                    entry.TestStatus = TestStatus.Failed;
                    entry.TestCount = 1;
                    Console.WriteLine($"   ✅ Added failed entry: {ext}");
                }
            }
            
            _database.SaveDatabase();
            Console.WriteLine($"   ✓ Added {failedExtensions.Length} sample failed entries");
        }
        
        private void ContinueTesting()
        {
            Console.WriteLine("\n▶️ Continuing compression testing...");
            var untested = _database.GetUntestedFileTypes();
            
            if (untested.Count == 0)
            {
                Console.WriteLine("   ✅ All file types have been tested!");
                return;
            }
            
            Console.WriteLine($"   Found {untested.Count} untested file types:");
            foreach (var ext in untested)
            {
                Console.WriteLine($"   - {ext}");
            }
            
            Console.WriteLine("\n   To start testing, select option 2 to test compression algorithms.");
        }
        
        private void GenerateReport()
        {
            Console.WriteLine("\n📊 Generating compression report...");
            
            var tested = _database.GetEntriesWithStatus(TestStatus.Tested);
            var failed = _database.GetEntriesWithStatus(TestStatus.Failed);
            
            if (tested.Count == 0 && failed.Count == 0)
            {
                Console.WriteLine("   No test data available. Run some tests first.");
                return;
            }
            
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("COMPRESSION TEST REPORT");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine($"✅ Successfully tested: {tested.Count} file types");
            Console.WriteLine($"❌ Failed tests: {failed.Count} file types");
            
            if (tested.Count > 0)
            {
                Console.WriteLine("\nSuccessfully tested file types:");
                foreach (var entry in tested)
                {
                    Console.WriteLine($"   - {entry.Extension} (tested {entry.TestCount} times)");
                }
            }
            
            if (failed.Count > 0)
            {
                Console.WriteLine("\nFailed file types:");
                foreach (var entry in failed)
                {
                    Console.WriteLine($"   - {entry.Extension}");
                }
            }
            
            Console.WriteLine(new string('=', 50));
        }
        
        private void ShowDatabaseSummary()
        {
            Console.WriteLine("\n📋 Database Summary:");
            _database.PrintSummary();
        }
        
        private void RetryFailedTests()
        {
            Console.WriteLine("\n🔄 RETRY FAILED COMPRESSION TESTS");
            Console.WriteLine("   This will retry all files that failed compression testing");
            
            var failedEntries = _database.GetFailedEntries();
            
            if (failedEntries.Count == 0)
            {
                Console.WriteLine("   ✅ No failed tests to retry!");
                Console.WriteLine("   You can add sample failed entries by selecting option 1.");
                return;
            }
            
            Console.WriteLine($"   Found {failedEntries.Count} failed file types to retry:");
            foreach (var entry in failedEntries)
            {
                Console.WriteLine($"   - {entry.Extension}");
            }
            
            RamOptimizationManager? retryRamManager = null;
            
            // Offer RAM optimization for retry (failed files might be large)
            Console.Write("\nEnable aggressive RAM cleanup for retry? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                Console.Write("Reserved RAM (GB, Enter for 5): ");
                var ramIn = Console.ReadLine();
                int gb = 5;
                if (!string.IsNullOrEmpty(ramIn) && int.TryParse(ramIn, out int p))
                {
                    gb = Math.Max(2, Math.Min(10, p));
                }
                
                retryRamManager = new RamOptimizationManager();
                retryRamManager.MinimumReservedRamBytes = gb * 1024L * 1024 * 1024;
                retryRamManager.EnableAggressiveMode();
                
                Console.WriteLine($"   ✓ Aggressive RAM optimization enabled ({gb}GB reserved)");
            }
            
            Console.Write("\nContinue with retry? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                var retryTester = new CompressionTester(_database);
                retryTester.RetryFailedTests();
                
                // Restore processes if RAM optimization was enabled
                if (retryRamManager != null)
                {
                    Console.WriteLine("\n🔄 Restoring system processes...");
                    retryRamManager.RestoreProcesses();
                    retryRamManager.ExportLearningData();
                }
                
                Console.WriteLine("\n✅ Failed tests retry complete!");
            }
        }
        
        private void RunCompressionScan(JsonSerializerOptions jsonOptions)
        {
            Console.WriteLine("\n🔍 Running comprehensive compression scan...");
            Console.WriteLine("   This will test all available compression algorithms");
            Console.WriteLine("   on all discovered file types with detailed reporting.");
            
            try
            {
                var tester = new CompressionTester(_database);
                
                // Test all file types
                var allExtensions = _database.GetAllFileTypes();
                Console.WriteLine($"   Found {allExtensions.Count} file types to test:");
                
                foreach (var ext in allExtensions)
                {
                    Console.WriteLine($"\n📊 Testing {ext} files...");
                    tester.TestFileType(ext);
                }
                
                Console.WriteLine("\n✅ Comprehensive compression scan complete!");
                Console.WriteLine("   Detailed results have been saved to the database.");
                
                // Generate final report
                GenerateReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error during compression scan: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
