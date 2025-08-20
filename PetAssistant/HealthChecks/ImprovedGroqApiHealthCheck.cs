using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetAssistant.HealthChecks;

/// <summary>
/// Improved version using the abstract base class
/// Compare this to GroqApiHealthCheck.cs to see the difference!
/// Notice how much cleaner this is - just the core logic!
/// </summary>
public class ImprovedGroqApiHealthCheck : BaseHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public ImprovedGroqApiHealthCheck(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<ImprovedGroqApiHealthCheck> logger) : base(logger)
    {
        _httpClient = httpClient;
        _apiUrl = configuration.GetValue<string>("GroqSettings:ApiUrl") 
            ?? "https://api.groq.com/openai/v1/chat/completions";
    }

    /// <summary>
    /// Only need to implement the core check logic
    /// Base class handles timing, logging, error handling
    /// </summary>
    protected override async Task<HealthCheckResult> CheckHealthCore(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        request.Headers.Add("Authorization", "Bearer test");
        request.Content = new StringContent(
            """{"model":"test","messages":[{"role":"user","content":"test"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        // Expected responses that indicate API is working
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return HealthCheckResult.Healthy(
                "Groq API is responding",
                new Dictionary<string, object> 
                { 
                    ["status_code"] = response.StatusCode.ToString(),
                    ["api_url"] = _apiUrl
                });
        }

        return HealthCheckResult.Degraded(
            $"Unexpected status: {response.StatusCode}",
            new Dictionary<string, object> 
            { 
                ["status_code"] = response.StatusCode.ToString() 
            });
    }
    
    // Override the timeout for this specific check if needed
    protected override TimeSpan GetTimeout() => TimeSpan.FromSeconds(10);
}

/// <summary>
/// COMPARISON:
/// 
/// Original GroqApiHealthCheck.cs: 60 lines
/// - Handles its own try/catch blocks
/// - Implements logging at each step
/// - Manages timing manually
/// - Duplicates error handling patterns
/// 
/// This ImprovedGroqApiHealthCheck: 35 lines of actual logic
/// - Just implements the core check
/// - Gets timing, logging, error handling from base class
/// - Cleaner, focused on what's unique to this check
/// 
/// The base class handles:
/// - Try/catch wrapper
/// - Timing with Stopwatch
/// - Logging start/completion
/// - Error logging
/// - Adding metadata (duration, timestamp, check name)
/// </summary>