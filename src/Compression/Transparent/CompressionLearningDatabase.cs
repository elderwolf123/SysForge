using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.Transparent
{
    /// <summary>
    /// Learning database that tracks which compression algorithms work best
    /// for different games and file types
    /// </summary>
    public class CompressionLearningDatabase
    {
        private readonly string _databasePath;
        private readonly ILogger? _logger;
        private LearningData _data;

        public CompressionLearningDatabase(ILogger? logger = null)
        {
            _logger = logger;
            _databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RamOptimizer",
                "compression_learning.json"
            );
            
            LoadDatabase();
        }

        /// <summary>
        /// Get the best algorithm for a specific game and file type
        /// </summary>
        public CompactAlgorithm? GetBestAlgorithm(string gameName, string fileExtension)
        {
            // First, check game-specific profile
            if (_data.GameProfiles.TryGetValue(gameName, out var gameProfile))
            {
                if (gameProfile.FileTypeResults.TryGetValue(fileExtension, out var result))
                {
                    if (result.SampleCount >= 5) // Confident in result
                    {
                        _logger?.LogInformation($"Using learned algorithm for {gameName} {fileExtension}: {result.BestAlgorithm}");
                        return result.BestAlgorithm;
                    }
                }
            }

            // Fallback to global file type profile
            if (_data.GlobalFileTypeProfiles.TryGetValue(fileExtension, out var globalResult))
            {
                if (globalResult.SampleCount >= 10) // Need more samples for global
                {
                    _logger?.LogInformation($"Using global learned algorithm for {fileExtension}: {globalResult.BestAlgorithm}");
                    return globalResult.BestAlgorithm;
                }
            }

            // No learned data - will need to benchmark
            return null;
        }

        /// <summary>
        /// Record compression results to improve future recommendations
        /// </summary>
        public void RecordResult(
            string gameName,
            string fileExtension,
            CompactAlgorithm algorithm,
            double compressionRatio,
            TimeSpan compressionTime)
        {
            // Update game-specific profile
            if (!_data.GameProfiles.ContainsKey(gameName))
            {
                _data.GameProfiles[gameName] = new GameProfile
                {
                    GameName = gameName,
                    FileTypeResults = new Dictionary<string, FileTypeResult>()
                };
            }

            var gameProfile = _data.GameProfiles[gameName];
            
            if (!gameProfile.FileTypeResults.ContainsKey(fileExtension))
            {
                gameProfile.FileTypeResults[fileExtension] = new FileTypeResult
                {
                    Extension = fileExtension,
                    AlgorithmResults = new Dictionary<CompactAlgorithm, AlgorithmMetrics>()
                };
            }

            var fileTypeResult = gameProfile.FileTypeResults[fileExtension];
            
            if (!fileTypeResult.AlgorithmResults.ContainsKey(algorithm))
            {
                fileTypeResult.AlgorithmResults[algorithm] = new AlgorithmMetrics();
            }

            // Update metrics (running average)
            var metrics = fileTypeResult.AlgorithmResults[algorithm];
            metrics.SampleCount++;
            metrics.AverageRatio = ((metrics.AverageRatio * (metrics.SampleCount - 1)) + compressionRatio) / metrics.SampleCount;
            metrics.AverageTime = TimeSpan.FromTicks(((metrics.AverageTime.Ticks * (metrics.SampleCount - 1)) + compressionTime.Ticks) / metrics.SampleCount);

            // Update best algorithm for this file type
            fileTypeResult.SampleCount = fileTypeResult.AlgorithmResults.Values.Sum(m => m.SampleCount);
            var bestAlgo = fileTypeResult.AlgorithmResults.OrderBy(kvp => kvp.Value.AverageRatio).First();
            fileTypeResult.BestAlgorithm = bestAlgo.Key;
            fileTypeResult.BestRatio = bestAlgo.Value.AverageRatio;
            fileTypeResult.BestTime = bestAlgo.Value.AverageTime;

            // Also update global file type profile
            if (!_data.GlobalFileTypeProfiles.ContainsKey(fileExtension))
            {
                _data.GlobalFileTypeProfiles[fileExtension] = new FileTypeResult
                {
                    Extension = fileExtension,
                    AlgorithmResults = new Dictionary<CompactAlgorithm, AlgorithmMetrics>()
                };
            }

            var globalResult = _data.GlobalFileTypeProfiles[fileExtension];
            
            if (!globalResult.AlgorithmResults.ContainsKey(algorithm))
            {
                globalResult.AlgorithmResults[algorithm] = new AlgorithmMetrics();
            }

            var globalMetrics = globalResult.AlgorithmResults[algorithm];
            globalMetrics.SampleCount++;
            globalMetrics.AverageRatio = ((globalMetrics.AverageRatio * (globalMetrics.SampleCount - 1)) + compressionRatio) / globalMetrics.SampleCount;
            globalMetrics.AverageTime = TimeSpan.FromTicks(((globalMetrics.AverageTime.Ticks * (globalMetrics.SampleCount - 1)) + compressionTime.Ticks) / globalMetrics.SampleCount);

            globalResult.SampleCount = globalResult.AlgorithmResults.Values.Sum(m => m.SampleCount);
            var globalBest = globalResult.AlgorithmResults.OrderBy(kvp => kvp.Value.AverageRatio).First();
            globalResult.BestAlgorithm = globalBest.Key;
            globalResult.BestRatio = globalBest.Value.AverageRatio;
            globalResult.BestTime = globalBest.Value.AverageTime;

            SaveDatabase();
        }

        /// <summary>
        /// Get recommended algorithm for a file based on learning
        /// </summary>
        public CompactAlgorithm GetRecommendedAlgorithmForFile(string filePath, string gameName)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            var learned = GetBestAlgorithm(gameName, extension);
            if (learned.HasValue)
            {
                return learned.Value;
            }

            // Fallback to smart defaults based on file type
            return extension switch
            {
                ".txt" or ".log" or ".xml" or ".json" or ".shader" or ".glsl" or ".hlsl" => CompactAlgorithm.LZX, // Text compresses best with LZX
                ".wav" or ".flac" or ".bmp" or ".tga" => CompactAlgorithm.LZX, // Uncompressed media → LZX
                ".dds" or ".exe" or ".dll" => CompactAlgorithm.XPRESS16K, // Already compressed or binary → faster algorithm
                _ => CompactAlgorithm.LZX // Default: best compression
            };
        }

        /// <summary>
        /// Get statistics for a specific game
        /// </summary>
        public GameProfile? GetGameProfile(string gameName)
        {
            return _data.GameProfiles.TryGetValue(gameName, out var profile) ? profile : null;
        }

        /// <summary>
        /// Clear learning data for a specific game
        /// </summary>
        public void ClearGameProfile(string gameName)
        {
            _data.GameProfiles.Remove(gameName);
            SaveDatabase();
        }

        #region Persistence

        private void LoadDatabase()
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    string json = File.ReadAllText(_databasePath);
                    _data = JsonSerializer.Deserialize<LearningData>(json) ?? new LearningData();
                    _logger?.LogInformation($"Loaded learning database: {_data.GameProfiles.Count} games tracked");
                }
                else
                {
                    _data = new LearningData();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load learning database: {ex.Message}");
                _data = new LearningData();
            }
        }

        private void SaveDatabase()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_data, options);
                File.WriteAllText(_databasePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to save learning database: {ex.Message}");
            }
        }

        #endregion

        #region Data Classes

        public class LearningData
        {
            public Dictionary<string, GameProfile> GameProfiles { get; set; } = new();
            public Dictionary<string, FileTypeResult> GlobalFileTypeProfiles { get; set; } = new();
        }

        public class GameProfile
        {
            public string GameName { get; set; } = string.Empty;
            public Dictionary<string, FileTypeResult> FileTypeResults { get; set; } = new();
        }

        public class FileTypeResult
        {
            public string Extension { get; set; } = string.Empty;
            public CompactAlgorithm? BestAlgorithm { get; set; }
            public double BestRatio { get; set; }
            public TimeSpan BestTime { get; set; }
            public int SampleCount { get; set; }
            public Dictionary<CompactAlgorithm, AlgorithmMetrics> AlgorithmResults { get; set; } = new();
        }

        public class AlgorithmMetrics
        {
            public int SampleCount { get; set; }
            public double AverageRatio { get; set; }
            public TimeSpan AverageTime { get; set; }
        }

        #endregion
    }
}
