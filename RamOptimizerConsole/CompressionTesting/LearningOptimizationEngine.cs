using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RamOptimizer.Logging;

namespace RamOptimizerConsole.CompressionTesting;

/// <summary>
* AI-powered learning and optimization engine for compression settings
/// </summary>
public class LearningOptimizationEngine
{
    private readonly ComprehensiveLogger _logger;
    private readonly CompressionDatabase _database;
    private readonly Dictionary<string, CompressionModel> _models;
    private readonly Dictionary<string, OptimizationHistory> _optimizationHistory;

    public LearningOptimizationEngine(ComprehensiveLogger logger)
    {
        _logger = logger;
        _database = new CompressionDatabase();
        _models = new Dictionary<string, CompressionModel>();
        _optimizationHistory = new Dictionary<string, OptimizationHistory>();
    }

    /// <summary>
    * Train compression models based on historical data
    /// </summary>
    public async Task TrainModelsAsync()
    {
        _logger.LogInfo("Starting compression model training");
        
        try
        {
            // Initialize database connection
            await _database.InitializeAsync();

            // Get historical test data
            var testHistory = await _database.GetTestHistoryAsync(1000);
            
            if (!testHistory.Any())
            {
                _logger.LogWarning("No historical data available for training");
                return;
            }

            // Train models for each file type
            var fileTypes = await _database.GetFileTypePerformanceAsync();
            
            foreach (var fileType in fileTypes.Keys)
            {
                await TrainFileTypeModelAsync(fileType, testHistory);
            }

            // Train general compression model
            await TrainGeneralModelAsync(testHistory);

            _logger.LogInfo($"Successfully trained {_models.Count} compression models");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during model training: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    * Train model for specific file type
    /// </summary>
    private async Task TrainFileTypeModelAsync(string fileType, List<CompressionTestRecord> testHistory)
    {
        _logger.LogInfo($"Training model for file type: {fileType}");
        
        var model = new CompressionModel
        {
            FileType = fileType,
            TrainingData = new List<TrainingSample>(),
            OptimalSettings = new CompressionSettings(),
            PerformanceMetrics = new PerformanceMetrics()
        };

        // Get file-specific test data
        var fileTests = await _database.GetFileTestsByExtensionAsync(fileType);
        
        if (!fileTests.Any())
        {
            _logger.LogWarning($"No training data available for file type: {fileType}");
            return;
        }

        // Process training samples
        foreach (var fileTest in fileTests)
        {
            var sample = new TrainingSample
            {
                OriginalSize = fileTest.OriginalSize,
                CompressionRatio = fileTest.CompressionRatio,
                CompressionTime = TimeSpan.FromMilliseconds(fileTest.CompressionTime),
                Success = fileTest.Success,
                CompressionTier = fileTest.CompressionTier,
                AlgorithmUsed = fileTest.AlgorithmUsed,
                FileExtension = fileTest.Extension,
                Timestamp = fileTest.TestedAt
            };

            model.TrainingData.Add(sample);
        }

        // Analyze patterns and find optimal settings
        AnalyzeFileTypePatterns(model);
        
        // Store trained model
        _models[fileType] = model;
        
        _logger.LogInfo($"Model trained for {fileType}: {model.TrainingData.Count} samples, optimal ratio: {model.OptimalSettings.TargetCompressionRatio:P0}");
    }

    /// <summary>
    * Train general compression model
    /// </summary>
    private async Task TrainGeneralModelAsync(List<CompressionTestRecord> testHistory)
    {
        _logger.LogInfo("Training general compression model");
        
        var model = new CompressionModel
        {
            FileType = "General",
            TrainingData = new List<TrainingSample>(),
            OptimalSettings = new CompressionSettings(),
            PerformanceMetrics = new PerformanceMetrics()
        };

        // Process all test data
        foreach (var test in testHistory)
        {
            var sample = new TrainingSample
            {
                OriginalSize = test.TotalOriginalSize,
                CompressionRatio = test.AverageCompressionRatio,
                CompressionTime = TimeSpan.FromMilliseconds(test.AverageCompressionTime),
                Success = test.SuccessRate > 0.5,
                CompressionTier = test.BestPerformingTier,
                AlgorithmUsed = "General",
                FileExtension = ".all",
                Timestamp = test.CreatedAt
            };

            model.TrainingData.Add(sample);
        }

        // Analyze patterns
        AnalyzeGeneralPatterns(model);
        
        // Store trained model
        _models["General"] = model;
        
        _logger.LogInfo($"General model trained: {model.TrainingData.Count} samples");
    }

    /// <summary>
    * Analyze patterns for specific file type
    /// </summary>
    private void AnalyzeFileTypePatterns(CompressionModel model)
    {
        if (!model.TrainingData.Any())
            return;

        var successfulTests = model.TrainingData.Where(t => t.Success).ToList();
        
        if (!successfulTests.Any())
            return;

        // Calculate optimal compression ratio
        model.OptimalSettings.TargetCompressionRatio = successfulTests.Average(t => t.CompressionRatio);
        
        // Find best performing tier
        var tierPerformance = successfulTests
            .GroupBy(t => t.CompressionTier)
            .Select(g => new { Tier = g.Key, SuccessRate = (double)g.Count(t => t.Success) / g.Count(), AvgRatio = g.Average(t => t.CompressionRatio) })
            .OrderByDescending(t => t.SuccessRate)
            .ThenByDescending(t => t.AvgRatio)
            .FirstOrDefault();

        if (tierPerformance != null)
        {
            model.OptimalSettings.RecommendedTier = tierPerformance.Tier;
        }

        // Calculate optimal file size ranges
        var sizeGroups = successfulTests.GroupBy(t => GetSizeCategory(t.OriginalSize));
        foreach (var group in sizeGroups)
        {
            var category = group.Key;
            var avgRatio = group.Average(t => t.CompressionRatio);
            var avgTime = group.Average(t => t.CompressionTime.TotalMilliseconds);
            
            model.OptimalSettings.SizePerformance[category] = new SizePerformance
            {
                AverageCompressionRatio = avgRatio,
                AverageCompressionTime = TimeSpan.FromMilliseconds(avgTime),
                FileCount = group.Count()
            };
        }

        // Calculate performance metrics
        model.PerformanceMetrics.AverageCompressionRatio = successfulTests.Average(t => t.CompressionRatio);
        model.PerformanceMetrics.AverageCompressionTime = TimeSpan.FromMilliseconds(successfulTests.Average(t => t.CompressionTime.TotalMilliseconds));
        model.PerformanceMetrics.SuccessRate = (double)successfulTests.Count / model.TrainingData.Count;
        model.PerformanceMetrics.TotalSamples = model.TrainingData.Count;
    }

    /// <summary>
    * Analyze general patterns
    /// </summary>
    private void AnalyzeGeneralPatterns(CompressionModel model)
    {
        if (!model.TrainingData.Any())
            return;

        var successfulTests = model.TrainingData.Where(t => t.Success).ToList();
        
        if (!successfulTests.Any())
            return;

        // Calculate general metrics
        model.OptimalSettings.TargetCompressionRatio = successfulTests.Average(t => t.CompressionRatio);
        
        // Find best overall tier
        var tierPerformance = successfulTests
            .GroupBy(t => t.CompressionTier)
            .Select(g => new { Tier = g.Key, SuccessRate = (double)g.Count(t => t.Success) / g.Count(), AvgRatio = g.Average(t => t.CompressionRatio) })
            .OrderByDescending(t => t.SuccessRate)
            .ThenByDescending(t => t.AvgRatio)
            .FirstOrDefault();

        if (tierPerformance != null)
        {
            model.OptimalSettings.RecommendedTier = tierPerformance.Tier;
        }

        // Calculate general performance metrics
        model.PerformanceMetrics.AverageCompressionRatio = successfulTests.Average(t => t.CompressionRatio);
        model.PerformanceMetrics.AverageCompressionTime = TimeSpan.FromMilliseconds(successfulTests.Average(t => t.CompressionTime.TotalMilliseconds));
        model.PerformanceMetrics.SuccessRate = (double)successfulTests.Count / model.TrainingData.Count;
        model.PerformanceMetrics.TotalSamples = model.TrainingData.Count;
    }

    /// <summary>
    * Get optimal compression settings for file
    /// </summary>
    public async Task<CompressionSettings> GetOptimalSettingsAsync(string filePath, long fileSize)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        
        // Get specific model for file type
        if (_models.ContainsKey(extension))
        {
            return _models[extension].OptimalSettings;
        }
        
        // Fall back to general model
        if (_models.ContainsKey("General"))
        {
            return _models["General"].OptimalSettings;
        }
        
        // Default settings
        return new CompressionSettings
        {
            TargetCompressionRatio = 0.5,
            RecommendedTier = "Standard",
            SizePerformance = new Dictionary<string, SizePerformance>
            {
                ["Small"] = new SizePerformance { AverageCompressionRatio = 0.6, AverageCompressionTime = TimeSpan.FromMilliseconds(100) },
                ["Medium"] = new SizePerformance { AverageCompressionRatio = 0.5, AverageCompressionTime = TimeSpan.FromMilliseconds(500) },
                ["Large"] = new SizePerformance { AverageCompressionRatio = 0.4, AverageCompressionTime = TimeSpan.FromMilliseconds(2000) }
            }
        };
    }

    /// <summary>
    * Optimize compression parameters
    /// </summary>
    public async Task<OptimizationResult> OptimizeCompressionParametersAsync(string filePath, long fileSize)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var optimalSettings = await GetOptimalSettingsAsync(filePath, fileSize);
        
        var result = new OptimizationResult
        {
            FilePath = filePath,
            FileSize = fileSize,
            Extension = extension,
            OptimalSettings = optimalSettings,
            Recommendations = new List<string>()
        };

        // Generate specific recommendations
        var sizeCategory = GetSizeCategory(fileSize);
        
        if (optimalSettings.SizePerformance.ContainsKey(sizeCategory))
        {
            var sizePerf = optimalSettings.SizePerformance[sizeCategory];
            
            result.Recommendations.Add($"For {sizeCategory} files, expect {sizePerf.AverageCompressionRatio:P0} compression ratio");
            result.Recommendations.Add($"Expected compression time: {sizePerf.AverageCompressionTime.TotalMilliseconds:F0}ms");
        }

        // Tier-specific recommendations
        if (!string.IsNullOrEmpty(optimalSettings.RecommendedTier))
        {
            result.Recommendations.Add($"Recommended compression tier: {optimalSettings.RecommendedTier}");
        }

        // Performance predictions
        result.PredictedCompressionRatio = optimalSettings.TargetCompressionRatio;
        result.PredictedCompressionTime = EstimateCompressionTime(fileSize, extension);
        
        // Store optimization history
        StoreOptimizationHistory(result);
        
        return result;
    }

