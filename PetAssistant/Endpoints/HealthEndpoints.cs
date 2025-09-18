using PetAssistant.Services;
using StackExchange.Redis;

namespace PetAssistant.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
      
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

        
        app.MapGet("/api/health/redis", async (IConnectionMultiplexer redis, ILogger<Program> logger) =>
        {
            try
            {
                var db = redis.GetDatabase();

                var pingTime = await db.PingAsync();

                var testKey = $"health-check:{Guid.NewGuid()}";
                await db.StringSetAsync(testKey, "test abc", TimeSpan.FromSeconds(10));
                var testValue = await db.StringGetAsync(testKey);
                await db.KeyDeleteAsync(testKey);
                var response = new
                {
                    Status = "Connected",
                    Redis = new
                    {
                        IsConnected = redis.IsConnected,
                        PingTime = $"{pingTime.TotalMilliseconds:F2}ms",
                        Endpoint = redis.GetEndPoints()[0].ToString(),
                    },
                    TestResult = testValue == "test abc" ? "Read/Write OK" : "Read/Write Failed",
                    Timestamp = DateTime.UtcNow
                };

                try
                {
                    var server = redis.GetServer(redis.GetEndPoints()[0]);
                    var serverInfo = await server.InfoAsync();

                    return Results.Ok(new
                    {
                        response.Status,
                        Redis = new
                        {
                            response.Redis.IsConnected,
                            response.Redis.PingTime,
                            response.Redis.Endpoint,
                            ServerVersion = serverInfo?.FirstOrDefault(x => x.Key == "Server")?.FirstOrDefault(x => x.Key == "redis_version").Value ?? "Unknown",
                            ConnectedClients = serverInfo?.FirstOrDefault(x => x.Key == "Clients")?.FirstOrDefault(x => x.Key == "connected_clients").Value ?? "Unknown",
                            UsedMemory = serverInfo?.FirstOrDefault(x => x.Key == "Memory")?.FirstOrDefault(x => x.Key == "used_memory_human").Value ?? "Unknown"
                        },
                        response.TestResult,
                        response.Timestamp
                    });
                }
                catch (RedisCommandException cmdEx) when (cmdEx.Message.Contains("admin mode"))
                {
                    logger.LogWarning("Redis INFO command requires admin mode - returning basic health check");
                    return Results.Ok(response);
                }
            }
            catch (RedisConnectionException ex)
            {
                logger.LogError(ex, "Redis connection failed");
                return Results.Problem(
                    title: "Redis Connection Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error checking Redis health");
                return Results.Problem(
                    title: "Health Check Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("RedisHealthCheck")
        .WithOpenApi()
        .WithSummary("Check Redis connection status and performance");

        app.MapGet("/metrics", (
            ITelemetryPetService telemetryService,
            IConversationCleanupPetService cleanupService) =>
        {
            var telemetryStats = telemetryService.GetStats();
            var conversationStats = cleanupService.GetStats();

            return Results.Ok(new
            {
                Service = "Pet Assistant API",
                Timestamp = DateTime.UtcNow,
                Telemetry = telemetryStats,
                Conversations = conversationStats
            });
        })
        .WithName("Metrics")
        .WithOpenApi()
        .WithSummary("Get application metrics and statistics");
    }
}