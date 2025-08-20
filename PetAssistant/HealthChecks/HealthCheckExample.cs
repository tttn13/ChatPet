using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetAssistant.HealthChecks;

/// <summary>
/// This file shows how abstract classes work in practice
/// </summary>
public static class HealthCheckExample
{
    /// <summary>
    /// Register all health checks - shows polymorphism in action
    /// All these different checks are treated as BaseHealthCheck
    /// </summary>
    public static void AddHealthChecks(WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Your existing health check (not using abstract class)
            .AddTypeActivatedCheck<GroqApiHealthCheck>(
                "groq_api_original",
                HealthStatus.Degraded,
                tags: new[] { "api", "external" })
            
            // New health checks using abstract class
            .AddTypeActivatedCheck<CacheHealthCheck>(
                "cache",
                HealthStatus.Degraded,
                tags: new[] { "cache", "internal" })
            
            .AddTypeActivatedCheck<DatabaseHealthCheck>(
                "database", 
                HealthStatus.Unhealthy,
                tags: new[] { "database", "critical" });
    }

    /// <summary>
    /// Map health check endpoints
    /// </summary>
    public static void MapHealthChecks(WebApplication app)
    {
        // Basic health endpoint
        app.MapHealthChecks("/health");
        
        // Detailed health endpoint with custom response
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthResponse
        });
        
        // Critical checks only (database)
        app.MapHealthChecks("/health/critical", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("critical")
        });
    }

    private static async Task WriteDetailedHealthResponse(
        HttpContext context, 
        HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration_ms = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration_ms = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data,
                error = entry.Value.Exception?.Message
            })
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// LEARNING SUMMARY - Why Abstract Classes Matter Here:
/// 
/// 1. CODE REUSE: All health checks get timing, logging, error handling for free
/// 
/// 2. CONSISTENCY: Every health check follows the same pattern
/// 
/// 3. ENFORCEMENT: Can't create a health check without implementing CheckHealthCore
/// 
/// 4. EXTENSIBILITY: Easy to add new health checks - just inherit and implement one method
/// 
/// 5. MAINTENANCE: Change logging/timing once in base class, affects all checks
/// 
/// Compare this to your original GroqApiHealthCheck:
/// - Original: 60 lines with all logic mixed together
/// - With abstract class: ~30 lines of just the core logic
/// - Shared functionality: Timing, logging, error handling (20+ lines saved per check)
/// 
/// With 3+ health checks, you're saving 60+ lines of code and ensuring consistency!
/// </summary>