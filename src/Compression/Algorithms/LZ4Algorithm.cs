using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;

namespace RamOptimizer.Compression.Algorithms
{
    /// <summary>
    /// LZ4 compression algorithm - extremely fast decompression
    /// </summary>
    public class LZ4Algorithm : ICompressionAlgorithm
    {
        public string Name => "LZ4";

        public async Task<CompressionResult> CompressAsync(Stream input, Stream output, int level)
        {
            var stopwatch = Stopwatch.StartNew();
            long originalSize = input.Length;

            try
            {
                // LZ4 level mapping: 1-3 = fast, 4-9 = high compression
                var lz4Level = level switch
                {
                    <= 3 => K4os.Compression.LZ4.LZ4Level.L00_FAST,
                    <= 6 => K4os.Compression.LZ4.LZ4Level.L03_HC,
                    <= 9 => K4os.Compression.LZ4.LZ4Level.L09_HC,
                    _ => K4os.Compression.LZ4.LZ4Level.L12_MAX
                };

                using var compressor = LZ4Stream.Encode(output, lz4Level, leaveOpen: true);
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
            using var decompressor = LZ4Stream.Decode(input, leaveOpen: true);
            await decompressor.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
