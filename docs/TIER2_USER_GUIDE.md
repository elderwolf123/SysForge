# Tier 2: WinFsp Virtual File System - User Guide

## Overview

Tier 2 uses **WinFsp** (Windows File System Proxy) to create a virtual file system that presents compressed files as normal files with transparent on-the-fly decompression.

---

## How It Works

```
Original Game (100GB)
         ↓
Compress with Zstandard level 19
         ↓
Compressed Storage (25GB, ~75% compression)
├─ texture.dds.zst
├─ audio.wav.zst
├─ .compression_metadata.json
         ↓
Mount as Virtual FS at original path
         ↓
C:\Games\Skyrim\ ← Virtual drive
├─ texture.dds  ← WinFsp intercepts
├─ audio.wav    ← Decompresses on-the-fly
└─ (Game sees normal files!)
         ↓
Launch game from Steam/desktop
Game runs normally, doesn't know files are compressed!
```

---

## Benefits vs Tier 1

| Feature | Tier 1 (Windows Compact) | Tier 2 (WinFsp) |
|---------|--------------------------|----------------|
| **Compression** | ~55% (LZX) | **~75%** (Zstd-19) |
| **Algorithm** | Windows built-in | Custom Zstandard |
| **Speed** | Instant access | Cache d access (fast) |
| **Installation** | Built-in | Requires WinFsp |
| **Complexity** | Low | Medium |

---

## Requirements

**1. Install WinFsp:**
- Download from: https://winfsp.dev/rel/
- Install WinFsp runtime
- Reboot (may be required)

**2. System Requirements:**
- RAM for cache (default: 2GB, configurable)
- Admin rights (for junction creation)

---

## Usage Workflow

### Compress a Game

```
1. Select game folder: C:\Games\Skyrim
2. Choose Tier 2 compression
3. Set cache size (default 2GB)
4. Click "Compress Game"
5. Wait for compression (Zstd level 19)
6. Result: Compressed storage at C:\Games\.tier2\Skyrim\
```

### Mount & Play

```
1. Compressed storage created at: C:\Games\.tier2\Skyrim\
2. Virtual FS mounted at: C:\Games\Skyrim\
3. Junction created: Original path → Virtual FS
4. Launch game normally from Steam/desktop
5. Game runs, files decompressed on-the-fly
6. Everything transparent!
```

### Unmount

```
When done playing:
1. Close game
2. Click "Unmount"
3. Virtual FS unmounted
4. Junction removed
5. Original path restored
```

---

## Cache System

**Purpose:** Avoid decompressing same file repeatedly

**How it works:**
```
First access to texture.dds:
├─ Read texture.dds.zst
├─ Decompress with Zstd
├─ Cache in RAM (2GB default)
└─ Return to game

Second access to texture.dds:
├─ Check cache → Found!
└─ Return from RAM (instant!)
```

**LRU Eviction:**
- When cache full, removes least recently used files
- Frequently accessed files stay in cache
- Configurable cache size (512MB - 8GB)

---

## File Structure

**Compressed Storage:**
```
C:\Games\.tier2\Skyrim\
├─ Data\
│  └─ Textures\
│     └─ landscape.dds.zst  ← Compressed with Zstd
├─ SkyrimSE.exe.zst
└─ .compression_metadata.json  ← Metadata database
```

**Metadata:**
```json
{
  "GameName": "Skyrim",
  "OriginalPath": "C:\\Games\\Skyrim",
  "CompressionAlgorithm": "Zstandard-19",
  "Files": {
    "\\Data\\Textures\\landscape.dds": {
      "CompressedPath": "..\\landscape.dds.zst",
      "OriginalSize": 16777216,
      "CompressedSize": 4194304,
      "Sha512Hash": "...",
      "LastModified": "2024-01-15T..."
    }
  }
}
```

---

## Steam Compatibility

**How junction works:**
```
Original: C:\Games\Skyrim\SkyrimSE.exe
          ↓ (game compressed, storage moved)
Storage:  C:\Games\.tier2\Skyrim\SkyrimSE.exe.zst
          ↓ (virtual FS mounted)
Mount:    C:\Temp\Tier2Mounts\Skyrim\SkyrimSE.exe
          ↓ (junction created)
Junction: C:\Games\Skyrim → C:\Temp\Tier2Mounts\Skyrim
          
Steam sees: C:\Games\Skyrim\SkyrimSE.exe ✅
Actually:   Virtual FS with on-the-fly decompression!
```

**Result:** Steam launches game normally, no path changes needed!

---

## Performance

**Compression Speed:**
- Zstd level 19: ~5-15 MB/s
- 100GB game: ~2-6 hours one-time

**Decompression Speed:**
- Zstd decompress: ~200-400 MB/s
- With cache: Instant (RAM speed)
- Without cache: Slight delay first access

**Real-World:**
- Game load times: +5-10% (cached: 0%)
- In-game performance: No difference
- Level streaming: Minimal impact

---

## Troubleshooting

**"Mount failed":**
- Ensure WinFsp is installed
- Check admin rights
- Reboot after WinFsp install

**"Game won't launch":**
- Check junction was created
- Verify mount is active
- Check antivirus isn't blocking WinFsp

**"Performance issues":**
- Increase cache size
- Close other applications
- Check if files are being cached

---

## Safety

**Data Integrity:**
- SHA-512 hashes verify all files
- Metadata tracks original sizes
- Read-only virtual FS (no writes)
- Original files not modified  

**Reversible:**
- Can unmount anytime
- Can decompress back to normal
- Junction easily removed

---

## Comparison: Real-World Example

**Skyrim (100GB):**

**Tier 1 (Windows Compact LZX):**
```
100GB → 45GB (55% compression)
Files at: C:\Games\Skyrim\ (in-place)
Access: Instant
```

**Tier 2 (WinFsp Zstd-19):**
```
100GB → 25GB (75% compression) ← 20GB MORE saved!
Storage: C:\Games\.tier2\Skyrim\
Mount: C:\Games\Skyrim\ (virtual)
Access: Fast (cached) / minimal delay (uncached)
```

**Result:** Tier 2 saves **20GB more** than Tier 1!

---

## Next Steps

Once comfortable with Tier 2, consider:
- **Tier 3 (Ultra Archive):** 90% compression for rarely-played games
- **Auto warm-up:** Automatically mount on game launch
- **Smart caching:** Pre-load frequently used files
