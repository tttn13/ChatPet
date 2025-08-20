using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace PetAssistant.HealthChecks;

/// <summary>
/// Abstract base class for all health checks in the system.
/// This demonstrates a REAL use case where abstract classes make sense:
/// - Multiple health checks (Database, API, Cache, etc.)
/// - Shared logic (timing, logging, error handling)
/// - Enforced structure (all checks must implement CheckHealthCore)
/// </summary>
public abstract class BaseHealthCheck : IHealthCheck
{
    protected readonly ILogger Logger;
    private readonly string _checkName;
    
    protected BaseHealthCheck(ILogger logger)
    {
        Logger = logger;
        _checkName = GetType().Name;
    }
    
    /// <summary>
    /// Template Method Pattern - defines the algorithm structure
    /// Derived classes can't change this flow, only implement CheckHealthCore
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Log start
            Logger.LogDebug("Starting health check: {CheckName}", _checkName);
            
            // Call the abstract method - derived classes MUST implement this
            var result = await CheckHealthCore(cancellationToken);
            
            // Add common metadata
            var data = new Dictionary<string, object>(result.Data)
            {
                ["check_name"] = _checkName,
                ["duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["timestamp"] = DateTime.UtcNow
            };
            
            // Log result
            Logger.LogInformation("Health check {CheckName} completed: {Status} in {Duration}ms", 
                _checkName, result.Status, stopwatch.ElapsedMilliseconds);
            
            return new HealthCheckResult(result.Status, result.Description, data: data);
        }
        catch (Exception ex)
        {
            // Common error handling for all health checks
            Logger.LogError(ex, "Health check {CheckName} failed", _checkName);
            
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"{_checkName} check failed: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["check_name"] = _checkName,
                    ["duration_ms"] = stopwatch.ElapsedMilliseconds,
                    ["error"] = ex.Message
                });
        }
    }
    
    /// <summary>
    /// Abstract method - each health check MUST implement this
    /// This is where the specific check logic goes
    /// </summary>
    protected abstract Task<HealthCheckResult> CheckHealthCore(CancellationToken cancellationToken);
    
    /// <summary>
    /// Virtual method - derived classes CAN override if needed
    /// Provides a default timeout behavior
    /// </summary>
    protected virtual TimeSpan GetTimeout() => TimeSpan.FromSeconds(5);
}