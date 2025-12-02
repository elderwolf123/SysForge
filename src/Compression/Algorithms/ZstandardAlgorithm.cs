using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ZstdSharp;

namespace RamOptimizer.Compression.Algorithms
{
    /// <summary>
    /// Zstandard compression algorithm - excellent balance of speed and compression ratio
    /// </summary>
    public class ZstandardAlgorithm : ICompressionAlgorithm
    {
        public string Name => "Zstandard";

        public async Task<CompressionResult> CompressAsync(Stream input, Stream output, int level)
        {
            var stopwatch = Stopwatch.StartNew();
            long originalSize = input.Length;

            try
            {
                using var compressor = new CompressionStream(output, level, leaveOpen: true);
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
            using var decompressor = new DecompressionStream(input, leaveOpen: true);
            await decompressor.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
