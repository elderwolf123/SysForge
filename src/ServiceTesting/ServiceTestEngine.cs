using System.ServiceProcess;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.ServiceTesting;

/// <summary>
/// Result of testing a single service
/// </summary>
public class ServiceTestResult
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsProblematic { get; set; }
    public BlacklistReason Reason { get; set; }
    public PerformanceImpact Impact { get; set; } = new();
    public List<string> EventLogErrors { get; set; } = new();
    public bool WasSkipped { get; set; }
    public string? SkipReason { get; set; }
}

/// <summary>
/// Overall test session results
/// </summary>
public class TestSessionResults
{
    public int TotalServices { get; set; }
    public int TestedServices { get; set; }
    public int SkippedServices { get; set; }
    public int ProblematicServices { get; set; }
    public List<ServiceTestResult> Results { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
}

/// <summary>
/// Main orchestrator for service testing with enhanced recovery
/// </summary>
public class ServiceTestEngine
{
    private readonly ServiceBlacklist _blacklist;
    private readonly CheckpointManager _checkpoint;
    private readonly MetricsMonitor _metrics;
    private readonly RecoverySystem _recovery;
    private readonly ILogger? _logger;
    
    private CancellationTokenSource? _cts;
    private TestSessionResults? _currentSession;

    public event EventHandler<ServiceTestResult>? ServiceTested;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<(int current, int total)>? ProgressChanged;

    public ServiceTestEngine(ILogger? logger = null)
    {
        _logger = logger;
        _blacklist = new ServiceBlacklist();
        _checkpoint = new CheckpointManager();
        _metrics = new MetricsMonitor();
        _recovery = new RecoverySystem(logger);
    }

    #region Test Execution

    /// <summary>
    /// Run comprehensive service testing
    /// </summary>
    public async Task<TestSessionResults> RunComprehensiveTestAsync(
        bool includeUserServices = true,
        bool includeSystemServices = true,
        bool skipEssentialServices = true,
        CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            _currentSession = new TestSessionResults
            {
                StartTime = DateTime.Now
            };

            OnStatusChanged("Initializing test session...");

            // 1. Check for pending recovery
            if (_checkpoint.HasPendingTest())
            {
                _logger?.LogWarning("Resuming from previous test session");
                OnStatusChanged("Resuming from checkpoint...");
            }
            else
            {
                // First time - create restore point
                OnStatusChanged("Creating system restore point...");
                await _recovery.CreateRestorePoint("Before RamOptimizer Service Testing");
            }

            // 2. Enumerate services
            OnStatusChanged("Enumerating services...");
            var services = GetServicesToTest(includeUserServices, includeSystemServices, skipEssentialServices);
            
            _currentSession.TotalServices = services.Count;
            _checkpoint.StartTest(services.Count);

            _logger?.LogInformation($"Found {services.Count} services to test");

            // 3. Test each service
            int index = 0;
            foreach (var service in services)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    _logger?.LogWarning("Test cancelled by user");
                    break;
                }

                index++;
                OnProgressChanged(index, services.Count);

                // Skip already tested services (from checkpoint)
                if (_checkpoint.WasServiceTested(service.ServiceName))
                {
                    _logger?.LogInformation($"Skipping already tested service: {service.DisplayName}");
                    _currentSession.SkippedServices++;
                    continue;
                }

                var result = await TestSingleServiceAsync(service, _cts.Token);
                
                _currentSession.Results.Add(result);
                _currentSession.TestedServices++;

                if (result.IsProblematic)
                {
                    _currentSession.ProblematicServices++;
                }

                ServiceTested?.Invoke(this, result);

                // Update checkpoint
                _checkpoint.UpdateProgress(service.ServiceName, completed: true);

                // Small delay between tests
                await Task.Delay(1000, _cts.Token);
            }

            _currentSession.EndTime = DateTime.Now;
            _checkpoint.EndTest();

            OnStatusChanged($"Testing complete: {_currentSession.ProblematicServices} problematic services found");
            
            return _currentSession;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Test operation was cancelled");
            _currentSession.EndTime = DateTime.Now;
            OnStatusChanged("Test cancelled");
            return _currentSession;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Critical error during testing: {ex.Message}");
            OnStatusChanged("Test failed - initiating recovery");
            
