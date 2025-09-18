using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PetAssistant.Services;

public interface ITelemetryPetService
{
    void RecordApiCall(string endpoint, TimeSpan duration, bool success);
    void RecordCacheHit(bool hit);
    void RecordGroqApiCall(TimeSpan duration, bool success, string? errorCode = null);
    void RecordUserRequest(string sessionId, bool hasProfile);
    TelemetryStats GetStats();
}
/// <summary>
/// monitoring and metrics collection service that tracks various performance and usage metrics for PetAssistant API
/// </summary>
public class TelemetryService : ITelemetryPetService, IDisposable
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _apiCallsCounter;
    private readonly Counter<long> _cacheHitsCounter;
    private readonly Counter<long> _groqApiCallsCounter;
    private readonly Counter<long> _userRequestsCounter;
    
    // Histograms
    private readonly Histogram<double> _apiDurationHistogram;
    private readonly Histogram<double> _groqDurationHistogram;
    
    // Manual tracking for basic stats
    private long _totalApiCalls = 0;
    private long _failedApiCalls = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private long _groqApiCalls = 0;
    private long _groqApiErrors = 0;
    private readonly Dictionary<string, long> _errorCounts = new();

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _meter = new Meter("PetAssistant.API", "1.0.0");
        
        // Initialize counters
        _apiCallsCounter = _meter.CreateCounter<long>(
            "pet_assistant_api_calls_total", 
            "Total number of API calls");
            
        _cacheHitsCounter = _meter.CreateCounter<long>(
            "pet_assistant_cache_hits_total", 
            "Total number of cache hits/misses");
            
        _groqApiCallsCounter = _meter.CreateCounter<long>(
            "pet_assistant_groq_calls_total", 
            "Total number of Groq API calls");
            
        _userRequestsCounter = _meter.CreateCounter<long>(
            "pet_assistant_user_requests_total", 
            "Total number of user requests");
        
        // Initialize histograms
        _apiDurationHistogram = _meter.CreateHistogram<double>(
            "pet_assistant_api_duration_seconds", 
            "API call duration in seconds");
            
        _groqDurationHistogram = _meter.CreateHistogram<double>(
            "pet_assistant_groq_duration_seconds", 
            "Groq API call duration in seconds");
    }

    public void RecordApiCall(string endpoint, TimeSpan duration, bool success)
    {
        Interlocked.Increment(ref _totalApiCalls);
        if (!success)
        {
            Interlocked.Increment(ref _failedApiCalls);
        }

        var tags = new TagList
        {
            { "endpoint", endpoint },
            { "success", success.ToString().ToLower() }
        };

        _apiCallsCounter.Add(1, tags);
        _apiDurationHistogram.Record(duration.TotalSeconds, tags);

        _logger.LogInformation("API call recorded: {Endpoint}, Duration: {Duration}ms, Success: {Success}", 
            endpoint, duration.TotalMilliseconds, success);
    }

    public void RecordCacheHit(bool hit)
    {
        if (hit)
        {
            Interlocked.Increment(ref _cacheHits);
        }
        else
        {
            Interlocked.Increment(ref _cacheMisses);
        }

        var tags = new TagList { { "result", hit ? "hit" : "miss" } };
        _cacheHitsCounter.Add(1, tags);
    }

    public void RecordGroqApiCall(TimeSpan duration, bool success, string? errorCode = null)
    {
        Interlocked.Increment(ref _groqApiCalls);
        if (!success)
        {
            Interlocked.Increment(ref _groqApiErrors);
            
            if (!string.IsNullOrEmpty(errorCode))
            {
                lock (_errorCounts)
                {
                    _errorCounts[errorCode] = _errorCounts.GetValueOrDefault(errorCode) + 1;
                }
            }
        }

        var tags = new TagList
        {
            { "success", success.ToString().ToLower() }
        };
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            tags.Add("error_code", errorCode);
        }

        _groqApiCallsCounter.Add(1, tags);
        _groqDurationHistogram.Record(duration.TotalSeconds, tags);

        _logger.LogInformation("Groq API call recorded: Duration: {Duration}ms, Success: {Success}, ErrorCode: {ErrorCode}", 
            duration.TotalMilliseconds, success, errorCode ?? "none");
    }

    public void RecordUserRequest(string sessionId, bool hasProfile)
    {
        var tags = new TagList
        {
            { "has_profile", hasProfile.ToString().ToLower() }
        };

        _userRequestsCounter.Add(1, tags);

        _logger.LogDebug("User request recorded: SessionId: {SessionId}, HasProfile: {HasProfile}", 
            sessionId[..8] + "...", hasProfile);
    }

    public TelemetryStats GetStats()
    {
        var totalRequests = _cacheHits + _cacheMisses;
        var cacheHitRatio = totalRequests > 0 ? (double)_cacheHits / totalRequests * 100 : 0;
        var apiSuccessRate = _totalApiCalls > 0 ? (double)(_totalApiCalls - _failedApiCalls) / _totalApiCalls * 100 : 0;
        var groqSuccessRate = _groqApiCalls > 0 ? (double)(_groqApiCalls - _groqApiErrors) / _groqApiCalls * 100 : 0;

        Dictionary<string, long> errorCountsCopy;
        lock (_errorCounts)
        {
            errorCountsCopy = new Dictionary<string, long>(_errorCounts);
        }

        return new TelemetryStats(
            TotalApiCalls: _totalApiCalls,
            FailedApiCalls: _failedApiCalls,
            ApiSuccessRate: Math.Round(apiSuccessRate, 2),
            CacheHits: _cacheHits,
            CacheMisses: _cacheMisses,
            CacheHitRatio: Math.Round(cacheHitRatio, 2),
            GroqApiCalls: _groqApiCalls,
            GroqApiErrors: _groqApiErrors,
            GroqSuccessRate: Math.Round(groqSuccessRate, 2),
            ErrorCounts: errorCountsCopy
        );
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}

public record TelemetryStats(
    long TotalApiCalls,
    long FailedApiCalls,
    double ApiSuccessRate,
    long CacheHits,
    long CacheMisses,
    double CacheHitRatio,
    long GroqApiCalls,
    long GroqApiErrors,
    double GroqSuccessRate,
    Dictionary<string, long> ErrorCounts
);