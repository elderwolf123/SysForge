## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-24 - [Avoid Directory.GetFiles for Large Trees]
**Learning:** The codebase previously used `Directory.GetFiles` with `SearchOption.AllDirectories` to scan whole drives in the compression system, which aggressively allocates a massive string array upfront and easily leads to OutOfMemoryException or severe memory spikes on large file systems.
**Action:** Replaced `Directory.GetFiles` with `Directory.EnumerateFiles` which returns an `IEnumerable<string>` and evaluates paths lazily, drastically reducing the memory footprint during deep directory traversal.
