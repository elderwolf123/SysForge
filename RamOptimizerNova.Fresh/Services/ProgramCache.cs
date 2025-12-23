using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Services
{
    public class ProgramCache
    {
        private readonly FileLogger _logger = FileLogger.Instance;
        private readonly string _cacheFilePath;
        
        public ProgramCache()
        {
            // Store cache in app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _cacheFilePath = Path.Combine(appDir, "program_cache.json");
            _logger.Log($"[CACHE] Cache file: {_cacheFilePath}");
        }
        
        /// <summary>
        /// Save programs to cache file
        /// </summary>
        public async Task SaveAsync(List<InstalledProgram> programs)
        {
            try
            {
                _logger.Log($"[CACHE] Saving {programs.Count} programs to cache...");
                
                var cache = new CacheData
                {
                    LastUpdated = DateTime.Now,
                    Programs = programs
                };
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                var json = JsonSerializer.Serialize(cache, options);
                await File.WriteAllTextAsync(_cacheFilePath, json);
                
                _logger.Log($"✓ Cache saved successfully ({new FileInfo(_cacheFilePath).Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save program cache: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Load programs from cache file
        /// </summary>
        public async Task<List<InstalledProgram>?> LoadAsync()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    _logger.Log("[CACHE] No cache file found");
                    return null;
                }
                
                _logger.Log("[CACHE] Loading programs from cache...");
                
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                
                var cache = JsonSerializer.Deserialize<CacheData>(json, options);
                
                if (cache == null)
                {
                    _logger.LogWarning("Cache file is empty or invalid");
                    return null;
                }
                
                var age = DateTime.Now - cache.LastUpdated;
                _logger.Log($"✓ Loaded {cache.Programs.Count} programs from cache (Age: {age.TotalHours:F1} hours)");
                
                // Return cached programs if less than 7 days old
                if (age.TotalDays < 7)
                {
                    return cache.Programs;
                }
                else
                {
                    _logger.LogWarning($"Cache is {age.TotalDays:F0} days old - should refresh");
                    return cache.Programs; // Still return but mark as stale
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load program cache: {ex.Message}", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Check if cache exists and is recent
        /// </summary>
        public bool IsCacheValid()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return false;
                    
                var fileInfo = new FileInfo(_cacheFilePath);
                var age = DateTime.Now - fileInfo.LastWriteTime;
                
                return age.TotalDays < 7;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Clear the cache
        /// </summary>
        public void Clear()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    File.Delete(_cacheFilePath);
                    _logger.Log("✓ Cache cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to clear cache: {ex.Message}", ex);
            }
        }
    }
    
    /// <summary>
    /// Cache data structure
    /// </summary>
    public class CacheData
    {
        public DateTime LastUpdated { get; set; }
        public List<InstalledProgram> Programs { get; set; } = new();
    }
}