            await _recovery.HandleCriticalFailure("TestEngine", ex);
            throw;
        }
    }

    /// <summary>
    /// Test a single service
    /// </summary>
    private async Task<ServiceTestResult> TestSingleServiceAsync(
        ServiceController service,
        CancellationToken ct)
    {
        var result = new ServiceTestResult
        {
            ServiceName = service.ServiceName,
            DisplayName = service.DisplayName
        };

        try
        {
            _logger?.LogInformation($"Testing service: {service.DisplayName} ({service.ServiceName})");
            OnStatusChanged($"Testing: {service.DisplayName}");

            // Mark as current service in checkpoint
            _checkpoint.SetCurrentService(service.ServiceName);

            // 1. Check if essential
            if (EssentialServices.IsEssential(service.ServiceName))
            {
                result.WasSkipped = true;
                result.SkipReason = "Essential service";
                _logger?.LogInformation($"Skipped essential service: {service.DisplayName}");
                return result;
            }

            // 2. Check if service is running
            service.Refresh();
            if (service.Status != ServiceControllerStatus.Running)
            {
                result.WasSkipped = true;
                result.SkipReason = $"Not running ({service.Status})";
                _logger?.LogInformation($"Skipped non-running service: {service.DisplayName}");
                return result;
            }

            //3. Capture baseline metrics
            OnStatusChanged($"Capturing baseline: {service.DisplayName}");
            var baseline = await _metrics.CaptureBaseline(durationSeconds: 5);

            // 4. Stop the service
            OnStatusChanged($"Stopping service: {service.DisplayName}");
            var stopped = await _recovery.StopService(service.ServiceName);
            
            if (!stopped)
            {
                result.WasSkipped = true;
                result.SkipReason = "Failed to stop";
                return result;
            }

            // 5. Monitor during test period
            OnStatusChanged($"Monitoring system: {service.DisplayName}");
            var testMetrics = await _metrics.MonitorDuring(durationSeconds: 20);

            // 6. Check for issues
            var impact = MetricsMonitor.CalculateImpact(baseline, testMetrics);
            result.Impact = impact;

            if (impact.IsSignificant())
            {
                result.IsProblematic = true;
                result.Reason = DetermineReason(impact);
                
                _logger?.LogWarning($"Service {service.DisplayName} is problematic: {result.Reason}");

                // Add to blacklist
                _blacklist.Add(new ServiceBlacklistEntry
                {
                    ServiceName = service.ServiceName,
                    DisplayName = service.DisplayName,
                    Reason = result.Reason,
                    TestedOn = DateTime.Now,
                    Impact = impact
                });
            }

            // 7. Restart the service
            OnStatusChanged($"Restarting service: {service.DisplayName}");
            await _recovery.RestartService(service.ServiceName);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error testing service {service.DisplayName}: {ex.Message}");
            
            result.WasSkipped = true;
            result.SkipReason = $"Error: {ex.Message}";
            
            // Try to recover
            await _recovery.RestartService(service.ServiceName);
            
            return result;
        }
    }

    #endregion

    #region Service Enumeration

    private List<ServiceController> GetServicesToTest(
        bool includeUser,
        bool includeSystem,
        bool skipEssential)
    {
        var allServices = ServiceController.GetServices();
        var servicesToTest = new List<ServiceController>();

        foreach (var service in allServices)
        {
            // Skip if already blacklisted
            if (_blacklist.IsBlacklisted(service.ServiceName))
            {
                continue;
            }

            // Skip if essential (if option enabled)
            if (skipEssential && EssentialServices.IsEssential(service.ServiceName))
            {
                continue;
            }

            servicesToTest.Add(service);
        }

        return servicesToTest;
    }

    #endregion

    #region Analysis

    private BlacklistReason DetermineReason(PerformanceImpact impact)
    {
        var reasons = new List<BlacklistReason>();

        if (impact.RAMDeltaMB > 100)
            reasons.Add(BlacklistReason.HighRAMUsage);
        
        if (impact.CPUDeltaPercent > 10)
            reasons.Add(BlacklistReason.HighCPUUsage);
        
        if (impact.IODeltaMBps > 10)
            reasons.Add(BlacklistReason.HighIOUsage);
        
        if (impact.PowerDeltaWatts > 3)
            reasons.Add(BlacklistReason.HighPowerUsage);

        return reasons.Count > 1 ? BlacklistReason.Mixed : 
               reasons.Count == 1 ? reasons[0] : BlacklistReason.None;
    }

    #endregion

    #region Control

    /// <summary>
    /// Stop the currently running test
    /// </summary>
    public async Task StopTestAsync()
    {
        _logger?.LogWarning("Stopping test and initiating rollback");
        OnStatusChanged("Stopping test...");
        
        _cts?.Cancel();
        
        // Process rollback queue
        OnStatusChanged("Processing rollback queue...");
        await _recovery.ProcessRollbackQueue();
        
        _checkpoint.EndTest();
        OnStatusChanged("Test stopped");
    }

    #endregion

    #region Events

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    private void OnProgressChanged(int current, int total)
    {
        ProgressChanged?.Invoke(this, (current, total));
    }

    #endregion

    #region Properties

    public ServiceBlacklist Blacklist => _blacklist;
    public bool IsTestRunning => _cts != null && !_cts.IsCancellationRequested;

    #endregion
}
