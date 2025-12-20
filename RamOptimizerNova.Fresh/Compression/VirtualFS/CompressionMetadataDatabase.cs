using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RamOptimizer.Compression.VirtualFS
{
    /// <summary>
    /// Metadata database for compressed games in Tier 2 virtual file system
    /// Tracks original file information for transparent access
    /// </summary>
    public class CompressionMetadataDatabase
    {
        private readonly string _metadataPath;
        private CompressionMetadata _metadata;

        public CompressionMetadataDatabase(string compressedGamePath)
        {
            _metadataPath = Path.Combine(compressedGamePath, ".compression_metadata.json");
            LoadMetadata();
        }

        public void SetFileMetadata(string virtualPath, FileMetadata metadata)
        {
            _metadata.Files[virtualPath] = metadata;
            SaveMetadata();
        }

        public FileMetadata? GetFileMetadata(string virtualPath)
        {
            return _metadata.Files.TryGetValue(virtualPath, out var metadata) ? metadata : null;
        }

        public IEnumerable<string> GetAllFilePaths()
        {
            return _metadata.Files.Keys;
        }

        public void SetGameInfo(string gameName, string originalPath, DateTime compressionDate)
        {
            _metadata.GameName = gameName;
            _metadata.OriginalPath = originalPath;
            _metadata.CompressionDate = compressionDate;
            SaveMetadata();
        }

        private void LoadMetadata()
        {
            try
            {
                if (File.Exists(_metadataPath))
                {
                    string json = File.ReadAllText(_metadataPath);
                    _metadata = JsonSerializer.Deserialize<CompressionMetadata>(json) ?? new CompressionMetadata();
                }
                else
                {
                    _metadata = new CompressionMetadata();
                }
            }
            catch
            {
                _metadata = new CompressionMetadata();
            }
        }

        private void SaveMetadata()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_metadata, options);
                File.WriteAllText(_metadataPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }

    public class CompressionMetadata
    {
        public string GameName { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public DateTime CompressionDate { get; set; }
        public string CompressionAlgorithm { get; set; } = "Zstandard-19";
        public Dictionary<string, FileMetadata> Files { get; set; } = new();
    }

    public class FileMetadata
    {
        public string VirtualPath { get; set; } = string.Empty;
        public string CompressedPath { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public string Sha512Hash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool IsDirectory { get; set; }
    }
}
