# Handover Instructions: Compression Benchmark Tool

## 🛑 Critical User Reports (Work In Progress)

The user has reported three specific issues that need immediate attention. The code changes may have been made in source but were **not correctly reflected in the compiled .exe**, or the logic is incomplete.

### 1. Workflow Mismatch (Priority: High)
*   **Issue**: User reports that the auto-restart prompts still appear *after* the RAM cleanup in the compiled `.exe`, even though the code in `Program.cs` seems to have been updated to place them before.
*   **Cause**: Likely a build artifact issue or the `dotnet publish --force` command didn't overwrite the file correctly in the previous session.
*   **Action**: 
    *   Verify `Program.cs` has `// STEP 0: Setup auto-restart FIRST` before `// STEP 1: Optional aggressive RAM cleanup`.
    *   Run a clean build: `dotnet clean` followed by `dotnet publish ... --force`.
    *   **verify timestamp** of the resulting `.exe`.

### 2. Resume Logic Missing RAM Cleanup (Priority: High)
*   **Issue**: When the tool auto-resumes after a crash (via `AutoRestartManager.IsAutoResuming()`), it *only* calls `tester.TestAllUntested()`. It **skips** the RAM optimization step.
*   **Result**: The benchmark resumes but without the aggressive RAM cleanup, potentially causing it to crash again or run slowly.
*   **Action**: 
    *   In `Program.cs`, inside the `if (AutoRestartManager.IsAutoResuming())` block:
    *   Add logic to checks if aggressive mode *was* enabled (maybe save this state to the flag file or a separate settings file).
    *   If enabled, re-initialize `RamOptimizationManager` and call `EnableAggressiveMode()` *before* resuming tests.

### 3. Retry Failed Files (Priority: Medium)
*   **Issue**: User requested a feature to "try testing the failed files after all is done".
*   **Current State**: Failed files might be skipped or marked as error.
*   **Action**:
    *   Add a menu option (e.g., Option 8 or sub-option) or an automatic step at the end of the full workflow.
    *   Logic: Iterate through database, find entries with `TestStatus.Failed` (or similar error state), and attempt to re-run `TestAlgorithm` on them.

---

## 📂 Key Files
*   `CompressionBenchmark/Program.cs`: Main workflow logic, auto-restart detection.
*   `CompressionBenchmark/RamOptimizationManager.cs`: Logic for killing/restoring processes.
*   `CompressionBenchmark/AutoRestartManager.cs`: Task scheduler integration.
*   `CompressionBenchmark/FileTypeDatabase.cs`: State tracking (saves progress).

## 🚀 Next Steps for Next AI
1.  **Read** `Program.cs` to confirm the workflow order is correct in source.
2.  **Fix** the Auto-Resume block in `Program.cs` to include RAM optimization.
3.  **Implement** the "Retry Failed" feature (likely in `CompressionTester.cs` and `Program.cs`).
4.  **Rebuild** with `dotnet clean` and `dotnet publish --force`.
5.  **Confirm** with user that the new .exe works as expected.
