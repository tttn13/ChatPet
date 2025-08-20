using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetAssistant.Services;

namespace PetAssistant.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        // Detailed health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    Status = report.Status.ToString(),
                    Service = "Pet Assistant API",
                    Timestamp = DateTime.UtcNow,
                    Duration = report.TotalDuration.TotalMilliseconds,
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration.TotalMilliseconds,
                        Description = e.Value.Description
                    })
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

        // Simple health check endpoint
        app.MapGet("/api/health", () =>
            Results.Ok(new
            {
                Status = "Healthy",
                Service = "Pet Assistant API",
                Timestamp = DateTime.UtcNow
            }))
            .WithName("HealthCheck")
            .WithOpenApi()
            .WithSummary("Simple health check endpoint");

        // Metrics endpoint
        app.MapGet("/metrics", (
            ITelemetryService telemetryService,
            ICacheService cacheService,
            IConversationCleanupService cleanupService) =>
        {
            var telemetryStats = telemetryService.GetStats();
            var cacheStats = cacheService.GetStatistics();
            var conversationStats = cleanupService.GetStats();

            return Results.Ok(new
            {
                Service = "Pet Assistant API",
                Timestamp = DateTime.UtcNow,
                Telemetry = telemetryStats,
                Cache = cacheStats,
                Conversations = conversationStats
            });
        })
        .WithName("Metrics")
        .WithOpenApi()
        .WithSummary("Get application metrics and statistics");
    }
}