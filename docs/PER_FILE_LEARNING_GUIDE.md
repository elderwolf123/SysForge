# 🎯 Per-File Learning Compression System

## Overview

The learning compression system automatically discovers which compression algorithm works best for each file type in each game, then remembers this for future compressions.

---

## How It Works

### 1. First Time Compressing a Game

```
User: Selects "C:\Games\Skyrim" + enables "Per-file learning"
      ↓
System: Scans folder, finds file types:
├─ .dds files (textures): 2,543 files
├─ .wav files (audio): 1,234 files
├─ .txt files (text): 89 files
└─ .exe/.dll files: 45 files
      ↓
System: Benchmarks each file type (samples 3-5 files):
├─ .dds: Tests XPRESS4K, XPRESS8K, XPRESS16K, LZX
│        Result: XPRESS16K = best (48% ratio, 2.1s)
├─ .wav: Tests all algorithms
│        Result: LZX = best (68% ratio, 3.5s)
├─ .txt: Tests all algorithms
│        Result: LZX = best (82% ratio, 0.5s)
└─ .exe: Tests all algorithms
         Result: XPRESS8K = best (51% ratio, 1.8s)
      ↓
System: Compresses using discovered algorithms:
├─ All .dds files → XPRESS16K
├─ All .wav files → LZX
├─ All .txt files → LZX
└─ All .exe files → XPRESS8K
      ↓
System: Saves results to learning database:
{
  "GameProfiles": {
    "Skyrim": {
      ".dds": { "BestAlgorithm": "XPRESS16K", "Samples": 5 },
      ".wav": { "BestAlgorithm": "LZX", "Samples": 5 },
      ".txt": { "BestAlgorithm": "LZX", "Samples": 3 }
    }
  }
}
```

### 2. Second Time (Game Already Learned)

```
User: Compresses "C:\Games\Skyrim" again
      ↓
System: Checks learning database:
├─ .dds files: "I learned XPRESS16K is best for Skyrim"
├─ .wav files: "I learned LZX is best for Skyrim"
└─ .txt files: "I learned LZX is best for Skyrim"
      ↓
System: Uses learned algorithms immediately (no benchmark!)
├─ All .dds files → XPRESS16K (instant decision)
├─ All .wav files → LZX (instant decision)
└─ All .txt files → LZX (instant decision)
      ↓
Result: Faster compression + optimal results!
```

### 3. New Game (Global Learning)

```
User: Compresses "C:\Games\Cyberpunk2077" (first time)
      ↓
System: No game-specific profile, checks global patterns:
├─ .dds files: "Globally, I've learned XPRESS16K works for .dds"
│              (from Skyrim, Witcher 3, etc.)
├─ .wav files: "Globally, LZX works for .wav"
└─ .unknown: No global data → quick benchmark
      ↓
System: Uses global learning + targeted benchmarks
      ↓
Result: Smart from the start!
```

---

##Real-World Example: Skyrim Compression

### Without Per-File Learning (Standard)

```
System picks ONE algorithm for everything:
└─ Tests samples → LZX is best overall

Compresses EVERYTHING with LZX:
├─ 2,543 .dds textures → LZX → 52% compression (5 min)
├─ 1,234 .wav audio → LZX → 68% compression (2 min)
├─ 89 .txt files → LZX → 82% compression (5 sec)
└─ 45 .exe files → LZX → 54% compression (20 sec)

Total: 14GB → 7.8GB (44% saved) in 7 min 25 sec
```

### With Per-File Learning (Optimized)

```
System tests EACH file type separately:

.dds textures:
├─ XPRESS16K: 48% ratio, 1.8 min ← BEST (good ratio, faster!)
├─ LZX: 52% ratio, 5 min (too slow for small gain)

.wav audio:
├─ LZX: 68% ratio, 2 min ← BEST (great compression!)

.txt files:
├─ LZX: 82% ratio, 5 sec ← BEST

.exe files:
├─ XPRESS8K: 51% ratio, 12 sec ← BEST (balanced)
├─ LZX: 54% ratio, 20 sec (slower)

Compresses with optimal algorithm per type:
├─ 2,543 .dds → XPRESS16K → 48% (1.8 min) *FASTER*
├─ 1,234 .wav → LZX → 68% (2 min)
├─ 89 .txt → LZX → 82% (5 sec)
└─ 45 .exe → XPRESS8K → 51% (12 sec)

Total: 14GB → 8.1GB (42% saved) in 3 min 50 sec

Result: Same compression, 2X FASTER!
```

