using System;
using System.Collections.Generic;
using System.Linq;

namespace RamOptimizer.Compression.VirtualFS
{
    /// <summary>
    /// Intelligent cache for decompressed files with LRU eviction
    /// </summary>
    public class IntelligentDecompressionCache
    {
        private readonly long _maxCacheSizeBytes;
        private readonly Dictionary<string, CachedFile> _cache;
        private long _currentCacheSize;

        public IntelligentDecompressionCache(long maxCacheSizeMB = 2048)
        {
            _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
            _cache = new Dictionary<string, CachedFile>();
            _currentCacheSize = 0;
        }

        /// <summary>
        /// Get file from cache or decompress and cache it
        /// </summary>
        public byte[] GetOrDecompress(string virtualPath, Func<byte[]> decompressFunc)
        {
            // Check cache
            if (_cache.TryGetValue(virtualPath, out var cached))
            {
                cached.LastAccess = DateTime.UtcNow;
                cached.AccessCount++;
                return cached.Data;
            }

            // Decompress
            var data = decompressFunc();

            // Add to cache if space available or after evicting
            EnsureSpace(data.Length);

            _cache[virtualPath] = new CachedFile
            {
                VirtualPath = virtualPath,
                Data = data,
                LastAccess = DateTime.UtcNow,
                AccessCount = 1,
                Size = data.Length
            };
            _currentCacheSize += data.Length;

            return data;
        }

        /// <summary>
        /// Get specific byte range from cached file
        /// </summary>
        public void ReadRange(string virtualPath, byte[] buffer, long offset, int length, Func<byte[]> decompressFunc)
        {
            var fullData = GetOrDecompress(virtualPath, decompressFunc);
            
            // Copy requested range
            Array.Copy(fullData, offset, buffer, 0, length);
        }

        /// <summary>
        /// Clear cache for specific file
        /// </summary>
        public void Invalidate(string virtualPath)
        {
            if (_cache.TryGetValue(virtualPath, out var cached))
            {
                _currentCacheSize -= cached.Size;
                _cache.Remove(virtualPath);
            }
        }

        /// <summary>
        /// Clear entire cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _currentCacheSize = 0;
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                CachedFiles = _cache.Count,
                CurrentSizeMB = _currentCacheSize / (1024.0 * 1024.0),
                MaxSizeMB = _maxCacheSizeBytes / (1024.0 * 1024.0),
                TotalAccesses = _cache.Values.Sum(c => c.AccessCount)
            };
        }

        #region Private Helpers

        private void EnsureSpace(long neededBytes)
        {
            // If already fits, nothing to do
            if (_currentCacheSize + neededBytes <= _maxCacheSizeBytes)
                return;

            // Evict LRU items until we have space
            long freedSpace = 0;
            long targetFree = neededBytes - (_maxCacheSizeBytes - _currentCacheSize);

            var sortedByLRU = _cache.OrderBy(kvp => kvp.Value.LastAccess).ToList();

            foreach (var kvp in sortedByLRU)
            {
                _cache.Remove(kvp.Key);
                _currentCacheSize -= kvp.Value.Size;
                freedSpace += kvp.Value.Size;

                if (freedSpace >= targetFree)
                    break;
            }
        }

        #endregion

        public class CachedFile
        {
            public string VirtualPath { get; set; } = string.Empty;
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public DateTime LastAccess { get; set; }
            public int AccessCount { get; set; }
            public long Size { get; set; }
        }

        public class CacheStatistics
        {
            public int CachedFiles { get; set; }
            public double CurrentSizeMB { get; set; }
            public double MaxSizeMB { get; set; }
            public int TotalAccesses { get; set; }
        }
    }
}
