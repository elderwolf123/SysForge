using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamOptimizerNova.Models;

namespace RamOptimizerNova.Services;

/// <summary>
/// Interface for performance monitoring service
/// </summary>
public interface IPerformanceMonitoringService : IDisposable
{
    /// <summary>
    /// Event raised when performance metrics are updated
    /// </summary>
    event EventHandler<PerformanceMetricsUpdatedEventArgs>? PerformanceMetricsUpdated;

    /// <summary>
    /// Event raised when a performance alert is detected
    /// </summary>
    event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;

    /// <summary>
    /// Event raised when a performance error occurs
    /// </summary>
    event EventHandler<PerformanceErrorEventArgs>? PerformanceError;

    /// <summary>
    /// Initialize the performance monitoring service
    /// </summary>
    /// <returns>Task representing the initialization operation</returns>
    Task InitializeAsync();

    /// <summary>
    /// Start monitoring performance metrics
    /// </summary>
    /// <returns>Task representing the start operation</returns>
    Task StartMonitoringAsync();

    /// <summary>
    /// Stop monitoring performance metrics
    /// </summary>
    /// <returns>Task representing the stop operation</returns>
    Task StopMonitoringAsync();

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    /// <returns>Current performance metrics</returns>
    PerformanceMetricsModel GetCurrentMetrics();

    /// <summary>
    /// Get performance history
    /// </summary>
    /// <param name="count">Number of recent snapshots to retrieve</param>
    /// <returns>List of performance snapshots</returns>
    List<PerformanceSnapshot> GetPerformanceHistory(int count = 10);

    /// <summary>
    /// Analyze performance over a specified time range
    /// </summary>
    /// <param name="timeRange">Time range to analyze</param>
    /// <returns>Performance analysis report</returns>
    Task<PerformanceAnalysisReport> AnalyzePerformanceAsync(TimeSpan timeRange);

    /// <summary>
    /// Get current health status
    /// </summary>
    /// <returns>Current health status</returns>
    Task<PerformanceHealthStatus> GetHealthStatusAsync();

    /// <summary>
    /// Force a performance snapshot
    /// </summary>
    /// <returns>Task representing the snapshot operation</returns>
    Task ForceSnapshotAsync();
}