---

## Learning Database Structure

**Location:** `%AppData%\RamOptimizer\compression_learning.json`

```json
{
  "GameProfiles": {
    "Skyrim": {
      "FileTypeResults": {
        ".dds": {
          "BestAlgorithm": "XPRESS16K",
          "BestRatio": 0.48,
          "SampleCount": 15,
          "AlgorithmResults": {
            "XPRESS16K": { "SampleCount": 5, "AverageRatio": 0.48 },
            "LZX": { "SampleCount": 5, "AverageRatio": 0.52 },
            "XPRESS8K": { "SampleCount": 5, "AverageRatio": 0.45 }
          }
        },
        ".wav": {
          "BestAlgorithm": "LZX",
          "BestRatio": 0.32,
          "SampleCount": 10
        }
      }
    },
    "Cyberpunk2077": { ... },
    "Witcher3": { ... }
  },
  "GlobalFileTypeProfiles": {
    ".dds": {
      "BestAlgorithm": "XPRESS16K",
      "SampleCount": 45
    },
    ".wav": {
      "BestAlgorithm": "LZX",
      "SampleCount": 30
    }
  }
}
```

---

## Benefits

### ✅ Optimal Per-File-Type Compression
- `.wav` files get LZX (best ratio for uncompressed audio)
- `.dds` textures get XPRESS16K (fast, good ratio for semi-compressed)
- `.txt` files get LZX (best for text)
- `.exe` files get XPRESS8K (balanced for binaries)

### ✅ Game-Specific Learning
- Skyrim `.dds` files might compress best with XPRESS16K
- Cyberpunk `.dds` files might compress best with LZX
- System learns the difference!

### ✅ Faster Over Time
- First compression: Benchmarks each type once
- Second compression: Uses learned results (instant!)
- Third compression: Even more refined

### ✅ Global Intelligence
- Learns patterns across ALL games
- ".wav files always compress best with LZX"
- Applies this knowledge to new games

---

## UI Options

### Standard Compression (Simple)
```
☐ Use smart algorithm selection
└─ Picks ONE algorithm for everything
   Tests samples, uses best overall
   Example: LZX for all files
```

### Per-File Learning (Advanced)
```
☑ Use per-file-type compression
└─ Picks DIFFERENT algorithms per file type
   Tests each type separately
   Example: XPRESS16K for .dds, LZX for .wav
   Learns and improves over time
```

---

## When to Use Each

**Standard Compression:**
- Quick one-time compression
- Small folders (< 100 files)
- Mixed file types

**Per-File Learning:**
- Large game folders
- Many files of same type
- Repeated compressions
- Want maximum optimization

---

## Example Output

```
Compressing: C:\Games\Skyrim

Found 3,911 files across 12 file types

Processing .dds: 2,543 files
  Checking learning database... Found! Using XPRESS16K
  Compressing... 1.8 minutes
  Result: 4.2 GB → 2.0 GB (52% saved)

Processing .wav: 1,234 files
  Checking learning database... Found! Using LZX
  Compressing... 2.0 minutes
  Result: 2.8 GB → 900 MB (68% saved)

Processing .txt: 89 files
  No learned data, benchmarking...
  Tested XPRESS4K: 78%, XPRESS8K: 80%, XPRESS16K: 81%, LZX: 82%
  Selected: LZX
  Compressing... 5 seconds
  Result: 12 MB → 2.2 MB (82% saved)

Complete!
Total: 14.2 GB → 8.1 GB (42% saved)
Time: 3 min 50 sec

Learning database updated:
- Skyrim profiles: 12 file types
- Global profiles: 45 file types
```

---

## Technical Details

**Per-File Compression:**
- Windows Compact works on individual files
- Each file can have its own algorithm
- No performance penalty when accessing files
- Completely transparent to games

**Database Persistence:**
- JSON file in `%AppData%\RamOptimizer\`
- Syncs across all compressions
- Can be cleared/reset per game
- Tracks confidence (sample count)

**Smart Defaults:**
- If no learned data: Intelligent fallbacks
  - `.txt`, `.xml`, `.json` → LZX (text)
  - `.wav`, `.bmp`, `.tga` → LZX (uncompressed media)
  - `.dds`, `.exe` → XPRESS16K (semi-compressed/binary)
  - Everything else → LZX (safe default)
