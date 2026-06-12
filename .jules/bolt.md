## 2024-06-12 - File Enumeration Optimization
**Learning:** `Directory.GetFiles` causes massive upfront string array allocations which can lead to huge memory spikes when exploring entire drives or deep directory structures in file scanners/compressors.
**Action:** Prefer `Directory.EnumerateFiles` when working with LINQ extensions like `.Where` or simple `foreach` iterations, as it lazily evaluates the file tree and significantly reduces memory footprint.
