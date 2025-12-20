using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ProcessManagement
{
    public class FileCompressionSystem
    {
        private readonly string _compressionDirectory;
        private readonly Dictionary<string, CompressionLevel> _fileTypeCompressionLevels;

        public FileCompressionSystem(string compressionDirectory)
        {
            _compressionDirectory = compressionDirectory;
            _fileTypeCompressionLevels = new Dictionary<string, CompressionLevel>
            {
                { ".txt", CompressionLevel.Optimal },
                { ".log", CompressionLevel.Optimal },
                { ".csv", CompressionLevel.Optimal },
                { ".json", CompressionLevel.Optimal },
                { ".xml", CompressionLevel.Optimal },
                { ".html", CompressionLevel.Optimal },
                { ".css", CompressionLevel.Optimal },
                { ".js", CompressionLevel.Optimal },
                { ".png", CompressionLevel.Fastest },
                { ".jpg", CompressionLevel.Fastest },
                { ".jpeg", CompressionLevel.Fastest },
                { ".gif", CompressionLevel.Fastest },
                { ".bmp", CompressionLevel.Fastest },
                { ".tiff", CompressionLevel.Fastest },
                { ".zip", CompressionLevel.Fastest },
                { ".rar", CompressionLevel.Fastest },
                { ".7z", CompressionLevel.Fastest },
                { ".tar", CompressionLevel.Fastest },
                { ".gz", CompressionLevel.Fastest },
                { ".bz2", CompressionLevel.Fastest }
            };
        }

        public void CompressFile(string filePath)
        {
            Task.Run(async () =>
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File does not exist: {filePath}");
                    return;
                }

                string fileExtension = Path.GetExtension(filePath);
                if (!_fileTypeCompressionLevels.ContainsKey(fileExtension))
                {
                    Console.WriteLine($"No compression level defined for file type: {fileExtension}");
                    return;
                }

                CompressionLevel compressionLevel = GetDynamicCompressionLevel();
                string compressedFilePath = Path.Combine(_compressionDirectory, Path.GetFileName(filePath) + ".gz");

                try
                {
                    using (FileStream originalFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (FileStream compressedFileStream = new FileStream(compressedFilePath, FileMode.Create, FileAccess.Write))
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, compressionLevel))
                    {
                        await originalFileStream.CopyToAsync(compressionStream);
                    }

                    if (IsCompressionSuccessful(compressedFilePath))
                    {
                        Console.WriteLine($"File compressed: {filePath} -> {compressedFilePath}");
                    }
                    else
                    {
                        Console.WriteLine($"Compression failed for file: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to compress file: {filePath}. Error: {ex.Message}");
                }
            });
        }

        private CompressionLevel GetDynamicCompressionLevel()
        {
            double systemLoad = GetSystemLoad();
            if (systemLoad < 30)
            {
                return CompressionLevel.Optimal;
            }
            else if (systemLoad < 60)
            {
                return CompressionLevel.Fastest;
            }
            else
            {
                return CompressionLevel.NoCompression;
            }
        }

        private double GetSystemLoad()
        {
            try
            {
                // Use PerformanceCounter for CPU load monitoring (Windows-compatible)
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First call returns 0, initialize
                System.Threading.Thread.Sleep(100); // Wait for accurate reading
                return cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get CPU load: {ex.Message}");
                // Default to low load (use Optimal compression)
                return 0;
            }
        }

        public void DecompressFile(string compressedFilePath, string originalFilePath)
        {
            try
            {
                using (FileStream compressedFileStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read))
                using (FileStream originalFileStream = new FileStream(originalFilePath, FileMode.Create, FileAccess.Write))
                using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(originalFileStream);
                }

                Console.WriteLine($"File decompressed: {compressedFilePath} -> {originalFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to decompress file: {compressedFilePath}. Error: {ex.Message}");
            }
        }

        private bool IsCompressionSuccessful(string compressedFilePath)
        {
            return File.Exists(compressedFilePath) && new FileInfo(compressedFilePath).Length > 0;
        }
    }
}