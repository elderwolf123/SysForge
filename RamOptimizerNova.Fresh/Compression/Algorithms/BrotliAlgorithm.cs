using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace RamOptimizer.Compression.Algorithms
{
    /// <summary>
    /// Brotli compression algorithm - excellent for text and web files
    /// </summary>
    public class BrotliAlgorithm : ICompressionAlgorithm
    {
        public string Name => "Brotli";

        public async Task<CompressionResult> CompressAsync(Stream input, Stream output, int level)
        {
            var stopwatch = Stopwatch.StartNew();
            long originalSize = input.Length;

            try
            {
                // Brotli quality: 0-11 (we map our 1-22 to 0-11)
                int brotliQuality = Math.Clamp(level / 2, 0, 11);

                using var compressor = new BrotliStream(output, (System.IO.Compression.CompressionLevel)brotliQuality, leaveOpen: true);
                await input.CopyToAsync(compressor);
                await compressor.FlushAsync();

                stopwatch.Stop();

                return new CompressionResult
                {
                    OriginalSize = originalSize,
                    CompressedSize = output.Length,
                    Algorithm = Name,
                    CompressionTime = stopwatch.Elapsed,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new CompressionResult
                {
                    OriginalSize = originalSize,
                    Algorithm = Name,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task DecompressAsync(Stream input, Stream output)
        {
            using var decompressor = new BrotliStream(input, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
            await decompressor.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
