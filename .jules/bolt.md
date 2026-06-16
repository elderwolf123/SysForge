## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2026-06-16 - [Optimizing file tree scanning]
**Learning:** In C#, `Directory.GetFiles()` eagerly materializes the entire list of files into a `string[]`. When dealing with large directories or entire drives (like when scanning for files to compress), this leads to massive upfront string array allocations, causing memory spikes and longer time to first evaluation. `Directory.EnumerateFiles()` returns an `IEnumerable<string>` instead, which is evaluated lazily as we iterate over it.
**Action:** Use `Directory.EnumerateFiles` over `Directory.GetFiles` in large directories/whole drives scanning where results can be processed sequentially (e.g. in `AdvancedFileCompressionSystem`).