    /// <summary>
    * Estimate compression time based on file characteristics
    /// </summary>
    private TimeSpan EstimateCompressionTime(long fileSize, string extension)
    {
        // Base time calculation
        double baseTime = fileSize / (1024.0 * 1024.0) * 10; // 10ms per MB
        
        // Adjust for file type
        var typeMultiplier = GetTypeCompressionMultiplier(extension);
        baseTime *= typeMultiplier;
        
        // Add random variation (±20%)
        var variation = 1.0 + (new Random().NextDouble() * 0.4 - 0.2);
        baseTime *= variation;
        
        return TimeSpan.FromMilliseconds(baseTime);
    }

    /// <summary>
    * Get compression multiplier for file type
    /// </summary>
    private double GetTypeCompressionMultiplier(string extension)
    {
        var multipliers = new Dictionary<string, double>
        {
            [".txt"] = 1.0,      // Text compresses quickly
            [".doc"] = 1.2,      // Documents take longer
            [".pdf"] = 1.5,      // PDFs are complex
            [".jpg"] = 0.8,      // Images compress quickly
            [".png"] = 1.0,      // PNG is moderate
            [".mp3"] = 0.5,      // Audio is fast
            [".wav"] = 1.2,      // Uncompressed audio takes longer
            [".mp4"] = 0.7,      // Video is fast
            [".zip"] = 0.3,      // Archives are very fast
            [".exe"] = 2.0,      // Executables are slow
            [".dll"] = 1.8,      // DLLs are slow
            [".log"] = 0.9,      // Logs are fast
            [".csv"] = 1.0,      // CSV is moderate
            [".json"] = 1.1,     // JSON is moderate
            [".xml"] = 1.3,      // XML is slower
            [".html"] = 1.0,     // HTML is moderate
            [".css"] = 0.8,      // CSS is fast
            [".js"] = 1.0,       // JavaScript is moderate
            [".py"] = 1.1,       // Python is moderate
            [".cs"] = 1.2,       // C# is slower
            [".java"] = 1.2,     // Java is slower
            [".tmp"] = 0.7,      // Temporary files are fast
            [".bak"] = 0.8,      // Backups are fast
            [".all"] = 1.0       // Default
        };

        return multipliers.ContainsKey(extension) ? multipliers[extension] : 1.0;
    }

