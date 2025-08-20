using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetAssistant.HealthChecks;

/// <summary>
/// Concrete implementation #1: Cache Health Check
/// Inherits from BaseHealthCheck to get common functionality
/// </summary>
public class CacheHealthCheck : BaseHealthCheck
{
    private readonly ICacheService _cacheService;

    public CacheHealthCheck(ICacheService cacheService, ILogger<CacheHealthCheck> logger) 
        : base(logger) // Call base constructor
    {
        _cacheService = cacheService;
    }

    /// <summary>
    /// Must implement this abstract method from BaseHealthCheck
    /// This is the ONLY thing we need to implement!
    /// </summary>
    protected override async Task<HealthCheckResult> CheckHealthCore(CancellationToken cancellationToken)
    {
        // Test if cache is working by setting and getting a value
        var testKey = "health_check_test";
        var testValue = Guid.NewGuid().ToString();
        
        // Try to cache a test response
        await _cacheService.SetCachedResponseAsync(testKey, null, testValue, null);
        
        // Try to retrieve it
        var retrieved = await _cacheService.GetCachedResponseAsync(testKey, null);
        
        if (retrieved == testValue)
        {
            var stats = _cacheService.GetStatistics();
            return HealthCheckResult.Healthy(
                "Cache is working",
                new Dictionary<string, object>
                {
                    ["hit_ratio"] = stats.HitRatio,
                    ["total_hits"] = stats.Hits,
                    ["total_misses"] = stats.Misses
                });
        }
        
        return HealthCheckResult.Unhealthy("Cache test failed - unable to retrieve test value");
    }
    
    // We can override the timeout if this check needs more time
    protected override TimeSpan GetTimeout() => TimeSpan.FromSeconds(2);
}