## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.
## 2024-05-15 - [Avoid Directory.GetFiles for whole drives]
**Learning:** Found an aggressive memory allocation issue in `AdvancedFileCompressionSystem` when scanning large directories or whole drives. `Directory.GetFiles` loads all matching file paths into a single array before processing, leading to massive memory spikes or OutOfMemory exceptions when run on `drive.Name`.
**Action:** Replace `Directory.GetFiles` with `Directory.EnumerateFiles` to enable lazy evaluation and stream the file paths, avoiding the upfront allocation of large arrays.

## 2024-05-15 - [Linux Build Environment Limitations with WPF Tests]
**Learning:** `RamOptimizer.Tests.csproj` fails to build in the Linux environment due to missing dependencies and unresolved namespaces (like `Microsoft.Extensions.Logging` and missing `RamOptimizer.csproj` reference).
**Action:** When validating non-logic structural or allocation-related optimizations in Linux, rely on `dotnet build src/ProcessManagement/ProcessManagement.csproj -p:EnableWindowsTargeting=true` as per the memory rule instead of running the broken test project directly.
