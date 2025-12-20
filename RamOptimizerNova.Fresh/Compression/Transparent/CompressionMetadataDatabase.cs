using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace RamOptimizer.Compression.Transparent
{
    /// <summary>
    /// Tracks compressed files and their metadata for Tier 2 transparent compression
    /// </summary>
    public class CompressionMetadataDatabase
    {
        private readonly string _databasePath;
        private Dictionary<string, FileMetadata> _metadata;

        public CompressionMetadataDatabase(string databasePath)
        {
            _databasePath = databasePath;
            _metadata = new Dictionary<string, FileMetadata>();
            Load();
        }

        public void AddFile(string originalPath, string compressedPath, long originalSize, long compressedSize, string hash)
        {
            _metadata[originalPath] = new FileMetadata
            {
                OriginalPath = originalPath,
                CompressedPath = compressedPath,
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                Hash = hash,
                CompressedDate = DateTime.UtcNow
            };
            Save();
        }

        public FileMetadata? GetMetadata(string originalPath)
        {
            return _metadata.TryGetValue(originalPath, out var metadata) ? metadata : null;
        }

        public IEnumerable<FileMetadata> GetAllMetadata()
        {
            return _metadata.Values;
        }

        public bool ContainsFile(string originalPath)
        {
            return _metadata.ContainsKey(originalPath);
        }

        public void RemoveFile(string originalPath)
        {
            _metadata.Remove(originalPath);
            Save();
        }

        public string ComputeFileHash(string filePath)
        {
            using var sha = SHA512.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void Load()
        {
            if (File.Exists(_databasePath))
            {
                try
                {
                    var json = File.ReadAllText(_databasePath);
                    _metadata = JsonSerializer.Deserialize<Dictionary<string, FileMetadata>>(json) 
                                ?? new Dictionary<string, FileMetadata>();
                }
                catch
                {
                    _metadata = new Dictionary<string, FileMetadata>();
                }
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_databasePath, json);
        }

        public class FileMetadata
        {
            public string OriginalPath { get; set; } = "";
            public string CompressedPath { get; set; } = "";
            public long OriginalSize { get; set; }
            public long CompressedSize { get; set; }
            public string Hash { get; set; } = "";
            public DateTime CompressedDate { get; set; }
            public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
        }
    }
}
