using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetAssistant.HealthChecks;

public class GroqApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqApiHealthCheck> _logger;
    private readonly string _apiUrl;

    public GroqApiHealthCheck(HttpClient httpClient, IConfiguration configuration, ILogger<GroqApiHealthCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiUrl = configuration.GetValue<string>("GroqSettings:ApiUrl") ?? "https://api.groq.com/openai/v1/chat/completions";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a minimal request to test API connectivity
            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.Add("Authorization", $"Bearer test");
            request.Content = new StringContent(
                """{"model":"test","messages":[{"role":"user","content":"test"}]}""",
                System.Text.Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // We expect 401 Unauthorized (invalid token) or 400 Bad Request (invalid model)
            // These indicate the API is responding, just rejecting our test request
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return HealthCheckResult.Healthy("Groq API is responding");
            }

            // If we get a different status, log it for investigation
            _logger.LogWarning("Groq API health check returned unexpected status: {StatusCode}", response.StatusCode);
            return HealthCheckResult.Degraded($"Groq API returned unexpected status: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Groq API health check timed out");
            return HealthCheckResult.Unhealthy("Groq API health check timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Groq API health check failed due to network error");
            return HealthCheckResult.Unhealthy($"Groq API network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API health check failed unexpectedly");
            return HealthCheckResult.Unhealthy($"Groq API health check failed: {ex.Message}");
        }
    }
}