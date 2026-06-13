## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-23 - [Lazy File Enumeration]
**Learning:** Found an O(N) memory allocation bottleneck using `Directory.GetFiles` for large directories/drives in `AdvancedFileCompressionSystem.cs`. `Directory.EnumerateFiles` avoids this by yielding results lazily, preventing massive upfront array memory allocations.
**Action:** Use `Directory.EnumerateFiles` instead of `Directory.GetFiles` when traversing large directories or whole drives.
