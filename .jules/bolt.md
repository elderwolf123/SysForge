## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-06-20 - [Avoid Large Upfront Memory Allocations in Directory Scanning]
**Learning:** In C#, using `Directory.GetFiles` on large directories or entire drives causes massive upfront memory allocations because it returns a fully populated array of strings. This can lead to significant latency before iteration begins and memory spikes.
**Action:** Always prefer `Directory.EnumerateFiles` when traversing large directories or whole drives (e.g., in file scanners or compression utilities) to enable lazy evaluation of large file trees.
