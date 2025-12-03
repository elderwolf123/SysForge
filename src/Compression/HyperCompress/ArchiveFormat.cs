using System;
using System.IO;
using System.Text;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Defines the .hca (HyperCompressed Archive) file format structures.
/// </summary>
public static class ArchiveFormat
{
    // Magic bytes for .hca files
    public static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("HCMPRES");
    
    public const ushort CurrentVersion = 0x0100; // Version 1.0
    public const int HeaderSize = 512;
    public const int DefaultChunkSize = 4 * 1024 * 1024; // 4MB
    
    /// <summary>
    /// Archive header (512 bytes).
    /// </summary>
    public class Header
    {
        public byte[] Magic { get; set; } = new byte[7]; // "HCMPRES"
        public ushort Version { get; set; }
        public int ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public byte[] AlgorithmFlags { get; set; } = new byte[128]; // Bitfield of used algorithms
        public long LearningDBOffset { get; set; }
        public long IndexOffset { get; set; }
        public byte[] Reserved { get; set; } = new byte[351];
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(ChunkSize);
            writer.Write(TotalChunks);
            writer.Write(AlgorithmFlags);
            writer.Write(LearningDBOffset);
            writer.Write(IndexOffset);
            writer.Write(Reserved);
        }
        
        public static Header ReadFrom(BinaryReader reader)
        {
            var header = new Header
            {
                Magic = reader.ReadBytes(7),
                Version = reader.ReadUInt16(),
                ChunkSize = reader.ReadInt32(),
                TotalChunks = reader.ReadInt32(),
                AlgorithmFlags = reader.ReadBytes(128),
                LearningDBOffset = reader.ReadInt64(),
                IndexOffset = reader.ReadInt64(),
                Reserved = reader.ReadBytes(351)
            };
            
            return header;
        }
        
        public bool IsValid()
        {
            if (Magic.Length != MagicBytes.Length) return false;
            for (int i = 0; i < MagicBytes.Length; i++)
            {
                if (Magic[i] != MagicBytes[i]) return false;
            }
            return Version <= CurrentVersion;
        }
    }
    
    /// <summary>
    /// Chunk table entry.
    /// </summary>
    public class ChunkEntry
    {
        public int ChunkID { get; set; }
        public long FileOffset { get; set; }
        public int CompressedSize { get; set; }
        public int UncompressedSize { get; set; }
        public HyperAlgorithm Algorithm { get; set; }
        public uint Checksum { get; set; } // CRC32
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ChunkID);
            writer.Write(FileOffset);
            writer.Write(CompressedSize);
            writer.Write(UncompressedSize);
            writer.Write((byte)Algorithm);
            writer.Write(Checksum);
        }
        
        public static ChunkEntry ReadFrom(BinaryReader reader)
        {
            return new ChunkEntry
            {
                ChunkID = reader.ReadInt32(),
                FileOffset = reader.ReadInt64(),
                CompressedSize = reader.ReadInt32(),
                UncompressedSize = reader.ReadInt32(),
                Algorithm = (HyperAlgorithm)reader.ReadByte(),
                Checksum = reader.ReadUInt32()
            };
        }
    }
    
    /// <summary>
    /// File index entry.
    /// </summary>
    public class FileEntry
    {
        public int FileID { get; set; }
        public string Name { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public int[] ChunkIDs { get; set; } = Array.Empty<int>();
        public int OffsetInFirstChunk { get; set; }
        public HyperAlgorithm Algorithm { get; set; }
        public DateTime LastModified { get; set; }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(FileID);
            writer.Write((ushort)Name.Length);
            writer.Write(Encoding.UTF8.GetBytes(Name));
            writer.Write(OriginalSize);
            writer.Write(ChunkIDs.Length);
            foreach (var chunkId in ChunkIDs)
                writer.Write(chunkId);
            writer.Write(OffsetInFirstChunk);
            writer.Write((byte)Algorithm);
            writer.Write(LastModified.ToBinary());
        }
        
        public static FileEntry ReadFrom(BinaryReader reader)
        {
            var entry = new FileEntry
            {
                FileID = reader.ReadInt32()
            };
            
            var nameLength = reader.ReadUInt16();
            entry.Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            entry.OriginalSize = reader.ReadInt64();
            
            var chunkCount = reader.ReadInt32();
            entry.ChunkIDs = new int[chunkCount];
            for (int i = 0; i < chunkCount; i++)
                entry.ChunkIDs[i] = reader.ReadInt32();
            
            entry.OffsetInFirstChunk = reader.ReadInt32();
            entry.Algorithm = (HyperAlgorithm)reader.ReadByte();
            entry.LastModified = DateTime.FromBinary(reader.ReadInt64());
            
            return entry;
        }
    }
    
    /// <summary>
    /// Compute CRC32 checksum for chunk data.
    /// </summary>
    public static uint ComputeCRC32(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;
        
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
        }
        
        return ~crc;
    }
}