    /// <summary>
    * Get size category
    /// </summary>
    private string GetSizeCategory(long fileSize)
    {
        if (fileSize < 1024 * 1024) // < 1MB
            return "Small";
        else if (fileSize < 100 * 1024 * 1024) // < 100MB
            return "Medium";
        else
            return "Large";
    }

    /// <summary>
    * Store optimization history
    /// </summary>
    private void StoreOptimizationHistory(OptimizationResult result)
    {
        var key = $"{result.Extension}_{GetSizeCategory(result.FileSize)}";
        
        if (!_optimizationHistory.ContainsKey(key))
        {
            _optimizationHistory[key] = new OptimizationHistory();
        }

        _optimizationHistory[key].AddResult(result);
    }

    /// <summary>
    * Get optimization insights
    /// </summary>
    public async Task<OptimizationInsights> GetOptimizationInsightsAsync()
    {
        var insights = new OptimizationInsights();
        
        // Analyze model performance
        insights.TotalModels = _models.Count;
        insights.TotalTrainingSamples = _models.Values.Sum(m => m.TrainingData.Count);
        
        // Calculate overall accuracy
        var allPredictions = _optimizationHistory.Values.SelectMany(h => h.Results).ToList();
        if (allPredictions.Any())
        {
            insights.AveragePredictionAccuracy = allPredictions.Average(r => 
                Math.Abs(r.PredictedCompressionRatio - r.ActualCompressionRatio) / r.ActualCompressionRatio);
        }

        // Find best performing file types
        insights.BestPerformingFileTypes = _models
            .Where(m => m.Value.TrainingData.Count > 10)
            .OrderByDescending(m => m.Value.PerformanceMetrics.SuccessRate)
            .Take(5)
            .ToDictionary(m => m.Key, m => m.Value.PerformanceMetrics.SuccessRate);

        // Find most efficient compression tiers
        insights.MostEfficientTiers = _models.Values
            .SelectMany(m => m.TrainingData)
            .GroupBy(t => t.CompressionTier)
            .Select(g => new { Tier = g.Key, Efficiency = g.Average(t => t.CompressionRatio / t.CompressionTime.TotalSeconds) })
            .OrderByDescending(t => t.Efficiency)
            .Take(3)
            .ToDictionary(t => t.Tier, t => t.Efficiency);

        return insights;
    }

