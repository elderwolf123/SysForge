using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression
{
    /// <summary>
    /// File type enumeration for smart algorithm selection
    /// </summary>
    public enum FileType
    {
        Unknown,
        Text,
        Document,
        Image,
        Video,
        Audio,
        Executable,
        Archive,
        Database
    }

    /// <summary>
    /// Classifies files by type for optimal compression algorithm selection
    /// </summary>
    public class FileTypeClassifier
    {
        private readonly Dictionary<string, FileType> _extensionMap;

        public FileTypeClassifier()
        {
            _extensionMap = new Dictionary<string, FileType>(StringComparer.OrdinalIgnoreCase)
            {
                // Text files - Best with Brotli or Zstd
                [".txt"] = FileType.Text,
                [".log"] = FileType.Text,
                [".csv"] = FileType.Text,
                [".json"] = FileType.Text,
                [".xml"] = FileType.Text,
                [".html"] = FileType.Text,
                [".htm"] = FileType.Text,
                [".css"] = FileType.Text,
                [".js"] = FileType.Text,
                [".ts"] = FileType.Text,
                [".md"] = FileType.Text,
                [".yaml"] = FileType.Text,
                [".yml"] = FileType.Text,
                [".ini"] = FileType.Text,
                [".cfg"] = FileType.Text,
                [".conf"] = FileType.Text,
                
                // Game Assets - Shaders, scripts (compress very well!)
                [".shader"] = FileType.Text,
                [".glsl"] = FileType.Text,
                [".hlsl"] = FileType.Text,
                [".cg"] = FileType.Text,
                [".fx"] = FileType.Text,
                [".lua"] = FileType.Text,
                [".uc"] = FileType.Text,
                
                // Documents - Good with Zstd
                [".pdf"] = FileType.Document,
                [".doc"] = FileType.Document,
                [".docx"] = FileType.Document,
                [".xls"] = FileType.Document,
                [".xlsx"] = FileType.Document,
                [".ppt"] = FileType.Document,
                [".pptx"] = FileType.Document,
                [".odt"] = FileType.Document,
                [".ods"] = FileType.Document,
                [".odp"] = FileType.Document,
                
                // Images - Already compressed, skip or use LZ4
                [".jpg"] = FileType.Image,
                [".jpeg"] = FileType.Image,
                [".png"] = FileType.Image,
                [".gif"] = FileType.Image,
                [".bmp"] = FileType.Image,      // Uncompressed - compresses well!
                [".tiff"] = FileType.Image,     // Uncompressed - compresses well!
                [".tif"] = FileType.Image,
                [".tga"] = FileType.Image,      // Game texture - often uncompressed
                [".dds"] = FileType.Image,      // Game texture - may be uncompressed
                [".webp"] = FileType.Image,
                [".svg"] = FileType.Text,       // SVG is text-based
                [".ico"] = FileType.Image,
                
                // Video - Already compressed, skip
                [".mp4"] = FileType.Video,
                [".avi"] = FileType.Video,      // Old format - often uncompressed
                [".mkv"] = FileType.Video,
                [".mov"] = FileType.Video,
                [".wmv"] = FileType.Video,      // Old format - sometimes uncompressed
                [".flv"] = FileType.Video,
                [".webm"] = FileType.Video,
                [".m4v"] = FileType.Video,
                
                // Audio - Already compressed, skip
                [".mp3"] = FileType.Audio,
                [".wma"] = FileType.Audio,
                [".aac"] = FileType.Audio,
                [".ogg"] = FileType.Audio,      // Vorbis - already compressed
                [".flac"] = FileType.Audio,     // Lossless - can still compress 20-30%!
                [".m4a"] = FileType.Audio,
                [".opus"] = FileType.Audio,
                [".wav"] = FileType.Audio,      // Uncompressed - compresses 50-70%!
               
                // Executables - Zstd or LZMA
                [".exe"] = FileType.Executable,
                [".dll"] = FileType.Executable,
                [".sys"] = FileType.Executable,
                [".so"] = FileType.Executable,
                [".dylib"] = FileType.Executable,
                
                // Archives - Already compressed, skip
                [".zip"] = FileType.Archive,
                [".rar"] = FileType.Archive,
                [".7z"] = FileType.Archive,
                [".tar"] = FileType.Archive,
                [".gz"] = FileType.Archive,
                [".bz2"] = FileType.Archive,
                [".xz"] = FileType.Archive,
                [".zst"] = FileType.Archive,
                
                // Databases - Good with Zstd
                [".db"] = FileType.Database,
                [".sqlite"] = FileType.Database,
                [".mdb"] = FileType.Database,
                [".accdb"] = FileType.Database
            };
        }

        public FileType Classify(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            
            if (string.IsNullOrEmpty(extension))
                return FileType.Unknown;
            
            return _extensionMap.TryGetValue(extension, out var fileType) 
                ? fileType 
                : FileType.Unknown;
        }

        public bool ShouldSkipCompression(string filePath, bool allowMediaCompression = false)
        {
            var fileType = Classify(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Always allow uncompressed formats (even if media compression disabled)
            // These compress 50-80% typically
            var uncompressedFormats = new[]  {
                ".bmp", ".tif", ".tiff", ".tga", ".dds",  // Uncompressed/game images
                ".wav", ".flac", ".aiff", ".aif",          // Uncompressed audio
                ".avi", ".wmv",                            // Old uncompressed video
                ".cr2", ".nef", ".arw", ".dng", ".raw"     // RAW camera images
            };
            
            if (uncompressedFormats.Contains(extension))
                return false;  // Don't skip - these compress well!
            
            // If media compression allowed, don't skip any media
            if (allowMediaCompression)
                return false;
            
            // Skip already compressed formats
            return fileType switch
            {
                FileType.Image => true,   // JPEGs, PNGs (already compressed)
                FileType.Video => true,   // Video always compressed
                FileType.Audio => true,   // Audio usually compressed (MP3, AAC, etc.)
                FileType.Archive => true, // Archives already compressed
                _ => false
            };
        }

        public string GetRecommendedAlgorithm(string filePath, long fileSize)
        {
            var fileType = Classify(filePath);
            
            // Size-based decisions
            bool isLargeFile = fileSize > 100 * 1024 * 1024; // > 100MB
            bool isSmallFile = fileSize < 1024 * 1024;        // < 1MB
            
            return (fileType, isLargeFile, isSmallFile) switch
            {
                // Text files - Brotli for best compression
                (FileType.Text, _, _) => "Brotli",
                
                // Documents - Zstd for balance
                (FileType.Document, _, _) => "Zstandard",
                
                // Executables - Zstd (or LZMA for very small files)
                (FileType.Executable, _, true) => "Zstandard", // Could use LZMA
                (FileType.Executable, _, _) => "Zstandard",
                
                // Databases - Zstd for good compression + reasonable speed
                (FileType.Database, _, _) => "Zstandard",
                
                // Large files - faster algorithm
                (_, true, _) => "Zstandard",  // Fast + good compression
                
                // Small files - maximize compression
                (_, _, true) => "Zstandard",
                
                // Default
                _ => "Zstandard"
            };
        }

        public int GetRecommendedCompressionLevel(string filePath, long fileSize)
        {
            var fileType = Classify(filePath);
            bool isLargeFile = fileSize > 100 * 1024 * 1024; // > 100MB
            
            return (fileType, isLargeFile) switch
            {
                // For very large files, use lower compression for speed
                (_, true) => 5,
                
                // Text files compress very well, can use higher levels
                (FileType.Text, _) => 15,
                
                // Documents - moderate level
                (FileType.Document, _) => 10,
                
                // Executables - good compression
                (FileType.Executable, _) => 12,
                
                // Default - balanced
                _ => 10
            };
        }
    }
}