    /// <summary>
    * Export model data
    /// </summary>
    public async Task ExportModelDataAsync(string outputPath)
    {
        var modelData = new
        {
            Models = _models.ToDictionary(m => m.Key, m => new
            {
                m.Value.FileType,
                m.Value.OptimalSettings,
                m.Value.PerformanceMetrics,
                TrainingDataCount = m.Value.TrainingData.Count
            }),
            Insights = await GetOptimizationInsightsAsync(),
            ExportDate = DateTime.Now
        };

        var json = System.Text.Json.JsonSerializer.Serialize(modelData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json);
    }
}

/// <summary>
* Compression model for machine learning
/// </summary>
public class CompressionModel
{
    public string FileType { get; set; }
    public List<TrainingSample> TrainingData { get; set; }
    public CompressionSettings OptimalSettings { get; set; }
    public PerformanceMetrics PerformanceMetrics { get; set; }
}

/// <summary>
* Training sample for machine learning
/// </summary>
public class TrainingSample
{
    public long OriginalSize { get; set; }
    public double CompressionRatio { get; set; }
    public TimeSpan CompressionTime { get; set; }
    public bool Success { get; set; }
    public string CompressionTier { get; set; }
    public string AlgorithmUsed { get; set; }
    public string FileExtension { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
* Compression settings optimized by ML
/// </summary>
public class CompressionSettings
{
    public double TargetCompressionRatio { get; set; }
    public string RecommendedTier { get; set; }
    public Dictionary<string, SizePerformance> SizePerformance { get; set; }
}

/// <summary>
* Size-specific performance metrics
/// </summary>
public class SizePerformance
{
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public int FileCount { get; set; }
}

/// <summary>
* Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public double SuccessRate { get; set; }
    public int TotalSamples { get; set; }
}

/// <summary>
* Optimization result
/// </summary>
public class OptimizationResult
{
    public string FilePath { get; set; }
    public long FileSize { get; set; }
    public string Extension { get; set; }
    public CompressionSettings OptimalSettings { get; set; }
    public List<string> Recommendations { get; set; }
    public double PredictedCompressionRatio { get; set; }
    public TimeSpan PredictedCompressionTime { get; set; }
    public double ActualCompressionRatio { get; set; }
    public TimeSpan ActualCompressionTime { get; set; }
    public bool Success { get; set; }
    public DateTime OptimizationTime { get; set; }
}

/// <summary>
* Optimization history tracking
/// </summary>
public class OptimizationHistory
{
    public List<OptimizationResult> Results { get; set; } = new List<OptimizationResult>();

    public void AddResult(OptimizationResult result)
    {
        result.OptimizationTime = DateTime.Now;
        Results.Add(result);
    }

    public double GetAverageAccuracy()
    {
        if (!Results.Any())
            return 0;

        return Results.Average(r => 
            Math.Abs(r.PredictedCompressionRatio - r.ActualCompressionRatio) / r.ActualCompressionRatio);
    }
}

/// <summary>
* Optimization insights
/// </summary>
public class OptimizationInsights
{
    public int TotalModels { get; set; }
    public int TotalTrainingSamples { get; set; }
    public double AveragePredictionAccuracy { get; set; }
    public Dictionary<string, double> BestPerformingFileTypes { get; set; }
    public Dictionary<string, double> MostEfficientTiers { get; set; }
}